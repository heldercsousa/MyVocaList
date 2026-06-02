# S5.2.1 — Dependency Ordering Fragility

**Status:** Researched
**Predecessor(s) ID:** S5.2

## Changelog
| Date/Time | Type | Description |
|-----------|------|-------------|
| 2026-04-26 | Created | Initial file creation |
| 2026-05-02 | Researched | Content written by research agent; inter-agent misalignment, hidden dependencies, and verification patterns documented |

---

## Overview

Dependency ordering fragility is a critical failure mode that occurs when a parallel task sequence appears valid at planning time but breaks at execution time due to hidden inter-task dependencies that the plan did not surface. The plan correctly identifies direct data dependencies (Task B depends on Task A's output), but misses implicit dependencies (shared types, configuration, naming conventions, or integration points) that are discovered only when agents attempt to execute.

The core problem is structural: the planning phase creates a task decomposition and dependency graph based on what was *apparent* in the spec and initial analysis, but the implementation phase uncovers semantic coupling that the static analysis missed. When hidden dependencies emerge at runtime, the decision is binary: either re-sequence the tasks (trading parallelism for correctness) or proceed with speculation and risk integration failure.

This fragility is distinct from other parallel execution risks (S5.2.2 documents cross-agent spec conflicts; S6.4.1 documents six drift categories in CI/CD). Dependency ordering fragility is about *incomplete knowledge at planning time*, not conflicting outputs or code drift.

---

## Manifestation

### At Plan Time

The Coordinator analyzes the spec and existing codebase. It identifies tasks A, B, and C:
- Task A: Define a shared DTO with fields `{id, name, status}`
- Task B: Implement service X that consumes that DTO
- Task C: Implement service Y that also consumes that DTO

The Coordinator sees no blocking dependency: A is read-only scaffolding; B and C are independent consumers. All three are assigned to Wave 1 (parallel).

### At Execution Time

Implementor A completes the DTO and commits it. Implementor B reads the spec and the DTO and begins service X. But partway through, B discovers a use case the spec did not fully articulate: services X and Y need a derived field `computed_status` that is not in the DTO spec. B calculates that they can add it themselves, but it creates a version mismatch.

Alternatively, B encounters a validation rule in the spec ("name must be unique") that implies the DTO needs an additional `uniqueKey` field for database indexing. That field exists in B's interpretation but not in A's output.

The consequence: B produces code that is locally correct (compiles, tests pass) but integrates incorrectly with C. The Verifier may catch this post-hoc, but re-sequencing (moving B or C to a later wave) is now required mid-execution.

### Root Causes

Hidden dependencies are most common in these categories:

1. **Shared interface / type mutations:** An interface initially specced as `{id, name, status}` is extended by one Implementor to add `computed_field`, but a parallel Implementor reads the original and produces code incompatible with the extended version.

2. **Database schema implications:** A Coordinator task to "define the User table schema" hides the fact that service X needs a calculated column (e.g., `age` from `birth_date`) that alters the schema query interface. A parallel migration task assumes the original schema.

3. **Configuration and DI registration:** Two Implementors both need to register dependencies in `MauiProgram.cs` or a shared config file. The plan assumes they own disjoint registration blocks, but they discover they need to register the same interface with different implementations.

4. **Implicit naming conventions:** The spec describes "sort by recency," but does not specify the field name: `lastModified` vs `updated_at` vs `recency_score`. Two parallel Implementors independently choose different names. Code compiles but contracts are broken.

5. **Transitive dependencies through event schemas:** Task A publishes a domain event `UserCreated(id, email)`. Task B consumes it to send email notifications. Task C consumes it to update a search index. The Coordinator sees three independent tasks. At execution, A discovers the event needs a `createdAt` timestamp, and C fails because its index query expects it.

6. **Subtle integration points:** Implementor A writes a repository method `GetUsersAsync(filter)`. Implementor B reads the spec, assumes the method signature includes pagination `(filter, page, size)` based on a pattern in another spec, and calls it that way. A implemented without pagination. Code fails at integration.

### Why Plans Miss These

1. **Spec under-specification:** Most specs define *what* a feature does but not every implementation detail. The details are discovered during coding.

2. **Asymmetric visibility:** In a parallel wave, Implementor A's completed output is visible to B and C only after A finishes. If B needs to see A's choices to make correct decisions, B must wait for A (sequential dependency) or guess and verify later.

3. **Semantic vs syntactic dependencies:** The dependency graph typically captures *syntactic* dependencies (file imports, explicit data flow). Semantic dependencies (naming conventions, validation rules, event schemas) are harder to surface in a static plan.

4. **Context compression between waves:** In wave-based execution (S5.2), Wave 1 outputs are compressed to 300–500 token summaries before being injected into Wave 2 prompts. These summaries miss fine-grained details that Wave 2 agents need.

---

## Historical Evidence

### SDD Workflow Enforcement Gaps (2026)

A production SDD system at Gentleman-Programming (documented in GitHub issue #262, April 2026) found that sub-agents on later waves reported "done" without completing integrations:

> "Method name mismatch between two new components: SDD-3 created `UnitOfWork.commit()` calling `self._outbox.enqueue()`, and SDD-2 created `OutboxWriter.store()`. The method name mismatch is invisible to both agents. Each only sees its own spec. The orchestrator doesn't verify integration points between SDDs."

This is a textbook case of dependency ordering fragility: two tasks appeared independent in the plan, both succeeded locally, but broke at integration.

### Inter-Agent Misalignment in LLM-Based DAG Execution (2025)

An academic study published in October 2025 (OpenReview) examined LLM-agent systems that decompose objectives into subtask dependency graphs. The study found:

> "This assumption [of conditional independence among subtasks] frequently breaks during execution, as ground-truth responses are inaccessible, leading to inter-agent misalignment — failures caused by inconsistencies and coordination among agents."

The paper proposed SEQCV, a dynamic framework that executes sequentially and verifies each response before incorporating it into global context. Conditional independence among subtask outputs rarely holds in practice.

### Parallel Tool Use in AI Agents (2025)

Wu et al.'s GAP (Graph-based Asymmetric Parallelism) framework addressed the problem of parallel tool calls in agents. Their key finding:

> "The sequential constraint in ReAct is overly conservative, but not every tool call is truly independent. Hidden dependencies surface when agents reason about what operations can run in parallel without full information about downstream task requirements."

---

## Verification Approaches

### The Verification Failure Mode (Pre-Integration Detection)

Because dependency ordering fragility is a *semantic* problem, the Verifier phase must check not just local correctness but *cross-task contracts*. A standard Verifier that checks "does this code compile?" and "do unit tests pass?" will not catch it.

**Correct verification for Wave N+1 requires:**

1. **Contract validation:** Enumerate all shared artifacts (DTOs, interfaces, event schemas, configuration keys). Check that Wave N+1 agents produce outputs that conform to contracts derived from Wave N.

2. **Integration point tracing:** For every file that Wave N+1 agents import or consume from Wave N, manually verify the dependency (name, signature, behavior) is correct.

3. **Cross-service smoke tests:** Wire up Wave N and Wave N+1 outputs in a minimal integration test and exercise the critical paths. This catches method name mismatches, field presence, and event schema violations.

**Example verification checklist for a Wave 2 that consumes Wave 1 DTO:**
- Wave 1 DTO field names are exactly what Wave 2 code expects
- Wave 1 DTO null handling matches Wave 2 code's assumptions
- Wave 1 DTO serialization (JSON, XML, etc.) is compatible with Wave 2's deserialization
- Any calculated or virtual fields added by Wave 2 do not conflict with Wave 1's real fields

---

## Mitigation Strategies

### Primary: Coordinator Analysis Depth

The planning phase must be thorough enough to surface semantic dependencies before tasks launch. This requires:

1. **Artifact enumeration:** List every shared type, interface, event schema, configuration file, and naming convention that will be used by multiple agents. Make these explicit in the task list.

2. **Dependency graph with semantic edges:** Beyond "Task B depends on Task A's output," add semantic edges: "Task B depends on the field names in Task A's DTO" or "Task C depends on the event schema defined in Task A."

3. **Integration point documentation:** For every shared artifact, write a brief spec (1–2 paragraphs) that defines its expected shape, behavior, and constraints. Include this in each agent's spawn prompt.

4. **Dry-run analysis:** Before launching a wave, simulate the interfaces: ask "If Task A produces X, and Task B consumes X, will they align?" Use concrete examples from the codebase or spec.

### Secondary: Conservative Wave Sizing

Tasks whose dependencies are uncertain should not be parallelized speculatively. The cost of sequential waves is often cheaper than the cost of a failed parallel attempt:

- **One extra wave (sequential) costs:** Time to plan, dispatch, and execute — typically 20–30 minutes.
- **A failed parallel wave costs:** Re-sequencing mid-execution, debugging integration, retrying agents, re-verifying — typically 60–90 minutes and one agent retry loop.

Conservative guidance: If a dependency is uncertain, move one task to Wave N+1. If two tasks have subtle semantic coupling, sequence them.

### Tertiary: Shared Contracts Injected into Spawn Prompts

When Wave N+1 agents spawn, do not reference Wave N's outputs — *paste them directly* into the spawn prompt.

**Bad (reference-based):**
```
Agent B's spawn prompt:
"Implement service X using the DTO defined by Agent A in src/Contracts/UserDto.cs"
```

**Good (contract-injected):**
```
Agent B's spawn prompt:
"Implement service X using this DTO (defined by Agent A):
[ACTUAL DTO CODE: class UserDto { int Id; string Name; string Status; }]
Here are the validation rules: Name is unique, Status is one of [active, inactive, suspended]"
```

The injected contract is ground truth. B builds against concrete definitions, not guesses.

### Quaternary: Living Spec Updates During Execution

When an Implementor makes a spec-level decision (adding a field to a DTO, defining an event schema, extending an interface), that decision is written back to the spec *before* parallel agents consume the same interface.

This is the "living spec mechanism" documented in Augment Code's CIV documentation:
- Agent A completes Task A and discovers it needs an additional field
- Agent A updates the spec document with the new field and posts a discovery brief
- The Coordinator reads the discovery brief and updates Wave 2 agents' prompts before dispatch

### Quinary: Structured Task Tracking

Instead of agents self-reporting "done," use structured task trackers that record:
- Tasks completed
- Tasks skipped
- Tasks failed and why
- Integration points discovered vs. planned
- Verification status (local vs. cross-task)

The CODITECT Task Orchestrator (2026) implements this with 500+ atomic task granules, dependency resolution, and execution tracking. When a task completes, the system knows *exactly* which downstream tasks are unblocked.

---

## Implementation Patterns

### Pattern 1: Synchronous Cross-Wave Verification

After Wave N completes, do not immediately dispatch Wave N+1. Insert a verification gate:

1. Coordinator reads Wave N outputs
2. Coordinator simulates integration with Wave N+1's expected inputs
3. If simulations pass, inject contracts into Wave N+1 prompts
4. Dispatch Wave N+1

This adds latency (one sequential gate between waves) but eliminates most hidden dependency failures.

### Pattern 2: Task-Decoupled Planning (TDP)

Task-Decoupled Planning (arXiv:2601.07577, 2025) is a formal framework addressing this problem:

1. Supervisor decomposes the task into a directed acyclic graph (DAG) of sub-tasks
2. Planner generates plans conditioned *solely on node-relevant context* — not the global context
3. Executor translates plans into actions
4. Execution errors trigger *local* replanning within the sub-task's scope, not global replanning

The key insight: sub-task decoupling prevents error propagation across independent decisions. If Task B's hidden dependency on Task A is discovered, only Task B is replanned; Task A and Task C remain unchanged.

### Pattern 3: SEQCV — Sequential Verification with Early Termination

SEQCV (OpenReview, Oct 2025) proposes executing subtasks in *dynamic* order based on verification:

1. Task A executes and produces output
2. Verifier validates Task A's output with cross-model checks (does it match spec? Does it align with Task B's assumptions?)
3. Task B executes only after Task A is verified
4. If Task B reveals a missing dependency from Task A, SEQCV recursively splits Task A into finer-grained subtasks and reruns

This is "sequential verification with dynamic task splitting." It eliminates the binary choice between parallelism and correctness by parallelizing only when safe and dynamically downgrading to sequential + splitting when hidden dependencies emerge.

### Pattern 4: File Ownership with Strict Scope Isolation

Assign every file that might change to exactly one agent. This prevents hidden dependencies that arise from conflicting writes:

```yaml
wave:
  1:
    agents:
      - name: ContractAgent
        files_owned: [src/Contracts/**, src/Events/**]
      - name: RepositoryAgent
        files_owned: [src/Data/**, migrations/**]
  2:
    agents:
      - name: ServiceAgent
        files_owned: [src/Services/**]
        depends_on: [wave.1]
```

With strict file ownership, hidden dependencies *still* exist (they are semantic, not syntactic), but merge conflicts are structurally impossible.

---

## Practical Limits

### When Parallelism Becomes Uneconomical

Parallelism loses value when:

1. **Dependency density is high:** If more than 50% of tasks depend on prior tasks, parallelism gains evaporate. Use sequential execution.

2. **Hidden dependencies are frequent:** If executing a prior wave consistently uncovers dependencies in later waves, the retry cost outweighs parallelism gains. Either improve the Coordinator's analysis or use sequential execution.

3. **Verification is costly:** If validating cross-task contracts requires extensive manual review (because semantic dependencies are hard to automate), the verification gate becomes a bottleneck.

4. **Task granularity is poor:** If task decomposition is too coarse (e.g., "implement the entire API layer" as one task), hidden dependencies within that task create failures that re-sequencing cannot fix.

### Recommendation: Conservative Wave Design

For teams new to parallel agent execution:

- **Wave 1:** 2 agents, completely independent (no shared files, no shared types)
- **Wave 2:** 2–3 agents that consume only Wave 1's outputs (with contracts injected)
- **Subsequent waves:** Increase to 3–4 agents only after Waves 1–2 completed with no hidden dependency surprises

The overhead of sequential gates between waves (typically 15–20 minutes) is cheap insurance against re-sequencing mid-execution.

---

## Relation to Other Topics

**S5.2 — Parallel Agent Execution** defines the wave-based pattern and coordination mechanisms.

**S5.2.2 — Cross-Agent Spec Conflicts** covers the case where both agents complete successfully but produce outputs that are locally correct but globally contradictory (e.g., different DTO field names). Dependency ordering fragility is about one agent *failing* to complete because a dependency was missed; cross-agent conflicts are about both completing but misaligned.

**S3.1.2 — Dependency Analysis Incompleteness** is the planning-phase correlate: it documents how dependency analysis at plan time is inherently incomplete, and hidden coupling surfaces during coding. S5.2.1 describes what happens at *execution time* when those hidden dependencies emerge.

**S6.4.1 — Six Drift Categories** documents silent divergence where code and spec part ways. Dependency ordering fragility is about code failing *during* generation because the dependencies were spec'd incompletely.

**S9.1 — TDD Integration** can mitigate dependency ordering fragility: tests written against shared interfaces before implementation force Implementors to coordinate on contracts.

---

## Sources

### Tier 1 — Primary Sources

- [SEQCV: Dynamic Framework for Reliable Multi-LLM-Agent Task Execution — OpenReview (Oct 2025)](https://openreview.net/pdf/d84e4a86a34dd41d3f1e6d33048f4c075db78d62.pdf) — Inter-agent misalignment, hidden dependencies, conditional independence violations, dynamic task splitting with sequential verification
- [SDD Workflow Enforcement Gaps — GitHub Issue #262 (April 2026)](https://github.com/Gentleman-Programming/gentle-ai/issues/262) — Real-world case study of method name mismatch between parallel SDD tasks, verification gaps, integration failures
- [Task-Decoupled Planning for Long-Horizon Agents — arXiv:2601.07577 (2025)](https://arxiv.org/abs/2601.07577) — DAG-based task decoupling, local replanning, error propagation prevention, context isolation at sub-task level
- [Dependency Aware Task Scheduling: How Agents Execute Plans as Parallel DAGs — Muthu's Engineering Notes (March 2026)](https://notes.muthu.co/2026/03/dependency-aware-task-scheduling-how-agents-execute-plans-as-parallel-dags/) — Implicit dependencies, critical path analysis, semantic vs syntactic dependencies, dynamic DAG validation
- [Breaking the Sequential Bottleneck: Parallel Tool Use in AI Agents — ORAA Research Blog (2025)](https://ordoresearch.ai/blog/agent-parallel-tool-use-gap-framework-2025) — GAP framework, hidden dependencies in tool orchestration, dynamic graph-based scheduling

### Tier 2 — Secondary Sources

- [Running Multiple AI Coding Agents in Parallel: Guardrails, Lock Mechanisms, and Scope Isolation — Fazm (Dec 2025)](https://fazm.ai/t/parallel-ai-coding-agents-guardrails) — File ownership enforcement, lock mechanisms, parallel agent scope isolation, conflict prevention
- [Multi-Agent AI Coding Workflow: The Complete Guide — AppxLab Blog (April 2026)](https://blog.appxlab.io/2026/04/06/multi-agent-ai-coding-workflow/) — Task decomposition for parallelization, "one file, one owner" principle, merge conflict prevention, review workflow as critical path
- [Optimizing Sequential Multi-Step Tasks with Parallel LLM Agents (M1-Parallel) — arXiv:2507.08944](https://arxiv.org/html/2507.08944v1) — Parallel plan execution, early termination strategy, task completion variance, planning diversity
- [Agent Dependency Mapping — How to Think AI (Feb 2026)](https://www.howtothink.ai/learn/agent-dependency-mapping) — Dependency graphs, DAG structures, hidden critical paths, unnecessary dependencies, deadlock detection
- [CODITECT Task Orchestrator Documentation — SDD Task Orchestrator](https://docs.coditect.ai/projects/task-orchestrator/sdd-task-orchestrator) — Atomic task management, semantic agent matching, dependency resolution, task readiness calculation
- [sdd-pi 2.42.0 — Dependency-Aware Dispatch — Libraries.io](https://libraries.io/npm/sdd-pi) — Dependency-aware task dispatch using `depends_on`, fresh context per task, roadmap reassessment after task completion

### Tier 3 — Tertiary Sources

- [Orchestrating Multiple Parallel Agents — Developer Toolkit (April 2026)](https://developertoolkit.ai/en/codex/productivity-patterns/multi-agent-workflows/) — Practical patterns for parallel coding agents, merge strategies, conflict resolution, guardrails for cost and concurrency
- [Robust and Efficient Tool Orchestration via Layered Execution Structures (RETO) — arXiv:2602.18968](https://arxiv.org/html/2602.18968v2) — Layered execution structures, implicit prerequisite relations, reflective error correction, local vs global replanning
- [Parallel Multi-Agent Codegen — GitHub (March 2026)](https://github.com/tathadn/parallel-multi-agent-codegen) — DAG-based task decomposition, concurrent coder workers, integration step for interface mismatch resolution, surgical revisions
- [PARC: Hierarchical Multi-Agent Architecture for Long-Horizon Computational Tasks — arXiv:2512.03549](https://arxiv.org/pdf/2512.03549) — Plan-and-execute pattern, self-feedback across parallel workers, task-level quality gates, error accumulation mitigation
