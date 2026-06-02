# S3.2 — Implementation Phase

**Status:** Researched  
**Predecessor(s) ID:** S3

## Changelog
| Date/Time | Type | Description |
|-----------|------|-------------|
| 2026-04-26 | Created | Initial file creation |
| 2026-04-30 | Researched | Content written by research agent; authoritative sources added |

---

## Overview

The Implementation Phase is where AI agents execute the approved design and task list from the Planning Phase. It is the phase in which humans shift from author to reviewer: the agent does the code generation; the human validates that the generated code conforms to the spec and design intent before marking tasks complete.

Implementation is structured as **task-by-task execution**, not monolithic code generation. Each task from `tasks.md` is delegated to an AI agent, which:

1. Reads the spec, design, and its assigned task
2. Writes code that satisfies the task's acceptance criteria
3. Runs the build and tests to confirm correctness
4. Commits and signals completion

The phase succeeds when every task builds with zero errors, all tests pass, and the human has reviewed and approved the focused diff before proceeding to the next task.

---

## Core Design Principles

### The Spec Is the Contract

The agent does not improvise beyond the spec. When the implementation encounters ambiguity not resolved by the specification, the correct response is to surface that ambiguity to the human (via task-log updates or blocking on that task) rather than to make an unilateral architectural decision.

The spec — requirements.md, design.md, and tasks.md — is the oracle. Code is generated to satisfy it; if the code and spec diverge, the spec is updated first, and the task is re-executed.

### Tasks Are the Unit of Delegation

A subagent receives exactly one task from tasks.md. It has:
- The task description
- The acceptance criteria (from requirements.md)
- The design context (from design.md)
- The codebase rules (CLAUDE.md, rules files)

The agent completes the task, commits, and returns. Context isolation (each subagent starts fresh) prevents accumulated errors from contaminating subsequent tasks.

### Build Must Pass Before Completion

A task is not marked done unless:
- The code builds with zero compiler/type errors
- Tests that cover the task's acceptance criteria pass
- The focused diff is reviewed and approved by the human

This is enforced as a mandatory exit condition, not a best practice. If the build fails, the agent must fix errors before committing. If tests fail, the task is incomplete.

### Wave-Based Parallelism With Hard Limits

When tasks are independent (identified in tasks.md with parallel markers), multiple subagents may run concurrently. The constraint is:
- **Maximum 4 concurrent subagents at any one time**
- Work is dispatched in waves: spawn up to 4 agents, wait for all to complete, then start the next wave
- Never spawn a 5th concurrent subagent — this creates resource contention and exceeds human review capacity

After a subagent completes, its context is discarded. Do not reuse the same subagent instance for a second task.

### Context Isolation and Ephemeral Sessions

Each subagent is instantiated with a fresh context window that contains:
- The specific task specification
- Relevant excerpts from requirements.md and design.md (not the full codebase history)
- The project's CLAUDE.md and applicable rules files
- No prior chat history from other tasks or the parent agent

This isolation shields agents from:
- Accumulated debugging traces from failed approaches in prior tasks
- Large context footprints from unrelated explorations
- Implicit knowledge that might cause hallucination or drift in later tasks

When a task depends on output from a prior task, the orchestrating agent explicitly includes that output in the subagent's prompt (e.g., "Here is the API shape from Task #1; your Task #2 implementation must conform to it").

---

## Implementation Workflow

### Phase Inputs

The Implementation Phase begins when all three artifacts from the Planning Phase are approved:
1. **requirements.md** — Feature specification with user stories and acceptance criteria (approved by product/domain owner)
2. **design.md** — Technical design with architecture, interfaces, and interaction flows (approved by technical lead)
3. **tasks.md** — Ordered, checkboxed task list with dependencies (approved by both)

### Dispatch and Execution

1. **Task Assignment**
   - The orchestrating agent (human or lead agent) reads tasks.md
   - It identifies which tasks are ready (dependencies satisfied) and which are blocked
   - It selects up to 4 independent ready tasks and dispatches subagents in parallel

2. **Subagent Execution**
   - Each subagent receives a focused prompt containing:
     - Its task description
     - Acceptance criteria from requirements.md
     - Relevant design decisions from design.md
     - Project rules and conventions (CLAUDE.md, rules files)
     - Output from dependent prior tasks (if any)
   - The agent reads the codebase, writes code, runs build and tests
   - If build or tests fail, the agent iterates (Red → Green loop) until passing
   - Once tests pass, the agent commits changes with a focused commit message

3. **Task Completion Signal**
   - The subagent updates tasks.md to mark its task(s) checked off
   - It commits the changes
   - It pushes to the remote branch
   - It signals completion (in MyVocaList, via Docs/task-log.md with status "done" or "fail")

4. **Human Review**
   - The human pulls the completed work
   - Reviews the focused diff (only the files touched by that one task)
   - Confirms the implementation matches the spec intent
   - Either approves (task marked complete) or requests changes (task sent back to agent for revision)

5. **Iteration**
   - If review rejects the task, the agent revises and re-submits
   - If review approves, the human proceeds to the next batch of ready tasks
   - Loop until all tasks are complete

### Handling Failures and Blocking

- **Build failure:** The agent is responsible for fixing compiler/type errors before completing the task. If the agent cannot fix the error, it marks the task failed with a detailed diagnostic.
- **Test failure:** The agent has two options: (a) implement to make tests pass (preferred — it forces clarity on acceptance criteria), or (b) if the test itself is wrong per the spec, surface the contradiction to the human and request clarification.
- **Blocked dependency:** If a task cannot start because its prerequisite is blocked, the agent waits and reports the blocker to the orchestrator.
- **Spec ambiguity:** If the task reveals ambiguity in the spec that the agent cannot resolve, the agent documents the ambiguity, pauses the task, and escalates to the human for spec refinement.

---

## Context Window Management

The Implementation Phase is the phase most directly constrained by LLM context window limits. Large tasks that span many files accumulate tool-call traces, reasoning steps, and artifact history until the window is exhausted.

### Task Granularity Calibration

The primary defense is **task sizing**. Tasks must be granular enough to complete within a single agent session without requiring context compaction or window exhaustion. A properly sized task:

1. **Fits within a single context window** — typically 50–150 tool invocations (reads, globs, edits) before reaching diminishing returns
2. **Produces a reviewable diff** — focused changes to 1–5 files, not 20+ files
3. **Maps to a single acceptance criterion** — one user story, one feature behavior, one integration point

If a task cannot fit within these bounds, it must be split into smaller tasks during the Planning Phase.

### Ephemeral State Separation (CODEDELEGATOR Pattern)

Research on multi-agent code generation (arXiv:2601.14914) demonstrates that **ephemeral-persistent state separation** improves agent reliability:

- **Orchestrator (persistent):** Maintains strategic oversight, task list, committed results, and global architectural state
- **Coder agents (ephemeral):** Execute individual tasks in isolated contexts; debugging traces and local state are discarded after task completion

This prevents a single task's debugging traces (failed attempts, dead-end explorations) from polluting the orchestrator's context or the next task's reasoning. Each agent starts fresh with only the information it needs.

---

## Task Dependencies and Ordering

### The DRY Onion Pattern

When tasks have dependencies, order them inside-out (dependencies first):

```
Layer 1 (Innermost):   Database schema, domain entities
Layer 2:                Repository/data access interfaces
Layer 3:                Business logic (Services)
Layer 4:                ViewModels and UI orchestration
Layer 5 (Outermost):   UI pages and components
```

Each layer depends on the shape of the layer below. Tasks in the same layer may run in parallel. Tasks in different layers must run sequentially (inner layer completes before outer layer begins).

This ordering prevents rework: if the database schema is wrong, discovering it after UI is built forces re-implementation at every layer. Discovering it early (during schema task) costs only that task to fix.

### Dependency Graph Construction

Tasks.md should declare dependencies explicitly:

```markdown
## Task 1: Create User entity and schema
**Status:** [ ] Done
**Depends on:** None
**Blocks:** Tasks 2, 3

## Task 2: Implement IUserRepository and queries
**Status:** [ ] Done
**Depends on:** Task 1
**Blocks:** Task 4

## Task 3: Implement IUserService business logic
**Status:** [ ] Done
**Depends on:** Task 1
**Blocks:** Task 4
```

The orchestrator reads this and schedules:
- Tasks 2 and 3 in parallel (both depend on Task 1, neither blocks each other)
- Task 4 only after both Tasks 2 and 3 complete

---

## Integration with TDD

The Implementation Phase leverages Test-Driven Development as a forcing function:

1. **Spec → Tests:** Acceptance criteria from requirements.md are translated into executable test cases (unit, integration, or acceptance tests)
2. **Test → Code:** The agent writes tests that encode the spec, then implements code that makes tests pass
3. **Verification:** Passing tests prove the implementation satisfies the spec

This integration closes the loop: the spec is not just documentation; it is enforced by automated assertions that the implementation must pass.

**Key insight:** Tests are not written after implementation; they are derived from the spec before implementation begins. The agent implements to the test, not the other way around.

---

## Coordination Patterns

### Orchestrator-Worker Pattern

A lead agent (orchestrator) coordinates multiple worker subagents:

1. **Orchestrator:** Reads tasks.md, identifies dependencies, decides which tasks to dispatch, receives results, updates shared state, proceeds to next batch
2. **Workers:** Each spawned with a specific task, isolated context; completes and returns result

The orchestrator never writes code; it only delegates, reviews, and coordinates. This keeps orchestration context lightweight and prevents implementation details from bloating the lead's reasoning.

### Agent Teams (Peer Coordination)

Emerging tooling (Claude Code Agent Teams, LangGraph LoopAgent) enables peer-to-peer coordination where agents communicate with each other rather than always routing through a lead:

- Teammates share a task list with dependency tracking
- Each team member can claim unclaimed tasks
- Teammates message each other directly about context
- A lead still coordinates the team but does not manually schedule each task

This is more efficient than strict orchestrator-worker but requires:
- A shared, file-locked task manifest
- Defined message protocols between agents
- Escape hatches (if an agent is stuck, the lead can reassign or kill it)

### Wave Synchronization

At the end of each wave:
1. All subagents have completed, committed, and signaled completion
2. The orchestrator aggregates results (reads each task's commits, collects side effects)
3. The human reviews (or automates review gates)
4. The orchestrator proceeds to the next wave of tasks, or declares the phase complete

**Never** cascade waves (start the next batch before the previous batch is reviewed). This risks compounding errors and makes rollback difficult.

---

## Quality Gates and Verification

### Pre-Commit Validation

Before marking a task complete, the agent must satisfy:

1. **Build:** Zero compiler/type/lint errors. `dotnet build` (or language equivalent) succeeds.
2. **Tests:** All tests pass, including:
   - Unit tests for the task's logic
   - Integration tests that verify the task's interaction with dependencies
   - Regression tests to ensure prior tasks are not broken
3. **Code style:** Follows CLAUDE.md conventions (naming, structure, documentation)
4. **No side effects:** The task only modifies files and layers it is scoped to; it does not refactor unrelated code or migrate unrelated tasks

### Human Review Gate

After the agent completes, the human reviews:

1. **Diff scope:** Does the diff touch only the files declared in the task? Are there unexpected changes?
2. **Spec compliance:** Does the code implement the acceptance criteria? Are all edge cases from requirements.md covered?
3. **Design alignment:** Does the code follow the architecture from design.md? Are layers clean? Are dependencies unidirectional?
4. **Test coverage:** Are the tests adequate? Do they cover the spec's scenarios or just happy paths?

If the review finds issues, the human either:
- **Requests changes** — the agent revises and resubmits
- **Rejects and returns to planning** — if the spec itself is ambiguous or wrong, the Planning Phase reopens; design.md and requirements.md are updated; the task is re-tasked and re-delegated

### Continuous Conformance

After a task is approved and merged, spot checks occur throughout the phase:

- Random integration tests across completed tasks (do they still compose correctly?)
- Sampling of code style against conventions (is drift accumulating?)
- Dependency audits (did a task accidentally couple to a layer it shouldn't?)

These checks are automated where possible (linters, test suites) and manual spot-checks where necessary.

---

## Common Failure Modes and Prevention

### Context Window Exhaustion

**Problem:** An agent accumulates so much chat history and tool invocation traces that it can no longer reason effectively.

**Prevention:**
- Keep tasks small enough to fit in a single session without compaction
- Use ephemeral agents (fresh context per task) rather than reusing the same agent across multiple tasks
- Use `--reset` or session restart to clear accumulated traces if an agent must continue on a new task

### Accumulated Hallucination

**Problem:** An agent makes an error early (e.g., misreading the spec), and subsequent agents inherit that error as implicit context.

**Prevention:**
- Ephemeral state separation: each subagent starts fresh with only its task and relevant design
- Orchestrator explicitly includes prior task outputs in the next task's prompt (not implicit)
- Review gates catch errors early, before they propagate to dependent tasks

### Spec Drift

**Problem:** The implementation diverges from the spec as tasks are executed, but no mechanism re-syncs them.

**Prevention:**
- The spec is updated **before** tasks are re-executed, not after
- If implementation reveals a spec error, the Verification Phase triggers re-planning before the task is accepted
- CI/CD gates enforce spec-code traceability (acceptance criteria → tests → code)

### Uncontrolled Parallelism

**Problem:** Too many agents running simultaneously cause merge conflicts, human review bottleneck, or resource contention.

**Prevention:**
- Hard cap: 4 concurrent subagents maximum
- Explicit dependencies in tasks.md prevent over-parallelization
- Wave synchronization ensures each batch completes and is reviewed before the next begins
- File ownership rules (each file belongs to one agent) prevent merge conflicts

---

## Successful Implementation Patterns

### Spec → Test → Code Flow

```
1. Agent reads Task #N from tasks.md
2. Agent reads acceptance criteria from requirements.md
3. Agent writes test cases that encode the spec
4. Agent runs tests (they fail — Red)
5. Agent writes implementation
6. Agent runs tests (they pass — Green)
7. Agent commits: "feat: Task N - [description]"
8. Agent signals completion
9. Human reviews diff and test results
10. Human approves or requests changes
```

### Dependency-Aware Sequencing

Example: Build a REST API with database persistence

```markdown
Wave 1 (parallel):
- Task 1: Database schema + migrations
- Task 2: Entity definitions and domain layer

Wave 2 (sequential after Wave 1):
- Task 3: Repository interfaces and implementations
- Task 4: Service business logic (depends on Task 3)

Wave 3 (sequential after Wave 2):
- Task 5: API endpoint definitions (depends on Task 4)
- Task 6: API handlers and middleware

Wave 4 (sequential after Wave 3):
- Task 7: Integration tests (depends on Tasks 5–6)
- Task 8: Documentation and examples
```

Each wave completes, is reviewed, and approved before the next wave starts.

### Handling Context Limits in Long Features

If a feature requires 20+ tasks and a single agent session cannot hold all of them:

1. **Split across subagent waves:** Batch tasks into groups of 3–5 tasks per wave
2. **Checkpoint between waves:** After each wave, commit and push to remote; the next wave reads fresh from the remote (no accumulation)
3. **Use shared state files:** Store critical outputs (API schemas, database migrations, type definitions) in version-controlled files that each wave reads fresh

---

## Relationship to Other Phases

### Input from Planning Phase (S3.1)

The Implementation Phase depends on:
- Approved requirements.md (no spec ambiguities)
- Approved design.md (no architecture contradictions)
- Approved tasks.md with clear dependencies and acceptance criteria

If these are unclear, implementation stalls. The correct response is to surface the ambiguity and re-open the Planning Phase, not to have agents improvise.

### Output to Verification Phase (S3.3)

The Implementation Phase produces:
- Committed code in feature branches
- Passing test results
- A task-log documenting which tasks completed, which failed, and why
- Focused diffs per task (ready for review)

The Verification Phase consumes these artifacts and applies automated and human gates before accepting the feature.

---

## Sources

- [Spec-driven development with AI: Get started with a new open source toolkit — GitHub Blog (2025-09-02)](https://github.com/github/spec-kit)
- [Spec-Driven Development with AI: Complete Guide — prommer.net (2026-01-29)](https://prommer.net/en/tech/guides/spec-driven-development/)
- [Spec-Driven Development with AI Agents: A Practical Guide — Xcapit Inc. (2026-02-01)](https://www.xcapit.com/en/blog/spec-driven-development-ai-agents)
- [How AI Enhances Spec-Driven Development Workflows — Augment Code (2026-02-23)](https://www.augmentcode.com/guides/ai-spec-driven-development-workflows)
- [Spec-Driven Development: Building Production-Ready Software with AI — orchestrator.dev (2025-12-16)](https://orchestrator.dev/blog/2025-12-16-spec_driven_dev_article/)
- [Specification-Driven Development: How to Stop Vibe Coding and Actually Ship — Pockit Blog (2026-04-07)](https://pockit.tools/blog/specification-driven-development-ai-coding-agents-complete-guide/)
- [Spec-Driven LLM Development: Precise Engineering Through Specifications — David Lapsley (2026-01-11)](https://blog.davidlapsley.io/engineering/process/best%20practices/ai-assisted%20development/2026/01/11/spec-driven-development-with-llms.html)
- [Spec-Driven Development: From Code to Contract in the Age of AI Coding Assistants — arXiv:2602.00180 (2026-01-30)](https://arxiv.org/abs/2602.00180)
- [Multi-Agent AI Coding Workflow: The Complete Guide — The Agentic Blog (2026-04-06)](https://blog.appxlab.io/2026/04/06/multi-agent-ai-coding-workflow/)
- [Claude Code Subagents and the Orchestrator Pattern — Chanl Blog (2026-04-01)](https://www.channel.tel/blog/claude-code-subagents-orchestrator-pattern)
- [Deep Agents — Encyclopedia of Agentic Coding Patterns (2026)](https://aipatternbook.com/deep-agents)
- [Hierarchical AI Agent Coordination: Task Delegation, Review Loops — Zylos Research (2026-03-01)](https://zylos.ai/research/2026-03-01-hierarchical-ai-agent-coordination)
- [CODEDELEGATOR: Decoupling Planning from Implementation — arXiv:2601.14914 (2026)](https://arxiv.org/pdf/2601.14914)
- [Agent Delegation Platform Guide — levnikolaevich/claude-code-skills GitHub (2026)](https://github.com/levnikolaevich/claude-code-skills/blob/master/docs/architecture/AGENT_DELEGATION_PLATFORM_GUIDE.md)
- [Claude Code | Agentic Engineering — jayminwest.com](https://www.jayminwest.com/agentic-engineering-book/10-practitioner-toolkit/1-claude-code)
