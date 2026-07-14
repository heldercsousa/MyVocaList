# Ledger Command (`/sln-ledger`)

Maintain `Docs/Management/LEDGER.md` — the develop-branch tracker of WHERE every in-flight task/bug lives (branch, worktree, phase, status). See the ledger's own header for row lifecycle and status vocabulary.

**Invocation:** `/sln-ledger [update | resume | audit]` (default: `update`).

## When this runs (mandatory touchpoints — also without explicit invocation)

The main agent updates the ledger at every one of these moments, on `develop`:
1. **Dispatch** — a subagent is sent to a worktree/branch: add/refresh the row (ID, branch, worktree, phase, status `dispatched`).
2. **Phase transition** — status change reported by a subagent or hook (build/test failure, to review, blocked): update `Status`, `Last commit`, `Next action`.
3. **Merge** — branch merged into develop: status `merged`, move row to Completed.
4. **Session end** — before stopping, every Active row must reflect reality (`Next action` filled in).

## `update` (default)

1. `git branch -a --no-merged develop` + `git worktree list` — enumerate live task branches/worktrees.
2. For each Active row: verify the branch still exists; refresh `Last commit` (`git log -1 --format=%h <branch>`); reconcile `Status` with the task-log entry for that task.
3. For each live task branch with NO row: add one (this is drift — note it).
4. Commit the ledger on develop: `git add Docs/Management/LEDGER.md && git commit -m "docs: ledger update — <what changed>"` (push with the session's normal flow).

> The ledger is docs — it is ALWAYS edited and committed on develop, never inside a worktree (workflow.md Rule 2, Docs land on develop).

## `resume`

For resuming interrupted work: read the Active table, and for the chosen row output: branch, worktree path (recreate if removed: `git worktree add .worktrees/<name> <branch>`), status, last commit, and `Next action`. Cross-check the feature's `task-log.md` and lease state (`workflow.md § Rule 4` reclaim protocol) before continuing.

## `audit`

Full reconciliation: every unmerged branch ↔ ledger row ↔ `tasks.md` `[~]` markers ↔ BACKLOG status. Report orphan branches (no row), stale rows (branch gone), and status mismatches. Do not auto-delete branches — report to Helder.
