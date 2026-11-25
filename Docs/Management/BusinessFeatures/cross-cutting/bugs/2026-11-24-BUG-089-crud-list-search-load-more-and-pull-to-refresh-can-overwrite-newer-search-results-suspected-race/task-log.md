---
## Task: BUG-089 CRUD list search: load-more and pull-to-refresh can overwrite newer search results
**Plan:** Docs/Management/BusinessFeatures/cross-cutting/bugs/2026-11-24-BUG-089-crud-list-search-load-more-and-pull-to-refresh-can-overwrite-newer-search-results-suspected-race/README.md
**Status:** To Review
**Started:** 2026-11-25
**Completed:** 2026-11-25

### Reproduction
B6 and B7 reproduced at unit level (not on device). B8 is code-reading only: the stale check now also runs inside the UI-thread block; no separate test (low severity, probably hidden under the shimmer).

### Red (before fix)
`Tests/Unit/ViewModels/CrudListStaleLoadTests.cs`, run with the fix reverted:
- `LoadMore_StartedBeforeNewerSearch_DoesNotAppendItsPageToTheNewerRows`: FAILED, `Assert.DoesNotContain() Failure: Item found in collection` (old page appended).
- `Refresh_StartedBeforeNewerSearch_DoesNotOverwriteTheNewerRows`: FAILED, `Assert.DoesNotContain() Failure: Item found in collection` (stale refresh rows applied).

### Green (fix)
`CrudListViewModelBase`: `_queryGeneration` bumped by every first-page load (search, reload, refresh). Load-more captures generation and query before its fetch and drops the page if the generation moved; first-page apply and load-more apply re-check the generation inside `RunOnUiThread` (B8). Shimmer generation logic unchanged.

### Changed files:
- `MyVocaList/UI/ViewModels/CrudListViewModelBase.cs`
- `Tests/Unit/ViewModels/CrudListStaleLoadTests.cs` (new, 2 regression tests)
- `Docs/Management/BusinessFeatures/cross-cutting/bugs/2026-11-24-BUG-089-.../task-log.md` (new)
- `MyVocaList.sln` (registration of the new task-log)

### Verification evidence
- Filtered run `CrudListStaleLoadTests|CrudListViewModelBaseTests|LoadingHold`: 55 passed, 0 failed (after fix).
- Android build of MyVocaList: exit 0.

### Build notes
Build: passed (0 errors) | Tests: 55 passed (filtered) | Commit SHA: see git log
Files written and re-read: `CrudListViewModelBase.cs` (LoadFirstPageAsync head, apply block, LoadMore block), `CrudListStaleLoadTests.cs`.
