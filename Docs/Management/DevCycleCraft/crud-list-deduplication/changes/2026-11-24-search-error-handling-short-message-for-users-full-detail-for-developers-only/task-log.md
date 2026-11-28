---
## Task: Search error handling: short message for users, full detail for developers only (B5)
**Plan:** Docs/Management/DevCycleCraft/crud-list-deduplication/changes/2026-11-24-search-error-handling-short-message-for-users-full-detail-for-developers-only/README.md
**Status:** To Review
**Started:** 2026-11-25
**Completed:** 2026-11-25

### Approach (written into the README `## Approach` before coding)
Log-only for the CRUD list (no user-message mechanism exists in `CrudListViewModelBase`); the autocomplete already logs and shows no options. Raising a short user message is a Helder decision (see Open questions).

### Per-site status
| Site | Status |
|---|---|
| CRUD debounced search (`CrudListViewModelBase.TriggerSearchDebounce`, was `:329-337`) | Fixed: non-cancellation exceptions logged with `LogError(ex, ...)`; rows kept; shimmer ends via the existing `finally` |
| Sf autocomplete (`AutocompleteFilterSf`) | Already correct (logs via `LogSearchFailure`, shows no options). No change |
| DX original (`AutocompleteDXField`, `:385, :434`) | Not edited (DX-only original, removed by the migration) |
| `CrudListPageBase.cs:77` | Not changed: refresh and initial-load paths keep their existing propagation (out of the debounce scope) |

### Open questions (Helder)
- A short user message for a failed CRUD list search: no mechanism exists. Options: (A) log only (done); (B) add a page-level message through `SfBottomSheet`/status text (new component decision).
- `InitializeAsync` / `RefreshAsync` failures still propagate as before (`CrudListPageBase.cs:77`): confirm this is the wanted scope.

### TDD evidence
- Red: `CrudListStaleLoadTests.Search_FetchThrows_LogsTheFailureAndKeepsTheCurrentRows` FAILED before the fix ("Condition not reached before the timeout": no error logged).
- Green: same test passes; 56 passed across `CrudListStaleLoadTests|CrudListViewModelBaseTests|LoadingHold`.

### Changed files:
- `MyVocaList/UI/ViewModels/CrudListViewModelBase.cs` (catch in the debounce task)
- `Tests/Unit/ViewModels/CrudListStaleLoadTests.cs` (new test; fixture takes an optional logger; `using Microsoft.Extensions.Logging`)
- `Docs/Management/DevCycleCraft/crud-list-deduplication/changes/2026-11-24-search-error-handling-short-message-for-users-full-detail-for-developers-only/README.md` (approach section)
- `<this folder>/task-log.md` (new)
- `MyVocaList.sln` (registration)

### Build notes
Build: Android build run before commit | Tests: 56 passed (filtered) | Commit SHA: see git log
Files written and re-read: `CrudListViewModelBase.cs` L355-379; test file new test block.
