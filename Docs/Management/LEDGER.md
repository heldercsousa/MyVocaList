# Work Ledger — branch/status tracker (lives on develop)

> **Purpose:** single source of truth, always on `develop`, for WHERE every in-flight task/bug lives (branch, worktree) and its current phase/status. This is what makes resuming development, debugging, and merges into develop possible after any interruption. Maintained via `/sln-ledger` and at every phase transition (dispatch, phase change, merge, close). Rules: `workflow.md § Rule 2` (docs land on develop).
>
> **Row lifecycle:** add the row when work is dispatched; update `Phase/Step`, `Status`, `Last commit` at each transition; on merge set Status `merged` and move the row to Completed. `python .claude/scripts/ledger_rotate.py` then moves a Completed row to `LEDGER-archive.md` (verbatim) once its branch is deleted or 7 days after the merge; never move or edit those rows by hand. Pre-commit blocks above 15 Completed rows when rows can be rotated.
>
> **Row-length rule `[HARD RULE]`:** a row is ONE short status line — what, where it stands, next action, pointer to the item folder — **max 450 characters** (enforced by `.claude/scripts/ledger-row-guard.py`). Detail (history, rulings, findings) goes in the item's `task-log.md` or `README.md`, never in the ledger.

## Active

| ID | Feature / Bug | Phase / Step | Branch | Worktree | Status | Last commit | Next action |
|----|---------------|--------------|--------|----------|--------|-------------|-------------|
| ITF | Inline Trivial Fix lane — bounded orchestrator inline-edit exception | merged; 38/38 green; one gate left | develop | — | merged | a4c198a | Helder observes the FIRST live ITF fix end-to-end; decide on 4 misbased `agent-*` worktrees. Detail: `DevCycleCraft/inline-trivial-fix/handoff.md` + task-log § Ledger detail |
| SC-ENH | Session Continuity enhancements (Phase 8, Tasks 11–15) | complete, verifier PASS | develop (tooling + docs) | — | to review | 68e1852 | Helder runs the two-terminal live demo; optionally directs the 4 leftover agent worktrees. Detail: `DevCycleCraft/session-continuity-leasing/task-log.md` § Ledger detail |

## Completed (recent)

Older closed rows: `LEDGER-archive.md` (moved by `ledger_rotate.py`). SFT merge detail: the SFT `task-log.md`; verbose pre-2026-11-10 text of these rows is in `LEDGER-archive.md`.

| ID | Feature / Bug | Branch | Merged into develop | Commit |
|----|---------------|--------|---------------------|--------|

---

**Status vocabulary:** `dispatched` · `in progress` · `build failure` · `test failure` · `to review` · `blocked: <reason>` · `ready to merge` · `merged` · `abandoned: <reason>`

**Column notes:**
- **Branch** — the branch holding the changes (empty = work happened directly on develop, docs-only).
- **Worktree** — `.claude/worktrees/<name>` path, or `—` if the worktree was already removed (branch still holds the commits).
- **Last commit** — short hash on the task branch, so a resuming session can `git log <hash>..<branch>` instantly.
- **Next action** — one line: what the next session should do (e.g. "fix failing test X", "await Helder review", "merge after wave 2").

**Resume chain (no globbing):** this ledger row → the feature's `task-log.md` `### Checkpoint` block (live step state, pinged on a ~10-min heartbeat) → read ONLY its Context manifest files. See `session-ops.md § Checkpoint Ping & Context Manifest`.
