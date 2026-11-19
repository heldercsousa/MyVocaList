# Task log: Debug builds hold the loading shimmer for a while

---
## DLH1 (2026-11-17) — Check build
**Plan:** Docs/Management/BusinessFeatures/cross-cutting/changes/2026-11-17-debug-builds-hold-the-loading-shimmer-for-a-while/tasks.md
**Status:** Check build (E2E emulator not run in this session; see note)
**Started:** 2026-11-17
**Completed:** 2026-11-17

### Changed files:
- `MyVocaList/UI/Services/LoadingHold.cs` (new)
- `MyVocaList/MauiProgram.cs` (`#if DEBUG` assignment of `LoadingHold.Duration`)
- `MyVocaList/UI/ViewModels/CrudListViewModelBase.cs` (first-load hold in `InitializeAsync` / `LoadFirstPageAsync`)
- `MyVocaList/UI/ViewModels/SongFormViewModel.cs` (edit-mode hold in `LoadSongForEditAsync`; `using MyVocaList.UI.Services`)
- `MyVocaList.Tests/Unit/ViewModels/LoadingHoldTests.cs` (new, 3 tests, collection `LoadingHold` with `DisableParallelization`)
- `Docs/.../changes/2026-11-17-debug-builds-hold-the-loading-shimmer-for-a-while/tasks.md` (DLH1 ticked)
- `Docs/.../changes/2026-11-17-debug-builds-hold-the-loading-shimmer-for-a-while/requirements.md` (one `Spec updated` note in Design)
- `Docs/.../changes/2026-11-17-debug-builds-hold-the-loading-shimmer-for-a-while/task-log.md` (new)
- `MyVocaList.sln` (task-log.md registered in the change's solution folder)

### Traceability (AC → implementation → test)
| AC | Criterion | Implementation | Test |
|----|-----------|----------------|------|
| REQ-DLH-01 | First list load waits for the hold, off the UI thread | `CrudListViewModelBase.LoadFirstPageAsync` (`holdShimmer: true` from `InitializeAsync` first-load branch), `LoadingHold.WaitAsync` | `LoadingHoldTests.ListFirstLoad_WithHold_DoesNotEndBeforeTheHold` |
| REQ-DLH-01 | Song-form edit-mode load holds `IsSongLoading` | `SongFormViewModel.LoadSongForEditAsync` (`hold` awaited in `finally`) | `LoadingHoldTests.SongEditLoad_WithHold_IsSongLoadingStaysTrueUntilTheHoldEnds` |
| REQ-DLH-02 | Release builds have no hold | `MauiProgram.cs` assignment inside `#if DEBUG`; Duration stays zero otherwise | Release build compiles (see Verification); no runtime test |
| REQ-DLH-03 | Unit tests run with no hold | `LoadingHold.Duration` zero default; tests set and reset it in `finally`; collection `DisableParallelization` | `LoadingHoldTests.WaitAsync_ZeroDuration_ReturnsCompletedTaskAtOnce` |
| REQ-DLH-04 | One named value in one place | `MauiProgram.cs` single `TimeSpan.FromMilliseconds(1500)` | grep: one occurrence |

### Consumers and risk (CrudListViewModelBase / SongFormViewModel)
- `VenuesViewModel`: first load waits 1.5 s in debug only; risk is a slower first paint in debug, verified by DLH2 device check.
- `SongsViewModel`: same as Venues.
- `ArtistsViewModel`: same as Venues.
- `PersonsViewModel`: same as Venues.
- `SongFormViewModel` (edit mode): `IsSongLoading` true for the hold time in debug; add mode untouched; Save-during-load guard unchanged (still keyed on `IsSongLoading`).
- Searches, refresh, load-more, revisit: no hold (`holdShimmer` defaults to false); unchanged.

### Verification evidence
- Red: `LoadingHoldTests` before the view-model wiring: 2 hold tests FAIL at the "still loading" assertion; zero-duration test PASS.
- Green targeted: `dotnet test --filter "LoadingHoldTests|CrudListViewModelBaseTests|SongFormViewModel"`: 272 passed, 0 failed.
- Full suite: run 1: 1858 passed, 1 failed (`UnitOfWorkCompositionTests.Composition_RegistersFactoryAndUnitOfWork_ExactlyOnce`, not on the known-intermittent list; passes alone); run 2: **1859 passed, 0 failed**.
- `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android`: 0 errors, 27 warnings (package advisories, pre-existing).
- `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android -c Release`: 0 errors.
- E2E emulator: not run in this session. Device check is DLH2 (Helder). Status is therefore `Check build`, not `To Review`.

### Decisions not in the spec (recorded in requirements.md Design note)
- `LoadingHold` placed in `MyVocaList/UI/Services/` (where `AppImagePaths` lives).
- Fetch stopwatch measures fetch only; the hold is awaited after the fetch, so the shimmer is max(fetch, hold).
- Song form awaits the hold in `finally`, so the placeholder clears after the load and the hold, on success, failure and not-found alike.
- `WaitAsync` treats `Duration <= 0` as no hold (guards negative values, which `Task.Delay` rejects).
- In Release the hold call exists but is a no-op (zero duration); only the assignment is compiled out.

### Design concern
- None.

## DLH3 (2026-11-17) — To Review
**Plan:** Docs/Management/BusinessFeatures/cross-cutting/changes/2026-11-17-debug-builds-hold-the-loading-shimmer-for-a-while/tasks.md
**Status:** To Review
**Started:** 2026-11-17
**Completed:** 2026-11-17

### Resolved since the first hand-back (coordinator decisions)
1. **Revisit test (replaced by REQ-DLH-05).** `CrudListViewModelBaseTests.InitializeAsync_SecondCall_DoesNotToggleIsInitialLoading` asserted the old "shimmer only on the first load" behaviour. Replaced in place by `InitializeAsync_SecondCall_TogglesIsInitialLoading` (`// [AC] REQ-DLH-05`), which asserts the revisit raises true then false. No other existing test was changed.
2. **Songs artist-change guard (in scope).** The broadened flag made `SongsViewModel.OnArtistIdChanged` skip the reload during a search, a filter reload or a later visit. Fixed: the base now exposes `protected bool HasCompletedFirstLoad` (set on the UI thread in the apply block, where `_hasLoadedOnce` was set before), and the guard is `if (HasCompletedFirstLoad) ReloadAsync()`. BUG-086 intent kept: no reload before the first load, a reload on every artist change after it, including during a search. Red first: `SongsArtistChangeDuringSearchTests.ArtistIdChangedWhileSearchIsRunning_StillReloadsCatalogForNewArtist` failed with the broadened flag; green after the fix. The six BUG-086 tests in `SongsViewModelBug086Tests` pass unchanged.

### Changed files:
- `MyVocaList/UI/ViewModels/CrudListViewModelBase.cs` (shimmered InitializeAsync, search debounce and ReloadAsync; `_loadGeneration`; `BeginShimmeredLoad`, `IsSuperseded`, `CurrentLoadGeneration`; `LoadFirstPageAsync(ct, int? shimmerGeneration)` with superseded return and finally clear; `HasCompletedFirstLoad`; `_hasLoadedOnce` removed; `IsInitialLoading` XML doc broadened)
- `MyVocaList/UI/ViewModels/SongsViewModel.cs` (`OnArtistIdChanged` guard on `HasCompletedFirstLoad`)
- `MyVocaList.Tests/Unit/ViewModels/LoadingHoldShimmerTests.cs` (new, 9 tests, collection LoadingHold)
- `MyVocaList.Tests/Unit/ViewModels/SongsArtistChangeDuringSearchTests.cs` (new, 1 regression test)
- `MyVocaList.Tests/Unit/ViewModels/CrudListViewModelBaseTests.cs` (one test replaced, see item 1)
- `Docs/.../changes/2026-11-17-debug-builds-hold-the-loading-shimmer-for-a-while/requirements.md` (one `Spec updated` note under REQ-DLH-05)
- `Docs/.../changes/2026-11-17-debug-builds-hold-the-loading-shimmer-for-a-while/tasks.md` (DLH3 ticked)
- `Docs/.../changes/2026-11-17-debug-builds-hold-the-loading-shimmer-for-a-while/task-log.md` (this entry)

### Traceability (REQ-DLH-05)
| AC | Criterion | Implementation | Test |
|----|-----------|----------------|------|
| REQ-DLH-05 | Search shows the shimmer until its rows apply | `TriggerSearchDebounce` → `BeginShimmeredLoad`; apply block clears | `LoadingHoldShimmerTests.Search_ShowsShimmerUntilTheNewRowsApply` |
| REQ-DLH-05 | Filter reload shows the shimmer | `ReloadAsync` | `LoadingHoldShimmerTests.FilterReload_ShowsShimmerUntilTheNewRowsApply` |
| REQ-DLH-05 | Later visit shows the shimmer | `InitializeAsync` (both branches shimmered) | `LoadingHoldShimmerTests.LaterVisit_ShowsShimmerUntilTheNewRowsApply`; `CrudListViewModelBaseTests.InitializeAsync_SecondCall_TogglesIsInitialLoading` |
| REQ-DLH-05 | Pull-to-refresh does not show the shimmer | `RefreshAsync` passes no generation (unchanged) | `LoadingHoldShimmerTests.PullToRefresh_DoesNotShowShimmer` (guard) |
| REQ-DLH-05 | Load-more does not show the shimmer | `LoadMoreAsync` untouched | `LoadingHoldShimmerTests.LoadMore_DoesNotShowShimmer` (guard) |
| REQ-DLH-05 | Superseded search does not end the shimmer | `IsSuperseded` check and apply-block generation check | `LoadingHoldShimmerTests.SupersededSearch_DoesNotEndTheShimmerEarly` |
| REQ-DLH-05 | Debug hold applies to a search | `LoadFirstPageAsync` hold for shimmered loads | `LoadingHoldShimmerTests.Search_InDebugBuild_HoldsTheShimmer` |
| REQ-DLH-05 | Refresh gets no hold | refresh passes no generation | `LoadingHoldShimmerTests.PullToRefresh_InDebugBuild_DoesNotWait` (guard) |
| REQ-DLH-05 | Empty state follows a search | `NotifyEmptyStates` in apply block | `LoadingHoldShimmerTests.Search_EmptyStateShowsOnlyAfterTheSearchAppliesNoRows` (guard) |
| REQ-DLH-05 (BUG-086 guard) | Artist change during a search still reloads | `SongsViewModel.OnArtistIdChanged` on `HasCompletedFirstLoad` | `SongsArtistChangeDuringSearchTests.ArtistIdChangedWhileSearchIsRunning_StillReloadsCatalogForNewArtist` |

### Consumer risk (one line each)
- `SongsViewModel`: guard now on `HasCompletedFirstLoad`; BUG-086 tests pass; artist change during a search reloads (new test).
- `VenuesViewModel`, `ArtistsViewModel`, `PersonsViewModel`: no direct read of the flag; first load, later visit, search and reload now show the shimmer; refresh after delete keeps the refresh circle. Risk: none found in unit tests; device check is DLH2.
- `CrudListView` (governed): not edited.

### Verification evidence
- Red (before the base change): LoadingHold filter → 5 failed (search, reload, later visit, superseded search, search hold), 7 passed (3 DLH1 tests, 4 guards).
- Red (Songs, with the broadened flag): `SongsArtistChangeDuringSearchTests` → 1 failed; BUG-086 tests 6 passed.
- Full suite after the final change: **1869 passed, 0 failed**.
- `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android`: 0 errors (final code).
- `dotnet build ... -c Release` (final code): 0 errors.
- E2E emulator: not run; device check is DLH2 (Helder). Status is To Review per the coordinator decision.

### Decisions not in the spec (recorded in requirements.md Spec-updated note)
- The flag is set at the keystroke (debounce start), so it covers the 400 ms debounce too.
- A superseded shimmered load applies no rows.
- `_hasLoadedOnce` is replaced by `HasCompletedFirstLoad` (protected, set in the apply block).
- A shimmered load that fails or is cancelled as the newest ends the shimmer (finally block).

### Design concern
- None.
