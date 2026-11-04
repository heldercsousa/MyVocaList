# Development Workflow — Routing Table

> Never-miss rules only. Procedure detail per rule: `library/workflow-rule-N.md` (index `workflow-reference.md`) · hooks + self-enforced rules: `workflow-hooks-invariant.md` · orchestrator/implementor protocols: `.claude/agents/` · spec anatomy: `spec-writing-guide.md` · session artifacts: `session-ops.md`.

## Hook Enforcement (never-miss)
Hooks (`Stop`, `PostCompact`, `PostToolUse` on Services, `SessionStart`) enforce parts of these rules; the rest are self-enforced (list: `workflow-hooks-invariant.md`). At session start, confirm `.claude/settings.json` is valid and its hooks are healthy before dispatching anything.

## SDD Invariant (never-miss)
**Spec changes before code changes.** Not yet shipped → update the spec in place; shipped → never rewrite it, add `changes/YYYY-MM-DD-<slug>/` and `backlog_gen.py register` it. Code contradicts spec → the code is wrong. Spec incomplete → stop and ask Helder. Applies to every agent.

## Rule 1 — Spec-First
**Read the feature's `design.md` before writing implementation code.** Spec gap → stop, clarify with Helder, update the spec, then code.
- **Ceremony:** ≥ 2 layers or > 2 h → full (requirements + design + tasks); 1 file, < 1 h → light; typo / cosmetic / bug fix → commit message only. When in doubt, write a spec. Check the feature against CLAUDE.md's Constitutional Constraints first.
- **Status lives in item frontmatter**; BACKLOG.md is generated. Change it with `backlog_gen.py status <ID> "<status>"` (💡→📋→🗺️→🟢→🟡→✅); register untracked work with `backlog_gen.py register` *before* doing it. Row text is validated (Goal + Gate + one Pointer); detail goes in the README body.

Decision tables, new-feature steps, triage, discovery mode, brownfield rule: `workflow-rule-1.md`.

### Spec quality four-gate review
Before implementation: **Correctness · Completeness · Consistency · Testability** (`workflow-rule-1.md § Spec quality gate`).

### Spike validation task pattern
Spike = time-boxed exploration producing `findings.md`. Its code is throwaway, the time-box is hard, and **failure escalates to Helder — never pick an alternative alone** (`workflow-rule-1.md § Spike validation task pattern`).

## Rule 2 — Subagent Delegation
**Feature coding is done by subagents.** The main agent does shell steps (build, test, `dotnet ef`, git), docs, and the two orchestrator lanes below.

> **Orchestrator read-scope `[HARD RULE — amended 2026-11-03]`:** no source exploration by the orchestrator (plan mode included) — delegate to Explore/Plan. **Read lane:** grep + read **at most 2 source files** per written question, to *confirm* a diagnosis/signature/member. Bounds: `orchestrator.md § Orchestrator Read-Scope`.

### Inline Trivial Fix (ITF) lane `[amended 2026-11-03]` (never-miss summary)
Orchestrator only. **A fix that qualifies goes through ITF — dispatching for it is the wrong lane, not the safe one.** All of C0–C8 or dispatch: 1 file / ≤ 5 lines, fully diagnosed (read lane allowed), not `.xaml`, not a governed component, not hard sequential-only (`MauiProgram.cs` / `GlobalUsings.cs` / `Directory.Build.props` qualify after the sole-writer check), severity ≤ Major with no mandatory regression test, worktree on a task/spike branch, green build + tests. Run it with **`/sln-itf`** (marker + `ITF:` task-log line; Guard 3 enforces C1/C3/C4/C5). Trailer: `Lane: ITF (N files, N lines)`. Detail: `workflow-rule-2.md § Inline Trivial Fix (ITF) lane`.

- **Wave cap `[HARD RULE]`:** max **4** parallel subagents; wait for the whole wave; never reuse a finished subagent.
- **Worktrees `[HARD RULE]`:** every `.cs`/`.xaml` edit happens in a worktree on a task branch, never on `develop`/`main` (hook-enforced). **Base is `develop`** — after any `EnterWorktree`/`git worktree add`, verify `git merge-base --is-ancestor develop HEAD`.
- **Docs land on develop `[HARD RULE]`:** specs, task-logs, `tasks.md`, LEDGER, changelog — never stranded on a task branch (`/sln-docs-sync`).
- **Generated artifacts `[HARD RULE]`:** BACKLOG.md, `backlog-archive/`, README frontmatter are single-writer — a concurrent edit is silently overwritten (`workflow-rule-2.md § Generated artifacts`).
- **One writer per file;** a briefing over 5 files or 2 hours must be split.
- **Subagent exit checklist:** verification → build (0 errors, 3 tries) → test → re-read → `.sln` registration → living-spec check → task-log → commit → push.

### Sequential-only file registry
Never concurrent writers: `MauiProgram.cs`, `AppShell.xaml(.cs)`, `AppDbContext.cs`, `Migrations/*`, `GlobalUsings.cs`, `Directory.Build.props`, spec `tasks.md`. Rationale and the ITF soft/hard split: `workflow-rule-2.md § Sequential-only file registry`.

## Rule 3 — Commit After Every Task
**`/sln-commit` after every completed task.** Complete = 0 build errors + tests pass + `tasks.md` box checked. Before committing: demo verifiable · new types registered in `MauiProgram.cs` · AC evidence in the task-log · **`.sln` registration of every created/moved/deleted `Docs/` file — BLOCKING**. Session end: reconcile touched specs (in-place note if unshipped, `changes/` folder if shipped), update frontmatter + `backlog_gen.py regen`. Review: automatic under `subagent-driven-development`, otherwise run `/sln-review`. Detail: `workflow-rule-3.md`.

### Bug Fix Pattern — commit message as spec
```
fix: [component] — [symptom]

Root cause: [one sentence]
Fix: [one sentence]
Regression risk: [None | Low | Medium — reason]
Lane: ITF (N files, N lines)     ← ITF-lane fixes only
```
A bug that reveals a missing AC adds it to `requirements.md` in the same commit. Tracking + regression tests: `bug-tracking.md`.

## Rule 4 — Tasks.md Is the Source of Truth
Markers `[ ]` available · `[~]` claimed (never dispatch it) · `[x]` committed · `[CANCELLED: reason]`. Never start a task whose dependency is incomplete; `[P]` tasks may run as a wave.
- **DRY Onion `[HARD RULE]`:** Domain → Infra → Services → UI; Wave N+1 waits for the Wave N types it consumes.
- **`[~]` is a lease:** classify with `python .claude/scripts/lease/reclaim.py <my_session_id> <owner_session_id>` — `fresh` → leave it · `reclaimed` → resume with `resume.py` · `lost` → next `[ ]`. Never reset a `fresh` claim.

Task format and phase template: `workflow-rule-4.md`.

## Rule 5 — Task Status Registration
Outcomes go in the feature's `task-log.md`. A `To Review` entry without `### Changed files` is **invalid**; user-facing behavior needs the AC traceability matrix. **Checkpoint Ping `[HARD RULE]`:** multi-step work keeps a live `### Checkpoint` block — updated before each step, after each build/test, at phase changes, and at least every ~10 minutes — with a Context manifest (≤ 8 files) for resuming (`session-ops.md § Checkpoint Ping & Context Manifest`). Template: `workflow-rule-5.md`.

## Rule 6 — Research Tool Gate (never-miss)
Library/framework/API docs → Context7 first; `WebSearch`/`WebFetch` only for what Context7 does not cover. Main agent and subagents alike.

## Rule 7 — Session Start Protocol
Before code or dispatch, in order: hook health → `handoff.md` if present, else `python .claude/scripts/backlog/backlog_gen.py query --status "🟡,🟢"` (**not** BACKLOG.md) → `ACTIVE-CONSIDERATIONS.md` → the feature's `tasks.md`, `requirements.md`, `design.md`, `task-log.md` → lease refresh (`reclaim.py`, `resume.py <session_id>`). **Never glob `Docs/`.** Resuming interrupted work: LEDGER row → task-log `### Checkpoint` → only its manifest files. Detail: `workflow-rules-6-7-8.md § Rule 7`.

## Rule 8 — Pre-Task Collision Check
Before a wave: `git log --oneline -10`, `git status`, stray `[~]` in `tasks.md`. Classify every unowned `[~]` with `reclaim.py` before assuming it is abandoned — `fresh` means another live session owns it. Detail: `workflow-rules-6-7-8.md § Rule 8`.

> Authorship + approval provenance: `.claude/library/authorship-log.md`.
