# Task Log — AppBar / SearchAppBar Interaction Redesign

## 2026-07-20/21 — Emulator smoke-test bug triage (BUG-048, BUG-049)

Found by Helder during S23 emulator smoke test of `feature/persistent-searchbar` (worktree `MyVocaList-wt-searchbar`, pushed at `997e784`). Registered in BACKLOG.md before investigation per `bug-tracking.md` HARD RULE. Root-cause investigation performed by a read-only Explore subagent against the same worktree.

### BUG-048 — CrudListView pagination reload (Major)

**Symptom:** reaching the end of the currently-loaded page reloads/re-renders the list as if it were the first page load, instead of appending the next page.

**Root cause:** `ObservableRangeCollection.AddRange()` (`MyVocaList/UI/Collections/ObservableRangeCollection.cs:25`) raises `NotifyCollectionChangedAction.Reset` instead of an `Add` notification. DevExpress `DXCollectionView` treats `Reset` as "entire source changed" and re-renders + resets scroll position. The pagination bookkeeping itself (`CrudListViewModelBase.LoadMoreAsync`, `_currentPage`/`HasMoreItems`/`_totalCount`) is correct — confirmed via `git log` that `ObservableRangeCollection.cs` was not touched by the persistent-searchbar commits. **Pre-existing latent bug**, not a regression from this feature — surfaced now because smoke testing exercises the CRUD list pages more thoroughly.

**Governance:** fix is contained entirely within `ObservableRangeCollection.cs` (change `AddRange` to raise an `Add`-action event with correct starting index/new items). No `CrudListView.xaml`/`.xaml.cs` change required → **four-gate governed-component process does NOT apply**.

**Scope decision (Helder, 2026-07-21):** fix in a fresh session, single-file worktree task, normal bug-fix workflow (worktree, regression test — Major/testable — mandatory, commit-message-as-spec).

### BUG-049 — VenueFormPage post-save navigation (Major)

**Symptom:** after editing and saving a Venue, navigation sometimes lands on Queue (home) instead of VenuesPage.

**Root cause:** `SaveCommand` in `VenueFormViewModel.cs:44` (and `PersonFormViewModel.cs:70`) has no re-entrancy guard (`AsyncRelayCommandOptions`/`IsBusy` `CanExecute`). A fast double-tap fires `SaveAsync` twice concurrently; each successful path ends in a bare relative `Shell.Current.GoToAsync("..")` (`VenueFormViewModel.cs:114`). The second pop overshoots the section's nav-stack root once the first pop has already returned to it, and Shell falls back to the first-declared `FlyoutItem` in `AppShell.xaml` (`queue`, `AppShell.xaml:68-70`).

**Relation to BUG-037:** different symptom (BUG-037 = no navigation happens, likely a null-Shell issue already patched) but same *family*: fragile bare relative `Shell.Current.GoToAsync("..")` with no re-entrancy/stack-state guard. `PersonFormViewModel` already has a safer pattern elsewhere (`INavigationService`, from the BUG-044 fix) not applied consistently to `SaveCommand`.

**Scope decision (Helder, 2026-07-21):** fix all four form ViewModels (`VenueFormViewModel`, `PersonFormViewModel`, `ArtistFormViewModel`, `SongFormViewModel`) in one task — consistent re-entrancy guard on `SaveCommand`, not a Venue-only patch. Single worktree task, fresh session. Regression test mandatory (Major/testable) — at minimum a ViewModel-level test proving a second concurrent `SaveAsync` invocation is a no-op while the first is in flight.

### Next step
Both fixes dispatched together as one bug-fix task in a fresh session (see BACKLOG rows below, status → 📋 Ready). Neither requires component-change governance.
