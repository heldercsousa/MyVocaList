---
## Task: Venue map picker search can be cancelled (B10)
**Plan:** Docs/Management/DevCycleCraft/crud-list-deduplication/changes/2026-11-25-minor-findings-autocomplete-and-crud-list-search/changes/2026-11-25-venue-map-picker-search-can-be-cancelled/README.md
**Status:** To Review
**Started:** 2026-11-25
**Completed:** 2026-11-25

### Consumer map (shared ViewModel)
`VenueMapPickerViewModel` consumers: `VenueMapPickerSfPage.xaml.cs` (wired: `OnUnloaded` calls `CancelPendingSearch`), `VenueMapPickerPage.xaml.cs` (DX original, not edited: its pending search is not cancelled on leave until the DX removal; DX-only originals are not touched by this task).

### Per-consumer risk
| Consumer | What could break | Verification |
|---|---|---|
| VenueMapPickerSfPage | Unload now cancels a pending search; if the page is re-shown, the user searches again | Unit: `CancelPendingSearch_WhenThePageIsLeft_CancelsTheSearchToken`; device check pending |
| VenueMapPickerPage (DX) | none (no call added) | n/a |

### Changes
- `SearchAsync`: owns a per-search `CancellationTokenSource`; passes its token to `IPlaceLookupService.SearchAsync` (already accepted one); a cancelled search drops its answer; the latest search owns the busy flag.
- `CancelPendingSearch()` (new, public): cancels the latest search, clears the busy flag. Called by a newer search, by `OnMapTappedAsync` (a newer map tap), and by the Sf page on unload.
- Tap body moved to `LookUpTapAsync` (B9 venue part, same file; committed with item 4 in one commit).

### Test evidence
- New tests (in `Tests/Unit/ViewModels/VenueMapPickerViewModelTests.cs`): `CancelPendingSearch_WhenThePageIsLeft_CancelsTheSearchToken`, `SearchCommand_SecondSearch_CancelsTheFirst`.
- Red: the tests were written against the missing `CancelPendingSearch` (compile failure before the method existed); not run with the old SearchAsync. Minor severity: regression test optional per bug-tracking.md.
- `dotnet test --filter VenueMapPicker`: 23 passed, 0 failed.

### Changed files:
- `MyVocaList/UI/ViewModels/VenueMapPickerViewModel.cs`
- `MyVocaList/UI/Pages/Venues/VenueMapPickerSfPage.xaml.cs` (one line in `OnUnloaded`)
- `Tests/Unit/ViewModels/VenueMapPickerViewModelTests.cs` (two tests + helper)
- `MyVocaList.sln` (registration)
- `<this folder>/task-log.md` (new)

### Build notes
Build: see Android build below | Tests: 23 passed (venue filter) | Commit SHA: see git log
Files written and re-read: `VenueMapPickerViewModel.cs` L141-197 and the tap method; `VenueMapPickerSfPage.xaml.cs` L110-117.

### Round 2 (coordinator request): map tap cancels a pending search
- Test `MapTap_CancelsAPendingSubmitSearch` added (REQ-VEN-26 / B10).
- Red: with the `CancelPendingSearch()` call in `OnMapTappedAsync` temporarily commented out, the test FAILED (`Assert.True() Failure`). Line restored; green again (VenueMapPickerViewModelTests with SongForm suite: 128 passed).
