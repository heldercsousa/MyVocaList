---
## Task: Dispose the last search cancellation source (B9)
**Plan:** Docs/Management/DevCycleCraft/crud-list-deduplication/changes/2026-11-25-minor-findings-autocomplete-and-crud-list-search/changes/2026-11-25-dispose-the-last-search-cancellation-source/README.md
**Status:** To Review
**Started:** 2026-11-25
**Completed:** 2026-11-25

### Consumer map (shared ViewModel base and venue picker)
- `CrudListViewModelBase.TriggerSearchDebounce` (base of Songs, Artists, Persons, Venues list VMs): the search task now releases its own source in `finally`; the task no longer receives the token (a cancelled-before-start task would skip `finally`).
- `VenueMapPickerViewModel.OnMapTappedAsync`: the tap body moved unchanged into `LookUpTapAsync`; the owner's `finally` releases the tap source. Consumers: the venue map picker page only (no XAML change).

### Per-consumer risk
| Consumer | What could break | Verification |
|---|---|---|
| CRUD list VMs | a late `Cancel()` on an already-disposed source | Replacement path swallows it (existing try/catch in `TriggerSearchDebounce`); `CrudListViewModelBaseTests`, `CrudListStaleLoadTests`, `LoadingHold*` pass |
| VenueMapPickerViewModel | tap result applied after cancel | Tap guard `ct.IsCancellationRequested` unchanged; VenueMap tests pass |

### Decision: owner-gone disposal (spec gap, Blocking: No)
**Gap:** the spec says a source is disposed "when its owner goes away". The CRUD base has no lifecycle hook (no `Dispose`/`OnDisappearing`) and wiring one touches every list page.
**Option A (taken):** release the last source when its search task ends (`finally`). Covers the last source in practice: nothing is left undisposed after a completed search.
**Option B:** add `IDisposable` to the base and call it from each page's disappearing handler.
**Recommendation:** A now; B only if Helder wants the cancelled-but-never-finished case released on page exit.
**Assumption flagged:** a search that is still waiting when the page is left keeps its source until its 300 ms delay ends (then it is released). Not a leak; no timer is held.

### Test decision
Minor severity, mechanical release. No test added: asserting a private field's disposal would test the implementation, not behaviour. Existing suites cover the behaviour.

### Changed files:
- `MyVocaList/UI/ViewModels/CrudListViewModelBase.cs` (`TriggerSearchDebounce`: local `cts`, `finally` release)
- `MyVocaList/UI/ViewModels/VenueMapPickerViewModel.cs` (`OnMapTappedAsync` try/finally; body moved to `LookUpTapAsync`)
- `MyVocaList.sln` (registration)
- `<this folder>/task-log.md` (new)

### Verification evidence
- `dotnet test --filter VenueMap|CrudListViewModelBaseTests|CrudListStaleLoadTests|LoadingHold`: 81 passed, 0 failed.

### Build notes
Build: see Android build (exit recorded below) | Tests: 81 passed (filtered) | Commit SHA: see git log
Files written and re-read: `CrudListViewModelBase.cs` (TriggerSearchDebounce), `VenueMapPickerViewModel.cs` (L189-204 re-read).

### Note on the shared commit
`VenueMapPickerViewModel.cs` carries both this item's tap-source release and item 5's search cancellation. They are committed together (one commit, both task-logs), because the two edits share the same file.
