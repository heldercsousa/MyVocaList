# S5.2 — Parallel Agent Execution

**Status:** Researched
**Predecessor(s) ID:** S5

## Changelog
| Date/Time | Type | Description |
|-----------|------|-------------|
| 2026-04-26 | Created | Initial file creation |
| 2026-04-30 | Researched | Content written by research agent |

---

## Overview

Parallel agent execution is the practice of running multiple agents simultaneously on independent tasks, reducing total elapsed time compared to sequential execution. As specified in S5 — Agent Patterns, parallel execution is one of three dominant patterns in SDD practice (alongside Adversarial Patterns and Subagent Delegation).

The primary measured benefit is throughput reduction. Studies across 2025–2026 show consistent 30–60% speed improvements when tasks are truly independent. A content workflow that took 30 minutes sequentially completes in 18–20 minutes in parallel. An eight-API integration task drops from 1 hour 41 minutes to 22 minutes.

However, parallelism introduces coordination complexity. The pattern only works when tasks have:
1. **No shared file writes** — each agent modifies different files
2. **No output dependency** — task B does not consume task A's output

When either condition is violated, parallel execution creates merge conflicts, race conditions, or logical dependencies that force re-sequencing at runtime. The discipline of SDD around parallelism focuses on making these constraints explicit before agents launch.

---

## Core Mechanisms

### Fan-Out / Fan-In

The simplest parallel pattern: a dispatcher sends independent work items to multiple agents, each processes them in isolation, and results are collected and merged.

Example: A research workflow sends 20 links to four agents in parallel. Each agent processes 5 links independently, returns a structured findings list. A synthesizer agent merges all 80 items into a unified report.

**Characteristics:**
- All agents work on the same logical task (retrieve facts, write code for different modules, generate test cases)
- Agents do not communicate with each other — only with the orchestrator
- Merge happens after all agents report completion
- Fault tolerance: if one agent fails, only its shard is lost; others continue

### Pipeline Parallelism

Work flows through stages in sequence, but within each stage, multiple agents process different items concurrently.

Example: A 10-file codebase refactoring broken into phases:
- **Phase 1:** Two analysis agents examine different modules in parallel → findings
- **Phase 2:** Two refactoring agents work on different modules in parallel (waiting on Phase 1) → refactored code
- **Phase 3:** One testing agent validates all refactored code
- **Phase 4:** One documentation agent updates architecture docs

Each stage waits for the prior stage to complete. Within each stage, agents run in parallel.

**Characteristics:**
- Tasks have explicit dependencies across stages (phase N+1 depends on phase N output)
- Within a phase, agents process disjoint datasets or files
- Throughput is determined by the slowest stage (bottleneck)
- Ideal when work naturally decomposes into phases

### Wave-Based Execution

Wave-based execution is the canonical pattern for SDD parallelism, documented in practice by Google ADK, Augment Code, and implemented in production tools like Wave Orchestrator, Ninthwave, and Fleet.

**Structure:**

```
Coordinator analyzes spec dependencies
    ↓
Group tasks into waves:
  - Wave 1: tasks with no dependencies (run in parallel)
    ↓ [collect results]
  - Wave 2: tasks depending on Wave 1 (run in parallel, consuming Wave 1 outputs)
    ↓ [collect results]
  - Wave N: continue until all work complete
```

**Mechanics:**

1. **Dependency analysis.** The Coordinator identifies all inter-task dependencies, typically represented as a directed acyclic graph (DAG). A task is ready to run when all its dependencies are satisfied.

2. **Wave grouping.** Tasks with no unresolved dependencies are grouped into a wave. All tasks in a wave are spawned simultaneously.

3. **Wave execution.** All agents in the wave run concurrently. The orchestrator tracks completion and collects outputs.

4. **Wave completion handoff.** When all tasks in a wave complete, outputs are compressed into "discovery briefs" (typically 300–500 tokens per agent). These briefs capture what was built, key decisions, and discoveries that downstream agents need to know.

5. **Next wave planning.** The Coordinator reads the discovery briefs and injects them into the context of Wave N+1 agents, so downstream agents inherit context from upstream.

**Example:** Building an API + Frontend + Database system:

```
Wave 1: Independent (run in parallel)
  - Agent A: Design database schema
  - Agent B: Design API interface contracts
    ↓
    [Collect schema & API designs]
    [Compress to ~300-token briefs each]
    ↓
Wave 2: Dependent on Wave 1 (run in parallel)
  - Agent C: Implement database migrations (uses schema from A)
  - Agent D: Implement API endpoints (uses contracts from B)
  - Agent E: Implement frontend (uses contracts from B)
    ↓
    [All three complete independently]
    [Compress outputs to briefs]
    ↓
Wave 3: Dependent on Wave 2
  - Agent F: Integration tests (consumes all Wave 2 outputs)
```

---

## When Parallelism Is Valid

Parallelism is safe only when **both** conditions hold:

1. **Tasks write to different files.** No two agents in the same wave modify the same file. File ownership is assigned at planning time and enforced during execution (typically via git worktree isolation).

2. **Tasks have no output dependency.** Task B does not require the output of Task A as input. If B needs A's output, B must be moved to Wave N+1.

**Concrete checks:**

- **Shared types/interfaces:** If Agent A defines an interface in `contracts/IUserService.cs` and Agent B consumes it in `services/UserService.cs`, they cannot run in parallel. B must be Wave N+1 and receive the actual interface definition in its spawn prompt.

- **Database schema:** If Agent A writes a migration that creates a table and Agent B's migration depends on that table existing, A and B cannot be in the same wave.

- **Configuration files:** If both agents touch `MauiProgram.cs` or `appsettings.json`, they cannot be in the same wave.

- **Read-only dependencies:** Agent B *reading* Agent A's completed output (e.g., "implement API endpoints using these schema definitions") is safe for parallelism. The constraint is write conflicts, not reads.

---

## Dependency Ordering Fragility

Dependency ordering fragility (documented in S5.2.1) describes the failure mode where a parallel task sequence that appeared valid at planning time breaks at execution time because a hidden inter-task dependency was not captured in the task plan.

### Manifestation

At plan time, the Coordinator identifies tasks A, B, and C as independent. At execution time, Implementor B discovers it needs the output of task A — a type definition, a schema migration detail, or a shared constant — that the plan did not surface as a dependency edge.

Consequences:
- B cannot complete or produces incorrect output that passes local validation but breaks integration
- B may proceed with a wrong assumption, producing code that is locally consistent but globally broken
- The retry budget is consumed on a failure that only re-sequencing can resolve

### Root Causes

Hidden dependencies are most common in:

- **Shared interfaces and types.** An Implementor adding a method to an interface while a parallel Implementor consumes that interface without the new method.
- **Database schema and migrations.** A migration task whose schema is needed by a query generation task.
- **Configuration and DI registration.** Two Implementors editing shared files like `MauiProgram.cs` or `Directory.Build.props` without explicit write ownership.
- **Implicit naming conventions.** An Implementor generating a DTO with fields `{id, name, status}` while a parallel Implementor expects `{id, title, active}` based on different interpretation of a shared spec.

### Mitigation

**Primary:** Coordinator analysis quality. The planning step must explicitly enumerate all shared artifacts (types, interfaces, migrations, DI registrations, configuration files) and trace them through the dependency graph before assigning tasks to waves.

**Secondary:** Conservative wave sizing. Tasks whose dependencies are uncertain should be promoted to their own wave rather than parallelized speculatively. The performance cost of one extra sequential wave is cheaper than a failed Implementor consuming retry budget.

**Tertiary:** Shared contracts injected into spawn prompts. When Wave N+1 agents spawn, actual output from Wave N is pasted into their prompts, not references. This eliminates guesswork about what an interface or schema actually looks like.

---

## Cross-Agent Spec Conflicts

Cross-agent spec conflicts (documented in S5.2.2) occur when two agents working on interdependent tasks produce outputs that are locally correct against their individual task specs but globally contradictory when integrated.

### Manifestation

Agent A implements service X and defines a DTO with fields `{id, name, status}`. Agent B, working simultaneously on service Y that consumes service X, writes code expecting `{id, title, active}` — a different field naming convention. Both agents pass their local Verifier. Integration fails.

This failure mode is structurally distinct from dependency ordering fragility: both agents complete successfully; the conflict only surfaces at integration. The Verifier cannot catch cross-agent semantic contradictions unless the spec itself defines the shared contract with enough precision.

### Mitigation

**1. Shared contracts defined in the spec before implementation begins.** DTOs, API contracts, error code enumerations, and event schemas must be specified in the shared spec, not left to each agent's judgment.

**2. Living spec updates as implementation progresses.** When an Implementor makes a spec-level decision (e.g., choosing a DTO field name), that decision is written back to the spec before parallel agents consume the same interface. The Augment Code CIV documentation calls this the "living spec mechanism."

**3. Verifier validates cross-service contracts, not just local output.** The Verifier's job is to check against the full system specification. A Verifier limited to a local diff will pass locally-correct code that breaks integration.

---

## Coordination Primitives

Modern parallel agent systems (Wave Orchestrator, Ninthwave, Fleet, ControlFlow) implement shared coordination mechanisms:

### Shared Task List

A persistent, file-locked task queue that tracks task state:
- `pending` — waiting for dependencies
- `in_progress` — an agent claimed it
- `completed` — done, outputs ready
- `failed` — needs retry or escalation

Agents claim tasks from the queue at runtime. If an agent crashes, the task times out and returns to the queue.

### Discovery Relay / Handoff Blocks

After each wave completes, outputs are compressed to brief summaries (300–500 tokens) and injected into the next wave's prompts. This prevents downstream agents from rediscovering what upstream agents already found.

Example: If Wave 1 Agent A discovers "the API has rate limiting at 100 req/min," Wave 2 Agent C (building the frontend) starts with that knowledge and implements throttling without hitting limits first.

### File Ownership / Worktree Isolation

Each agent works in its own git worktree — a separate directory tree linked to the same repository history. This enforces:
- **Filesystem isolation:** No shared build caches, lock files, or intermediate objects
- **Clean merge paths:** Each worktree is an independent branch; merging is a simple git merge operation
- **Dependency installation:** The WorktreeCreate hook auto-installs dependencies in each worktree

### Contracts / Interface Specifications

Wave-based execution injects actual interface definitions into spawn prompts so agents build against concrete contracts rather than guesses. Example:

```
Agent C's spawn prompt:
"Implement the database layer using this schema:
[ACTUAL TABLE DEFINITIONS FROM AGENT A'S OUTPUT]
Here are the API contracts your layer must satisfy:
[ACTUAL INTERFACE DEFINITIONS FROM AGENT B'S OUTPUT]
File ownership: you own src/Data/, tests/Data.Tests/"
```

### Approval Gates / Manual Review Points

Human review steps inserted before downstream waves proceed. Example: "Wave 1 completes, human reviews the API design, approves or requests changes, Wave 2 proceeds only after approval."

---

## Practical Limits

Based on 2025–2026 production experience:

### Concurrency Ceiling

Most systems report diminishing returns beyond 4–6 parallel agents:

- **Sweet spot:** 3–5 parallel agents per wave
- **Hard limit:** 10 simultaneous agents (Claude Code Agent Teams documented limit)
- **Cost consideration:** Each agent consumes a full context window and token budget. Spawning 10 agents costs 10x tokens; the speedup is typically 3–5x, not 10x.

Beyond 4–6 agents, coordination overhead and context management complexity begin to offset the parallelism gains. The orchestrator spends more time merging partial outputs and resolving conflicts than agents save through parallelism.

### Wave Size Recommendation

- Start with 2 agents per wave
- Increase to 3–4 as you build confidence in task decomposition
- Rarely exceed 5 in the same wave unless tasks are completely orthogonal (no risk of hidden dependencies)

### Per-Operator Token Budget

Expect approximately:
- **Orchestrator overhead:** 200–300K tokens per wave for planning, dependency analysis, and wave supervision
- **Per-agent context:** 500–700K tokens per agent per wave (varies by task complexity)

For a 5-agent wave: ~200K orchestrator + 2.5M agent tokens = ~2.7M tokens per wave.

---

## Tools and Implementations

### Claude Code Agent Teams (Anthropic)

Official feature in Claude Code. An orchestrator agent spawns up to 10 specialized sub-agents on a shared task list. Agents track dependencies and avoid conflicts through explicit `depends_on` metadata. Documented speedup: 3–5x on highly parallelizable tasks.

### Wave Orchestrator (chllming, active 2026)

Open-source framework built for "vibe-coding discipline." Uses a blackboard-style multi-agent system with shared canonical state. Agents work against shared inboxes, explicit ownership, and staged closure. Produces machine-trustable authority sets and replayable execution traces.

### Ninthwave (roblambell, active 2026)

Orchestration layer for parallel AI coding. Decomposes plans into ~200–400 line work items, spawns agents in isolated worktrees, coordinates the full delivery loop (Implementer → CI → Reviewer → Merge). Multi-tool compatible (Claude Code, Copilot, Codex, OpenCode).

### Fleet (Citadel, active 2026)

Parallel campaign orchestration. Runs 2–3 agents per wave in separate worktrees. After each wave, outputs are compressed to ~500-token discovery briefs and injected into the next wave. Implements `WorktreeCreate` hooks for automatic dependency installation.

### ControlFlow (Smithbox-ai, active 2026)

Multi-agent orchestration for VS Code Copilot. Coordinates 13 specialized agents under P.A.R.T contracts (Prompt → Archive → Resources → Tools). Wave-based execution with adversarial plan auditors, assumption verifiers, and offline eval gates.

### Scout and Wave / SAW (blackwell-systems, active 2026)

File-ownership-first coordination. Scout agent analyzes codebase, assigns every file to exactly one agent, writes an IMPL doc (structured YAML with wave structure). Human reviews the plan before any agent launches. Wave agents execute in isolated worktrees with disjoint file ownership (merge conflicts become structurally impossible).

### Re-Cinq Wave (re-cinq, active 2026)

Orchestration layer with declarative persona scoping, contract-validated handoffs, and full audit trails. YAML-based pipeline definitions with built-in 82 pipelines for development, debugging, and GitHub automation.

---

## Best Practices

### 1. Plan Before Parallelizing

Never assume independence. Coordinator must:
- Read the full spec
- Trace all shared artifacts (types, schemas, configurations)
- Build a dependency DAG
- Identify true independent tasks
- Assign tasks to waves based on DAG, not convenience

### 2. Inject Contracts, Not References

When spawning Wave N agents, paste actual output from Wave N–1 directly into their prompts. Do not write "use the schema from Agent A" — paste the actual table definitions.

```
Good:
Agent B spawn prompt contains:
"Here is the schema Agent A created:
[ACTUAL SCHEMA DEFINITION]"

Bad:
Agent B spawn prompt contains:
"Agent A created a schema; reference it from src/schema.ts"
```

### 3. Enforce Disjoint File Ownership

Every file that will change is assigned to exactly one agent. Use a YAML coordination document (IMPL doc) to make this explicit. Human reviews it before agents launch.

```yaml
wave:
  1:
    agents:
      - name: DatabaseAgent
        files: [src/Data/**, migrations/**]
      - name: APIAgent
        files: [src/Services/**, src/API/**]
  2:
    agents:
      - name: FrontendAgent
        files: [src/UI/**, public/**]
        depends_on: [wave.1]
```

### 4. Use Worktree Isolation

Each agent works in its own git worktree. This prevents:
- Concurrent build cache corruption
- Lock file races
- Tool-cache interference
- Unresolvable merge conflicts (because file ownership is disjoint)

### 5. Compress and Relay Discoveries

After each wave:
1. Collect all agent outputs
2. Compress each to ~300–500 token discovery brief
3. Inject briefs into next wave's spawn prompts

This reduces redundant research and prevents downstream agents from making assumptions that upstream agents already disproved.

### 6. Cap Concurrency Conservatively

Start with 2–3 agents per wave. Increase to 4–5 only after confirming:
- No hidden dependencies surfaced at runtime
- No merge conflicts on expected disjoint files
- Wave completion happens on schedule (no timeout/retry loops)

### 7. Implement Manual Review Gates

Insert human approval points between critical waves, especially:
- After Wave 1 (review high-level design decisions before downstream work locks them in)
- Before integration wave (verify all outputs are ready before final merge)

### 8. Test Dependency Analysis

Before deploying a large parallel task, run a "dry run" to confirm:
- All tasks in Wave N have no pending dependencies
- Task assignments don't conflict (two agents assigned to same file)
- Contracts are passed as actual values, not references

---

## When Parallelism Costs More Than It Saves

Parallelism is not always the right choice:

- **Highly sequential workflows.** If 80% of work depends on the prior step's output, parallelism gains evaporate and coordination overhead dominates. Example: a multi-step data pipeline where each step refines previous outputs.
- **Tightly coupled codebases.** If every file depends on every other file (poor architecture), disjoint file ownership is impossible. Fix the architecture before attempting parallelism.
- **Exploratory / uncertain work.** If the dependencies are genuinely unknown, parallelism will expose them at runtime (failed Implementors, merge conflicts). Sequential execution is safer for research-phase work.
- **Single-agent performance is critical.** Some tasks benefit from a single agent with maximum context (all files visible at once). Splitting them into parallel agents forces scope reduction and may introduce errors.

---

## Current Limitations

### Tooling Immaturity

As of mid-2026, parallel agent coordination is active but not standardized:
- No single dominant tool (Wave, Ninthwave, Fleet, ControlFlow, SAW all coexist)
- Each has different YAML/configuration syntax and coordination models
- Multi-tool coordination (Claude Code + Copilot + Codex agents in one wave) is emerging but not mature

### Hidden Dependencies

Despite best-effort planning, hidden inter-task dependencies surface at runtime. The consequences (failed agents, merge conflicts, re-sequencing) are mitigated but not eliminated by current practice.

### Cost vs. Speedup Trade-Off

Speedup is typically 3–5x. Token cost increase is proportional to agent count (N agents, N× tokens). The ROI is strong when task independence is high, but economically marginal for sequential or tightly coupled work.

### Context Window Competition

Each parallel agent consumes a full context window. On token-limited plans or long-running waves, agents may hit context limits mid-execution.

---

## Sources

### Tier 1 — Primary Sources

- [Spec-Driven AI Code Generation With Multi-Agent Systems — Augment Code](https://www.augmentcode.com/guides/spec-driven-ai-code-generation-with-multi-agent-systems) — Coordinator-Implementor-Verifier architecture, wave-based parallelism, living spec mechanism
- [Claude Code Agent Teams: Advanced Multi-Agent Workflows in Practice — OpenAIToolsHub (reporting on Anthropic docs)](https://www.openaitoolshub.org/en/blog/claude-code-agent-teams-advanced) — Agent Teams feature, dependency tracking, `depends_on` field, speedup benchmarks (4–5x on parallelizable tasks)
- [How to Run Parallel AI Agents in 2026 — Fastio](https://fast.io/resources/parallel-ai-agents/) — Fan-out/fan-in, pipeline parallelism, wave mechanics, coordination primitives, 30–60% speed improvements
- [Scout and Wave / SAW — blackwell-systems (GitHub)](https://github.com/blackwell-systems/scout-and-wave) — File-ownership-first coordination, disjoint file assignment, IMPL doc (structured YAML), worktree isolation
- [Multi-Agent Workflows — Coordinate Multiple AIs for Faster Development — Antigravity Lab](https://antigravitylab.net/en/articles/agents/multi-agent-workflows) — Task decomposition, dependency resolution, bottleneck identification, role-based parallelism

### Tier 2 — Secondary Sources

- [Two Paradigms of Multi-Agent AI: Rust Parallel Agents vs Claude Code Agent Teams — Vadim's blog](https://www.vadim.blog/two-paradigms-multi-agent-ai-rust-vs-claude-teams) — Static fan-out vs. team-based coordination, token cost tradeoffs, integration patterns
- [Multi-Agent Parallel Execution: Running Multiple AI Agents Simultaneously — Skywork ai](https://skywork.ai/blog/agent/multi-agent-parallel-execution-running-multiple-ai-agents-simultaneously/) — 37% throughput improvement, role-based specialization, context control for hallucination reduction
- [Wave Orchestrator (chllming, GitHub)](https://github.com/chllming/agent-wave-orchestrator) — Blackboard-style coordination, shared canonical state, durable state management, replayable traces
- [Ninthwave — Orchestrate Parallel AI Coding Into Reviewable PRs](https://ninthwave.sh/) — Decomposition into 200–400 line work items, stacked PRs, parallel native sessions, review + feedback loops
- [Fleet — Parallel Campaign Orchestration (SethGammon/Citadel, GitHub)](https://github.com/SethGammon/Citadel/blob/main/docs/FLEET.md) — Discovery relay, wave mechanics, worktree isolation, scope overlap prevention
- [ControlFlow — Multi-Agent Orchestration for VS Code Copilot (Smithbox-ai, GitHub)](https://github.com/Smithbox-ai/ControlFlow) — 13-agent system, P.A.R.T contracts, wave-based execution, adversarial auditing
- [Agent Teams Workflow — Build This Now](https://www.buildthisnow.com/blog/guide/agents/agent-teams-workflow) — Seven-step workflow (brain dump → Q&A → plan → fresh context → contracts → waves → validation), dependency graph interpretation
- [Re-Cinq Wave (GitHub)](https://github.com/re-cinq/wave) — Declarative persona scoping, contract-validated handoffs, audit trails, 82 built-in pipelines
- [Agents — Verdent Documentation](https://docs.verdent.ai/verdent/core-features/agents) — Isolated context per agent, workspace flexibility, selective rebasing, performance through parallelization
- [M1-Parallel: Multi-Agent Teams with Parallel Plan Execution — arXiv:2507.08944](https://arxiv.org/pdf/2507.08944) — Early termination strategy (1.6–2.2× speedup), aggregation for completion rates, task completion variance analysis
- [DynTaskMAS: Dynamic Task Graph-Driven Framework — arXiv:2503.07675](https://arxiv.org/abs/2503.07675) — Dynamic task graph generation, asynchronous parallel execution, semantic-aware context management, 21–33% execution time reduction

### Tier 3 — Tertiary Sources

- [Claude Code Multi-Agent Parallel Execution Guide — ClaudeLab](https://claudelab.net/en/articles/claude-code/claude-code-multi-agent-subagent-parallel-guide) — Practical 3-agent parallel example, 30-to-8-minute reduction
- [How to Use Claude Code Subagents to Parallelize Development — Zach Wills](https://zachwills.net/how-to-use-claude-code-subagents-to-parallelize-development/) — Product/UX/engineer parallel trio pattern, sequential handoff assembly line

