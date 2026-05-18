# S3.2.1 — Task Granularity Calibration

**Status:** Researched
**Predecessor(s) ID:** S3.2

## Changelog
| Date/Time | Type | Description |
|-----------|------|-------------|
| 2026-04-26 | Created | Initial file creation |
| 2026-05-02 | Researched | Research completed; content written from authoritative sources |

---

## Overview

Task granularity calibration is the practice of right-sizing units of work delegated to AI agents. Too coarse a task risks overwhelming a single agent's context window and accumulating errors across many tool invocations; too fine a task fragments the work into artificial units that introduce coordination overhead and waste context on repetitive setup. The optimal granularity depends on task complexity, agent context window size, and the desired size of each review/commit boundary.

This is not a free variable. Empirical research (arXiv:2604.00690) demonstrates that task decomposition has a measurable performance "phase diagram" with distinct regimes:

- **Under-decomposition (Coarse tasks):** Agent fails because the task is too complex to fit in a single context window or reason chain
- **Optimal window:** Peak performance, where task complexity and context usage are balanced
- **Over-decomposition (Fine tasks):** Agent succeeds but coordination overhead (setup, handoffs, micro-commits) outweighs benefit

The goal is to land squarely in the optimal window. For SDD, this means: tasks that are meaningful enough to review in isolation, complex enough to justify agent work, and simple enough to fit in a fresh context window without accumulation.

---

## Core Principle: Context As Process Boundary

The context window is not merely a storage limit—it is the **process boundary** for a single agent session. Once a task is large enough that it exhausts 60-80% of available context, it has inherently defined a process boundary whether or not the human intended one.

Key implications:

1. **Context pollution:** As context accumulates during a long task, early decisions and file reads become buried. Later reasoning operates from degraded (middle-of-context) information retrieval. LLM recall degrades for content in the middle of contexts relative to content at the beginning or end.

2. **Error accumulation:** A wrong assumption made in minute 5 of a long task can contaminate all subsequent work. A fresh context per task means errors are isolated: if Task 7 fails, Tasks 1–6 are safely committed and can be rolled back without affecting them.

3. **Context economics:** Every token consumed by one task is unavailable for the next. A 100K-token task on a 200K context window leaves only 100K tokens for rollback, error recovery, or follow-up work—leaving zero margin for complexity.

4. **Commit atomicity:** Each task should produce a single, reviewable commit. A task that spans two independent features produces a mixed commit that is hard to review and harder to roll back.

---

## Empirical Phase Diagram

Research (arXiv:2604.00690) on task decomposition across 1,200 real tasks defines the **Decomposition Granularity Index (DGI)**:

```
DGI = (number of subtasks) / (minimum necessary steps)
```

Where "minimum necessary steps" is the absolute-minimum sequence of logical steps to accomplish the goal (with no further breakdown). DGI = 1.0 means "no decomposition"; DGI = 3.0 means "decomposed into 3x more subtasks than strictly necessary."

### The Three Phases

| Phase | DGI Range | Characteristic | Success Rate | Problem |
|-------|-----------|-----------------|--------------|---------|
| **I — Under-decomposition** | < DGI* | Task too coarse | 40–60% | Agent overwhelmed; context exhausted before completion |
| **II — Optimal** | ≈ DGI* | Task right-sized | **Peak (64–89%)** | **None** (this is where you want to be) |
| **III — Over-decomposition** | > DGI* | Task fragmented | 20–50% | Coordination overhead exceeds benefit; agents spend time on setup and handoffs |

### Optimal DGI by Complexity

The critical finding: **optimal DGI scales with task complexity.**

```
DGI* ≈ 0.85 × √S

where S = number of sequential reasoning steps in the task
```

This translates to practical task sizes:

| Complexity Regime | Task Duration | Estimated S | Optimal DGI* | Example |
|-------------------|---|---|---|---|
| **Simple** | 5–10 min | 5–8 | 0.8–1.2 | "Add a new table column and migrate one endpoint" |
| **Moderate** | 15–30 min | 10–15 | 1.8–2.4 | "Build a new API endpoint with database schema + service + tests" |
| **Complex** | 45–60 min | 20–30 | 3.0–4.5 | "Implement a multi-entity feature with refactoring, dependency changes, and integration tests" |

### Window Width Narrows With Complexity

The width of the optimal DGI window (how much variance is tolerable) shrinks as complexity increases:

- Simple tasks: Window width 0.4 (DGI 0.8–1.2 all work well)
- Moderate tasks: Window width 0.6 (DGI 1.8–2.4 work)
- Complex tasks: Window width 1.7 but performance is **fragile** (DGI 2.8–4.5 can work, but small changes in decomposition push you into Phase I or III)

**Implication:** For hard tasks, granularity is not a soft preference—it becomes critical. Small changes in task size can drop you from 70% success to 40%.

---

## Context Window Consumption Rules

Empirical data (from lean-spec.dev and other sources) shows how context fills during agent work:

### Fixed costs per session
- System prompt + instructions: 2–5K tokens
- Project rules (CLAUDE.md, rules files): 1–3K tokens
- Task specification: 0.5–2K tokens
- **Subtotal:** 3.5–10K tokens

### Per-file costs
- Small file (100–300 lines): 200–500 tokens
- Medium file (300–1000 lines): 1–2K tokens
- Large file (1000+ lines): 3–5K tokens

### Per-tool-output costs
- Successful build output: 200–500 tokens
- Test results (all passing): 500–1K tokens
- Test results (with failures): 1–3K tokens
- Grep/search results (10 matches): 500–1K tokens
- Each iteration of debugging: +500–2K tokens

### Budget calculation

For a 200K context window (Claude), the recommended operating envelope is:

- **Conservative:** Stay under 40% (< 80K tokens) to maintain margin for unexpected complexity
- **Danger zone:** 60% (120K tokens) and above; performance noticeably degrades
- **Exhaustion zone:** 80%+ (160K tokens); agent reasoning becomes unreliable and coordination becomes expensive

**Practical rule:** If a task is estimated to consume more than 80K tokens (40% of 200K window), split it into smaller tasks.

### Token estimation for a task

```
Estimated tokens = fixed_costs + (num_files × avg_file_tokens) + (num_iterations × iteration_cost) + margin

Example:
- Fixed: 5K
- Read 8 files (avg 800 tokens each): 6.4K
- 5 iterations of debugging (2K each): 10K
- Margin for test output and complexity: 5K
= 26.4K tokens (about 13% of 200K window — safe)

Another example (too large):
- Fixed: 5K
- Read 25 files (avg 1.2K each): 30K
- 15 iterations of debugging: 30K
- Margin: 10K
= 75K tokens (37.5% of 200K window — at boundary; consider splitting)
```

---

## Seven Practical Criteria for Right-Sizing

From Masaki Hirokawa's research (antigravitylab.net), seven specific checks for whether a task is appropriately sized:

### Signs the task is too coarse (over-decomposition risk)

1. **Does it touch more than 5 files?** If a single task modifies more than 5 files, it is likely combining multiple concerns. Consider whether the files naturally group into separate tasks (e.g., "refactor DAO layer" vs. "update service layer" vs. "add controller endpoints").

2. **Does it contain 3+ independent design decisions?** If the task requires deciding between multiple architectural approaches or making several unrelated technical choices, make those decisions before delegating. Task: "Decide between Option A/B for caching, then implement whichever you choose" becomes two tasks: "Research and recommend caching approach" (planning task) and "Implement chosen approach" (implementation task).

3. **Does it include deciding the testing strategy?** If the task requires the agent to invent how it will test (unit vs. integration vs. e2e), the task is too unstructured. The human should specify in advance: "Write unit tests using Moq for service logic, integration tests using TestDbContextFactory for repository queries."

### Signs the task is too fine (under-decomposition risk)

4. **Is the explanation longer than the work?** A task that takes 30 seconds to explain but 3 minutes to describe in detail is not worth delegating. ("Change `if (x > 0)` to `if (x >= 0)` in this file" is faster to do yourself.) Rule of thumb: delegate only tasks that are **10+ lines of code** or **5+ minutes of work**.

5. **Can the task even be judged without seeing surrounding code?** If changing a function signature but only the function is in scope, the task becomes "update the function and its callers." Packaging scope too narrowly creates downstream reconciliation work. Bundle "change return type to Promise" with "update the 3 call sites to async/await" as a single task.

6. **Does the slice leave you with working software?** Slicing tasks too aggressively ("build UI in isolation, wire logic later") creates reconciliation work downstream. Slice at boundaries where the software is still functional. A better task boundary: "Implement the repository interface and add one API endpoint that uses it" (not "implement repository" alone, then "implement endpoint" later).

7. **Are you writing governance rules into each task prompt?** If you notice repeating the same rule in multiple task prompts ("Follow the service return pattern from code-principles.md"), move it to CLAUDE.md or a rules file and reference it once. This reduces task-prompt duplication and keeps the task itself focused.

---

## Concrete Heuristics for SDD

### For simple features (1–5 tasks)

- **No decomposition needed.** Keep the entire feature in one or two tasks, each under 15 minutes.
- **Example:** "Add new field to venue entity, update repository queries, update service validation" → One task.

### For moderate features (5–20 tasks)

- **Group related work.** Bundle database schema + migrations together. Bundle service layer CRUD together. Keep API endpoint + its tests together.
- **Target 5–15 minutes per task.** This empirically fits well within a fresh context window and produces meaningful commits.
- **Use the DRY onion:** Layer 1 (database) → Layer 2 (repository) → Layer 3 (service) → Layer 4 (ViewModel) → Layer 5 (UI). Each layer is a cluster of tasks; tasks within a cluster can run in parallel; clusters must run sequentially.

### For complex features (20+ tasks)

- **Use checkpoints.** After every 5–7 tasks, insert a checkpoint task: "Run full test suite and verify no regressions." This breaks the work into manageable waves, each independently reviewable.
- **Target 8–12 minutes per task.** Smaller tasks reduce risk and improve agility if a mid-feature design change becomes necessary.
- **Apply the "Ralph Loop" pattern:** Break planning from execution. The human/orchestrator reads the full spec and creates the task list (planning phase). Each agent works one task per session (execution phase). Plan once; execute many times. This prevents context bloat from accumulating across 20+ task implementations.

### For spec-driven features with 50+ tasks

- **Expect context exhaustion with naive 1-task-per-context approach.** A 50-task feature cannot be implemented by a single agent reading the full task list and doing them all in sequence; context will be exhausted by task 10–15.
- **Use the session-per-task model (Ralph Loop).** Each task gets a fresh context. The agent reads only:
  - The full spec (requirements.md)
  - The full plan (plan.md)
  - Its specific task from tasks.md
  - A summary of completed prior tasks (compressed to ~500 tokens)
- **Session cost:** ~15–20K tokens per 5–8-minute task. Scaling to 50 tasks is predictable: 50 × 3 minutes = 150 minutes total execution, with no single session exceeding 20K tokens (10% of available context).

---

## Task Specification Template

When delegating a task, ensure it includes:

```markdown
## Task [N]: [Descriptive Title]

**Acceptance Criteria:**
- [ ] Criterion 1 (from requirements.md)
- [ ] Criterion 2
- [ ] Criterion 3

**Constraints:**
- Must follow the [pattern/rule] from [reference]
- Cannot modify [protected code/file]
- Must integrate with [Layer/Component] defined in design.md

**Dependencies:**
- Blocked by: [Task X]
- Blocks: [Task Y]
- Related tasks (can run in parallel): [Task Z]

**Estimated Size:** 5–15 minutes (complexity: Simple / Moderate / Complex)

**Context to read:**
- requirements.md § [section]
- design.md § [section]
- Prior task output: [Task M produced IFooRepository.cs; import it here]
```

This template ensures:
- The agent knows exactly what success looks like (acceptance criteria)
- The agent knows what rules apply (constraints)
- The orchestrator knows which tasks can run in parallel (dependencies)
- The agent can estimate context load before starting (estimated size + context to read)

---

## Anti-Patterns and Common Mistakes

### Anti-Pattern 1: The "Everything in One Task" Trap

**Mistake:** Handing a 50-task feature to an agent with instructions "implement all tasks in tasks.md."

**Why it fails:** Context exhaustion by task 12–15. The agent's reasoning becomes unreliable as context grows. Errors introduced early (wrong assumption about schema) are discovered late (during UI implementation) and require rework at every layer.

**Fix:** One task per session. After each task, commit and hand off to a fresh agent (or the same agent in a new session) with only the context it needs.

### Anti-Pattern 2: The Granularity Goldilocks Failure

**Mistake:** Sizing tasks entirely by gut feel ("seems like a medium task") rather than using criteria or estimation.

**Why it fails:** High variance in outcome. Some "medium" tasks are 10 minutes; others are 45. No predictability, no margin for error, frequent context exhaustion.

**Fix:** Use the seven criteria + estimated file count to pre-size. If estimated scope is uncertain, do a quick "What would the minimum files for this task be?" check. Err on the side of smaller tasks for complex features.

### Anti-Pattern 3: The Mixed-Concern Task

**Mistake:** "Implement the API endpoint and update the database schema and refactor the service layer" as one task.

**Why it fails:** Three independent decisions required. Two files modified per concern. Diffstat is large and hard to review. If the refactoring is rework or wrong, it contaminates the entire task.

**Fix:** Separate concerns into sequential tasks:
1. Update schema + migrations + service validation (Task N)
2. Implement API endpoint using updated service (Task N+1)
3. (Optional) Refactor service for reuse (Task N+2, separate from the feature delivery)

### Anti-Pattern 4: The "Decide Later" Task

**Mistake:** "Implement caching for the API, using whichever caching strategy you think best."

**Why it fails:** The agent spends context deciding between Redis/in-memory/distributed cache. This decision should be made by humans (with requirements context), not by the agent. The agent then implements a wrong choice, and the task fails.

**Fix:** Make decisions before delegating. Task instead becomes: "Implement Redis caching for the API (configuration in .env). Use the StackExchange.Redis pattern from [example file]."

---

## Monitoring and Iteration

### Signals of mis-sized tasks

**Too coarse:**
- Agent reports context exhaustion partway through the task
- Build fails with errors in files touched late in the session (agent didn't re-read them to verify changes)
- Agent completes some acceptance criteria but marks the task failed because it "ran out of context"
- Commit is very large (50+ file changes)

**Too fine:**
- Agent completes the task in 2–3 minutes; mostly setup/teardown
- Multiple tasks could have been combined without losing isolation
- Task-to-task coordination becomes a bottleneck (waiting for handoffs)

**Signals of good sizing:**
- Agent uses 30–50% of context window by task end (margin remains)
- Task takes 8–15 minutes (excludes setup/teardown)
- Commit is focused (5–10 files) and reviewable in 5–10 minutes of reading
- Acceptance criteria are all met; no mid-task design changes
- Rollback is clean if task fails; doesn't break prior tasks

### Feedback loop

After each task completion (or failure):

1. **Check context usage:** Did the task consume more or less than estimated?
2. **Review the diff:** Is it focused or scattered?
3. **Note the time:** Did it take longer or shorter than estimated?
4. **Adjust:** If tasks consistently run over, make the next batch smaller. If they're too small, batch related work together.

This feedback tightens granularity calibration over time, moving from rough estimates to predictable task sizing.

---

## Relationship to S3.2 — Implementation Phase

Task granularity is the **operational mechanism** that makes the Implementation Phase practical. S3.2 defines the workflow (spec → tasks → subagent delegation); S3.2.1 defines how to size those tasks so delegation works reliably.

- **Spec too vague?** → Planning Phase must refine (S3.1)
- **Tasks too coarse?** → Granularity miscalibration (S3.2.1) — split the task
- **Tasks too fine?** → Granularity miscalibration (S3.2.1) — batch related work
- **Task completed but build fails?** → Likely a context-window issue; next task in sequence should be smaller

---

## Sources

- [Task Decomposition Granularity and Agent Performance: An Empirical Phase Diagram — arXiv:2604.00690 (2026-04-04)](https://www.clawrxiv.io/abs/2604.00690)
- [Designing Task Granularity for Antigravity Agents — Masaki Hirokawa / Antigravity Lab (2026-04-22)](https://antigravitylab.net/en/articles/tips/antigravity-task-granularity-design-guide)
- [Why Your AI Agent Gets Dumber with Large Specs (And How to Fix It) — LeanSpec (2025-11-10)](https://lean-spec.dev/blog/ai-agent-performance)
- [Spec Kit + Ralph Loop: Solving AI Context Exhaustion in Large Features — Dominic Böttger (2026-01-18)](https://dominic-boettger.com/blog/speckit-ralph-loop-fresh-context-ai-development/)
- [Context Optimization: Keeping AI Agents in the Smart Zone — Agents Squads (2026-01-01)](https://agents-squads.com/engineering/context-optimization/)
- [The Context Window Is the Process Boundary — luaxe.dev (2026-03-17)](http://www.luaxe.dev/blog/2026-03-17-the-context-window-is-the-process-boundary/)
- [Small-Batch Agent Sessions — MinimumCD Practice Guide](https://migration.minimumcd.org/docs/agentic-cd/architecture/small-batch-sessions/)
- [Phase 4: Task-Based Implementation — The AI Agent Factory (2026-02-11)](https://agentfactory.panaversity.org/docs/General-Agents-Foundations/spec-driven-development/task-based-implementation)
- [Spec-Driven Development: From Code to Contract in the Age of AI Coding Assistants — arXiv:2602.00180 (2026-01-30)](https://arxiv.org/abs/2602.00180)
- [How to Use a Spec-Driven Approach for Coding with AI — JetBrains Junie Blog (2025-10-01)](https://blog.jetbrains.com/junie/2025/10/how-to-use-a-spec-driven-approach-for-coding-with-ai/)
- [ReCode: Unify Plan and Action for Universal Granularity Control — arXiv:2510.23564 (2025-10-27)](https://arxiv.org/abs/2510.23564v2)
- [Specification-Driven Agentic Task Systems Analysis — CODITECT Documentation](https://docs.coditect.ai/research/specification-driven-agentic-task-systems-analysis)
- [Spec-Driven Development with AI: Complete 2025 Guide — dplooy (2025)](https://www.dplooy.com/blog/spec-driven-development-with-ai-complete-2025-guide)
- [Scaling Agentic Capabilities, Not Context: ATLAS — arXiv:2603.06713 (2026)](https://arxiv.org/html/2603.06713v1)
