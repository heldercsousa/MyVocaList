# Design — Persistent SearchBar in CrudListView (bar-swap removal)

**Decision basis:** `2026-07-19-persistent-searchbar-decision.md`. Requirements: `requirements.md`.

## Current-state inventory (Explore report 2026-07-19)

- **Bar swap** on 4 CRUD pages (Artists, Songs, People, Venues): `SmallAppBar` + `SearchAppBar` stacked in `Shell.TitleView`, toggled by `IsSearchMode`. The `x:Name`s are dead (no code-behind references).
- **Swap machinery** in `CrudListViewModelBase`: `IsSearchMode` (line 26), `OpenSearchCommand` (65/79), `CloseSearchCommand` (66/80, `CloseSearch()` 344-348). Interface `ICrudListViewModel`: `IsSearchMode` (10), `CloseSearchCommand` (13). `CrudListPageBase.OnBackButtonPressed` (85-98) branch 2 executes `CloseSearchCommand` when `IsSearchMode`.
- **Kept plumbing:** `SearchText` + `OnSearchTextChanged` debounce (`TriggerSearchDebounce` 251-269), `IsScrolled`, `IsEmptyNoResults`, `AppBarTitle`/`AppBarSubtitle` derivations, `OnNavigatedTo` hamburger logic.
- **CrudListView layout:** root `Grid` — Row 0 `filterRow` (FilterContent host, Auto/0), Row 1 list + overlays. `SearchPlaceholder` BP exists but is dead ("informational — not rendered"). No `SearchText` BP yet.
- **SearchAppBar reusables:** flat `dxe:TextEdit` config (transparent borders, `ClearIconVisibility="Auto"`, `ReturnType="Search"`), `SearchText`/`Placeholder` BPs, `AppBarBase.IsElevated` + `UpdateContainerColor()`. NOT reused: back-arrow leading icon + `BackCommand`, auto-focus-on-visible.
- **Constraint discovered:** 4 picker pages (SongPicker, ArtistPicker, QueueSongPicker, YouTubeSearch) use `SearchAppBar` as sole TitleView with non-CRUD ViewModels → **SearchAppBar component must stay**; only its CRUD consumption is removed.

## Architecture

### New component: `SearchBar` (MD3 "Search bar", standalone/docked)

`MyVocaList/UI/Components/AppBars/SearchBar.xaml(.cs)`, subclassing `AppBarBase` (reuses `IsElevated`/`UpdateContainerColor` mechanism; action slots unused).

| Aspect | Value |
|---|---|
| Container | `dx:DXBorder` CornerRadius 28, HeightRequest 56, `Margin="16,8"` (16dp horizontal, 8dp vertical) |
| Background | `SurfaceContainerLow`; `IsElevated` → `SurfaceContainer` (REQ-11) |
| Leading | `search_outlined` icon, `OnSurfaceVariant`, non-interactive (no button) |
| Input | `dxe:TextEdit` transplanted from SearchAppBar: transparent bg/borders, bodyLarge 16sp, `ClearIconVisibility="Auto"`, `ReturnType="Search"`, `Keyboard="Text"` |
| BPs | `SearchText` (TwoWay), `Placeholder` — same shapes as SearchAppBar's |
| No | back arrow, `BackCommand`, auto-focus on load/visibility (REQ-03) |

MD3 naming: official component name is "Search bar" → class `SearchBar` (no invented names). DevExpress-first satisfied: built from `dx:DXBorder` + `dxe:TextEdit`.

Naming note: `SearchBar` collides with stock `Microsoft.Maui.Controls.SearchBar` — accepted deliberately (MD3-name fidelity wins per the terminology constraint). XAML must always use the `appbars:` prefix; the stock control is never used in this codebase (DevExpress-first), so bare `<SearchBar>` is a review flag.

### CrudListView integration (REQ-09, REQ-10)

- New Row inserted at top of `rootGrid`: Row 0 = `SearchBar`, Row 1 = `filterRow` (FilterContent chips), Row 2 = list + overlays (row indices of existing children shift by one).
- New BPs on `CrudListView`: `SearchText` (TwoWay, propagates to internal `SearchBar.SearchText`), `IsSearchBarElevated` bound from page (`IsScrolled`) or forwarded internally. Existing `SearchPlaceholder` re-pointed to `SearchBar.Placeholder` (doc-comment updated — no longer "informational").
- Page usage after change (Venues reference):
  ```xml
  <views:CrudListView
      ItemsSource="{Binding Venues}"
      SearchText="{Binding SearchText, Mode=TwoWay}"
      SearchPlaceholder="Search venues..."
      ... />
  ```
- `Shell.TitleView` shrinks to a bare `SmallAppBar` (no Grid, no converter, no `x:Name`).

### Deletions

- ViewModels: `IsSearchMode`, `OpenSearchCommand`, `CloseSearchCommand`, `CloseSearch()` from `CrudListViewModelBase`; `IsSearchMode` + `CloseSearchCommand` from `ICrudListViewModel`.
- `CrudListPageBase.OnBackButtonPressed`: remove branch 2 (search dismiss). `Shell.BackButtonBehavior` suppression stays (still needed for the confirm-sheet branch).
- Page XAML: `SearchAppBar` element, TitleView `Grid`+converter, `Action1Icon`/`Action1Command` search entries on `SmallAppBar`.

## Design decisions

- **D-1 — SearchAppBar survives for pickers.** Retirement is a follow-up feature (picker migration), registered on BACKLOG at ship time. This feature only removes CRUD consumption.
- **D-2 — Search bar lives inside CrudListView, not in page XAML.** Single wiring point; consistent with CrudListView already owning toolbar/FAB/empty states.
- **D-3 — Search bar stays visible during multi-select.** Selection count lives in the app bar title (unchanged); hiding the bar would cause layout jumps and hide an active filter. No mode-coupling.
- **D-4 — Elevation source.** `CrudListView` owns the `Scrolled` event already (`OnCollectionViewScrolled` lives at page-base level via `IsScrolled`); the page binds `IsScrolled` → CrudListView `IsSearchBarElevated`. (Implementor may instead forward internally if the scroll handler already lives in CrudListView — verify at implementation; either satisfies REQ-11.)
- **D-5 — `CloseSearch()` text-clear semantics.** The old back-arrow cleared text on dismiss. With a persistent bar the trailing clear icon covers this; no replacement API needed.

## Governance (component-change four gates)

1. **Dedicated task + MD3 review:** this feature IS the dedicated task; MD3 review anchored to `m3-appbars.md § M3 Search (standalone/detached)` + the decision record's MD3 validation.
2. **Consumer map:** Explore report 2026-07-19 §1 (grep-derived, not memory) — recorded above and in `task-log.md` at implementation.
3. **Per-consumer risk assessment:**

| Consumer | Risk | Verification |
|---|---|---|
| VenuesPage | Lowest — title only. Back-path re-check after branch removal (old BUG-007 double-arrow rationale). | Emulator: search, clear, back gesture, confirm-sheet back priority |
| PeoplePage | Selection-count title with bar visible during multi-select (D-3). | Emulator: select items, verify title + bar coexist |
| ArtistsPage | FilterContent chips row must stack below the search bar (REQ-10). | Emulator: chips visible + functional under search bar; combined filter+search |
| SongsPage | Title+Subtitle + catalog mode crowding above a persistent bar. | Emulator: catalog mode (ArtistName title) + search bar layout |
| Picker pages ×4 | Must remain untouched/compiling (REQ-13). | Build + open each picker |
| CrudListViewModelBaseTests | Compile break on deleted members. | Test run green after update (REQ-14) |

4. **Helder approval:** decision approved 2026-07-19 ("I approve your words"); spec approval pending (this document's review gate).

## Guideline amendments (final task, `amend:` commits)

- `.claude/library/crud-appbar-list-toolbar.md`: replace the "Search belongs in Shell.TitleView via SearchAppBar" law with the persistent-SearchBar-in-CrudListView law; update the standard-configuration XAML.
- `.claude/library/m3-appbars.md`: promote "M3 Search (standalone/detached)" to implemented standard; mark "Pattern: Search replaces app bar" as retired for CRUD pages (still valid for pickers until their migration).
- `.claude/library/component-safety-gate.md`: add `SearchBar` to the governed list the moment the second consumer binds (it ships with 4 → governed at birth of consumption; add in the same implementation commit).

## Rollout order

Venue (pilot, lowest risk) → People → Artists (FilterContent) → Songs (subtitle/catalog), matching the Form & Autocomplete Overhaul convention. Component + base-class changes land first (they are shared), then per-page conversion is mechanical.
