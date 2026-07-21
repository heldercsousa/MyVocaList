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

---

## 2026-07-21 — BUG-048 / BUG-049 fix (worktree `bug-048-049`, branch `fix/bug-048-049-collection-reentrancy`)

**Plan:** N/A — commit-message-as-spec bug-fix pattern (`workflow.md § Bug Fix Pattern`).
**Status:** To Review
**Started:** 2026-07-21
**Completed:** 2026-07-21

### Changed files:
- `MyVocaList/UI/Collections/ObservableRangeCollection.cs` — BUG-048: `AddRange` now raises a single `NotifyCollectionChangedAction.Add` event (correct `NewStartingIndex` + `NewItems`) instead of `Reset`; also raises `Count`/`Item[]` `PropertyChanged` (previously missing, since `Items.Add` bypasses `ObservableCollection`'s own notification path).
- `MyVocaList/UI/ViewModels/VenueFormViewModel.cs` — BUG-049: `SaveCommand` now built with an explicit `() => !IsBusy` `CanExecute`; `OnIsBusyChanged` raises `NotifyCanExecuteChanged`; `SaveAsync` has an early-return `if (IsBusy) return;` guard.
- `MyVocaList/UI/ViewModels/PersonFormViewModel.cs` — same BUG-049 pattern.
- `MyVocaList/UI/ViewModels/ArtistFormViewModel.cs` — same BUG-049 pattern.
- `MyVocaList/UI/ViewModels/SongFormViewModel.cs` — same BUG-049 pattern.
- `MyVocaList.Tests/Unit/Collections/ObservableRangeCollectionTests.cs` — new file, 6 tests (BUG-048 regression + preserved null/empty no-op behavior).
- `MyVocaList.Tests/Unit/ViewModels/VenueFormViewModelTests.cs` — added `SaveCommand_DoubleInvokedWhileSaving_CallsCreateOnlyOnce` (BUG-049 regression).
- `MyVocaList.Tests/Unit/ViewModels/PersonFormViewModelTests.cs` — same, `CreatePersonAsync` variant.
- `MyVocaList.Tests/Unit/ViewModels/ArtistFormViewModelTests.cs` — same, `CreateArtistAsync` variant.
- `MyVocaList.Tests/Unit/ViewModels/SongFormViewModelTests.cs` — same, `ISongResolutionService.ResolveAsync` variant (Song's save path routes through the resolution engine before any create/update call).

### Verification evidence

**Red/Green protocol (Major severity — mandatory per `bug-tracking.md`):**
- `git stash push -- MyVocaList/UI/Collections/ObservableRangeCollection.cs` → ran the 6 new collection tests → 3 FAILED (Reset instead of Add, wrong `NewStartingIndex`, no `NewItems`) → `git stash pop` → re-ran → 6/6 PASSED.
- `git stash push -- <4 ViewModel files>` → ran the 4 new `DoubleInvokedWhileSaving` tests → all 4 FAILED (`Moq.MockException: Expected invocation once, but was 2 times`) → `git stash pop` → re-ran → 4/4 PASSED.

**Build:** `dotnet build MyVocaList.Tests/MyVocaList.Tests.csproj` → 0 errors.
**Full test suite:** `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj` → 511 passed, 0 failed.

### AC / regression-test traceability

| Bug | Regression test | Result |
|-----|------------------|--------|
| BUG-048 | `ObservableRangeCollectionTests.AddRange_OnPopulatedCollection_RaisesAddAction_NotReset` + 2 companion tests | Red→Green confirmed |
| BUG-049 | `{Venue,Person,Artist,Song}FormViewModelTests.SaveCommand_DoubleInvokedWhileSaving_CallsCreateOnlyOnce` (one per ViewModel) | Red→Green confirmed |

### Bug-fix commit messages (see git log for exact SHAs)
```
fix: ObservableRangeCollection — BUG-048 pagination reload resets scroll position

Root cause: AddRange raised NotifyCollectionChangedAction.Reset instead of Add, so DXCollectionView
treated every "load more" page fetch as a full source change and re-rendered/reset scroll position.
Fix: AddRange now raises a single Add-action event with the correct starting index and new items.
Regression risk: Low — ReplaceRange/ClearRange (which legitimately need Reset) are untouched.
```
```
fix: {Venue,Person,Artist,Song}FormViewModel — BUG-049 double-tap Save overshoots nav stack

Root cause: SaveCommand had no re-entrancy guard; a fast double-tap fired SaveAsync twice
concurrently, and the second bare relative Shell.Current.GoToAsync("..") overshot the nav stack
root, landing on the Queue FlyoutItem instead of the entity's list page.
Fix: SaveCommand now has an explicit CanExecute tied to IsBusy (NotifyCanExecuteChanged on
OnIsBusyChanged), plus an early-return `if (IsBusy) return;` guard inside SaveAsync as
defense-in-depth against callers that invoke the command without checking CanExecute.
Regression risk: Low — guard only blocks a second concurrent invocation; single-tap Save flow
is unchanged in all four ViewModels.
```

### Design concern (documented per Subagent Scope Constraint — not acted on)
CommunityToolkit.Mvvm's `AsyncRelayCommand` already defaults `AllowConcurrentExecutions = false`,
which should make `CanExecute` return `false` while a previous execution is still running. The
observed bug implies the DevExpress `DXButton` bound via `Command="{Binding SaveCommand}"` may
invoke `ICommand.Execute` without checking `CanExecute` first (or does not re-check between the
two taps of a fast double-tap). The explicit `IsBusy`-driven `CanExecute` + in-method early-return
guard added here is a safe, standard MVVM reinforcement regardless of the exact `DXButton` internals,
but the `DXButton` re-entrancy behavior itself was not root-caused further (out of scope — no
`DXButton`/XAML file was touched). Flagging for awareness in case other `DXButton`-bound commands
across the app have the same latent exposure.

