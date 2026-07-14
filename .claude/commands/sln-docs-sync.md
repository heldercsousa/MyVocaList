# Docs Sync Command (`/sln-docs-sync`)

Flush documentation changes stranded on worktree/task branches back to `develop`, keeping develop the single source of truth for all docs (`workflow.md § Rule 2` — Docs land on develop). Code stays on the task branch; only docs move.

**Doc paths in scope:** `Docs/**`, `.claude/**`, `CLAUDE.md`, `MyVocaList.sln` (registration entries for Docs files).

## Steps

1. **Enumerate candidates:** for every unmerged branch (`git branch --no-merged develop`), diff docs paths:
   `git diff develop...<branch> --name-only -- Docs/ .claude/ CLAUDE.md MyVocaList.sln`
2. **Skip if empty.** Report branches with doc deltas.
3. **For each branch with doc deltas** (main working tree, on develop, clean status required):
   - Preferred (keeps history): `git cherry-pick <doc-only commits>` if the branch has commits touching only doc paths.
   - Mixed commits (docs + code together): `git checkout <branch> -- <each doc file>`, review the staged diff (`git diff --cached`), then commit on develop:
     `git commit -m "docs: sync from <branch> — <what>"`.
   - **Conflict on a doc file** (develop's copy also changed): STOP and reconcile manually — never overwrite develop's version blindly; if unsure, report both versions to Helder.
4. **`.sln` registration check:** if any synced file is new under `Docs/`, verify it is registered in `MyVocaList.sln` (constraints-registry HARD GATE).
5. **Push** develop.
6. **Update LEDGER.md** if any synced task-log changes a task's status (`/sln-ledger update`).

## When to run

- Post-wave, before merging code branches into develop.
- Whenever the Stop hook or a review flags doc files changed inside a worktree.
- Before ending any session that dispatched subagents to worktrees.
