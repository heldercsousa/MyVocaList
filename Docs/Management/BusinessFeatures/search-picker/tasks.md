# Search Page Component — Tasks

**Feature:** Search Page Component  
**Status:** In Progress  
**Date:** 2026-06-03  
**Last updated:** 2026-06-04

> **Session state (2026-06-04):** Phase 1 in progress — `Contracts/Messages/` folder created, files not yet committed (build blocked by unrelated `CrudListPageBase.cs` error in parallel branch). Phase 3 split into 3 sequential tasks (one agent per page). Resume from Phase 1.  

---

## Phase 1 — Contracts (no dependencies)

- [x] **Add WeakReferenceMessenger message records** [SEQUENTIAL]
  - Produces: `Contracts/Messages/ArtistPickedMessage.cs`, `SongPickedMessage.cs`, `YouTubeVideoPickedMessage.cs`
  - Consumes: `MusicSearchResultDto` (`Contracts/DTOs/MusicSearchResultDto.cs`), `YouTubeSearchResultDto` (`Contracts/DTOs/List/YouTubeSearchResultDto.cs`)
  - Risk: Low
  - Files owned: the 3 message files
  - Demo: `dotnet build MyVocaList.sln` passes; records accessible from MAUI project
  - **State:** `Contracts/Messages/` folder exists, files not yet written. Start here.

---

## Phase 2 — Picker ViewModels (depends on Phase 1)

- [x] **Implement ArtistPickerViewModel** [P]
  - Produces: `MyVocaList/UI/ViewModels/ArtistPickerViewModel.cs`
  - Consumes: `ArtistPickedMessage`, `IMusicMetadataService.SearchArtistsAsync`, `IMessenger`
  - Risk: Medium — loading discipline and CancellationToken must follow the design contract exactly
  - Files owned: `ArtistPickerViewModel.cs`
  - Demo: unit tests pass (see Phase 2 tests below)

- [x] **Implement SongPickerViewModel** [P]
  - Produces: `MyVocaList/UI/ViewModels/SongPickerViewModel.cs`
  - Consumes: `SongPickedMessage`, `IMusicMetadataService.SearchSongsAsync`, `IMessenger`
  - Risk: Medium
  - Files owned: `SongPickerViewModel.cs`
  - Demo: unit tests pass

- [x] **Implement YouTubeSearchViewModel** [P]
  - Produces: `MyVocaList/UI/ViewModels/YouTubeSearchViewModel.cs`
  - Consumes: `YouTubeVideoPickedMessage`, `IYouTubeService`, `IMessenger`
  - Risk: Medium
  - Files owned: `YouTubeSearchViewModel.cs`
  - Demo: unit tests pass

---

## Phase 2 — ViewModel Tests (parallel with Phase 2 ViewModels; Tester writes first)

- [x] **Write ArtistPickerViewModel tests** [P]
  - Produces: `MyVocaList.Tests/Unit/ViewModels/ArtistPickerViewModelTests.cs`
  - Consumes: `ArtistPickerViewModel` interface (tests written Red-first)
  - Risk: Low
  - Files owned: test file
  - Key cases:
    - `SearchCommand` with empty text does nothing (no service call)
    - `SearchCommand` sets `IsLoading = true` before first await
    - `SearchCommand` clears prior `Results` before API call
    - `SearchCommand` cancels prior `CancellationToken` when called again mid-flight
    - On success: `Results` populated, `HasResults = true`, `HasSearched = true`, `IsLoading = false`
    - On empty result: `HasResults = false`, `HasSearched = true`, `IsLoading = false`
    - On exception: `IsLoading = false`, `HasSearched = true`, `HasResults = false`, exception logged
    - `SelectResultCommand` sends `ArtistPickedMessage` via injected `IMessenger`
  - Test isolation: inject `IMessenger` via constructor; use `new WeakReferenceMessenger()` (not `.Default`) in tests

- [x] **Write SongPickerViewModel tests** [P]
  - Produces: `MyVocaList.Tests/Unit/ViewModels/SongPickerViewModelTests.cs`
  - Risk: Low
  - Files owned: test file
  - Same key cases as above; also: `SongTitle = null` from DTO maps to empty string in `SongPickedMessage`

- [x] **Write YouTubeSearchViewModel tests** [P]
  - Produces: `MyVocaList.Tests/Unit/ViewModels/YouTubeSearchViewModelTests.cs`
  - Risk: Low
  - Files owned: test file

---

## Phase 3 — Picker Pages (depends on Phase 2 ViewModels) [SEQUENTIAL — one agent per page]

> **Split rationale:** XAML pages share a visual pattern but differ in data shape and list item layout. Each page is done by a fresh agent. 3b and 3c use 3a's committed XAML as their reference — they must not start until 3a is committed and building. Full agent brief for each sub-task: see `task-3a-artist-picker-page.md`, `task-3b-song-picker-page.md`, `task-3c-youtube-search-page.md`.

- [ ] **3a — Implement ArtistPickerPage** [SEQUENTIAL — first]
  - Produces: `MyVocaList/UI/Pages/Artists/ArtistPickerPage.xaml` + `.xaml.cs`
  - Consumes: `ArtistPickerViewModel`, `SearchAppBar` (`Action1Command`/`Action1Icon` slots), `ListItem`, `EmptyState`, `dx:ShimmerView`
  - Risk: Medium — establishes the XAML pattern all three pages share; get it right first
  - Files owned: both page files only — do NOT touch Routes.cs, AppShell, MauiProgram (Phase 4)
  - Demo: page compiles and builds; XAML structure matches design.md pattern
  - **Agent brief:** `Docs/Management/BusinessFeatures/search-picker/task-3a-artist-picker-page.md`

- [ ] **3b — Implement SongPickerPage** [SEQUENTIAL — after 3a committed]
  - Produces: `MyVocaList/UI/Pages/Songs/SongPickerPage.xaml` + `.xaml.cs`
  - Consumes: `SongPickerViewModel`, ArtistPickerPage.xaml as XAML reference
  - Risk: Low — follows 3a pattern; only difference is two-line ListItem (Headline=SongTitle, SupportingText=ArtistName)
  - Files owned: both page files only
  - Demo: page compiles; two-line list item visible in XAML
  - **Agent brief:** `Docs/Management/BusinessFeatures/search-picker/task-3b-song-picker-page.md`

- [ ] **3c — Implement YouTubeSearchPage** [SEQUENTIAL — after 3b committed] 🔴 BLOCKED
  - Produces: `MyVocaList/UI/Pages/Songs/YouTubeSearchPage.xaml` + `.xaml.cs`
  - Consumes: `YouTubeSearchViewModel`, ArtistPickerPage.xaml as XAML reference, `YouTubeSearchResultDto` (VideoId, Title, ChannelName, DurationSeconds, ThumbnailUrl)
  - Risk: Medium — leading image slot in ListItem requires verifying `ListItemLeadingImage` component API; SecondsToMinutesConverter for duration
  - Files owned: both page files only
  - Demo: page compiles; ListItem shows thumbnail image leading, title headline, channel/duration supporting
  - **Agent brief:** `Docs/Management/BusinessFeatures/search-picker/task-3c-youtube-search-page.md`
  - **Blocker note (2026-06-04):** Key-per-user requirement dropped — a single developer-held API key
    routed through a backend proxy is industry-standard and ToS-compliant. Remaining blocks:
    (1) SongPickerPage (3b) must be committed first; (2) quota math must be validated
    (100 searches/day default; quota increase process); (3) backend proxy decision needed for
    key security in a distributed app. See `BACKLOG.md` and `youtube-share/findings.md § Update 2026-06-04`.

---

## Phase 3d — Update coding guidelines with search picker pattern [SEQUENTIAL — after 3c committed]

- [ ] **Document search picker pattern in coding guidelines** [SEQUENTIAL]
  - Produces: updated `.claude/library/crud-pages.md` (or new `.claude/library/search-picker-pattern.md` if no suitable home exists)
  - Consumes: committed ArtistPickerPage, SongPickerPage, YouTubeSearchPage as reference implementations
  - Risk: Low — docs only, no code
  - Files owned: the guideline file only
  - What to document:
    - When to use a picker page vs inline search (API cost → explicit submit; local DB → reactive)
    - SearchAppBar wiring via `Action1Command`/`Action1Icon` (no component modification)
    - ViewModel shape: `SearchCommand` (IAsyncRelayCommand), `SelectResultCommand`, `BackCommand`, `IsLoading`, `HasResults`, `HasSearched`, `IsShowEmptyState`, `EmptyStateMessage`, `CancellationTokenSource` pattern
    - WeakReferenceMessenger result return: register before navigate, unregister after receive
    - Page structure: `SafeAreaEdges="Container"`, `Shell.NavBarIsVisible="False"`, shimmer skeleton, DXCollectionView results, EmptyState
    - ListItem variants: single-line (headline only), two-line (headline + supporting), leading image
    - Reference files: `ArtistPickerPage.xaml`, `ArtistPickerViewModel.cs`, `ArtistPickedMessage.cs`

---

## Phase 4 — Route Registration + DI [SEQUENTIAL — waits for Phase 3]

- [ ] **Register routes and DI** [SEQUENTIAL]
  - Produces: route entries for `artist-picker`, `song-picker`, `youtube-search`; transient registrations for 3 pages + 3 ViewModels; `IMessenger` singleton (if not already registered)
  - Consumes: all 3 page types
  - Risk: Low — single-writer constraint on `AppShell.xaml.cs` and `MauiProgram.cs`
  - Files owned: `AppShell.xaml.cs`, `MauiProgram.cs`
  - Demo: `GoToAsync("artist-picker")` navigates without exception

---

## Phase 5a — Wire ArtistFormPage [SEQUENTIAL — waits for Phase 4]

- [ ] **Wire ArtistFormPage XAML** [SEQUENTIAL]
  - Produces: updated `ArtistFormPage.xaml`
  - Consumes: `ListItem` component, `ArtistPickerPage` route
  - Risk: Low — XAML-only change
  - Files owned: `ArtistFormPage.xaml`
  - Demo: API search strip Border is gone; `ListItem` trigger row is in its place with search icon + "Search music database" + chevron

- [ ] **Wire ArtistFormViewModel** [SEQUENTIAL — waits for XAML above]
  - Produces: updated `ArtistFormViewModel.cs`
  - Consumes: `ArtistPickedMessage`, `IMessenger`, `ArtistPickerPage` route
  - Risk: Medium — removing existing search properties and commands; message registration must unregister correctly
  - Files owned: `ArtistFormViewModel.cs`
  - Demo: tapping trigger navigates to ArtistPickerPage; selecting result populates `ArtistName`; back without selecting leaves field unchanged

---

## Phase 5b — Wire SongFormPage [SEQUENTIAL — waits for Phase 4]

- [ ] **Wire SongFormPage XAML** [SEQUENTIAL]
  - Produces: updated `SongFormPage.xaml`
  - Consumes: `ListItem` component
  - Risk: Medium — 2 strips removed across a 275-line XAML; Paste URL section must be untouched; YouTube trigger visibility conditional on `HasYouTubeApiKey`
  - Files owned: `SongFormPage.xaml`
  - Demo: music-DB trigger row present; YouTube trigger row present (visible only when API key set); Paste URL section unchanged; no-API-key nudge unchanged

- [ ] **Wire SongFormViewModel** [SEQUENTIAL — waits for XAML above]
  - Produces: updated `SongFormViewModel.cs`
  - Consumes: `SongPickedMessage`, `YouTubeVideoPickedMessage`, `IMessenger`
  - Risk: Medium — removing 10+ properties and 4 commands; 2 new navigate commands + 2 message registrations
  - Files owned: `SongFormViewModel.cs`
  - Demo: music-DB trigger navigates to SongPickerPage, returns title + artist; YouTube trigger navigates to YouTubeSearchPage, returned video appears in `KaraokeUrls`; Paste URL Add still works

---

## Phase 6 — Cleanup + .sln Registration [SEQUENTIAL — waits for Phase 5a and 5b]

- [ ] **Register new files in MyVocaList.sln and add BACKLOG entries** [SEQUENTIAL]
  - Produces: updated `MyVocaList.sln` (all new files registered); 3 new BACKLOG entries (Card Component, Multi-type Video Links, YouTube Preview)
  - Consumes: all new file paths
  - Risk: Low
  - Files owned: `MyVocaList.sln`, `Docs/Management/BACKLOG.md`
  - Demo: all new pages, ViewModels, and spec files visible in VS Solution Explorer

---

## .sln Registration Note

Every subagent that creates files in Phases 1–5 must register those files in `MyVocaList.sln` **in the same commit** (workflow.md § subagent exit checklist). Do not defer to Phase 6. Phase 6 only handles spec files and any stragglers missed in earlier phases.

---

## Removed Properties / Commands Reference

### ArtistFormViewModel — remove

`ApiSearchText`, `IsApiSearching`, `HasApiStatusMessage`, `ApiStatusMessage`, `HasApiResults`, `ApiResults`, `SearchApiCommand`, `SelectApiResultCommand`

### SongFormViewModel — remove

`ApiSearchText`, `IsApiSearching`, `HasApiStatusMessage`, `ApiStatusMessage`, `HasApiResults`, `ApiResults`, `SearchApiCommand`, `SelectApiResultCommand`, `YoutubeSearchQuery`, `IsYouTubeSearching`, `HasYouTubeSearchStatus`, `YoutubeSearchStatus`, `SearchResults`, `SearchYouTubeCommand`, `AddFromSearchCommand`
