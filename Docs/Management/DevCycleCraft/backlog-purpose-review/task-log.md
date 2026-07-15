# Task Log — backlog-purpose-review


## Moved from BACKLOG.md (2026-07-15) — BACKLOG.md purpose review — restore it as a PO-level business artifact

> Verbatim row moved during the BACKLOG.md PO-level restructure (`Docs/Management/DevCycleCraft/backlog-purpose-review/`). Original table row preserved below.

| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-07-14 | **BACKLOG.md purpose review — restore it as a PO-level business artifact** | 💡 Pending | Registered by Helder 2026-07-14. BACKLOG.md was created to reproduce a SCRUM Product Owner's backlog: business-level tracking of the app's software cycle — what each item's final goal is and its high-level status. It has drifted: rows routinely accumulate deep technical detail (root causes, commit hashes, file paths, AC numbers, per-step status trails) that a PO would never read, making the file large and token-expensive to load when only sequencing/status is needed. In SCRUM, technical information lives in nested tasks/sub-items owned by the technical team; the backlog entry states the goal. **Review scope:** (1) define what belongs in a row (goal, business value, status, owner gate, pointer to the feature folder) vs. what must move out (technical narrative → the feature's `design.md`/`task-log.md`/`findings.md`; branch/step tracking → `LEDGER.md` + task-log Checkpoint); (2) slim existing rows accordingly, preserving history by moving (not deleting) detail into the dedicated docs; (3) propose a row template + length guideline and register it as a rule so agents stop re-fattening the file; (4) measure token cost before/after. Interacts with: *Spec Evolution, Versioning & Feature-Folder Organization* (status vocabulary/nesting) and *Richer task-status vocabulary* — align, don't duplicate. Propose the template to Helder before any mass edit. |

---
## Task: BACKLOG.md purpose review — restructure as PO-level artifact
**Plan:** `Docs/Management/DevCycleCraft/backlog-purpose-review/design.md` (approved design = plan; docs-only task)
**Status:** To Review
**Started:** 2026-07-15
**Completed:** 2026-07-15

> Helder PRE-APPROVED spec, plan, and execution 2026-07-15 — executed end-to-end without intermediate approval gates, committed directly to `develop` (docs-only; worktree rule covers code files only).

### Changed files:
- `Docs/Management/BACKLOG.md` (rewritten: header rule block + slim active rows only)
- `Docs/Management/backlog-archive/BACKLOG-ARCHIVE-2026-03.md` (new)
- `Docs/Management/backlog-archive/BACKLOG-ARCHIVE-2026-04.md` (new)
- `Docs/Management/backlog-archive/BACKLOG-ARCHIVE-2026-05.md` (new)
- `Docs/Management/backlog-archive/BACKLOG-ARCHIVE-2026-06.md` (new)
- `Docs/Management/backlog-archive/BACKLOG-ARCHIVE-2026-07.md` (new)
- `Docs/Management/cross-cutting-log.md` (new — shared narrative home for folder-less items)
- `Docs/Management/DevCycleCraft/backlog-purpose-review/design.md` (new, approved 2026-07-15)
- `Docs/Management/DevCycleCraft/backlog-purpose-review/findings.md` (new — before/after measurement)
- `Docs/Management/DevCycleCraft/backlog-purpose-review/task-log.md` (new — this file)
- Relocation appends (verbatim `## Moved from BACKLOG.md (2026-07-15)` blocks):
  - `BusinessFeatures/artists-songs/task-log.md`, `.../changes/2026-07-10-form-ux-redesign/task-log.md`, `.../form-validation-task-log.md`
  - `BusinessFeatures/search-picker/task-log.md`, `BusinessFeatures/youtube-share/findings.md`
  - `BusinessFeatures/queue-management/task-log.md`, `BusinessFeatures/app-settings/task-log.md`, `BusinessFeatures/backup-restore/task-log.md`
  - `BusinessFeatures/user-suggestions/task-log.md` (new), `BusinessFeatures/app-update-check/task-log.md` (new)
  - `BusinessFeatures/venues/form-validation-task-log.md`, `BusinessFeatures/persons/form-validation-task-log.md`
  - `DevCycleCraft/hamburger-nav-pattern/task-log.md`, `DevCycleCraft/crud-form-action-pattern/task-log.md`, `DevCycleCraft/autocomplete-component/task-log.md`
  - `DevCycleCraft/page-load-frozen/task-log.md`, `DevCycleCraft/session-continuity-leasing/task-log.md`, `DevCycleCraft/backlog-first-registration/task-log.md`
  - `DevCycleCraft/per-agent-context-isolation/task-log.md`, `DevCycleCraft/rules-file-refactoring/task-log.md`, `DevCycleCraft/ui-form-validation-guide/task-log.md`
  - `DevCycleCraft/crud-list-deduplication/task-log.md` (new), `DevCycleCraft/UI-2nd-refactor/task-log.md` (new), `DevCycleCraft/spec-evolution-versioning/findings.md`
- `MyVocaList.sln` (registration of all new Docs files)
- Separate `amend:` commit: `.claude/rules/workflow.md` (Rule 1 pointer) + `Docs/Changelog/changelog.md`

### Verification evidence
- Before: 250 lines / 17,019 words; ~56k tokens (exceeded 25k single-Read cap — required 8 paged reads).
- After: 123 lines / 2,840 words; **single full Read succeeded with no truncation** (primary success check).
- Nothing deleted: all displaced narratives moved verbatim (full original table rows) under dated headings; script-driven line extraction guaranteed verbatim fidelity.

### Judgment calls (for Helder review)
1. `cross-cutting-log.md` named per the task briefing (design.md said `cross-cutting-bugs-log.md`); it also hosts folder-less non-bug items, so the broader name fits.
2. BUG-011 (✅ Fixed but fix branch `fix/bug-011-queue-bottomsheet` NOT merged, E2E blocked): archived to 2026-06 per its ✅ status, with the unmerged-branch + blocked-E2E caveat in the outcome sentence.
3. BUG-019 ("⚠️ Partially regressed"): archived to 2026-06 as closed — name-visibility fix holds; the regression is separately tracked by active BUG-028.
4. BUG-008 ("🔵 Superseded") and the parent "Search AppBar Pattern" row ("🔵 Deferred/Superseded 2026-07-10"): treated as superseded-closed → archived to 2026-07; the superseding *AppBar / SearchAppBar Interaction Redesign* row was promoted to a top-level active row.
5. Duplicate BUG-017 rows (two rows for the same bug) consolidated into one archive row.
6. BUG-043 status "⏳ Phase 3 — Manual E2E pending" normalized to `🟡 In Progress` (Helder E2E gate in Notes) — ⏳ is not in the status vocabulary.
7. "✅ Done (CRUD-only)" hamburger row archived as ✅ Done with the scope narrowing in the outcome sentence.
8. Form-validation sub-tasks 02–06 archived to 2026-07 (E2E/merge completion dates), guide 01 to 2026-06 (done 2026-06-30).
9. "MCP governance sync" archived as ✅ per its status even though residual Docs-housekeeping items remain — they are preserved in its relocated narrative in `cross-cutting-log.md`.

### Checkpoint
Replaced by this final entry — task complete pending Helder review.
