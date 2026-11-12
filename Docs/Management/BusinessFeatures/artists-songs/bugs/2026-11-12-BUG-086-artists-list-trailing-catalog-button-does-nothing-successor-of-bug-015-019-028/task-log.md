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

---
## Task: BUG-086 round 2 — emulator reproduction and fix (Release build failed on device)
**Plan:** Docs/Management/BusinessFeatures/artists-songs/bugs/2026-11-12-BUG-086-artists-list-trailing-catalog-button-does-nothing-successor-of-bug-015-019-028/README.md
**Status:** To Review
**Started:** 2026-11-12
**Completed:** 2026-11-12
**Branch:** `fix/bug-086-round2` (worktree `agent-a4f99347fe068d9b7`, based on `fix/bug-086-artists-catalog-nav` c23c7b85)

### Root cause (round 2, reproduced on emulator)
`SongsViewModel.AppBarTitle` depends on `ArtistName`, but `ArtistName` raised no change notification. The Shell applies
query values in URL order (`artistId` before `artistName`), so when `ArtistIdRaw` notifies the title, `ArtistName` still
holds the **previous** artist's name. The later `ArtistName` assignment never re-notifies the title, so the bound label
keeps the stale name. Result: tapping artist B's catalog button shows A's name in the title while the list shows B's songs
(the "another artist name added to the label" symptom). On a cold start the first title is blank for the same reason.
The list itself loads correctly for the tapped artist (see evidence below), so the round-1 reload path is not the cause.
Reproduced on the **Release** build; the mechanism is platform-independent; Debug was not separately run.

### Fix (round 2)
- `SongsViewModel.OnArtistNameChanged` re-raises `PropertyChanged(AppBarTitle)`. Global mode (`ArtistId == 0`) is unchanged ("Songs").
- Regression test `SongsViewModelBug086Tests.ArtistNameArrivingAfterArtistId_RaisesAppBarTitleWithNewName`:
  Red seen first (`Assert.Equal() Failure: Expected "Bravo Band", Actual "Alpha Artist"`), Green after the fix.
- No change to `ArtistsViewModel`, `CrudListViewModelBase`, `CrudListPageBase`, XAML, AppShell or MauiProgram.

### Symptom (b): empty list: NOT reproduced as a load failure
- Artists with catalog rows load correctly: 7 consecutive catalog taps across Alpha / Bravo (cold start, after returning
  via flyout, alternating artists) all showed the correct songs (Alpha: "Alpha Song One", "Alpha Song Two"; Bravo: "Bravo Hit").
- The empty list appears only when the artist's catalog has **zero rows** (Charlie Choir, and Alpha/Bravo before seeding).
  The screen then shows the generic "No song registered" empty state. Catalog rows cannot be created from the UI:
  `SongsViewModel.AddToCatalogAsync` is the empty Phase 15 stub, and a song created with Alpha as its artist did not add a
  catalog row (Alpha stayed at "No catalog"). If the artist(s) tapped on device had no catalog rows, (b) is the expected
  empty state; if they had rows, this build does not reproduce the failure.
- Observation (out of scope, not changed): in catalog mode the empty state text is the global "No song registered".

### Symptom (c): FAB on catalog page
Confirmed no-op on the emulator (tapping `+` leaves the page unchanged, no crash, no error logged). Expected from the
`AddToCatalogAsync` stub (Phase 15). Not implemented, per instruction.

### Emulator verification (Release build, emulator `pixel_5_-_api_36_0_1`, package `com.myvocalist`)
Driven with adb input + uiautomator dump; DB seeded via `adb root` and a host-side copy (see seeding note).

| Step | Round-1 build (before) | Round-2 build (after) |
|------|------------------------|-----------------------|
| Alpha catalog, cold start | list 2 songs, title blank | list 2 songs, title "Alpha Artist" |
| Bravo catalog after Alpha | list "Bravo Hit", title "Alpha Artist" (stale) | list "Bravo Hit", title "Bravo Band" |
| Alpha catalog after Bravo | list correct, title "Bravo Band" (stale) | list correct, title "Alpha Artist" |
| 4 cycles A/B/A/B via flyout | title one step stale every cycle | title matches tapped artist every cycle |
| Charlie Choir (0 catalog rows) | title "Alpha Artist" (stale), "No song registered" | title "Charlie Choir", "No song registered" |
| Global Songs from flyout after catalog visits | n/a | title "Songs", all 3 songs |
| Hardware back from global Songs | n/a | returns to Bravo catalog page (title and list correct) |

Evidence (scratchpad, not committed): before: `evidence_a_title_stale_bravo.png`, `evidence_first_tap_alpha_blank_title.png`;
after: `evidence_fixed_bravo.png`, `evidence_fixed_charlie_empty.png`, `evidence_fab_catalog_noop.png`.
Crash check: `logcat` shows no AndroidRuntime / FATAL / Unhandled lines for the app process.
The catalog route `//songs?...` resolves on the hidden `songs` FlyoutItem on device (the round-1 "not verified on device" item is closed).

### Seeding note (emulator only, not committed)
The Release app is not debuggable, so `run-as` is unavailable. `adb root` worked on this emulator. The DB was pulled to the
host, rows were inserted with Python `sqlite3` (Songs insert triggers parked for the host copy only, because the host SQLite
lacks the trigram tokenizer; recreated verbatim), and pushed back with the app stopped. Test data: Alpha Artist (2 catalog
songs), Bravo Band (1 catalog song), Charlie Choir (0). Reinstalling the APK reset app data once; the seeded DB was re-applied.

### Test results (round 2)
- Red: `ArtistNameArrivingAfterArtistId_RaisesAppBarTitleWithNewName` failed (Expected "Bravo Band", Actual "Alpha Artist").
- Green: `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --filter FullyQualifiedName~Bug086` -> 5 passed, 0 failed.
- Full suite: `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj` -> 972 passed, 0 failed.
- Build: `dotnet build MyVocaList/MyVocaList.csproj -c Release -f net10.0-android -t:Install -p:AndroidSigningKeyStore=` -> 0 Erro(s), 134 Aviso(s).

### Open questions for Helder
1. Symptom (b): did the artist(s) you tapped have catalog rows? In this build the catalog cannot be populated from the UI (Phase 15 stub), so an empty list is the expected state for an artist with no rows.
2. Empty-state text in catalog mode reads "No song registered" (global wording). Keep, or give catalog mode its own message? (A governed-component/XAML decision, not made here.)

### Changed files:
- `MyVocaList/UI/ViewModels/SongsViewModel.cs`
- `MyVocaList.Tests/Unit/ViewModels/SongsViewModelBug086Tests.cs`
- `Docs/Management/BusinessFeatures/artists-songs/bugs/2026-11-12-BUG-086-artists-list-trailing-catalog-button-does-nothing-successor-of-bug-015-019-028/task-log.md`
