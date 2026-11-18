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
