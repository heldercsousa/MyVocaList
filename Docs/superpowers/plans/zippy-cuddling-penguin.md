# Plan: Phase 16 — Bug Fixes + API Form Integration

**Created:** 2026-05-17  
**Spec:** `Docs/specs/artists-songs/requirements.md` + `design.md`  
**Tasks file:** `Docs/specs/artists-songs/tasks.md`

---

## Context

Six issues were identified after Phases 10–15:

1. **Authors/Performers menu items don't navigate** — `AppShellViewModel.NavigateAsync` does a `PageTypes.TryGetValue(route)` lookup, but the route passed is `"artists?mode=author"` (with query params). The dictionary key is just `"artists"`, so the lookup fails silently.
2. **Menu structure mismatch** — Spec / user expectation: single "Artists" entry in menu, with an in-page filter (2 tabs or 2 checkboxes). Current code: two broken separate menu entries.
3. **Missing filter UI** — `ArtistsPage.xaml` has no tab bar or filter control. `ArtistRoleFilter` enum and ViewModel support exist but nothing drives it from the UI.
4. **`person_outlined` icon broken** — Used in `ArtistsPage.xaml` empty state illustration. Not valid in this DevExpress version.
5. **API integration absent from forms** — `IMusicMetadataService`, `MusicBrainzProvider`, `DeezerProvider`, and `MusicMetadataService` were implemented and registered in DI at Phase 4 (`53fcfb7`). However, Phases 6/7 (UI) and Phase 14 (UI refactor) never wired them to `ArtistFormPage` (US-4) or `SongFormPage` (US-11). The API search strip is completely absent from both form pages.
6. **Double back arrow in SongsPage search mode** — `SongsPage.xaml` is missing `<Shell.BackButtonBehavior IsVisible="False" IsEnabled="False" />`. VenuesPage and ArtistsPage already have this fix. Without it, Shell renders its native back arrow alongside the `SearchAppBar`'s leading icon.

---

## Approach

Split into three sequential sub-phases:

### Sub-phase A — Navigation + Filter UI + Quick Fixes (Issues 1, 2, 3, 4, 6)

**A.1 — Fix `AppShellViewModel.NavigateAsync` (Issue 1)**  
Extract base route before `?` for the `PageTypes` lookup. Pass the full route string (including query) to `GoToAsync` instead of `PushAsync`. This also future-proofs any other query-param menu routes.  
- **File:** `MyVocaList/UI/ViewModels/AppShellViewModel.cs`

**A.2 — Simplify menu to single "Artists" entry (Issue 2)**  
Replace the two "Authors"/"Performers" entries with a single "Artists" entry (no query param). Route: `Routes.Artists`.  
- **File:** `MyVocaList/Navigation/NavigationConfig.cs`

**A.3 — Add filter UI on ArtistsPage (Issue 2/3)**  
Add a filter row below the app bar. Two options (user to confirm):
- **Option A: 2 tabs (Authors | Performers)** — uses `DXTabView` or a segmented chip group. No "All" tab; default state = all.  
- **Option B: 2 checkboxes (Authors ☑ | Performers ☑)** — both checked = all; uncheck one = filter. Allows "both" (artists in both roles).

`ArtistsViewModel` already has `ArtistRoleFilter` (All/AuthorsOnly/PerformersOnly) and `OnRoleFilterChanged` wired. Only the XAML binding is missing.

- **Files:** `MyVocaList/UI/Pages/Artists/ArtistsPage.xaml`  
- **ViewModel already ready** — no C# changes needed for tabs; for checkboxes, add `ShowAuthors`/`ShowPerformers` bool properties that map to `RoleFilter`.

**A.4 — Fix `person_outlined` empty state icon (Issue 4)**  
Replace `Illustration="person_outlined"` with `group_outlined` (known-working icon used in the People menu item).  
- **File:** `MyVocaList/UI/Pages/Artists/ArtistsPage.xaml` line 122

**A.5 — Fix double back arrow on SongsPage search (Issue 6)**  
Add `<Shell.BackButtonBehavior IsVisible="False" IsEnabled="False" />` to `SongsPage.xaml`. The code-behind already has `OnBackButtonPressed` handling search close and confirm sheet dismiss.  
- **File:** `MyVocaList/UI/Pages/Songs/SongsPage.xaml`  
- **Pattern from:** `VenuesPage.xaml` lines 20–26

---

### Sub-phase B — API Search Strip on Forms (Issue 5)

**Services already exist and are DI-registered** (confirmed in `MauiProgram.cs`):
- `IMusicMetadataService` / `MusicMetadataService` — provider-chain orchestrator
- `MusicBrainzProvider` (primary) + `DeezerProvider` (fallback)
- `MusicSearchResultDto(ExternalId, Provider, ArtistName, SongTitle?, FeaturedArtists?)`

**B.1 — ArtistFormPage API strip (US-4, US-3 duplicate detection)**

What to add:
- `ArtistFormViewModel`: inject `IMusicMetadataService`; add `ApiSearchText`, `ApiResults` (list of `MusicSearchResultDto`), `IsApiSearching`, `ApiStatusMessage`, `SearchApiCommand`; add `ArtistSuggestions` (duplicate detection using `IArtistService.SearchArtistsByNameAsync`), `SuggestionSelectedCommand`; add `HasManualEdits` tracking.
- `ArtistFormPage.xaml`: add API search strip below Name field (search input + "Search" button, result list, error/status message); add duplicate detection suggestions below Name field.

**B.2 — SongFormPage API strip (US-11)**

What to add:
- `SongFormViewModel`: inject `IMusicMetadataService`; add `ApiSearchText`, `ApiResults`, `IsApiSearching`, `ApiStatusMessage`, `SearchApiCommand`; add `SelectApiResultCommand` that populates Title, FeaturedArtists, and locks the Artist field if a match is found in registered artists.
- `SongFormPage.xaml`: add API search strip below Title field.

**B.3 — Build + tests green**

---

### Sub-phase C — Final Gate (was Phase 16)

**C.1** End-to-end smoke test on emulator (Phase 16.1 checklist from tasks.md)  
**C.2** Build — 0 errors  
**C.3** `/project:review`  
**C.4** Update `Docs/Changelog/changelog.md`  
**C.5** `/project:commit`

---

## Key files

| File | Change |
|------|--------|
| `MyVocaList/UI/ViewModels/AppShellViewModel.cs` | Fix NavigateAsync for query-param routes |
| `MyVocaList/Navigation/NavigationConfig.cs` | Single "Artists" entry |
| `MyVocaList/UI/Pages/Artists/ArtistsPage.xaml` | Add filter UI + fix icon |
| `MyVocaList/UI/Pages/Songs/SongsPage.xaml` | Add Shell.BackButtonBehavior |
| `MyVocaList/UI/ViewModels/ArtistsViewModel.cs` | Bind filter control (if checkboxes: add helpers) |
| `MyVocaList/UI/Pages/Artists/ArtistFormPage.xaml` | Add API strip + duplicate suggestions |
| `MyVocaList/UI/ViewModels/ArtistFormViewModel.cs` | API + duplicate state + commands |
| `MyVocaList/UI/Pages/Songs/SongFormPage.xaml` | Add API strip |
| `MyVocaList/UI/ViewModels/SongFormViewModel.cs` | API state + SearchApiCommand |
| `Docs/specs/artists-songs/tasks.md` | Add sub-phases A/B/C tasks |

## Reuse

- `AutocompleteField` component — already used in `SongFormPage` for artist autocomplete; the API results list can follow the same overlay pattern.
- `MusicMetadataService` + `MusicBrainzProvider` + `DeezerProvider` — fully implemented, just needs ViewModel injection.
- `MusicSearchResultDto` — DTO for API results already defined.
- `OnBackButtonPressed` in SongsPage.xaml.cs — already handles search close.

## Pending user decision

**Filter UI on ArtistsPage (A.3):** Tabs or checkboxes?  
- 2 tabs (Authors | Performers) — mutually exclusive, matches DXTabView spec direction  
- 2 checkboxes (Authors ☑ | Performers ☑) — combinable, allows "show both" = All
