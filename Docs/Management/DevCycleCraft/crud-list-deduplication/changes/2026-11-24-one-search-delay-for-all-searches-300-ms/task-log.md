---
## Task: One search delay for all searches (300 ms)
**Plan:** Docs/Management/DevCycleCraft/crud-list-deduplication/changes/2026-11-24-one-search-delay-for-all-searches-300-ms/README.md
**Status:** To Review
**Started:** 2026-11-25
**Completed:** 2026-11-25

### Spec update (first)
README `## Decision` gained a `> **Spec updated 2026-11-25:**` note: every search waits `SearchConstants.AutocompleteDebounceMilliseconds` (300 ms).

### Consumer map (governed-adjacent: shared constant and CRUD list base)
- `SearchConstants.AutocompleteDebounceMilliseconds` consumers: `AutocompleteFieldSf.xaml.cs:496` (Sf field, unchanged), `AutocompleteDXField.xaml.cs:403` (DX original, untouched), `CrudListViewModelBase.cs` (changed: literal 400 replaced).
- `CrudListViewModelBase` is the base of every CRUD list VM (Songs, Artists, Persons, Venues); the change touches only the debounce delay.

### Per-consumer risk
| Consumer | What could break | Verification |
|---|---|---|
| Songs / Artists / Persons / Venues list VMs | Shimmer may flicker on each keystroke at 300 ms | Unit: `CrudListViewModelBaseTests`, `LoadingHold*` (57 passed incl. SearchConstants); device check pending (see E2E) |
| AutocompleteFieldSf | None (constant value unchanged at 300) | `AutocompleteFieldSfCharacterizationTests` (item 1) |
| AutocompleteDXField | None (not edited) | n/a |

### Test notes
- The CRUD list tests do not wait a literal 400 ms: they poll with 5 s deadlines (`CrudListViewModelBaseTests.cs`, `LoadingHoldShimmerTests.cs` use 50/200/10 ms probes only). No test edit was needed; no test was weakened.
- Constant name kept (`AutocompleteDebounceMilliseconds`) because the DX original references it; rename deferred to the DX removal.

### E2E
E2E: emulator not run in this session. Manual check needed: 300 ms must not make the shimmer flash on every keystroke on device (README requirement). If it does, report to Helder rather than picking another value.

### Changed files:
- `MyVocaList/UI/ViewModels/CrudListViewModelBase.cs` (literal 400 → shared constant; `using MyVocaList.Domain.Constants`)
- `Domain/Constants/SearchConstants.cs` (doc comment only)
- `Docs/Management/DevCycleCraft/crud-list-deduplication/changes/2026-11-24-one-search-delay-for-all-searches-300-ms/README.md` (spec note)
- `Docs/Management/DevCycleCraft/crud-list-deduplication/changes/2026-11-24-one-search-delay-for-all-searches-300-ms/task-log.md` (new)
- `MyVocaList.sln` (registration)

### Verification evidence
- `dotnet test --filter CrudListViewModelBaseTests|LoadingHold|SearchConstantsTests|CrudListStaleLoadTests`: 57 passed, 0 failed.

### Build notes
Build: see commit (Android build run before commit) | Tests: 57 passed (filtered), full suite runs in pre-commit hook | Commit SHA: see git log
Files written and re-read: `CrudListViewModelBase.cs` (line 359 and using block), `SearchConstants.cs` (L36-40), item README decision note.
