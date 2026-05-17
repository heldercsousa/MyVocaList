# Plan: Phase 16 — Bug Fixes + API Form Integration

**Created:** 2026-05-17  
**Spec:** `Docs/specs/artists-songs/requirements.md` + `design.md`  
**Tasks file:** `Docs/specs/artists-songs/tasks.md`

---

## Context

Six issues found after Phases 10–15:

1. **Authors/Performers menu items don't navigate** — `AppShellViewModel.NavigateAsync` looks up routes in `PageTypes` via exact string match; `"artists?mode=author"` fails because the key is `"artists"`.
2. **Menu structure mismatch** — User expectation: single "Artists" entry, with in-page filter. Current: two broken separate entries.
3. **No filter UI on ArtistsPage** — `ArtistRoleFilter` enum + ViewModel support exist but no UI widget drives it.
4. **`person_outlined` icon broken** in ArtistsPage empty state.
5. **API form integration absent** — `IMusicMetadataService`, `MusicBrainzProvider`, `DeezerProvider`, `MusicMetadataService` were fully implemented and DI-registered at Phase 4 (`53fcfb7`). Phases 6/7 (UI) and 14 (UI refactor) never wired them to `ArtistFormPage` (US-4) or `SongFormPage` (US-11). No API search strip exists on either form.
6. **Double back arrow in SongsPage search mode** — `SongsPage.xaml` is missing `<Shell.BackButtonBehavior IsVisible="False" IsEnabled="False" />`. VenuesPage and ArtistsPage already have this fix.

---

## MD3 Decision: Filter chips (not tabs, not checkboxes)

From `m3.material.io/components/chips/guidelines#filter-chips`:

> "Filter chips use tags or descriptive words to filter content. They can be a good alternative to segmented buttons or checkboxes when viewing a list or search results."

- **Correct MD3 pattern for list filtering** = **Filter chip** (variant of Chips)
- When activated: leading checkmark icon appears automatically
- Multi-select: ✓ (multiple chips active = AND logic; user can filter to Authors only, Performers only, or both = All)
- Labels must be **nouns** describing categories to **include** ("Authors", "Performers" — both correct)
- Placement: horizontal row, below the app bar, above the list

**DevExpress implementation**: `dxe:FilterChipGroup` — built-in, no custom component needed.
- `ItemsSource` = list of role strings `["Authors", "Performers"]`
- `SelectedItems` = bindable collection (bound to ViewModel)
- Namespace: `dxe` (`DevExpress.Maui.Editors`) — already used in `SongFormPage.xaml`, `ArtistFormPage.xaml`

**Library files to update after implementation:**
- `.claude/library/devexpress-patterns.md` — add `FilterChipGroup` usage pattern
- `.claude/library/m3-components.md` — add Filter Chip section with MD3 terminology and guidelines

---

## Sub-phase A — Navigation + Filter UI + Quick Fixes

### A.1 — Fix `AppShellViewModel.NavigateAsync`
- Split route at `?` to extract `baseRoute` and `queryString`
- Look up `PageTypes[baseRoute]` (not the full route string)
- Create page via DI as before, then push with query: use `Shell.Current.GoToAsync($"///{baseRoute}?{queryString}")` OR apply query params manually to the page's BindingContext
- **Simpler approach**: Since `artists` is a `FlyoutItem` and routes WITH query params need Shell's query mechanism, the cleanest fix is: extract base route for PageTypes lookup, then use `Shell.Current.GoToAsync` for the full route. Requires registering Artists+Songs as `Routing.RegisterRoute` so GoToAsync can push them. Alternatively, create the page via DI and set query props on the ViewModel manually.
- **Recommended**: Parse query params and set them on the ViewModel after `PushAsync` — minimal disruption.
- **File:** `MyVocaList/UI/ViewModels/AppShellViewModel.cs`

### A.2 — Simplify menu: single "Artists" entry
- Replace two "Authors"/"Performers" entries with one "Artists" entry, route = `Routes.Artists` (no query param)
- The in-page `FilterChipGroup` handles role filtering
- **File:** `MyVocaList/Navigation/NavigationConfig.cs`

### A.3 — Add `FilterChipGroup` filter row to ArtistsPage
- Add `dxe` namespace import to `ArtistsPage.xaml` (already present in other pages)
- Add a horizontal `FilterChipGroup` below the `Shell.TitleView` Grid, above the `ShimmerView`
- `ItemsSource` = inline `x:Array` of strings: `["Authors", "Performers"]`
- `SelectedItems` bound two-way to new `SelectedRoleFilters` observable property on ViewModel
- In ViewModel: `partial void OnSelectedRoleFiltersChanged(IList value)` → map to `ArtistRoleFilter` → triggers `OnRoleFilterChanged` → reloads list
- Mapping logic: both or neither selected → `All`; only "Authors" → `AuthorsOnly`; only "Performers" → `PerformersOnly`
- **Files:** `MyVocaList/UI/Pages/Artists/ArtistsPage.xaml`, `MyVocaList/UI/ViewModels/ArtistsViewModel.cs`

### A.4 — Fix broken empty state icon
- Replace `Illustration="person_outlined"` → `Illustration="group_outlined"` (confirmed working, used in People menu)
- **File:** `MyVocaList/UI/Pages/Artists/ArtistsPage.xaml` line ~122

### A.5 — Fix double back arrow on SongsPage
- Add `<Shell.BackButtonBehavior IsVisible="False" IsEnabled="False" />` before `<Shell.TitleView>` in `SongsPage.xaml`
- The code-behind `OnBackButtonPressed()` already handles: confirm sheet dismiss → search close → default back
- **Pattern from:** `VenuesPage.xaml` lines 20–26
- **File:** `MyVocaList/UI/Pages/Songs/SongsPage.xaml`

### A.6 — Build + test
- `dotnet build` — 0 errors
- `dotnet test` — all passing

### A.7 — Update library pattern files
- `.claude/library/devexpress-patterns.md`: add `FilterChipGroup` section with full XAML example, SelectedItems binding pattern, namespace reference
- `.claude/library/m3-components.md`: add **Filter Chip** section — MD3 terminology, usage rules (nouns, multi-select, leading checkmark on select, horizontal row placement, "don't use single chip alone")

---

## Sub-phase B — API Search Strip on Forms

### Context
- `IMusicMetadataService`, `MusicMetadataService`, `MusicBrainzProvider`, `DeezerProvider` — fully implemented at Phase 4, registered in DI (`MauiProgram.cs` lines 56–76)
- `MusicSearchResultDto(ExternalId, Provider, ArtistName, SongTitle?, FeaturedArtists?)` — exists
- US-4 (artist form) and US-11 (song form) are the spec stories

### B.1 — ArtistFormViewModel: API + duplicate detection state
- Inject `IMusicMetadataService`
- Add observable properties: `ApiSearchText`, `ApiResults` (`IEnumerable<MusicSearchResultDto>`), `IsApiSearching`, `ApiStatusMessage` (error/no-results inline message)
- Add `SearchApiCommand` (AsyncRelayCommand) — calls `_metadataService.SearchArtistsAsync(ApiSearchText)`, populates `ApiResults`, handles empty/error per AC-4.3–4.4
- Add `SelectApiResultCommand` (RelayCommand<MusicSearchResultDto>) — populates `ArtistName`, stores `ExternalId`/`ExternalProvider` for save, sets `HasManualEdits = false`
- Add duplicate detection: debounced `SearchArtistsByNameAsync` (already on `IArtistService`) → `DuplicateSuggestions` list; `SelectDuplicateCommand` navigates to edit form for that artist
- `SaveAsync`: include `ExternalId`, `ExternalProvider`, `HasManualEdits` in create/update call (check if `IArtistService.CreateArtistAsync` accepts these — if not, service update needed)
- **File:** `MyVocaList/UI/ViewModels/ArtistFormViewModel.cs`

### B.2 — ArtistFormPage.xaml: API strip + duplicate suggestions UI
- Below the Name field: duplicate suggestions `AutocompleteField` (reuse existing component with `DuplicateSuggestions` binding) — shown as "Did you mean?" style list
- Below duplicate suggestions: API search strip — `dxe:TextEdit` (pre-filled with Name) + `DXButton "Search"` — triggers `SearchApiCommand`
- API status label (bound to `ApiStatusMessage`, hidden when empty)
- API results list (DXCollectionView or VerticalStackLayout with BindableLayout, up to 5 items, each row shows `ArtistName` + `Provider`, tapping fires `SelectApiResultCommand`)
- **File:** `MyVocaList/UI/Pages/Artists/ArtistFormPage.xaml`

### B.3 — SongFormViewModel: API state
- Inject `IMusicMetadataService`
- Add `ApiSearchText`, `ApiResults`, `IsApiSearching`, `ApiStatusMessage`
- Add `SearchApiCommand` — calls `_metadataService.SearchSongsAsync(ApiSearchText, artistHint: SelectedArtistName)` 
- Add `SelectApiResultCommand(MusicSearchResultDto)` — populates `SongTitle`, `FeaturedArtists`; if `ArtistName` matches a registered artist (via `IArtistService.SearchArtistsByNameAsync`), pre-fills artist and locks it (`IsArtistLocked = true`); stores `ExternalId`/`ExternalProvider`
- **File:** `MyVocaList/UI/ViewModels/SongFormViewModel.cs`

### B.4 — SongFormPage.xaml: API strip
- Below Title field: API strip (`dxe:TextEdit` pre-filled with `SongTitle` + `DXButton "Search"`)
- API status label, API results list (same pattern as B.2)
- **File:** `MyVocaList/UI/Pages/Songs/SongFormPage.xaml`

### B.5 — Build + test
- `dotnet build` — 0 errors
- `dotnet test` — all passing (no new service tests required; form VM logic is UI-layer only)

---

## Sub-phase C — Final Gate

- **C.1** End-to-end smoke test on emulator (Phase 16.1 checklist from tasks.md)
- **C.2** `dotnet build` — 0 errors
- **C.3** `/project:review`
- **C.4** Update `Docs/Changelog/changelog.md`
- **C.5** `/project:commit`

---

## Key files

| File | Sub-phase | Change |
|------|-----------|--------|
| `MyVocaList/UI/ViewModels/AppShellViewModel.cs` | A.1 | Fix NavigateAsync query-param routing |
| `MyVocaList/Navigation/NavigationConfig.cs` | A.2 | Single "Artists" menu entry |
| `MyVocaList/UI/Pages/Artists/ArtistsPage.xaml` | A.3, A.4 | FilterChipGroup + icon fix |
| `MyVocaList/UI/ViewModels/ArtistsViewModel.cs` | A.3 | `SelectedRoleFilters` + mapping |
| `MyVocaList/UI/Pages/Songs/SongsPage.xaml` | A.5 | BackButtonBehavior |
| `MyVocaList/UI/ViewModels/ArtistFormViewModel.cs` | B.1 | API + duplicate state |
| `MyVocaList/UI/Pages/Artists/ArtistFormPage.xaml` | B.2 | API strip + duplicate suggestions |
| `MyVocaList/UI/ViewModels/SongFormViewModel.cs` | B.3 | API state |
| `MyVocaList/UI/Pages/Songs/SongFormPage.xaml` | B.4 | API strip |
| `.claude/library/devexpress-patterns.md` | A.7 | FilterChipGroup pattern |
| `.claude/library/m3-components.md` | A.7 | Filter Chip MD3 section |
| `Docs/specs/artists-songs/tasks.md` | — | Add sub-phases A/B/C tasks |

## Reused without change

- `AutocompleteField` component — duplicate detection suggestions on ArtistFormPage
- `IMusicMetadataService.SearchArtistsAsync` / `SearchSongsAsync` — already DI-registered
- `MusicSearchResultDto` — DTO for API results
- `ArtistRoleFilter` enum (`All`/`AuthorsOnly`/`PerformersOnly`) — no change needed
- `OnRoleFilterChanged` in `ArtistsViewModel` — already triggers list reload
- `OnBackButtonPressed` in `SongsPage.xaml.cs` — already handles search close + confirm sheet

## Incremental edit order (per XAML safety rule)

Sub-phase A: ArtistsPage.xaml → build → SongsPage.xaml → build  
Sub-phase B: ArtistFormPage.xaml → build → SongFormPage.xaml → build
