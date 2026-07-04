# Risk Analysis: Docs/Management Token Footprint & the "Bypass Folder" Proposal

## Context

Helder noticed `Docs/Management` is 43 MB (Explorer property dialog) and asked whether every file in it is read every session (no — see below), then proposed a structural fix: keep `Docs/Management` empty except for the currently active task's files, with a `ManagementByPass` folder holding everything else, and have each worktree/subagent copy files in before starting a task and back out when done. Helder explicitly asked for research before any agreement, since a change to this shared structure has high blast radius across hooks, `.sln` sync, and session-continuity files.

This plan is the output of that research: two Explore agents mapped (1) every hardcoded reference to `Docs/Management` across CLAUDE.md/rules/agents/hooks/`.sln`, and (2) the actual contents of `ManagementByPass`, the `context-budget` skill, and worktree conventions — plus two web searches for prior art. The conclusion is that the bypass-folder scheme should **not** be adopted, and the actual problem is something much smaller and safer to fix.

## Finding 1 — Files are NOT read every prompt; only on explicit read, and cached briefly after

Confirmed from Claude Code's context model (established in this same conversation): a file's tokens are spent only when it is actually `Read()` (or matched by a non-excluded `Glob`), and repeat reads within ~5 minutes hit prompt cache at a fraction of the cost. `.claudeignore` only prevents *automatic discovery* (glob scans) — it never blocks an explicit-path `Read()`, and per `workflow.md` Rule 7 / `orchestrator.md` / `implementor.md`, sessions are already instructed to read only the active feature's folder by explicit path and never `Glob("Docs/**")`. So the "43 MB read every session" fear is not how the mechanism works today — the actual risk is only careless `Glob`/broad search calls.

## Finding 2 — The 43 MB is 86% one leftover debug-log file, not spec bloat

Actual breakdown (298 files, 44 MB total):
- `BusinessFeatures/` — **2.5 MB** across ~14 feature folders (small `.md` files: design/plan/requirements/tasks/task-log)
- `DevCycleCraft/` — **42 MB**, but **38 MB of that is one file**: `DevCycleCraft/page-load-frozen/2026-12-06-1855 - Release test S23 Ultra.txt.txt` (a raw device debug-log capture). A few more debug logs in the same folder (~450 KB combined) plus normal small `.md` docs.
- Loose root files (`BACKLOG.md`, `EMULATOR_TEST_MASTER_LIST.md`, several agent-generated scratch files like `cheerful-conjuring-ullman.md`) — **~192 KB**.

Strip out that one 38 MB log file and the entire real spec corpus is **~3.7 MB across 298 small files** — not a meaningful token risk under the explicit-path-only reading discipline already in place. `.claudeignore` currently excludes specific binary extensions (`.pdf`, `.png`, etc.) but has no rule for large `.txt`/debug-log captures, so this file (and its siblings) would be pulled in by any careless glob or explicit read.

## Finding 3 — No prior art or tooling exists for the bypass-folder pattern

Two web searches (Claude Code context-management best practices; MCP/plugin ecosystem) turned up no "stage files in/out of context per task" pattern anywhere — the universally documented solution is `.claudeignore` (gitignore-syntax auto-discovery exclusion), which explicitly preserves explicit-path reads. No MCP server or plugin does automated file staging for this purpose; it would be a fully custom, unprecedented mechanism.

The only installed skill with "context-budget" in the name (`teslasoft-skills` plugin) is unrelated — it manages the **conversation window's** token tiering (summarization, checkpointing), not repository file placement. It offers nothing applicable here.

## Finding 4 — The bypass-folder scheme has real, mapped blast radius

`Docs/ManagementByPass` currently exists but is **empty, untracked, and not wired into `.gitignore` or anything else** — a blank slate, not a working pattern to extend. Meanwhile `Docs/Management` is deeply hardcoded as a fixed structural root:
- **`MyVocaList.sln`**: 283 literal `Docs\Management\...` path entries; a dedicated `PostToolUse` hook runs `sync-docs-to-sln.ps1` on every `Write`, using a hardcoded folder→GUID map. Moving files out/in per task would fight this hook every time (and its errors are silenced, so breakage would be invisible).
- **`BACKLOG.md`**: sits at the literal fixed path `Docs/Management/BACKLOG.md`, referenced in `workflow.md` Rule 7 (session-start fallback anchor), `orchestrator.md`, and — most critically — in the `UserPromptSubmit` hook, which re-injects this exact path into **every single user turn**. It also has its own dedicated `.sln` root-folder entry, separate from any per-feature folder.
- **`handoff.md`**: the session-continuity artifact lives at `.../[feature]/handoff.md`, read at Rule 7 step 1 and written before every session end (`orchestrator.md`). Physically relocating a feature's folder mid-task risks losing or orphaning this file exactly when it's most needed (session interruption).
- **Parallel worktrees already solve the actual isolation need.** Per `orchestrator.md § Git Worktrees as Isolation Primitive`, every parallel wave already gets its own `.worktrees/<name>` checkout — each subagent already works from its own full copy of the repo (including `Docs/Management`) and commits independently. A bypass/copy-in-copy-out scheme would duplicate a problem worktrees already solve, while adding a second, uncoordinated moving-parts layer (worktree isolation + manual file staging both trying to manage the same concern).
- A move-in/move-out cycle done by different subagents (across parallel worktrees) creates exactly the shared-mutable-file race that `workflow.md`'s "single-writer rule for hotspot files" was written to prevent — `BACKLOG.md`, `tasks.md`, and `.sln` are already flagged as sequential-only for this reason.

## Recommendation — do not adopt the bypass-folder scheme; fix the actual size driver instead

1. **Reject `ManagementByPass`.** No prior art, no tooling support, doesn't address the real 86%-one-file cause, and its cost (breaking `.sln` sync, BACKLOG.md's fixed-path assumptions, handoff.md continuity, worktree redundancy) outweighs a token-savings benefit that already doesn't materialize under current explicit-read discipline. Delete the empty `Docs/ManagementByPass` folder (nothing depends on it).
2. **Move (not delete — Helder should confirm disposition) the 38 MB debug-log file** out of `Docs/Management/DevCycleCraft/page-load-frozen/` — e.g. to `Docs/Changelog/Archive/` or a new git-ignored local `Docs/_logs/` capture folder — since raw device logcat captures are diagnostic artifacts, not specs, and don't belong in the versioned spec tree at all.
3. **Add a `.claudeignore` rule** excluding large raw log/debug captures from glob discovery going forward, e.g. a pattern targeting `*debug log*.txt` / `*logcat*.txt` / `*Release test*.txt` under `Docs/Management/**`, so a future accidental drop of a similar file doesn't silently re-inflate the tree or get swept into a careless glob.
4. **Archive the loose auto-generated scratch files** at `Docs/Management/` root (`cheerful-conjuring-ullman.md`, `pure-petting-noodle.md`, `jiggly-cooking-cocoa.md`, `lively-cooking-torvalds.md`, `pure-skipping-firefly.md`, `replicated-napping-globe.md`, `cls-mellow-lighthouse.md`, `in-docs-management-backlog-md-a-calm-dolphin.md`) — these are leftover plan-mode files from prior sessions (this very session will add one more). Recommend a light housekeeping pass: read each briefly, confirm superseded/stale, then move to `Docs/Changelog/Archive/` or delete if truly disposable. This is separate from the main decision and low-risk either way.
5. **Leave `Docs/Changelog/changelog.md` in place** — it is actively wired into `/project:commit` (steps 4 and 9, mandatory per commit) and is not redundant with per-feature `task-log.md`/`BACKLOG.md`: those are per-feature state; changelog.md is the repo-wide chronological narrative. Confirmed already being archived by Helder (`Docs/Changelog/Archive/changelog-jan2026-to-jun2026.md` exists). Two small, separate, low-priority follow-ups — neither blocks this decision:
   - The archive file (named `jan2026-to-jun2026`) currently still contains the same July 2026 entries that are also in the live `changelog.md` — the split wasn't clean, so there's duplicate content between live and archive right now. Worth trimming the archive (or the live file) to remove the overlap next time changelog is touched.
   - The 07/03/2026 entries in the live file are unusually long (near task-log-level detail) — worth tightening `.claude/commands/changelog.md`'s "keep concise, one sentence" rule adherence going forward.

## Verification

- After moving the debug log and adding the `.claudeignore` rule: re-run `du -sh Docs/Management` (or Explorer properties) and confirm the tree drops to ~4-5 MB.
- Confirm `git status` shows the moved file as a rename/move (not delete+new) if git mv is used, preserving history.
- Confirm no `.sln` entries reference the moved file's old path after the move (update `.sln` per `constraints-registry.md` HARD GATE in the same commit).
- No changes to `workflow.md`, `orchestrator.md`, hooks, or `.sln` GUID structure are needed under this recommendation — the existing session-start/explicit-path-read discipline already does the job once the one oversized file is out of the tree.
