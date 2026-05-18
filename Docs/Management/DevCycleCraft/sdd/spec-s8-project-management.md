# S8 — Project Management

**Status:** Researched
**Predecessor(s) ID:** —

## Changelog
| Date/Time | Type | Description |
|-----------|------|-------------|
| 2026-04-26 | Created | Initial file creation |
| 2026-04-30 | Researched | Content written by research agent |

---

## Overview

Project management in SDD is not conventional project management applied to AI-assisted development — it is a discipline that has been reshaped by the nature of AI agents as workers. Three concerns dominate: how work is tracked (S8.1), how parallel execution is coordinated safely (S8.2), and how progress is made visible across sessions and agents (S8.3).

The central insight driving SDD project management is that AI agents are not developers who carry context in their heads between sessions. They need structured, externalized state: an ordered task list that is the authoritative record of what has and has not been done; isolated execution environments that prevent one agent's work from contaminating another's; and persistent memory mechanisms that survive context window limits and session resets. Without these three things, multi-agent SDD work degrades into inconsistent state, duplicate effort, and lost progress.

---

## S8.1 — Task Tracking

### tasks.md as the ordered checklist

In the MyVocaList workflow and in the broader SDD practitioner community, `tasks.md` is the authoritative record of implementation work. It is a Markdown file checked into the repository, co-located with the feature's `requirements.md` and `design.md`. Each task is a checkbox item. Tasks are checked off as they complete. The list is ordered — an agent should never start task N before task N-1 is committed.

This pattern emerged from a practical constraint: AI agents have no durable memory. Between sessions — or even within a session after context compaction — an agent cannot reliably remember what it has done. The externalized checklist provides the ground truth. An agent that reads `tasks.md` at session start can determine exactly where to resume without relying on conversational context.

The conventions used in MyVocaList's `workflow.md` encode this explicitly:
- Tasks.md is the source of truth (Rule 4)
- Never start a new task before the previous one is committed (Rule 4)
- Each task completion requires: code builds with no errors, tests pass, checkbox checked (Rule 3)

This three-condition gate matters because it prevents the common failure mode where an agent marks a task done before verifying it. The checklist status and the build status must match.

The `task-log.md` pattern is a related but distinct artifact: while `tasks.md` tracks the current feature's implementation tasks, `task-log.md` records cross-session outcomes for completed and in-progress research/development tasks, providing an audit trail for the human architect.

### S8.1.1 — Task Atomization

Task atomization is the discipline of decomposing work into units that are independently executable by an AI agent without causing conflicts with other concurrent work. The calibration problem is significant: tasks that are too large exhaust an agent's context window and produce incomplete or drifted output; tasks that are too small create artificial fragmentation and coordination overhead.

Industry patterns from 2025–2026 converge on several principles:

**Spec-completeness:** Each task must contain everything an agent needs to execute it without asking questions mid-task. This means: which files to modify, what the acceptance criteria are, what quality gates to run. Vague tasks — "improve the login flow" — generate agents that guess wrong and produce revision cycles.

**File-disjoint scoping:** For parallel execution, each task must own a non-overlapping set of files. Two agents writing to the same file produces merge conflicts at best and semantic conflicts at worst. The decomposition is correct only when the task boundaries imply disjoint file sets.

**Additive vs. edit distinction:** Additive tasks (new features, new files, new tests) can run in parallel. Edit tasks (refactoring shared code, migrating global patterns, updating shared config) must be sequenced — one agent finishes and merges before the next begins. Confusing these two categories is the most common parallelism mistake.

**Size calibration:** Practitioners who have run long-horizon agent sessions report that individual tasks of 15–20 minutes of agent execution are optimal. Shorter tasks do not justify the overhead of spinning up an agent context. Longer tasks risk context degradation — the agent "forgets" earlier constraints as the conversation grows. The OpenAI Codex team demonstrated a 25-hour autonomous run, but only by externalizing all state to markdown files that the agent re-read at each milestone; the session itself was structured as a sequence of bounded checkpoints rather than one monolithic task.

---

## S8.2 — Parallel Work Coordination

### Git worktrees as the isolation primitive

Git worktrees are the infrastructure primitive that makes parallel agent execution safe. A worktree is a separate working directory linked to an existing repository: each worktree has its own checked-out branch and its own file system state, but they all share a single `.git` object database — no duplicated history, no wasted disk. Multiple agents can each work in their own worktree simultaneously, on separate branches, with no filesystem conflicts between them.

The pattern has moved from advanced technique to industry default between 2025 and 2026. Cursor 2.0 (October 2025) shipped parallel agent support built on worktrees. VS Code 1.107 (July 2025) added automatic worktree isolation for Copilot background agents. Claude Code's documentation describes `isolation: worktree` as a subagent configuration option. Gartner reported a 1,445% surge in multi-agent system adoption from Q1 2024 to Q2 2025 — worktrees are the mechanism that made this scale safe.

The standard setup is: one worktree per agent task, one branch per worktree.

```bash
git worktree add ../agent-api -b feat/api-validation
git worktree add ../agent-ui -b feat/checkout-component
git worktree add ../agent-tests -b fix/test-coverage
```

Each agent works in its own directory. Conflicts, if any, surface cleanly at merge time rather than during execution. When each agent finishes, the orchestrator reviews the branch and merges it. The worktree is then cleaned up.

The MyVocaList workflow enforces a 4-agent wave cap (Rule 2): never spawn more than 4 parallel subagents. This is not a technical limit of worktrees but a human-review constraint — the orchestrating agent (or the human architect) can meaningfully review up to 4 concurrent outputs before the coordination overhead exceeds the productivity gain.

### Merge sequencing

Parallel worktrees eliminate filesystem conflicts during execution but do not eliminate semantic conflicts at merge time. Two agents that implement incompatible assumptions about a shared interface will produce branches that merge cleanly at the file level but break at runtime.

The industry pattern for merge sequencing:
1. Identify dependency order: which branches must be merged before others depend on their output.
2. Merge dependency-first: if Agent A built a shared API client that Agents B, C, and D consume, merge A first.
3. Rebase downstream branches on the updated main before merging them.
4. If semantic conflicts are found after merge, use an orchestrator agent (not assigned to any feature) to inspect both branches and identify where incompatible assumptions were encoded.

### S8.2.1 — Cross-Team Spec Consistency

When multiple agents or teams work on interdependent specs simultaneously, the risk is that they encode contradictory decisions that only surface when implementation artifacts are combined. A spec written in isolation may be internally consistent but conflict with a spec written in a parallel workstream.

No current tooling fully resolves this — it remains a known tension in SDD. The mitigations in active use:

**Shared project memory:** Tools like Colign maintain a "Project Memory" that stores domain rules, constraints, and technical decisions that are automatically injected into every spec change. This gives all working agents a common factual baseline.

**Single-writer rules for hotspot files:** Routes registries, shared configuration, global interfaces — files that every feature touches — must be designated as single-writer at any one time. If multiple specs need to modify the same hotspot, they are sequenced, not parallelized.

**Spec delta checkpoints:** Platforms like Specledger track every spec change with its intent. Checkpoints create alignment points where human review confirms that parallel spec changes are not contradictory before any implementation begins.

**Coordinator-reviewer pattern:** A designated orchestrator agent reviews all agent branches before merge, specifically looking for semantic contradictions. This is distinct from per-branch review; its job is cross-branch consistency, not per-branch correctness.

---

## S8.3 — Progress Visibility

### The visibility problem in agent-driven development

Traditional project management tools were designed for human workers who carry context between sessions and can self-report status. AI agents do not. An agent that marks a task done may have done so without running the required verification. An agent that stops mid-session leaves no natural checkpoint that the next session can resume from. Status dashboards built on human self-reporting fail in agent-driven workflows.

The emerging response is: externalize all state, make state machine-readable, and verify status programmatically rather than trusting agent self-report.

### tasks.md and task-log.md as the progress surface

In the MyVocaList workflow, progress visibility operates at two levels:

**Feature level:** `tasks.md` provides granular progress within a feature. Each checkbox is either checked or not. The definition of "checked" requires build passing and tests passing — it is not self-reported by the agent but verified by the main agent running `dotnet build` and `dotnet test` before the checkbox is considered valid.

**Session/cross-feature level:** `task-log.md` records completed task outcomes across sessions. It is append-only — agents only add rows, never edit history. This provides the human architect with an audit trail: which tasks completed, which failed, and why. It is the recovery surface: if a session is lost, the log shows where work stopped.

### Native task management in AI runtimes

Claude Code introduced native task management in January 2025, shifting from session-scoped `TodoWrite` checklists to a persistent multi-session system using `TaskCreate`, `TaskGet`, `TaskUpdate`, and `TaskList` tools. Tasks persist in `~/.claude/tasks/` across context compactions and session resets. Multiple Claude Code sessions can coordinate on the same task list via the `CLAUDE_CODE_TASK_LIST_ID` environment variable, enabling:
- Frontend and backend sessions sharing blocker status
- Resuming work after closing the laptop without re-explaining context
- An orchestrator agent marking tasks as `in_progress` so specialist agents know what is claimed

The status lifecycle (`pending → in_progress → completed`) matters for visibility: an agent sets `in_progress` before starting work, so the orchestrator can detect stuck agents (tasks that have been `in_progress` for too long without a commit).

### Memory persistence across sessions

Progress visibility depends on memory that survives session boundaries. The SDD practitioner community has converged on two complementary mechanisms:

**Context files (CLAUDE.md, rules files):** Loaded at session start, they provide stable architectural context — decisions made, patterns confirmed, constraints in force. They do not track progress but they prevent agents from re-litigating resolved questions.

**External state files (tasks.md, task-log.md, documentation.md):** Written during the session by the agent, re-read at session start. These carry dynamic state: what has been done, what decisions were made during execution, what is next. An agent that re-reads its own externalized notes after context compaction recovers working state without relying on conversational memory.

The OpenAI Codex 25-hour autonomous session worked specifically because all state was externalized: a `spec.md` with constraints, a `plans.md` with milestones and acceptance criteria, an `implement.md` runbook, and a `documentation.md` audit log. The agent re-read these files at each milestone restart. This prevented drift across the full session without requiring a context window large enough to hold 25 hours of conversation.

### Live dashboards

For teams running multiple concurrent agents, real-time dashboards add a visual layer to the file-based tracking. Spec Kitty provides a kanban board (`spec-kitty dashboard`) showing work packages across lifecycle lanes with agent assignments and completion percentages. Agiflow provides a telemetry viewport for multi-agent synthetic workforces. These tools read the same underlying state files — they are visualization layers, not separate state stores. The authoritative record remains the markdown files in the repository.

The key principle: **dashboards are read surfaces, not write surfaces.** Progress is driven by agents executing against specs and committing changes. Dashboards observe the git history and the state files, not the reverse.

---

## Sources

- [Spec Kitty — open source CLI for spec-driven development with git worktrees and kanban dashboard](https://github.com/priivacy-ai/spec-kitty)
- [How to Run a Multi-Agent Coding Workspace — Augment Code](https://www.augmentcode.com/guides/how-to-run-a-multi-agent-coding-workspace) (2026-03-16)
- [Git Worktree: The Infrastructure That Unlocks Agentic Development — htek.dev](https://htek.dev/articles/git-worktree-unlocks-agentic-development/) (2026-03-19)
- [Multi-Agent AI Development: How to Orchestrate AI Coding Agents — Katyella](https://katyella.com/blog/multi-agent-ai-development-workflows) (2026-03-05)
- [Parallel AI Coding Agents: The Git Worktrees Workflow Guide — Agentic Blog](https://blog.appxlab.io/parallel-ai-coding-agents-git-worktrees/) (2026-04-01)
- [Running Parallel AI Agents on Isolated Git Worktrees — Fazm Blog](https://fazm.ai/blog/parallel-agents-isolated-worktrees-small-prs) (2025-12-01)
- [Run Long Horizon Tasks with Codex — OpenAI Developers](https://developers.openai.com/cookbook/examples/codex/long_horizon_tasks)
- [Claude Code Task Management: Native Multi-Session AI — claudefa.st](https://claudefa.st/blog/guide/development/task-management) (2026-04-29)
- [Colign — Spec-Driven Development Platform with real-time collaboration and task management](https://colign.co/)
- [Specledger — Spec-Driven Development Platform with human dashboards and spec deltas](https://specledger.io/)
- [Agiflow — Project Management for AI Agents with multi-agent coordination](https://agiflow.io/docs/features/spec-development/)
- [SpecWeave — Autonomous spec-driven development with JIRA/GitHub sync](https://spec-weave.com/)
- [Epic: Improving Session Continuity and Coherence in Gemini CLI — GitHub](https://github.com/google-gemini/gemini-cli/issues/21792) (2026-03-10)
