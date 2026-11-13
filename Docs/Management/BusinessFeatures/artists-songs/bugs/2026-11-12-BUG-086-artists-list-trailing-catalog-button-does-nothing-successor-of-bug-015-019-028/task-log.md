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

---
## Task: BUG-086 round 3 — catalog page back arrow, exit confirmation, empty title (emulator)
**Plan:** Docs/Management/BusinessFeatures/artists-songs/bugs/2026-11-12-BUG-086-artists-list-trailing-catalog-button-does-nothing-successor-of-bug-015-019-028/README.md
**Status:** To Review (emulator-verified except (1) on a catalog with real rows; see Round 3 result)

### Checkpoint (final)
- Step 6 of 6 complete: code, tests (975 passed), Release build + install, emulator verification, task-log. Commit and push pending (see Build notes).
- Context manifest: `UI/ViewModels/ArtistsViewModel.cs`, `UI/ViewModels/SongsViewModel.cs`, `UI/Services/INavigationService.cs`, `UI/Services/NavigationService.cs`, `UI/ViewModels/AppShellViewModel.cs`, `UI/Pages/Songs/SongsPage.xaml.cs`, `MyVocaList.Tests/Unit/ViewModels/ArtistsViewModelBug086Tests.cs`, `MyVocaList.Tests/Unit/ViewModels/SongsViewModelBug086Tests.cs`

### Emulator repro (Release build, pixel_5 android-36, package com.myvocalist, pre-fix)
- (2) reproduced: catalog page "Zed Quartet" shows the hamburger (`Open navigation drawer` at 77,213) and no back arrow.
- (3) reproduced: device BACK on the catalog page (`KEYCODE_BACK`) leaves the app to the launcher (Google search screen). No "Exit MyVocaList?" sheet appears. Stack was [Songs] only.
- Control: device BACK on Venues root shows "Exit MyVocaList?" (exit confirmation works on the root).
- (1) NOT reproduced: artist "Zed Quartet" (0 catalog rows, created via Artist form + Song form) shows title "Zed Quartet" and "No song registered", on warm and cold start. Artist "Ana&Co" also shows its title. Song creation through UI did not add catalog rows (Phase 15 stub).
**Started:** 2026-11-12
**Branch:** `fix/bug-086-round3` (worktree `agent-adac9fdc44e3d19cd`, based on `fix/bug-086-round2` e39010d2); no code changed

### Checkpoint
- Branch/worktree: `fix/bug-086-round3` in `.claude/worktrees/agent-adac9fdc44e3d19cd`, clean (no commits yet)
- Step 1 of 6 (last completed: branch created, code read; BLOCKED at start-emulator: no Android emulator binary on this machine)
- Last build/test state: none run in round 3 (round 2 base: 972 passed)
- Blocker: `adb` exists (`C:\Android\platform-tools\adb.exe`), but `emulator.exe` is not on PATH and not found under `C:\Android`, `C:\Program Files\Android`, `C:\Users\helde\AppData\Local\Android` or the registry. AVD `pixel_5_-_api_36_0_1` exists in `C:\Users\helde\.android\avd` but its system image (`system-images\android-36\google_apis\x86_64`) is missing; `adb devices` lists no device.
- Next command once an SDK with `emulator.exe` is restored: `emulator -avd pixel_5_-_api_36_0_1`
- Context manifest:
  - `MyVocaList/UI/ViewModels/AppShellViewModel.cs` — flyout push path (PushAsync, query applied after push)
  - `MyVocaList/UI/Pages/Base/CrudListPageBase.cs` — OnNavigatedTo forces hamburger (governed; do not edit)
  - `MyVocaList/UI/ViewModels/SongsViewModel.cs` — catalog mode, title, reload guards
  - `MyVocaList/UI/ViewModels/ArtistsViewModel.cs` — NavigateToCatalogAsync (GoToAsync `//songs`)
  - `MyVocaList/UI/Services/INavigationService.cs` + `NavigationService.cs` — navigation seam
  - `MyVocaList/AppShell.xaml` + `AppShell.xaml.cs` — hidden `songs` FlyoutItem; OnBackButtonPressed fallback
  - `MyVocaList/UI/Pages/Venues/VenuesPage.xaml.cs` — exit confirmation at root
  - `MyVocaList/Navigation/NavigationConfig.cs` — PageTypes map

### Round 3 result (emulator, Release build, fixed)
**Root cause (confirmed on emulator):** the catalog was reached by an absolute `//songs?...` route, which replaces the Shell
navigation stack. Songs became the root page: the hamburger was shown (CrudListPageBase forces it), and device back on the
root Songs page closed the app (no "Exit MyVocaList?" sheet). Confirmed on the pre-fix build with `KEYCODE_BACK`.

**Fix:**
- `INavigationService.PushAsync(route)` / `PopAsync()` (new). `NavigationService` (constructor takes `IServiceProvider`,
  DI-resolves the page, applies the query BEFORE `PushAsync`, unescapes values, then pushes). Same mechanism as the flyout.
- `AppShellViewModel.NavigateAsync` (non-exit routes) delegates to `INavigationService.PushAsync` (constructor takes `INavigationService`, not `IServiceProvider`).
- `ArtistsViewModel.NavigateToCatalogAsync` calls `PushAsync("songs?artistId=..&artistName=..")` (awaited, no discard).
- `SongsViewModel` implements `IQueryAttributable` (`ApplyQueryAttributes`), takes `INavigationService`, and adds `ApplyAppBarNavigation()`:
  catalog mode shows `arrow_back_outlined` whose command pops the page. Global mode is unchanged.
- `SongsPage.OnNavigatedTo` override calls `ApplyAppBarNavigation()` AFTER `base.OnNavigatedTo` (CrudListPageBase is governed and unchanged).
- `[QueryProperty]` attributes kept (the hidden `songs` FlyoutItem route still resolves).
- `AppShell.xaml(.cs)`, `MauiProgram.cs`, `CrudListPageBase`, `CrudListViewModelBase` are NOT changed. Exit confirmation was not changed: it works because Venues stays the Shell root.

**Tests (Red seen first):** Red = `dotnet test --filter Bug086` failed to compile (CS1061 `PushAsync`, `PopAsync`, `ApplyQueryAttributes`,
`ApplyAppBarNavigation`; CS1729 ctor arity). Green: Bug086 filter 8 passed, 0 failed. Full suite 975 passed, 0 failed.
New: `ApplyQueryAttributes_BeforeFirstLoad_InitialLoadTargetsThatArtistWithTitle`, `ApplyQueryAttributes_ArtistWithZeroCatalogRows_KeepsNameAsTitle`,
`ApplyAppBarNavigation_CatalogMode_ShowsBackArrowThatPopsPage`. Updated: `ArtistsViewModelBug086Tests` now asserts `PushAsync` (was `GoToAsync("//songs")`).

**Emulator evidence (pre-fix build, same flow):**
- (2) hamburger on the catalog page ("Zed Quartet", 0 catalog rows): reproduced.
- (3) device back on the catalog page: app left to the launcher, no exit sheet: reproduced.
- Control: device back on Venues root shows "Exit MyVocaList?".
- (1) empty title: NOT reproduced. "Zed Quartet" and "Ana&Co" (created via the Artist form, zero catalog rows) show their name on warm and cold start.

**Emulator evidence (fixed build):**
- (2) catalog "Ana&Co": title "Ana&Co", back arrow (`arrow_back_outlined`) at top-left, no hamburger. Cold start "Zed Quartet": title "Zed Quartet", back arrow.
- Arrow tap on catalog returns to the Artists list (Zed catalog → Artists list).
- (3) device back from catalog → Artists list (app stays open). Device back from Artists → Venues. Device back on Venues → "Exit MyVocaList?" sheet (Cancel closes it; app stays open).
- Flyout Songs (global): title "Songs", hamburger, "No song registered" (DB was reset by reinstall; no songs).
- Device back from global Songs → Venues (no exit).
- logcat: 0 `FATAL EXCEPTION` / `F AndroidRuntime` lines for the app process.
- Not verified on emulator: catalog with real catalog rows (no UI path creates catalog rows; Phase 15 stub). Round 2 data showed rows display via the same code path, unchanged here.
- Note: the emulator DB was reset by the reinstall; the two artists were recreated through the Artist form.

**Still open for Helder (device check):**
1. Catalog page: back arrow shown; arrow and device back return to Artists; device back from Artists/Venues shows exit sheet.
2. Title for an artist with catalog rows (cannot be seeded from the UI) — expected to show the name.
3. Global Songs from the flyout: "Songs" with hamburger.

### Traceability (round 3)
| AC / case | Implementation | Test / evidence |
|-----------|----------------|-----------------|
| artists-songs AC-9.1 (catalog opens for that artist) | `ArtistsViewModel.NavigateToCatalogAsync` → `INavigationService.PushAsync` | `ArtistsViewModelBug086Tests.ViewCatalog_PushesRelativeSongsRouteWithArtistContext`; emulator (Zed, Ana&Co) |
| BUG-086 r3 (2) back arrow in catalog | `SongsViewModel.ApplyAppBarNavigation`, `SongsPage.OnNavigatedTo` | `ApplyAppBarNavigation_CatalogMode_ShowsBackArrowThatPopsPage`; emulator screenshot 22 |
| BUG-086 r3 (3) device back / exit sheet | Push keeps Venues as Shell root (no change to AppShell) | emulator: back to Artists, back to Venues, exit sheet on Venues |
| BUG-086 r3 (1) title on first load (query before push) | `SongsViewModel.ApplyQueryAttributes` before `PushAsync` | `ApplyQueryAttributes_BeforeFirstLoad_InitialLoadTargetsThatArtistWithTitle`; emulator title on cold start |
| zero catalog rows keeps name as title | — | `ApplyQueryAttributes_ArtistWithZeroCatalogRows_KeepsNameAsTitle`; emulator "Zed Quartet" + "No song registered" |

### Changed files:
- `MyVocaList/UI/Services/INavigationService.cs`
- `MyVocaList/UI/Services/NavigationService.cs`
- `MyVocaList/UI/ViewModels/AppShellViewModel.cs`
- `MyVocaList/UI/ViewModels/ArtistsViewModel.cs`
- `MyVocaList/UI/ViewModels/SongsViewModel.cs`
- `MyVocaList/UI/Pages/Songs/SongsPage.xaml.cs`
- `MyVocaList.Tests/Unit/ViewModels/ArtistsViewModelBug086Tests.cs`
- `MyVocaList.Tests/Unit/ViewModels/SongsViewModelBug086Tests.cs`
- `Docs/Management/BusinessFeatures/artists-songs/bugs/2026-11-12-BUG-086-artists-list-trailing-catalog-button-does-nothing-successor-of-bug-015-019-028/task-log.md`

### Build notes (round 3)
Build: `dotnet build MyVocaList/MyVocaList.csproj -c Release -f net10.0-android -t:Install` exit 0 (install verified: lastUpdateTime updated, versionName 0.2.0-alpha.0.52) | Tests: 975 passed, 0 failed | Commit SHA: see branch head `fix/bug-086-round3`
Files re-read after edit: `NavigationService.cs` and `INavigationService.cs` (written in full, content as intended), `AppShellViewModel.cs` (lines 1-70 read after edit), `SongsPage.xaml.cs` (Edit applied, then built).

### Earlier proposal (superseded by the implementation above)

### Static diagnosis from code reading (hypotheses, NOT reproduced)
- **(2) hamburger instead of back:** `CrudListPageBase.OnNavigatedTo` (`UI/Pages/Base/CrudListPageBase.cs`) unconditionally sets
  `AppBarNavigationIcon = "menu_outlined"` and the hamburger command on every navigation. Its comment states CRUD lists are
  always top-level, and the catalog is the first pushed CRUD sub-page. The catalog page therefore always shows the hamburger.
  Fixing this inside the base class touches a governed component (`component-safety-gate.md`), so it is out of bugfix scope.
- **(3) device back closes the app:** `ArtistsViewModel.NavigateToCatalogAsync` uses the absolute `//songs?...` route. An absolute
  route replaces the navigation stack, so Songs becomes the root page. `AppShell.OnBackButtonPressed` shows the exit confirmation
  only when `CurrentPage is VenuesPage` at root, so back on a root Songs page exits without confirmation. The flyout path
  (`AppShellViewModel.NavigateAsync`) uses `PushAsync` on top of Venues, so the stack keeps Venues as the confirmation root.
- **(1) empty title:** the round-2 fix covers the stale-name case. A separate cause is possible on the push path: the query is
  applied after `PushAsync`, and `OnAppearing` (which starts `InitializeAsync`) may run before the query arrives. Title and list
  ordering on device is unverified.

### Proposal (needs Helder's decision, not implemented)
1. Route the catalog through a push-based method on `INavigationService` (resolve the page, apply the query BEFORE `PushAsync`,
   then push), reusing the `AppShellViewModel.NavigateAsync` logic. This keeps Venues as the stack root (fixes 3).
2. Make the back/hamburger choice context-aware. A `SongsPage` override of `OnNavigatedTo` could set the back arrow in catalog mode
   after `base.OnNavigatedTo`, which avoids editing the governed base class. This needs a design decision (page override vs. governed change).
3. Re-verify (1) on the emulator once it is available, using a freshly created artist and songs.
