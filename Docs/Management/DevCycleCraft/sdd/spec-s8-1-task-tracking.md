# S8.1 — Task Tracking

**Status:** Researched  
**Predecessor(s) ID:** S8

## Changelog
| Date/Time | Type | Description |
|-----------|------|-------------|
| 2026-04-26 | Created | Initial file creation |
| 2026-05-02 | Researched | Research completed; content written |

---

## Overview

Task tracking in SDD is fundamentally different from traditional project management because the primary artifact is not a human-readable status dashboard but a machine-readable checklist stored in the repository. The `tasks.md` file (or variant names in different frameworks) serves as both the specification for what must be implemented and the running record of what has been done. For AI agents, this is essential: they cannot carry context between sessions, and they cannot reliably remember what they completed. An ordered, checkboxed markdown file is the ground truth.

The critical insight driving SDD task tracking is that tasks are not abstract work items assigned to developers — they are concrete, atomic units of work that an AI agent reads, executes, and marks complete. This means task design has two competing pressures: granularity must be fine enough that an agent can execute without losing context, but not so fine that coordination overhead overwhelms productivity.

---

## Core Definition: tasks.md as Authoritative Record

In SDD, `tasks.md` is a markdown file checked into the repository, co-located with the feature's `spec.md` and `plan.md` (or equivalent design documents). It is an ordered, checkboxed list where each item represents one discrete unit of implementation work. The checklist is not aspirational — it is the single source of truth for what work remains and what work has been completed.

**Key properties:**

- **Ordered:** Tasks are numbered and sequenced. Earlier tasks must complete before later tasks begin (respecting dependencies).
- **Checkboxed:** Each task is a markdown checkbox: `- [ ]` (not started), `- [x]` (completed), or `- [~]` (in progress, in frameworks that support it).
- **Atomic:** Each task is completable in a single agent session (typically 15–30 minutes of work).
- **Verifiable:** Completion is not self-reported. A task is checked only after the agent confirms: code builds, tests pass, acceptance criteria met.
- **Co-located:** Lives alongside spec and plan, not in a separate ticketing system.

The convention in MyVocaList's `workflow.md` Rule 4 codifies this: "Check off each task in `Docs/specs/[feature]/tasks.md` as it completes. Never start a new task before the previous one is committed. The task list is the audit trail for the feature — keep it accurate."

This pattern has emerged as industry standard practice in 2025–2026. GitHub Spec Kit, Amazon Kiro, SpecWeave, Agent OS, and the `tasks.md` lightweight spec (a companion to `AGENTS.md`) all converge on the same core principle: externalize task state to a machine-readable checklist.

---

## Task Structure and Metadata

While the simplest form of `tasks.md` is a flat checkbox list, production SDD frameworks have converged on minimal metadata to support proper execution:

### Basic Format (MyVocaList pattern)

```markdown
## Task Checklist

- [ ] Task 1 — Implement registration endpoint
- [ ] Task 2 — Add JWT validation middleware
- [ ] Task 3 — Write integration tests for login flow
- [ ] Task 4 — Update API documentation
```

### Enhanced Format (SpecWeave, Spec Kit, Agent OS)

Tasks include embedded metadata for traceability and testing:

```markdown
## T-001: Create User Model

**User Story**: US-001 (User Registration)  
**Satisfies ACs**: AC-US1-01, AC-US1-02  
**Status**: [ ] pending  
**Test Plan**:
- **Given** valid user email and password
- **When** CreateUser() is called
- **Then** user is persisted and JWT returned

**Test Cases**:
- Unit: `registerUser()` validates email format
- Integration: `POST /auth/register` stores user

**Implementation**:
- Create `User` domain model
- Add `IUserRepository.CreateAsync()`
- Create migration `Add_Users_Table`

**Dependencies**: None

---

## T-002: Implement JWT Manager

**User Story**: US-001  
**Satisfies ACs**: AC-US1-01  
**Status**: [ ] pending  
**Test Plan**:
- **Given** user payload
- **When** JwtManager.GenerateToken() is called
- **Then** valid JWT with 15-min expiry is returned

**Implementation**:
- Add `JwtManager` service
- RS256 signing algorithm
- Token refresh logic

**Dependencies**: T-001 (needs User model)
```

### Metadata Fields Across Frameworks

| Field | Purpose | Example | Framework |
|-------|---------|---------|-----------|
| **Task ID** | Unique reference | `T-001`, `001-user-auth` | Spec Kit, SpecWeave, taskmd |
| **User Story Link** | Traceability to spec | `US-001` | SpecWeave, Spec Kit |
| **Acceptance Criteria** | Verifiable completion | `AC-US1-01, AC-US1-02` | SpecWeave |
| **Status** | `[ ]` pending, `[x]` complete, `[~]` in progress | `[x]` | All |
| **Test Plan** | BDD format (Given/When/Then) | See above | SpecWeave, Agent OS |
| **Dependencies** | Blocking task IDs | `depends_on: T-001` | SpecWeave, taskmd, Spec Kit |
| **Priority** | P0–P3 ranking | `priority: P0` | tasks.md spec, taskmd |
| **Effort** | Time estimate | `effort: medium` | taskmd |
| **Tags** | Categorical markers | `tags: [backend, database, tdd]` | taskmd, aitasks |

The MyVocaList project uses a minimal subset — ordered checklist with optional dependencies — but understands that richer frameworks (SpecWeave, Spec Kit) embed test plans and acceptance criteria directly in the task file for single-source-of-truth verification.

---

## Task Atomization: Granularity Calibration

The central problem in task design is granularity: tasks that are too coarse cause agents to lose context and produce incomplete implementations; tasks that are too fine create fragmentation and coordination overhead.

### Size Heuristics

**Industry consensus (2025–2026) converges on:**

- **Optimal window:** 15–30 minutes of agent execution time per task
- **Upper bound:** If a task requires more than 3–4 tool calls (file reads, edits, builds, tests) to complete, it is too large — split it
- **Lower bound:** Tasks shorter than 5 minutes do not justify the context-switching overhead of reading prerequisites and building/testing
- **OpenAI Codex precedent:** The 25-hour autonomous session worked because work was broken into 15–30 minute checkpoints, not because the entire session was one monolithic task

### Atomization Rules

**Spec-completeness:** Each task must be fully specified within its own block. An agent should not need to re-read `spec.md` or `design.md` mid-task. The task must contain:
- What files to modify (exact paths)
- What to implement (not vague — "create login form" is vague; "create `SignupPage.xaml` with email/password inputs and validation" is concrete)
- What tests to write (if TDD — see S9.1)
- What verification looks like (build passes, tests pass, specific acceptance criteria met)

**File-disjoint scoping:** For parallel execution, each task must own a non-overlapping set of files. If two agents are writing to the same file simultaneously, merge conflicts are guaranteed. File disjointness is the safety invariant for parallelism.

**Additive vs. edit distinction:**
- **Additive tasks** (new features, new files, new tests) can run in parallel because they do not interfere with existing code.
- **Edit tasks** (refactoring shared code, migrating global patterns, renaming frequently-used symbols) must be sequenced — one agent finishes, merges, then the next agent begins on a fresh main branch. Confusing these two is the most common parallelism mistake.

**Dependency ordering:**
```markdown
## Phase 1: Foundation
- [ ] T-001 Create User model (no dependencies)
- [ ] T-002 Create UserRepository interface (depends_on: T-001)

## Phase 2: Service Layer
- [ ] T-003 Implement UserService (depends_on: T-001, T-002)

## Phase 3: API
- [ ] T-004 Create /register endpoint (depends_on: T-003)
- [ ] T-005 Create /login endpoint (depends_on: T-003)

## Phase 4: Testing
- [ ] T-006 Write integration tests for registration (depends_on: T-004)
- [ ] T-007 Write integration tests for login (depends_on: T-005)
```

T-004 and T-005 can run in parallel (both depend on T-003, neither depends on the other). T-006 and T-007 can run in parallel. But T-003 must complete before either T-004 or T-005 starts.

### Anti-Patterns

| Anti-Pattern | Why It Fails | Fix |
|--------------|--------------|-----|
| "Implement the feature" | Too vague; agent guesses wrong | Break into file-specific tasks: models, services, UI, tests |
| Task depends on itself indirectly | Hidden cycle in dependency graph | Audit the dependency chain visually before distributing tasks |
| One agent owns 10+ tasks | Context rot across the session | Split across agents or break into sub-tasks (see S8.2 for parallel structure) |
| Task X blocks task Y but task Y is listed first | Violates ordering assumption | Always list prerequisites before dependents |
| Mixed additive + edit in one task | Agent may refactor while building, causing conflicts | Separate "add feature" from "refactor shared code" |

---

## The task-log.md Pattern: Cross-Session Progress

Parallel to the feature-level `tasks.md`, many SDD projects maintain a `task-log.md` file at the project root. This is a cross-feature, cross-session audit trail.

**Purpose:** When a complex feature spans multiple sessions or multiple agents, the main agent (orchestrator) needs to know which subtasks have completed, which failed, and why. The `task-log.md` serves this role.

**Format (MyVocaList example):**

```markdown
# Task Log

| Date | Feature / Task | Status | Notes |
|------|---|--------|-------|
| 2026-05-01 | Venue CRUD — T-001: Create model | done | Model + repo interface checked |
| 2026-05-01 | Venue CRUD — T-002: VenueService | done | Validation logic + error handling |
| 2026-05-01 | Venue CRUD — T-003: Unit tests | done | 18 test cases, 95% coverage |
| 2026-05-01 | Venue CRUD — T-004: Page impl | fail | Context window exhaustion; UI sub-task split for next session |
| 2026-05-02 | Venue CRUD — T-004a: VenuesPage XAML | done | DevExpress DXCollectionView binding |
| 2026-05-02 | Venue CRUD — T-004b: VenuesViewModel | fail | Observable property wiring incomplete; needs verification |
| 2026-05-02 | Venue CRUD — T-004b (retry) | done | Resolved binding issue in view model |
```

**Rules:**
- Append-only: agents only add rows, never edit history
- Include task ID or name, status (`done` / `fail` / `in-progress`), and a one-line reason
- Provides the human architect with recovery info: if a session crashes, the log shows where to resume
- Acts as an implicit dependency tracker for researchers and planners who need to understand what has been tried and what failed

---

## Task Status Lifecycle in Native Agents

Claude Code introduced native task management in January 2025, replacing flat `TodoWrite` checklists with a persistent multi-session system. This is relevant to SDD because it shifts the burden of task state management from markdown files to a structured API.

### Claude Code TaskCreate / TaskUpdate Pattern

```
pending → in_progress → completed
```

**Workflow:**
1. Agent calls `TaskCreate` with subject, description, and optional `addBlockedBy` (dependency).
2. Agent sets status to `in_progress` before starting work (signals to other agents: this task is claimed).
3. Agent completes work and calls `TaskUpdate` with status `completed`.
4. If the agent is blocked (prerequisite task hasn't completed), it calls `TaskUpdate` with `addBlockedBy: [task-id]`.

**Key feature:** Tasks persist in `~/.claude/tasks/` across context compactions and session resets. Multiple Claude Code sessions coordinate via the environment variable `CLAUDE_CODE_TASK_LIST_ID`.

**Implication for SDD:** Native task management reduces the need for external `tasks.md` files because the runtime itself maintains persistent task state. However, markdown `tasks.md` remains useful for human visibility and version control. Many projects use both: `tasks.md` in git as the human-readable spec, and native Tasks as the agent-facing runtime state.

---

## Marked Checkpoints and Parallel Execution

When tasks are designed for parallel execution, frameworks use markers to signal which tasks can run concurrently:

**Parallel marker:**
```markdown
- [ ] T-004 [P] Create /register endpoint (depends_on: T-003)
- [ ] T-005 [P] Create /login endpoint (depends_on: T-003)
```

The `[P]` indicates these tasks can be assigned to different agents simultaneously. They have the same dependency (T-003), but neither depends on the other.

**In-progress marker:**
```markdown
- [~] T-002 AuthService implementation (in progress)
```

Some frameworks support `[~]` to indicate work has started. This prevents a second agent from claiming the same task.

**Skip marker (advanced):**
```markdown
- [!] T-009 Integration tests (SKIPPED — covered by T-008)
```

Some frameworks allow tasks to be marked skipped or superseded, with notes explaining why.

### MyVocaList Workflow Integration

The MyVocaList project's workflow.md enforces a 4-agent wave cap (Rule 2): never spawn more than 4 parallel subagents at once. This is not a technical limit but a human-review constraint — the orchestrating agent can meaningfully review up to 4 concurrent outputs before coordination overhead exceeds productivity gain.

Each task should include explicit dependency markers if it blocks later work:

```markdown
# Tasks: Singer Queue Management

## Phase 1: Domain Model
- [ ] T-001 Create Queue entity and repository interface (no dependencies)

## Phase 2: Service Layer (can start once T-001 is committed)
- [ ] T-002 QueueService — add/remove singer logic (depends_on: T-001)
- [ ] T-003 QueueService — reorder logic (depends_on: T-001)

## Phase 3: UI (can run in parallel once T-002, T-003 are committed)
- [ ] [P] T-004 QueuePage XAML (depends_on: T-002, T-003)
- [ ] [P] T-005 QueueViewModel (depends_on: T-002, T-003)

## Phase 4: Tests (runs last)
- [ ] T-006 QueueService unit tests (covers T-002, T-003)
- [ ] T-007 QueueViewModel integration tests (covers T-005)
```

---

## Verification and Completion Gates

A task is not considered complete until:

1. **Code builds with no errors** — agent runs `dotnet build` or equivalent
2. **Tests pass** (if the task includes tests or touches tested code) — agent runs `dotnet test`
3. **Checkpoint validated** — agent confirms acceptance criteria from the task description
4. **Checkbox marked** — agent updates the task status in `tasks.md` from `[ ]` to `[x]` (or calls native `TaskUpdate` in Claude Code)
5. **Committed and pushed** — agent commits all changes and pushes to origin

MyVocaList's Rule 3 enforces this: "Run `/project:commit` after every task from `tasks.md` is complete." A session that ends with uncommitted changes is one where progress is at risk.

### Verification Agents (Adversarial Pattern)

For high-stakes work, some teams use a "verification agent" pattern: the implementor agent completes a task, commits, and pushes. A separate verification agent (with opposing incentives — it is rewarded for finding problems) reviews the task output and either approves (moves task to `verified`) or sends it back for rework.

This pattern is mentioned in S5.1 (Adversarial Agent Pattern). It is not required for MyVocaList's single-orchestrator workflow but is a useful pattern for larger teams or compliance-required systems.

---

## Integration with Spec Drift Prevention

Task tracking feeds into the broader SDD commitment to keep code and spec in sync. When a task is completed and its checkbox marked, that completion is a data point: this feature has been implemented and tested. If later code review or testing reveals the implementation did not match the spec, the task status is not retroactively un-checked — instead, a new task is added to fix the mismatch, and the spec is updated.

The interplay is:
- **Spec defines** what the feature must do (S2.x)
- **Plan decomposes** the spec into architectural decisions (S3.x)
- **Tasks break down** the plan into atomic work units (S8.1 — this document)
- **Agents execute** tasks, commit, and mark complete (S3.2)
- **Tests verify** that implementation matches task acceptance criteria (S9.1)
- **Code review** checks that implementation matches spec intent (S3.3)

If code review finds a mismatch, it is not a task failure — it is a spec-code alignment issue, handled by updating the spec and regenerating affected code (advanced SDD) or creating a follow-up fix task.

---

## Tooling Landscape

A range of purpose-built tools have emerged to manage SDD task workflows:

| Tool / Framework | Task Storage | Status Tracking | Dependency Support | Features |
|---|---|---|---|---|
| **Markdown + Git** (baseline) | `tasks.md` in repo | Checkboxes | Manual (text notes) | Version control, human readable, no infrastructure |
| **GitHub Spec Kit** | `tasks.md` in repo | Checkboxes, GitHub issues | Implicit (by plan order) | Automated issue sync, skill-based execution |
| **Amazon Kiro** | Native (IDE state) | Checkbox + activity log | Implicit | VS Code fork, built-in IDE task pane |
| **SpecWeave** | `.specweave/increments/tasks.md` | Checkbox + embedded BDD | Explicit (`depends_on` field) | Test plan integration, AC traceability |
| **Agent OS** | `agent-os/specs/tasks.md` | Checkboxes | Manual | Specialty-based task grouping (DB, API, UI) |
| **tasks.md spec** | `TASKS.md` (lightweight) | Checkboxes, optional `(@agent-id)` claim | `Blocked by` field | Minimal, portable, cli-based board |
| **taskmd** | `tasks/001-task.md` (per-file) | Checkbox + YAML status | `dependencies: [002, 003]` | Web dashboard, kanban board, TUI |
| **aitasks** | `aitasks/*.md` (per-file) | Checkbox + claim metadata | Hierarchical (parent/child) | Python TUI board, GitHub issue sync |
| **Claude Code Tasks** | `~/.claude/tasks/` (native) | `pending → in_progress → completed` | `addBlockedBy` parameter | Multi-session coordination, persistent |
| **CODITECT Orchestrator** | Database (SQL) | Status + agent affinity | Dependency graph + ML inference | Intelligent agent assignment, performance learning |

**For MyVocaList:** The markdown + git baseline is appropriate. Task files live alongside specs in `Docs/specs/[feature]/tasks.md`, checked into version control. Complexity (rich metadata, test plans, AC traceability) can be added incrementally if feature scope grows.

---

## Practical Guidance for MyVocaList Task Design

When creating a new feature's `tasks.md`, follow these principles:

1. **Read the spec first.** Every task should be a logical unit that moves the feature from "not done" to "partially done" in one checkpoint.

2. **List dependencies explicitly.** If task 5 requires task 3's output, note it at the start of the file or inline in the task.

3. **Order sequentially by default.** Only use parallel markers `[P]` if tasks are truly file-disjoint and can run safely in isolation.

4. **Be specific about files.** "Create user service" is vague. "Create `Services/UserService.cs` with `CreateAsync()`, `ValidateEmailAsync()`, and error handling" is concrete.

5. **Include acceptance criteria.** What does "done" look like? Build passes, tests pass, and what else? State it.

6. **Estimate effort realistically.** If you think a task will take 45 minutes, split it. Better to err on smaller tasks.

7. **Commit after each task.** Do not batch multiple tasks into one commit. Atomic commits make it easier to find issues if something breaks.

8. **Update task-log.md at session end.** Record what you completed, what failed, and why. This is recovery data for the next session.

---

## Sources

- [Feature Request: Automatic Task Completion Tracking — GitHub Spec-Kit Issue #181](https://github.com/github/spec-kit/issues/181) (2025-09-11)
- [tasks.md (Task Checklist) — SpecWeave](https://spec-weave.com/docs/glossary/terms/tasks-md/) (2026-03-20)
- [GitHub Spec Kit — Toolkit for SDD](https://github.com/github/spec-kit) (2025-08-21)
- [Creating a Task List with Agent OS](https://buildermethods.com/agent-os/v2/create-tasks)
- [Spec Kit — Spec-Driven Development Framework](https://research.tedneward.com/languages/spec-kit/index.html) (2026-04)
- [How to Build speckit.tasks Agent Skill — SkillMD.ai](http://skillmd.ai/how-to-build/speckittasks/) (2026-02-01)
- [Spec-Kit-Antigravity-Skills — GitHub](https://github.com/compnew2006/Spec-Kit-Antigravity-Skills) (2026-01-24)
- [Project-Specific Tasks — SpecWeave](https://spec-weave.com/docs/guides/project-specific-tasks) (2026-04-05)
- [SDD Dashboard for VSCode](https://github.com/jlacube/spec-driven-development-vscode) (2026-03-29)
- [aitasks — File-based task management for AI agents](https://github.com/beyondeye/aitasks) (2026-02-11)
- [tasks.md — Lightweight spec for AI agent task queues](https://github.com/tasksmd/tasks.md) (2026-03-13)
- [sdd-tasks Agent Skill — Gentleman Programming](https://github.com/Gentleman-Programming/agent-teams-lite/blob/main/skills/sdd-tasks/SKILL.md)
- [SDD Phase 3: Tasks Integration — Google Gemini CLI Issue #23320](https://github.com/google-gemini/gemini-cli/issues/23320) (2026-03-20)
- [Claude Code Task Management: Native Multi-Session AI — ClaudeFast](https://claudefa.st/blog/guide/development/task-management) (2026-04-29)
- [taskmd — Markdown-based task management for humans and AI agents](https://github.com/driangle/taskmd) (2026-02-08)
- [SDD Task Orchestrator — CODITECT Documentation](https://docs.coditect.ai/projects/task-orchestrator/sdd-task-orchestrator)
