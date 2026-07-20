# Tasks — Persistent SearchBar in CrudListView

> Ordering (rev. 2 after spec review): component → CrudListView → page conversions → base-machinery deletion LAST, so every incremental build stays green (pages stop referencing the swap members before the members are deleted). Rollout Venue → People → Artists → Songs. Incremental XAML rule applies: one file → build → next. All code tasks run in a git worktree off `develop`.

- [x] **T1 — Create `SearchBar` component** (`UI/Components/AppBars/SearchBar.xaml(.cs)`, subclass `AppBarBase`): 56dp pill, `SurfaceContainerLow`, leading `search_outlined` (non-interactive), transplanted `TextEdit` config, `SearchText`/`Placeholder` BPs, `IsElevated` → `SurfaceContainer`, no auto-focus, no BackCommand.
  - Produces: `SearchBar` type · Consumes: `AppBarBase` · Risk: Low (new file) · Files owned: SearchBar.xaml, SearchBar.xaml.cs · Demo: component renders in isolation · ACs: REQ-02, REQ-03, REQ-11
- [x] **T2 — Integrate `SearchBar` into `CrudListView`**: new top row; `SearchText` (TwoWay) + elevation BPs; re-point `SearchPlaceholder` to the live placeholder (update its doc-comment); verify FilterContent row stacks below.
  - Produces: CrudListView search surface · Consumes: T1 · Risk: Medium (governed component, 4 consumers) · Files owned: CrudListView.xaml, CrudListView.xaml.cs · Demo: all 4 pages show the bar with zero page changes yet · ACs: REQ-01, REQ-09, REQ-10, REQ-12
- [x] **T3 — Convert VenuesPage (pilot)**: TitleView → bare SmallAppBar (drop Grid/converter/SearchAppBar/Action1 search); pass `SearchText` to CrudListView. Build green (swap members still exist but unreferenced by this page).
  - ACs: REQ-04, REQ-05, REQ-06 (Venues) · Files owned: VenuesPage.xaml · Build after this file.
- [x] **T4 — Convert PeoplePage** (same recipe). ACs: REQ-04/05/06 (People) · Files owned: PeoplePage.xaml · Build after this file.
- [x] **T5 — Convert ArtistsPage** (verify chips stacking, REQ-10). Files owned: ArtistsPage.xaml · Build after this file.
- [x] **T6 — Convert SongsPage** (verify subtitle/catalog layout). Files owned: SongsPage.xaml · Build after this file.
- [x] **T7 — Remove swap machinery from base layer** (now reference-free): `CrudListViewModelBase` (delete `IsSearchMode`/`OpenSearchCommand`/`CloseSearchCommand`/`CloseSearch()`), `ICrudListViewModel` (delete 2 members), `CrudListPageBase.OnBackButtonPressed` (delete search branch). Expected compile break only in `CrudListViewModelBaseTests` — fixed by T8 in the same commit.
  - Produces: cleaned base contract · Consumes: T3–T6 · Risk: Medium · Files owned: CrudListViewModelBase.cs, ICrudListViewModel.cs, CrudListPageBase.cs · Demo: back gesture = confirm-sheet else navigation · ACs: REQ-07, REQ-08
- [x] **T8 — Update unit tests** (same commit as T7): `CrudListViewModelBaseTests` — remove/replace swap-member tests; keep/extend `SearchText` debounce pipeline coverage (Level B). Full solution build + tests green here.
  - ACs: REQ-14 · Files owned: CrudListViewModelBaseTests.cs
- [x] **T9 — Verify picker pages untouched** (build + smoke: SongPicker, ArtistPicker, QueueSongPicker, YouTubeSearch still use SearchAppBar). AC: REQ-13
- [x] **T10 — Guideline amendments** (`amend:` commit + changelog): `crud-appbar-list-toolbar.md` law rewrite, `m3-appbars.md` promotion/retirement notes, `component-safety-gate.md` adds `SearchBar` to governed list.
- [x] **T11 — BACKLOG follow-up registration**: new row for picker-page SearchAppBar migration/retirement; update parent redesign row status; Helder emulator smoke test gate before ✅.

**Sequential-only files touched:** none from the registry (no MauiProgram/AppShell/DbContext changes — `SearchBar` needs no DI registration; it's a XAML component).
**Helder manual gate:** emulator smoke test across the 4 pages (per-consumer verification table in `design.md § Governance`).
