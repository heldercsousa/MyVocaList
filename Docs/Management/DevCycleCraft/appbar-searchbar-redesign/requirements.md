# Requirements — AppBar / SearchAppBar Interaction Redesign: persistent search bar

**Feature folder:** `Docs/Management/DevCycleCraft/appbar-searchbar-redesign/`
**Decision basis:** `2026-07-19-persistent-searchbar-decision.md` (Helder-approved 2026-07-19)
**Consumer map & code inventory:** Explore report 2026-07-19 (summarized in `design.md § Current-state inventory`)

## User stories

- **US-1:** As an admin using any CRUD list page, I want the search field always visible so I can filter the list without first discovering a hidden icon.
- **US-2:** As an admin, I want the back button/gesture to always mean "navigate back", never "dismiss a search mode", so navigation is predictable.
- **US-3:** As a developer, I want the search bar wired once inside `CrudListView` so new CRUD pages get standard search with zero per-page wiring.

## Acceptance criteria

| ID | Criterion |
|----|-----------|
| REQ-SEARCHBAR-01 | Each of the 4 CRUD list pages (Venues, People, Artists, Songs) shows a persistent search bar docked at the top of the list content area, visible on page load without any user action. |
| REQ-SEARCHBAR-02 | The search bar matches the M3 standalone search bar spec (`m3-appbars.md § M3 Search (standalone/detached)`): 56dp height, pill shape (CornerRadius 28), `SurfaceContainerLow` background, 16dp horizontal margins, leading `search_outlined` icon (non-interactive), trailing auto-clear icon, bodyLarge typography, `ReturnType="Search"`. |
| REQ-SEARCHBAR-03 | The search bar has NO leading back arrow and does NOT auto-focus on page load (keyboard must not open until the user taps the field). |
| REQ-SEARCHBAR-04 | Typing in the search bar filters the list inline with the existing debounce behavior (`TriggerSearchDebounce`); clearing the text restores the unfiltered list. Behavior identical to the pre-change `SearchText` pipeline. |
| REQ-SEARCHBAR-05 | `SmallAppBar` remains the sole `Shell.TitleView` occupant on all 4 CRUD pages; its search action icon (`Action1Icon="search_outlined"` / `OpenSearchCommand`) is removed. Title/subtitle/selection-count and navigation-icon behavior (hamburger on root via `CrudListPageBase.OnNavigatedTo`) are unchanged. |
| REQ-SEARCHBAR-06 | `SearchAppBar` no longer appears on any CRUD list page; the `IsSearchMode` swap (`Grid` + `InverseBoolConverter` in `Shell.TitleView`) is removed from all 4 pages. |
| REQ-SEARCHBAR-07 | `IsSearchMode`, `OpenSearchCommand`, and `CloseSearchCommand` are removed from `CrudListViewModelBase` and `ICrudListViewModel`; `SearchText`, debounce plumbing, `IsScrolled`, and `IsEmptyNoResults` are kept. |
| REQ-SEARCHBAR-08 | `CrudListPageBase.OnBackButtonPressed` no longer contains the `IsSearchMode` branch: back closes an open confirm sheet, else performs default navigation. Hardware/gesture back never touches search state. |
| REQ-SEARCHBAR-09 | The search bar is a `CrudListView` internal: `CrudListView` gains a TwoWay `SearchText` bindable property; the existing `SearchPlaceholder` property becomes the live placeholder. Pages pass no other search wiring. |
| REQ-SEARCHBAR-10 | On ArtistsPage, the search bar and the existing `FilterContent` chips both render without overlap: search bar row above the filter-chips row, both above the list. |
| REQ-SEARCHBAR-11 | Lift-on-scroll: when the list is scrolled (`IsScrolled` true), the search bar background transitions `SurfaceContainerLow` → `SurfaceContainer` (same mechanism as `AppBarBase.IsElevated`). |
| REQ-SEARCHBAR-12 | The empty-search-results state (`IsEmptyNoResults` EmptyState) continues to work with the new input path. |
| REQ-SEARCHBAR-13 | The 4 picker pages (SongPickerPage, ArtistPickerPage, QueueSongPickerPage, YouTubeSearchPage) are UNCHANGED and keep compiling/working — `SearchAppBar` stays alive for them. |
| REQ-SEARCHBAR-14 | Unit tests: `CrudListViewModelBaseTests` updated — swap-member tests removed/replaced; `SearchText` → debounce → query pipeline remains covered. |

## Validation rules

- No new native dialogs; no non-English text; MD3 terminology only (component name per MD3: **Search bar** → component `SearchBar`).
- Governed components touched: `SmallAppBar` (consumer XAML only — its own file is untouched), `SearchAppBar` (removed from CRUD consumers only — component file untouched), `CrudListView` (modified). Four-gate governance applies (see `design.md § Governance`).

## Out of scope

- Retiring/deleting the `SearchAppBar` component itself — blocked by the 4 picker pages; registered as a follow-up (BACKLOG: picker-page search migration) after this feature ships.
- Full-screen MD3 SearchView (suggestions/history) — not needed for local list filtering.
- `Search Pattern Standardization` (2026-06 BACKLOG row) — this feature informs it but does not execute it.
- Hiding the search bar during multi-select (design question raised in Explore report; resolved: bar stays visible — see design.md D-3).
- Guideline amendments to `crud-appbar-list-toolbar.md` / `m3-appbars.md` are IN scope (final task) since the code change invalidates the current written law.
