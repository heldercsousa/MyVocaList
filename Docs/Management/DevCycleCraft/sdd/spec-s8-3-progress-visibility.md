# S8.3 — Progress Visibility

**Status:** Researched
**Predecessor(s) ID:** S8

## Changelog
| Date/Time | Type | Description |
|-----------|------|-------------|
| 2026-04-26 | Created | Initial file creation |
| 2026-05-02 | Researched | Content written by research agent |

---

## Overview

Progress visibility in Spec-Driven Development is distinct from traditional project management because the workers are stateless AI agents, not humans who carry context and memory between sessions. Traditional dashboards built on human self-reporting fail in agent-driven workflows. The emerging SDD practice is: externalize all state to machine-readable files, verify status programmatically rather than trusting agent assertions, and provide real-time visibility surfaces that read (not write) the authoritative state files.

Three mechanisms have converged in SDD practice as of 2025–2026:
1. **Markdown files as the state machine** — `tasks.md` checkboxes and `task-log.md` audit trails are the ground truth
2. **Native agent runtime task systems** — Claude Code's task management, Cursor's parallel agents, and similar — provide session-scoped visibility that bridges to persistent state
3. **Specialized SDD dashboards** — visual layers that read markdown, git history, and metrics files and present them as real-time kanban boards and progress rings

The principle is: **dashboards are read surfaces, not write surfaces.** Progress is driven by agents executing against specs and committing changes. Dashboards observe the git history and state files; they do not reverse-drive work.

---

## Progress Visibility at Two Levels

### Feature-Level Visibility: tasks.md

Within a single feature (`Docs/specs/venues/tasks.md`), progress is visible at granular resolution. Each task is a checkbox. A task is checked off only when three conditions are met:
1. The code builds with no errors
2. Tests pass (if the task touched tested code)
3. The checkbox is explicitly checked in the file

This definition is not self-reported by the agent. The main orchestrator runs `dotnet build` and `dotnet test` to verify the conditions before accepting a checkbox as valid. The file acts as a state machine: the code cannot lie about whether it compiles or tests pass, so the checkbox becomes a reliable marker.

In the MyVocaList workflow, the pattern is: "Rule 3 — Commit After Every Task" requires that a task be committed only when all three conditions hold. An agent that marks a task done without verifying it is creating a false signal — the repository becomes the source of truth once again, not the spec.

### Session/Cross-Feature Visibility: task-log.md

`task-log.md` is an append-only audit log that records outcomes across sessions and features. Each row is a completed task from any feature or research activity, with a status (`done` / `fail`) and a one-line reason. The log answers four questions:
1. What was worked on?
2. Did it succeed?
3. Why did it succeed or fail?
4. When did it happen?

The log is the recovery surface when a session is lost. If a developer closes the laptop and returns hours or days later, reading `task-log.md` shows exactly where work stopped. It is also the audit trail for the human architect, providing visibility into cross-project effort distribution and failure patterns.

Example format (MyVocaList convention):
```
| 2026-05-02 | S8_3_Progress_Visibility | done | Research completed |
| 2026-05-01 | Venues_CRUD_Tests | done | All tests passing |
| 2026-05-01 | Venues_Edit_Dialog | fail | DevExpress validation conflict, blocked by S2.1 spec clarification |
```

The discipline is: agents only append; agents never edit history. This prevents false progress rewriting.

---

## Native Task Management in AI Runtimes

### Claude Code Task System (2025–Present)

Claude Code introduced native task management in January 2025, shifting from session-scoped `TodoWrite` checklists to a persistent multi-session system using `TaskCreate`, `TaskGet`, `TaskUpdate`, and `TaskList` tools. Tasks are stored in `~/.claude/tasks/` as session-durable artifacts.

**Key properties:**

- **Session-scoped but cross-context:** Tasks persist within a session even across context compactions. Multiple Claude Code sessions can coordinate on the same task list via `CLAUDE_CODE_TASK_LIST_ID` environment variable.
- **Status lifecycle:** `pending → in_progress → completed`. An agent sets a task to `in_progress` before starting work, so an orchestrator agent can detect stuck agents (tasks that have been `in_progress` for too long without a commit).
- **Dependency-aware:** Tasks can declare blocking dependencies. An orchestrator can determine which tasks are ready to run and which are blocked.
- **Hydration pattern:** Tasks are created from persistent specification files at session start, bridging sessions. A `tasks.md` file is read; each checkbox task is hydrated into Claude's task system. When the session ends, the task state is synced back to the file. This pattern allows persistent task structure to drive session-scoped execution.

The hydration pattern is the standard bridge between persistent markdown and session-scoped task systems. It works as follows:

1. At session start, read `tasks.md` (or `IMPLEMENTATION_PLAN.md` for some frameworks)
2. For each unchecked task, create a Claude task with `TaskCreate`
3. During execution, update task status as work progresses
4. At session end or milestone, sync task state back to the markdown file
5. On next session start, re-hydrate from the updated markdown

This pattern allows task progress to accumulate across sessions without losing state.

### Cursor and GitHub Copilot Parallel Agents

Cursor 2.0 (October 2025) shipped parallel agent support built on git worktrees. GitHub Copilot is developing similar multi-agent coordination. Both systems use native task coordination mechanisms to ensure that parallel agents don't step on each other's work and can signal progress to an orchestrator.

The pattern: each agent writes its task status to a shared state file (JSON checkpoint, JSONL log, or git-tracked markdown). The orchestrator reads this state to determine which agents are done, which are blocked, and which should start next.

---

## Memory Persistence Across Sessions

Progress visibility depends on memory that survives session boundaries. The SDD practitioner community has converged on two complementary mechanisms:

### Context Files (CLAUDE.md, Rules Files, Constitution)

These are loaded at session start and provide stable architectural context:
- Decisions already made (and why)
- Patterns confirmed
- Constraints in force
- Team conventions

Context files do not track progress, but they prevent agents from re-litigating resolved questions. An agent that re-reads context files after a break immediately understands the decision landscape without replaying the discussion.

### External State Files (tasks.md, task-log.md, findings.md)

These are written during the session and re-read at session start:
- `tasks.md` — current feature's implementation task checklist
- `task-log.md` — cross-session audit trail of completed work
- `findings.md` — technical discoveries, ADRs (Architecture Decision Records), debugging insights
- `ACTIVE-CONSIDERATIONS.md` — current blockers, open questions, priority stack

An agent that re-reads these files after context compaction recovers working state without relying on conversational memory.

**Evidence from 25-hour sessions:** OpenAI's Codex team demonstrated that a single agent can sustain productive work across 25 hours by externalizing all state to markdown files that the agent re-reads at each milestone. The session itself was structured as a sequence of bounded checkpoints, not one monolithic conversation. At each checkpoint, the agent reads the spec, the plan, and the state file; executes a bounded amount of work; writes the state file; and pauses. The next checkpoint resumes from the written state. This pattern eliminates context window as a hard limit on session length.

**Confidence decay:** Memories degrade over time. Industry consensus (GitHub Copilot, MemNexus, Zep) is that memories should decay by ~5% per week. Recent observations are weighted higher than old ones. This prevents stale decisions from blocking new work.

---

## Live Dashboards and Visualization Layers

### Dashboard Ecology (2025–2026)

Several open-source and commercial tools have emerged to visualize SDD progress:

#### Spec Kitty Dashboard
Real-time kanban board showing work packages across five lanes: `planned`, `doing`, `for_review`, `done`, `blocked`. The dashboard:
- Auto-refreshes via file watching (configurable debounce, typically 300–500ms)
- Shows cycle/sprint grouping with accordion collapse
- Displays metrics per cycle: work package completion %, task progress, functional requirement coverage
- Provides drill-down views showing task details, acceptance criteria, review feedback
- Supports agent handoff with buttons that move work packages between lanes

Activity logs are stored in the work package markdown file itself, timestamped with agent ID and shell PID for auditability.

#### Spec-Driven Development VS Code Extension (jlacube/spec-driven-development-vscode)
A visual pipeline orchestration tool for VS Code that:
- Auto-detects development cycles from numbered artifact prefixes (e.g., `001-user-auth`)
- Shows pipeline stages (Ideation → Spec → Plan → Code → Review) with visual flow
- Groups work by cycle with collapsible accordions and progress bars
- Displays metrics cards (work package completion, task progress, functional requirement coverage)
- Supports kanban board views and spec compliance summaries
- Uses file watching for real-time updates
- Runs entirely locally; stores state in `.sdd/` directory

#### SpecWeave Analytics Dashboard
Tracks agent activity, skill invocations, and agent spawns across a project:
- Records all invocations (commands, skills, agents) in JSONL format at `.specweave/state/analytics/events.jsonl`
- Provides 16 specialized pages: status overview, usage breakdown, increment detail, error tracing, sync audit, activity stream
- Shows WIP limits, active/paused/completed increments, stale work detection
- Auto-suggests what to work on next based on dependency analysis
- Provides CLI access: `specweave analytics --since 30d --export csv`

All analytics data is stored locally; nothing is sent externally.

#### visual-wiggum (spec-view)
A terminal and web UI for tracking specs across multiple SDD tools (spec-kit, Kiro, OpenSpec, and plain markdown):
- Auto-detects spec format (spec-kit's `## Phase N:` headers, Kiro's `.kiro/` structure, OpenSpec's numbered sections)
- Provides TUI (terminal UI) and web UI (localhost:8080)
- Shows spec structure with collapsible phases and progress bars
- Integrates with Ralph Wiggum (autonomous SDD loop) to auto-detect `IMPLEMENTATION_PLAN.md`
- Live file watching with sub-second update latency
- Color-coded story tags and phase indicators

#### SpecBoard
A visual kanban board for non-technical teams to create and track specs:
- 4-stage drag-and-drop pipeline: Backlog → Specs → Plan → Tasks
- AI-powered spec generation from feature name + description
- Task and checklist progress tracking
- Deep-linking for shareable URLs
- Accessible design (WCAG 2.2 AA)

### Key Dashboard Principle

**Dashboards are read-only surfaces.** They do not drive progress. Progress is driven by agents executing against specs and making commits. Dashboards observe the git history, markdown files, and metrics files; they do not reverse-engineer work assignments from dashboard state.

When an agent completes a task, it:
1. Checks the task in `tasks.md`
2. Commits the change to git
3. The dashboard detects the file change (within 300–500ms of file watch debounce)
4. The dashboard updates to reflect the new state

This is a pull model, not a push model. The dashboard never tells the agent "you are assigned task X." The spec tells the agent. The dashboard merely reflects what the spec and git history say.

---

## Cross-Session Continuity Patterns

### MemNexus / Agent Experience (AgentEx) Platform Pattern

Multiple specialized tools (MemNexus, Zep, Graphiti, GitHub Copilot's memory system, Amazon's AgentCore Memory) have converged on an agent memory pattern for cross-session continuity:

**Capture phase (end of session):**
- Session transcript is automatically captured
- A lightweight LLM extracts structured memory entries from the transcript
- Entries are classified by type: decision, fact, convention, bug-fix insight, architectural pattern
- Each entry is embedded into a vector database (ChromaDB, Qdrant, pgvector, or LanceDB)
- Metadata (project, memory type, timestamp) is attached for filtering
- Each memory includes confidence scoring and citations (links back to code locations)

**Retrieve phase (start of next session):**
- Relevant memories are fetched from the vector database via semantic search
- Memories are injected into the agent's context at session start
- Memories include citations (code location references) so the agent can verify they're still valid
- Citations are checked in real-time; if code has changed and contradicts the memory, the agent is prompted to update or discard the memory
- Memories decay over time (~5% per week confidence loss)

**Self-healing:** When an agent finds a memory is outdated or contradicted by current code, it stores a corrected version with updated citations. Over time, the memory pool self-corrects without manual curation.

**Impact:** GitHub's measurement (Jan 2026) showed 7% increase in Copilot coding agent task merge rates and 2% increase in code review precision when memory is enabled.

### auto-sdd Pattern: Learnings System with Signal Scores

The auto-sdd project (Superloop orchestrator) uses a learnings system with lifecycle and signal scoring:

**Learnings lifecycle:**
- Observation → Demonstrated → Validated → Core (top 17, inlined into CLAUDE.md)
- Signal scores (1–8+) track confidence based on how many independent sessions confirmed a pattern
- Entries can be REFUTED and archived with a reason

**Checkpoint protocol:**
- At session end, the agent scans for uncaptured learnings
- Approved entries are written to `learnings/` directory
- `.onboarding-state` file is updated with prompt count and state metadata
- `ACTIVE-CONSIDERATIONS.md` (priority stack) is updated
- Everything is committed and pushed
- The next session boots with learnings on first read

Each learning entry is self-contained — a fresh agent can read one entry and understand it without context.

### Tracking Files Pattern (OpenSpec Proposal)

An emerging pattern for structured session resumption:

```
openspec/changes/{change-id}/
├── task_plan.md       # Phase, goals, tasks, error log
├── findings.md        # Technical findings, ADR
├── progress.md        # Session progress log
└── delta-log.md       # Change records
```

Each file has a specific purpose:
- `task_plan.md` — current phase, blocked tasks, error log, next steps
- `findings.md` — technical discoveries, architecture decisions, "why did we choose this?"
- `progress.md` — session timestamps, what changed, what broke
- `delta-log.md` — machine-readable change audit for CI/CD

This provides a **3-strike protocol** for error recovery:
1. First error: diagnose & fix, update `progress.md`
2. Second error: try alternative approach, update `findings.md`
3. Third error: rethink problem, escalate to human, mark as `[BLOCKED]` in `task_plan.md`

---

## Architectural Decision: When to Externalize State

The fundamental decision in SDD progress visibility is: **what state must be externalized, and what can remain in conversation?**

| State Type | Externalize | Reason |
|-----------|------------|--------|
| Current task list (`tasks.md`) | Yes | Must survive context compaction |
| Technical findings (ADRs) | Yes | Decisions constrain future work |
| Test/build status | Yes | Non-deterministic if lost; must be re-verified |
| Session activity log | Yes | Audit trail and recovery surface |
| Spec content | Yes | Authoritative artifact that drives work |
| Architecture decisions | Yes | Context for all future agents |
| Code patterns observed | Yes | Prevent re-litigating style questions |
| Debugging insights | Yes | Dead-ends should not be re-explored |
| Conversational context | No | Too verbose; re-read specs instead |
| Intent negotiation | Partial | Document outcome in spec; discard conversation |
| Candidate approaches explored | Partial | Document chosen approach; discard rejected ones |

The rule of thumb: **if two agents working independently should reach the same decision, the decision must be externalized.** If one agent starts fresh and doesn't have the decision available, it will re-litigate.

---

## Automated Visibility at Scale

### Health Score / Project Grade

SpecMem (Superagentic AI) and similar tools compute an overall project health grade (A–F) based on:
- Spec coverage: what % of acceptance criteria have tests?
- Spec stability: how much has the spec changed in the last week? (High churn = low stability)
- Spec-code alignment: does code contain features not in the spec? (Drift indicator)
- Test coverage: line/branch coverage vs. requirements coverage
- Spec freshness: how old is the most recent update to each spec?

The grade is computed from machine-readable metrics; it is not self-reported. This allows human architects to spot projects at risk of spec-code divergence without reading every file.

### CI/CD Integration for Continuous Conformance

Platforms like Specledger and SpecWeave ship CI/CD integrations that:
- Run `specledger validate` on every PR to check for spec violations
- Compare spec intent to code changes; flag if PR violates spec constraints
- Track spec-code drift automatically
- Provide a "spec-code alignment report" showing which areas are drifting

This moves progress visibility from discrete dashboards to continuous verification: every commit is automatically assessed against the spec it claims to implement.

---

## Failure Modes and Mitigations

### Silent Task Completion

**Problem:** An agent marks a task done in `tasks.md` without running the required verification (build, test). The next agent reads the file and trusts the checklist, but the code is actually broken.

**Mitigation (MyVocaList Rule 3):** A task is only checked when the main orchestrator has run `dotnet build` and `dotnet test` and confirmed they pass. The checkpoint protocol in auto-sdd encodes this as mechanical enforcement: `.onboarding-state` file tracks prompt count and triggers interval checks.

### Memory Staleness

**Problem:** An agent reads a memory entry that was valid a month ago but is now outdated. The memory contradicts current code.

**Mitigation:** All memory systems store citations (code location references). When a memory is retrieved, the agent verifies the citations in real-time against current code. If citations are invalid or contradictory, the memory is marked invalid and the agent updates or discards it.

### Dashboard Lag

**Problem:** The dashboard shows a task as "done" but the agent's session is still running and has encountered a build error. The human reads the dashboard and thinks the work is finished when it's actually blocked.

**Mitigation:** Dashboards are observational layers that read git history and file timestamps. They reflect the last committed state, not real-time agent state. This is a feature, not a bug — it prevents the human from making decisions based on transient state. A task is only "done" when it is committed to git.

### Cross-Agent Spec Conflicts

**Problem:** Two agents working in parallel on interdependent features encode contradictory assumptions about a shared interface. Both agents' specs are internally consistent; the conflict only emerges at merge time.

**Mitigation:** Before running agents in parallel, use shared project memory or a coordinator-reviewer agent to scan all dependent specs for semantic conflicts. If conflicts are found, sequence the dependent specs (one agent finishes and merges before the next begins) or renegotiate the shared interface.

---

## Current SDD Practice (2025–2026)

The majority of teams running SDD workflows use a combination of:

1. **Markdown files as state** — `tasks.md`, `task-log.md`, findings, decisions
2. **Native agent task systems** — Claude Code tasks, Cursor agents, GitHub Copilot task coordination
3. **Git commits as verification** — a task is only "done" when pushed to the repository
4. **One visualization layer** — either a dashboard (Spec Kitty, spec-view) or a CLI summary tool (speckit-status)
5. **Memory persistence via files + vector DB** — either hand-written in markdown or automatically captured via embedding

This combination is mature, proven at scale (OpenAI's 25-hour sessions, auto-sdd, Cursor 2.0 parallel agents), and supported by open-source tooling. It requires no centralized service, no vendor lock-in, and works across multiple AI agents and platforms.

---

## Sources

- [SDD Dashboard for VS Code — spec-driven-development-vscode](https://github.com/jlacube/spec-driven-development-vscode) (2026-03-29)
- [Analytics Dashboard — SpecWeave](https://spec-weave.com/docs/guides/analytics-dashboard) (2026-03-25)
- [Parallel Implementation Tracking with Spec Kitty — Priivacy-ai/spec-kitty](https://github.com/Priivacy-ai/spec-kitty/blob/main/examples/parallel-implementation-tracking.md)
- [Status Command — SpecWeave](https://spec-weave.com/docs/commands/status-management/) (2026-03-13)
- [speckit-status — CLI for Spec Kit Task Progress](https://github.com/mkatanski/speckit-status) (2025-12-16)
- [SpecMem: Spec Management with Coverage & Health Scoring](https://super-agentic.ai/specmem/) — Superagentic AI
- [Spec Kit Explained with an End-to-End Example — ScaleMind](https://scalemind.dev/ai/ml/developer-tools/spec-kit-explained-with-end-to-end-example/) (2026-03-26)
- [SpecBoard — Visual Spec-Driven Development Dashboard](https://github.com/spec-board/spec-board) (2025-12-29)
- [Work Commands — fspec.dev](https://fspec.dev/commands/work/)
- [Agent Config for SDD Meta-System — fearovex/claude-config](https://github.com/fearovex/agent-config) (2026-02-25)
- [oh-my-opencode-lite: Delegate-First SDD with Thoth-Mem](https://github.com/EremesNG/oh-my-opencode-lite) (2026-03-24)
- [auto-sdd: Autonomous SDD Orchestrator with Learnings System](https://github.com/fischmanb/auto-sdd) (2026-02-20)
- [Built-in Context Persistence Support with Tracking Files — OpenSpec Issue #846](https://github.com/Fission-AI/OpenSpec/issues/846) (2026-03-16)
- [How to Give Your Coding Agent Persistent Memory — MemNexus](https://memnexus.ai/blog/2026-03-23-coding-agent-persistent-memory) (2026-03-23)
- [Building an Agentic Memory System for GitHub Copilot — GitHub Blog](https://github.blog/ai-and-ml/github-copilot/building-an-agentic-memory-system-for-github-copilot) (2026-01-15)
- [Claude Code Todos to Tasks: Hydration Pattern for Cross-Session Persistence — Rick Hightower / Medium](https://medium.com/@richardhightower/claude-code-todos-to-tasks-5a1b0e351a1c) (2026-01-26)
- [Persistent Memory for AI Coding Agents: Engineering Blueprint — Sourabh Sharma / Medium](https://medium.com/@sourabh.node/persistent-memory-for-ai-coding-agents-an-engineering-blueprint-for-cross-session-continuity-999136960877) (2026-02-22)
- [Agents.md: auto-sdd Learnings System & Checkpoint Protocol](https://github.com/fischmanb/auto-sdd/blob/main/Agents.md)
- [MemCoder: Continual Human-AI Co-Evolution via Project History — arXiv:2603.13258](https://arxiv.org/html/2603.13258) — Demonstrates code agent growth through structured memory from historical commits
