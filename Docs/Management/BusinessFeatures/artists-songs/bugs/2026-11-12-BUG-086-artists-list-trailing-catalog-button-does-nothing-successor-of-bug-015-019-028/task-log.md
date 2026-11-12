---
## Task: BUG-086 — Artists list trailing catalog button does nothing
**Plan:** Docs/Management/BusinessFeatures/artists-songs/bugs/2026-11-12-BUG-086-artists-list-trailing-catalog-button-does-nothing-successor-of-bug-015-019-028/README.md
**Status:** To Review
**Started:** 2026-11-12
**Completed:** 2026-11-12
**Branch:** `fix/bug-086-artists-catalog-nav` (off develop ff8a80ac), not merged

### Root cause (static analysis, confirmed by code reading; not reproduced on device)
`ArtistsViewModel.NavigateToCatalog` was a sync `RelayCommand` that discarded the navigation task
(`_ = Shell.Current.GoToAsync("songs?artistId=..")`). The relative route `songs` does not match a registered
route: `songs` is a hidden top-level `FlyoutItem` (AppShell.xaml), not `Routing.RegisterRoute`d. The failure
was swallowed, so the tap did nothing. `ArtistsViewModel` also had no `INavigationService` seam, so it was untestable.

### Fix
1. `ArtistsViewModel`: injects `INavigationService`; `ViewCatalogCommand` is an `AsyncRelayCommand<ArtistListItem>`
   that awaits `GoToAsync("//songs?artistId={id}&artistName={Uri.EscapeDataString(name)}")` (absolute route, no discard).
2. `SongsViewModel.OnArtistIdChanged`: reloads the list when `ArtistId` changes after the first load
   (`if (!IsInitialLoading) _ = ReloadAsync();`). Before the first load, `InitializeAsync` (OnAppearing) loads with the context already applied, so no duplicate load.
   `CrudListPageBase` / `CrudListViewModelBase` / XAML are unchanged.

### Open items (not implemented, reported to orchestrator)
- **Reset to global mode when Songs is opened without a query: NOT implemented.** Reading the code: the flyout
  path (`AppShellViewModel.NavigateAsync`) PushAsyncs a freshly DI-resolved `SongsPage` (new `SongsViewModel`,
  `ArtistId = 0`), so flyout-opened Songs is already global. The only stuck case is a Shell-cached Songs instance
  reached by `//songs` with no query, and no code path currently does that. A correct reset needs a page-level hook
  (e.g. `SongsPage.OnNavigatedTo` plus a Shell-location check), which is a design choice. Blocking: No; flagged for Helder.
- **Does `//songs?...` resolve with this custom flyout?** Judgement: yes. `songs` is a `FlyoutItem` with a
  `ShellContent` (route `songs`), `FlyoutItemIsVisible=False` only hides it from the menu, and Shell absolute routes
  resolve to FlyoutItem routes regardless of visibility. The flyout uses PushAsync, but `//songs` is an absolute
  navigation that replaces the stack. Not verified on device; see device check below.
- `ArtistsViewModel` Add/Edit navigation still uses `Shell.Current` directly (out of BUG-086 scope; not changed).

### Verification evidence
- Red: `SongsViewModelBug086Tests.ArtistIdChangedAfterFirstLoad_ReloadsCatalogForNewArtist` was run with the reload
  line disabled: **failed** (timeout waiting for catalog load). Restored, then passes.
- Green: `dotnet test --filter Bug086` -> 4 passed (2 ArtistsViewModel, 2 SongsViewModel).
- Full suite: `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj` -> 957 passed, 0 failed.
- `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android` -> exit 0, 0 errors.
- E2E emulator: NOT run by the implementor (not available in this session). Requires manual device check (below).

### Traceability
| AC | Criterion | Implementation | Test |
|----|-----------|----------------|------|
| artists-songs AC-9.1 | Catalog button navigates to Songs filtered to that artist's catalog | `ArtistsViewModel.NavigateToCatalogAsync` | `ArtistsViewModelBug086Tests.ViewCatalog_NavigatesToAbsoluteSongsRouteWithArtistContext` |
| BUG-086 (regression, no AC) | Navigation failure is not discarded | `ArtistsViewModel.ViewCatalogCommand` (awaited) | `ArtistsViewModelBug086Tests.ViewCatalog_NavigationFailure_PropagatesToCaller` |
| BUG-086 (regression, no AC) | Shell-cached Songs reloads on new artist context | `SongsViewModel.OnArtistIdChanged` | `SongsViewModelBug086Tests.ArtistIdChangedAfterFirstLoad_ReloadsCatalogForNewArtist` |
| BUG-086 (guard) | Pre-first-load artistId does not double-load | `SongsViewModel.OnArtistIdChanged` guard | `SongsViewModelBug086Tests.ArtistIdSetBeforeFirstLoad_DoesNotPreLoad_InitializeLoadsCatalog` |

### Device check (Helder)
1. Open Artists, tap the trailing catalog button on an artist row -> Songs opens, titled with the artist name, list filtered to that artist's catalog.
2. Open Songs from the flyout -> global song list, no artist title.
3. If step 1 does nothing, check the log for a `GoToAsync` route exception (the failure now propagates instead of being swallowed).

### Changed files:
- `MyVocaList/UI/ViewModels/ArtistsViewModel.cs`
- `MyVocaList/UI/ViewModels/SongsViewModel.cs`
- `MyVocaList.Tests/Unit/ViewModels/ArtistsViewModelBug086Tests.cs` (new)
- `MyVocaList.Tests/Unit/ViewModels/SongsViewModelBug086Tests.cs` (new)
- `Docs/Management/BusinessFeatures/artists-songs/bugs/2026-11-12-BUG-086-artists-list-trailing-catalog-button-does-nothing-successor-of-bug-015-019-028/task-log.md` (new)
- `MyVocaList.sln` (registers the new task-log.md under the BUG-086 solution folder)

### Build notes
Build: passed (0 errors) | Tests: 957 passed, 0 failed | Commit SHA: see branch head (fix/bug-086-artists-catalog-nav)
Files written and re-read: ArtistsViewModel.cs (grep-verified ctor, command, route), SongsViewModel.cs (lines 67-78 read, guard restored after red check), both new test files, sln line 130.
