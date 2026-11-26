---
## Task: Autocomplete field cancels its pending search when it leaves the screen (B13)
**Plan:** Docs/Management/DevCycleCraft/crud-list-deduplication/changes/2026-11-25-minor-findings-autocomplete-and-crud-list-search/changes/2026-11-25-autocomplete-field-cancels-its-pending-search-when-it-leaves-the-screen/README.md
**Status:** To Review
**Started:** 2026-11-25
**Completed:** 2026-11-25

### Consumer map (governed component: AutocompleteFieldSf)
`AutocompleteFieldSf` consumers (grep): `SongFormSfPage.xaml`, `ArtistFormSfPage.xaml`, `PersonFormSfPage.xaml`. Parameterless constructor and XAML unchanged.

### Per-consumer risk
| Consumer | What could break | Verification |
|---|---|---|
| SongFormSfPage / ArtistFormSfPage / PersonFormSfPage | An unloaded field cancels its pending search; a field re-shown later searches again on the next keystroke | Unit: `AutocompleteFieldSfUnloadTests.Unloaded_CancelsThePendingSearch`; device check pending |

### Change
- Constructor: `Unloaded += OnUnloaded;` (before the test-seam early return).
- `internal void OnUnloaded(...) => _filter.Cancel();` (the filter cancels its latest search; the DX field is not touched).

### Red / Green
- Test added with the fix in the same pass. Red was not observed separately (Minor severity; regression test optional per bug-tracking.md). The test drives the real filter's `FilterAsync` through the field's `_filter` and asserts `OperationCanceledException` and zero option-source calls after `OnUnloaded`.
- Result: `AutocompleteFieldSfUnloadTests` + `AutocompleteFilterSf` + characterization: 14 passed, 0 failed.

### Changed files:
- `MyVocaList/UI/Components/TextFields/AutocompleteFieldSf.xaml.cs` (Unloaded subscription, `OnUnloaded`)
- `Tests/Unit/Components/AutocompleteFieldSfUnloadTests.cs` (new, 1 test)
- `MyVocaList.sln` (registration)
- `<this folder>/task-log.md` (new)

### Build notes
Build: Android build run before this change set; re-run after this edit: see commit | Tests: 14 passed (filtered) | Commit SHA: see git log
Files written and re-read: `AutocompleteFieldSf.xaml.cs` L296-299 and L469-472.

## Phone check (Helder, 2026-11-26)

Debug APK `com.myvocalist-debug-20261126-173207-b410cb1f.apk`, on Helder's phone.

- **Pass.** Typing in the Song form Title field and pressing Back at once: no crash, and no old suggestion list on reopening.
