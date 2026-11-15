# Task log — Token-usage reduction

Plan: `plan.md` (approved by Helder 2026-11-15; workstreams A, B, C now; D and E after a brainstorm).

### Checkpoint

- **Updated:** 2026-11-15
- **Branch / worktree:** develop / main tree (hooks and docs only)
- **Step:** Step 1 done; Step 2 (worktree location) next
- **Context manifest:** `plan.md` · `.claude/scripts/read_budget.py` · `.claude/scripts/read-slice-guard.py` · `.claude/settings.json`

## Step 1 — A2 + A3: slice cap and size budget

**Status:** Done (2026-11-15)

### Changed files

- `.claude/scripts/read_budget.py` (new): budgets and `decide(path, offset, limit)`
- `.claude/scripts/read-slice-guard.py`: now a thin wrapper over `read_budget.decide`
- `.claude/scripts/tests/test_read_budget.py` (new): 12 tests

### What changed

- Full read allowed only within 200 lines **and** 12 KB.
- Over budget: `limit` is required and capped at 250 lines; `offset` alone no longer passes.
- A slice is also capped at 20 KB, measured on the actual lines requested. The plan said "about 12 KB"; 20 KB was chosen so a 250-line slice of ordinary code (about 50 to 80 bytes per line) is not refused.
- `BACKLOG.md` denial points to `backlog_gen.py query`.
- New: files with a NUL byte in the first 8 KB (images) and `.pdf` / `.ipynb` pass. Before, an image over 200 "lines" could be denied by accident.

### Verification evidence

- `python -m unittest test_read_budget`: 12 tests, OK.
- Live: `Read Docs/Management/BACKLOG.md` with no limit was denied with the query hint (144 lines, 35.2 KB).
- Piped payloads: `BACKLOG.md` with `offset 1, limit 30` allowed; garbage stdin allowed (fail-open).
