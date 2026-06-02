# S3.1.2 — Dependency Analysis Incompleteness

**Status:** Researched  
**Predecessor(s) ID:** S3.1

## Changelog
| Date/Time | Type | Description |
|-----------|------|-------------|
| 2026-04-26 | Created | Initial file creation |
| 2026-05-02 | Researched | Content written by research agent; sources from arXiv papers, Spec Kit Agents, GitHub spec-kit evolution, and practitioner tooling (Kiro, cc-sdd, tasker, CODITECT) |

---

## Overview

The Planning Phase (S3.1) produces a task list with explicit dependency declarations: Task B depends on Task A, Task C and D can run in parallel, Task E is blocked until migration X completes. These declarations are made with the best information available at planning time. However, **hidden coupling emerges during implementation** — dependencies between tasks that the planning phase did not anticipate or surface.

This incompleteness is structural, not accidental. Full dependency visibility requires *working code*: seeing how imports propagate, discovering that a utility function in Task A is needed by Task C, observing race conditions on a shared configuration file, or finding that a database migration both Task B and Task D assume has side effects only one of them can handle.

When hidden dependencies surface during implementation, the task list must be re-sequenced, and earlier tasks may require rework or rollback. This forcing function — discovering dependencies through implementation failures rather than planning analysis — is the core failure mode of incomplete dependency analysis.

---

## The Dependency Visibility Problem

A **dependency** in the SDD context is a finish-to-start constraint: Task B cannot begin until Task A completes and its artifacts are available. Dependencies can be:

1. **Explicit (declared in tasks.md)**: "Task 5 depends on database schema from Task 2"
2. **Implicit (discovered during coding)**:
   - Task C uses a utility function that Task B implements
   - Task D creates a configuration constant that Task A reads
   - Task E depends on a migration that both Task A and Task B assume already exists
   - Task F and Task G both write to the same database table; execution order matters

### Why dependency analysis is inherently incomplete

**Planning phase constraints:**
- Task analysis happens at the design level, not at the code level. Tasks are described as "implement user authentication" or "add payment API," not as line-by-line pseudocode.
- The planner has the architectural design and requirements but not the actual implementation. Hidden dependencies only become visible when types, imports, and runtime behavior are concrete.
- The human task planner cannot enumerate all data dependencies without executing the code or writing detailed pseudo-implementation, both of which defeat the purpose of SDD (to separate planning from execution).

**Evidence from research (arXiv:2604.05278 — Spec Kit Agents):**
> Agents are often blind to the current architecture, stale assumptions about dependencies, and mismatches with repository conventions. Context blindness—where the agent's intermediate artifacts can be internally coherent while being incompatible with the repository as it exists—is a core failure mode. Common symptoms include referencing non-existent APIs, proposing file paths that do not exist, and violating local architectural or stylistic conventions.

Even with explicit discovery hooks (pre-phase grounding), Spec Kit Agents found that **validation hooks to detect infeasible task ordering** are essential — meaning the dependencies declared during planning are validated against the repository *during* implementation, not before.

---

## Common Hidden Dependency Patterns

### Pattern 1: Shared Resource Mutation
**Scenario:** Task A creates a configuration file, Task B reads it, Task C also reads it.
- **Declared dependency:** Task B → Task A
- **Hidden dependency:** Task C → Task A (not declared, creates race condition if Task B and C run in parallel)
- **Discovery point:** During implementation, Task C fails because config file doesn't exist yet

### Pattern 2: Transitive Data Dependencies
**Scenario:** Task A creates entity model, Task B creates repository for that entity, Task C implements service that uses the repository.
- **Declared dependency:** Task B → Task A; Task C → Task B
- **Hidden dependency:** Task C may require validation rules or computed properties from Task A that Task B assumes exist but didn't document
- **Discovery point:** Task C's service implementation discovers that the repository method doesn't exist; Task B must be reworked

### Pattern 3: Migration Ordering
**Scenario:** Task A creates a table schema, Task B creates an index on that table.
- **Declared dependency:** Task B → Task A
- **Hidden dependency:** Task A's migration has a `DOWN` script that Task C must be aware of; Task C's migration assumes the index exists for query performance, creating a hidden dependency on Task B's order
- **Discovery point:** During rollback testing, the reverse migration order fails

### Pattern 4: Side Effects on Shared State
**Scenario:** Task A registers a singleton service, Task B configures the service, Task C uses it.
- **Declared dependency:** Task B → Task A; Task C → Task B
- **Hidden dependency:** Task C may initialize the service before Task B's configuration, creating a race condition if registration order is implicit
- **Discovery point:** Integration testing reveals non-deterministic failures depending on task execution order

### Pattern 5: Implicit API Contracts
**Scenario:** Task A implements an API endpoint, Task B implements UI that calls it.
- **Declared dependency:** Task B → Task A
- **Hidden dependency:** Task A's implementation uses a request format that Task B's UI doesn't anticipate; Task A's error handling assumes specific HTTP status codes that Task B doesn't handle
- **Discovery point:** Integration testing reveals API contract mismatches; Task A must be reworked to match UI assumptions

---

## Impact on Implementation

### Re-Sequencing Cost
When Task N discovers a hidden dependency on Task M (which is currently in progress or already started), the implementation plan must pause, reorder, and potentially restart work. This forces:

1. **Rollback of forward progress** — if Task M hasn't completed, Task N is blocked; if Task M is complete but the dependency changes its requirements, Task M must be reworked
2. **Context switching overhead** — the developer/agent must switch from Task N back to Task M, requiring re-examination of design rationale
3. **Re-validation of downstream tasks** — any tasks that depended on Task N's completion are now blocked

### Example (Venue CRUD feature, hypothetical):

| Phase | Task | Status | Hidden Dependency |
|-------|------|--------|-------------------|
| Plan | Task 1: Create Venue entity | Complete | — |
| Plan | Task 2: Create repository | Complete | — |
| Plan | Task 3: Implement service validation | In progress | Task 2 repository has no `ExistsByNameAsync` method; must rework Task 2 |
| Impact | Task 3 blocked | — | Re-sequence: complete Task 2 before Task 3 continues |

---

## Research Findings: How Current Tooling Addresses Incompleteness

### 1. Spec Kit Agents (arXiv:2604.05278)
**Approach:** Add explicit context-grounding and validation hooks at workflow boundaries.

- **Discovery hooks (pre-phase):** Before task generation, read-only probing of the repository collects evidence about existing APIs, conventions, and dependencies
- **Validation hooks (post-phase):** After task generation, validate each task against the repository to detect hallucinated paths, missing dependencies, and infeasible task ordering

**Result:** Catching hallucinated APIs and invalid task ordering *before* code generation compounds mistakes. Across 128 runs, explicit context-grounded orchestration improved mean accuracy by 12.2% and reduced completion time by 53.9%.

### 2. Repository Intelligence Graph (arXiv:2601.10112)
**Approach:** Construct a deterministic architectural map of buildable components and explicit dependency edges before agents begin.

- Represents build systems, test structure, and package managers as a navigable DAG
- Provides agents with the authoritative description of repository structure
- Result: 12.2% accuracy improvement and 57.8% reduction in seconds per correct answer; larger gains (17.7% accuracy, 69.5% efficiency) in multilingual repositories

**Key insight:** Agents perform better when they have an explicit, pre-computed dependency graph, not when they infer dependencies from natural language task descriptions.

### 3. GTool — Missing Dependency Prediction (OpenReview paper)
**Approach:** Use LLMs to predict missing edges in incomplete dependency graphs rather than assume declared dependencies are complete.

- Constructs a request-specific tool/task graph
- Applies missing dependency prediction (MDP) using LLM reasoning over graph structure
- Results show GTool is robust to missing ratios of 20–50% in dependency graphs while maintaining planning reliability

**Key insight:** Incomplete dependency declarations are inevitable; the question is not how to make them complete but how to make the system robust to incompleteness through active edge-prediction.

### 4. cc-sdd Boundary-First Approach (GitHub, April 2026)
**Approach:** Make task boundaries explicit before task ordering, declare allowed dependencies upfront, and validate task order respects boundary assumptions.

- Each spec owns specific artifacts and declares what it does *not* own
- Dependency ordering marked with `[P]` for parallel-safe tasks; sequential by default
- Before dispatch, validate that parallel markers match actual implementation boundaries
- Post-implementation, audit for hidden dependencies or shared ownership

**Practice pattern:**
```markdown
# Task A
- Files: src/domain/Venue.cs, tests/VenueTests.cs
- Produces: Venue entity, unit tests
- Consumes: [none]
- Boundary: Does not assume repository interface

# Task B  
- Files: src/infra/VenueRepository.cs
- Produces: Repository implementation
- Consumes: Venue entity (from Task A)
- Boundary: Must support IVenueRepository interface defined in Task A
- Dependency: Task A (entity must exist before repository implementation)
```

### 5. Tasker Protocol — DAG-Based Sequencing (GitHub)
**Approach:** Use backward-pass dependency analysis to build a DAG, then topologically sort for execution order.

1. For each task, ask: "What must exist before this can be done?"
2. Draw edges: Task A → Task B means "A must finish before B starts"
3. Build the DAG and identify critical path (longest dependent chain)
4. Sequence into batches: steel threads first, then parallel meat
5. Verify no cycles using topological sort (Kahn's algorithm)

**Key practice:** If a task ordering includes a cycle (A → B → C → A), the tooling reports the cycle chain and halts. No implementation begins until the DAG is acyclic.

---

## Mitigation Strategies for Planning Phase

### 1. Conservative Parallelism Marking
**Strategy:** Assume sequential unless absolutely certain tasks are independent.

- **Anti-pattern:** Mark 80% of tasks as `[P]` (parallel-safe) and discover conflicts during implementation
- **Better approach:** Mark tasks as `[P]` only when:
  - They touch completely different files or modules
  - No shared configuration, migrations, or state
  - Architects have verified no hidden imports or side effects

**Implementation:** In tasks.md, use explicit markers:
```markdown
- [ ] Task A: Implement Venue entity
- [ ] Task B: Implement repository [DEPENDS_ON: Task A]
- [ ] Task C: Implement service [DEPENDS_ON: Task B]
- [ ] Task D: Implement ViewModel [P] [DEPENDS_ON: Task C]
  (parallel with Task E because it doesn't touch data layer)
- [ ] Task E: Implement UI pages [P] [DEPENDS_ON: Task C, Task D]
```

### 2. Deep Dependency Review During Planning
**Strategy:** Have an architect who understands the codebase deeply review the task list before approval.

- **Review checklist:**
  - Which tasks create new types or constants?
  - Which tasks import or use those new artifacts?
  - Are there implicit dependencies (e.g., Task A's validation rule is used by Task C)?
  - Do any tasks assume a database migration order?
  - Are there shared configuration files, registries, or factory methods?

**Tool support:** Spec Kit Agents' discovery hooks can generate this report automatically by analyzing the repo.

### 3. Explicit Dependency Documentation
**Strategy:** For each task, document what it produces and what it consumes.

```markdown
## Task 2: Implement Repository

**Produces:**
- `src/Infra/VenueRepository.cs` — repository implementation
- `VenueRepository` class with methods: `AddAsync`, `GetByIdAsync`, `GetPagedAsync`

**Consumes:**
- `src/Domain/Entities/Venue.cs` — entity definition (from Task 1)
- `IVenueRepository` interface — assumed to exist from Task 1

**Hidden dependency risk:**
- Task 4 (validation service) may need repository methods not declared yet; if so, Task 4 must rework Task 2
```

### 4. Iron-out High-Risk Decisions Early
**Strategy:** If a key architectural choice is uncertain, run a pre-implementation spike to validate it.

- **Example:** If library selection (EF Core vs. Dapper) affects repository design, write a 30-minute prototype to verify the chosen library supports required access patterns before task planning finalizes
- **Outcome:** Prevent mid-implementation discovery that the chosen approach doesn't work

### 5. Watch for Blocking During Initial Execution
**Strategy:** During the first 2-3 task completions, monitor for unexpected blocking.

- If Task 5 discovers it needs something Task 2 didn't provide, immediately re-examine the task list
- Update the list and communicate the change to stakeholders
- This early feedback loop tightens dependency accuracy for remaining tasks

---

## Validation & Early Detection

### Automated Task Order Validation (Spec Kit Agents model)
Before implementation begins, validate task list with a checklist:

- [ ] Every task has a clear "must complete before" statement for tasks that depend on it
- [ ] No task ordering creates a cycle (A → B → C → A)
- [ ] Parallel-marked tasks (`[P]`) don't share files or create implicit ordering
- [ ] Every produced artifact in Task N is documented as consumed in Task N+1 (or marked as safe to ignore)
- [ ] Database migrations are ordered correctly (schema before indexes, before data mutations)
- [ ] Singletons, services, and configuration are initialized in declared order

### Topological Sort Verification
Use Kahn's algorithm to verify the task list forms an acyclic DAG:

1. Compute in-degree (number of incoming dependencies) for each task
2. Start with all tasks having in-degree 0
3. Pop one task, add to execution order, decrement in-degree of dependent tasks
4. If any tasks remain unprocessed, a cycle exists — halt and report the cycle chain

**Tool:** Tasker protocol implements this; Spec Kit Agents includes this validation.

---

## When Dependencies Emerge Too Late

If hidden dependencies surface after implementation has started:

### Short-term (immediate impact)
1. **Pause affected tasks** — if Task N depends on Task M and Task M hasn't been completed, move Task N to blocked state
2. **Re-sequence** — update tasks.md to reflect the new ordering; communicate the change
3. **Assess rework** — if Task M was already started, determine what must be reworked (often minimal if boundaries were clear)
4. **Proceed** — complete Task M, then continue with Task N

### Medium-term (learning)
1. **Document the discovery** — why was this dependency missed? Was it a hidden side effect, a missing API contract, or a shared state issue?
2. **Update task list audit** — add this pattern to the planning checklist for future features
3. **Consider boundary changes** — if hidden dependencies cluster around a boundary, redesign that boundary to make dependencies explicit

### Long-term (process improvement)
- Review which hidden dependencies are structural (inherent to the design) vs. accidental (could be avoided by better planning)
- If structural, accept that re-sequencing happens; minimize cost by keeping tasks small and boundaries clear
- If accidental, improve the planning phase: more detailed dependency review, deeper architect involvement, earlier validation

---

## Sources

- [Spec Kit Agents: Addressing Context Blindness in Multi-Agent SDD Workflows — arXiv:2604.05278 (2026-04-06)](https://arxiv.org/pdf/2604.05278v1)
- [Repository Intelligence Graph: Deterministic Architectural Map for LLM Code Assistants — arXiv:2601.10112 (2026-01-22)](https://arxiv.org/abs/2601.10112)
- [GTool: Graph-Enhanced Tool Planning for LLMs with Incomplete Dependencies — OpenReview (NeurIPS 2025 workshop)](https://openreview.net/pdf/9796faceb5c9f8007931e59b9c31d90d58695054.pdf)
- [cc-sdd: Boundary-First Spec Flow and Dependency Ordering — GitHub commit e199d9e (2026-04-08)](https://github.com/gotalab/cc-sdd/commit/e199d9edb67e7f657c06bf97f0875466973d40c0)
- [Tasker Protocol: Dependency Graph & Sequencing — GitHub](https://github.com/Dowwie/tasker/blob/main/docs/protocol.md)
- [SDD Plugin: Pipeline Overview and Spec Auditor — SDD Plugin docs](https://noelserdna-claude-plugin-sdd.mintlify.app/concepts/pipeline-overview)
- [Topological Sort in Practice: Ordering Work When Dependencies Matter — TheLinuxCode (2026-02-03)](https://thelinuxcode.com/topological-sort-in-practice-ordering-work-when-dependencies-matter/)
- [Dependency Resolver: Task Orchestration & Cycle Detection — CODITECT Documentation](https://docs.coditect.ai/reference/scripts/dependency-resolver)
- [Data Dependency-Aware Code Generation from Enhanced UML Sequence Diagrams — arXiv:2508.03379 (2025-08-05)](https://arxiv.org/abs/2508.03379)
- [The AI Agent Factory: Phase 4 Task-Based Implementation — Panaversity (2026-02-11)](https://agentfactory.panaversity.org/docs/General-Agents-Foundations/spec-driven-development/task-based-implementation)
