# Search Page Component — Tasks

**Feature:** Search Page Component  
**Status:** Spec  
**Date:** 2026-06-03  

---

## Phase 1 — Contracts (no dependencies)

- [ ] **Add WeakReferenceMessenger message records** [P]
  - Produces: `MyVocaList.Contracts/Messages/ArtistPickedMessage.cs`, `SongPickedMessage.cs`, `YouTubeVideoPickedMessage.cs`
  - Consumes: `MusicSearchResultDto`, `YouTubeSearchResultDto`
  - Risk: Low
  - Files owned: the 3 message files
  - Demo: files compile; records are accessible from the MAUI project

- [ ] **Extend SearchAppBar with SearchCommand** [P]
  - Produces: updated `MyVocaList/UI/Components/AppBars/SearchAppBar.xaml.cs` (add `SearchCommand` bindable property, wire to `searchEdit.Completed`)
  - Consumes: nothing new
  - Risk: Low — additive only; existing usage is unaffected
  - Files owned: `SearchAppBar.xaml.cs`
  - Demo: existing pages build and behave identically; `SearchCommand` property is visible in XAML intellisense

---

## Phase 2 — Picker ViewModels (depends on Phase 1)

- [ ] **Implement ArtistPickerViewModel** [P]
  - Produces: `MyVocaList/UI/ViewModels/ArtistPickerViewModel.cs`
  - Consumes: `ArtistPickedMessage`, `IMusicMetadataService.SearchArtistsAsync`, `IMessenger`
  - Risk: Medium — loading discipline and CancellationToken must follow the design contract exactly
  - Files owned: `ArtistPickerViewModel.cs`
  - Demo: unit tests pass (see Phase 2 tests below)

- [ ] **Implement SongPickerViewModel** [P]
  - Produces: `MyVocaList/UI/ViewModels/SongPickerViewModel.cs`
  - Consumes: `SongPickedMessage`, `IMusicMetadataService.SearchSongsAsync`, `IMessenger`
  - Risk: Medium
  - Files owned: `SongPickerViewModel.cs`
  - Demo: unit tests pass

- [ ] **Implement YouTubeSearchViewModel** [P]
  - Produces: `MyVocaList/UI/ViewModels/YouTubeSearchViewModel.cs`
  - Consumes: `YouTubeVideoPickedMessage`, `IYouTubeService`, `IMessenger`
  - Risk: Medium
  - Files owned: `YouTubeSearchViewModel.cs`
  - Demo: unit tests pass

---

## Phase 2 — ViewModel Tests (parallel with Phase 2 ViewModels; Tester writes first)

- [ ] **Write ArtistPickerViewModel tests** [P]
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

- [ ] **Write SongPickerViewModel tests** [P]
  - Produces: `MyVocaList.Tests/Unit/ViewModels/SongPickerViewModelTests.cs`
  - Risk: Low
  - Files owned: test file
  - Same key cases as above; also: `SongTitle = null` from DTO maps to empty string in `SongPickedMessage`

- [ ] **Write YouTubeSearchViewModel tests** [P]
  - Produces: `MyVocaList.Tests/Unit/ViewModels/YouTubeSearchViewModelTests.cs`
  - Risk: Low
  - Files owned: test file

---

## Phase 3 — Picker Pages (depends on Phase 2 ViewModels)

- [ ] **Implement ArtistPickerPage** [P]
  - Produces: `MyVocaList/UI/Pages/Artists/ArtistPickerPage.xaml` + `.xaml.cs`
  - Consumes: `ArtistPickerViewModel`, `SearchAppBar` (with new `SearchCommand`), `ListItem`, `EmptyState`, `dx:ShimmerView`
  - Risk: Low
  - Files owned: both page files
  - Demo: navigating to `artist-picker` shows page; entering a query and tapping search shows loading skeleton, then `ListItem` results; tapping a result pops and pre-fills the artist name field

- [ ] **Implement SongPickerPage** [P]
  - Produces: `MyVocaList/UI/Pages/Songs/SongPickerPage.xaml` + `.xaml.cs`
  - Consumes: `SongPickerViewModel`
  - Risk: Low
  - Files owned: both page files
  - Demo: results show two-line `ListItem` (title + artist)

- [ ] **Implement YouTubeSearchPage** [P]
  - Produces: `MyVocaList/UI/Pages/Songs/YouTubeSearchPage.xaml` + `.xaml.cs`
  - Consumes: `YouTubeSearchViewModel`
  - Risk: Low
  - Files owned: both page files
  - Demo: results show `ListItem` with thumbnail image in leading slot, title + channel/duration

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
