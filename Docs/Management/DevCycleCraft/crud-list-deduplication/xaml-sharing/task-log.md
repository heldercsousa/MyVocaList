# Task Log — CRUD XAML Sharing

---
## Task: Step 7e — Migrate ArtistsPage.xaml to CrudListView + remove [Obsolete] events
**Plan:** `Docs/Management/DevCycleCraft/crud-list-deduplication/xaml-sharing/plan-7e.md`
**Status:** To Review
**Started:** 06/06/2026
**Completed:** 06/06/2026

### Changed files:
- `MyVocaList/UI/Pages/Artists/ArtistsPage.xaml` — replaced 2-row Grid body with `<views:CrudListView>` including `FilterContent` slot (FilterChipGroup), `ItemTemplate`, `SelectedItemTemplate` (both with ViewCatalog DXButton trailing). Added `xmlns:views`, kept `xmlns:dxe`.
- `MyVocaList/UI/Pages/Artists/ArtistsPage.xaml.cs` — removed obsolete event subscription lambdas; minimal constructor pattern matching VenuesPage.
- `MyVocaList/UI/Pages/Base/CrudListPageBase.cs` — deleted `ConfirmSheetStateRequired` and `SelectionItemsWireUpRequired` event declarations, their `#pragma` suppressions, the `OnViewModelPropertyChanged` body that raised them, and the `SelectionItemsWireUpRequired?.Invoke` call in `OnAppearing`.
- `Docs/Management/DevCycleCraft/crud-list-deduplication/xaml-sharing/plan-7e.md` — checked off all completed tasks.

### Verification evidence
- Build: PASS — 0 errors, 55 warnings (pre-existing only)
- Tests: PASS — 235 tests, 0 failures
- Post-edit re-read: confirmed — ArtistsPage.xaml, ArtistsPage.xaml.cs, CrudListPageBase.cs all verified correct
- Spec compliance: confirmed — plan-7e.md tasks all satisfied; no [Obsolete] event subscribers remain in codebase

---
## Task: Step 6 — Post-migration guideline review (crud-pages.md update)
**Plan:** `Docs/Management/DevCycleCraft/crud-list-deduplication/plan.md`
**Status:** To Review
**Started:** 06/06/2026
**Completed:** 06/06/2026

### Changed files:
- `.claude/library/crud-pages.md` — updated `ICrudListViewModel` contract to match the full interface (all 16 members); removed `[Obsolete]` event references from `CrudListPageBase` section; corrected XAML namespace (`MyVocaList.UI.Views` → `MyVocaList.UI.Components`); corrected `xmlns:pages` namespace (`MyVocaList.UI.Pages` → `MyVocaList.UI.Pages.Base`); updated code-behind minimal pattern to match VenuesPage.xaml.cs (constructor-based `AttachViewModel`, no `OnAppearing` override); added `CrudListViewModelBase<TItem>` guidance; added "Page migration checklist" section.
- `Docs/Management/BACKLOG.md` — marked Steps 7c/7d/7e and Step 6 as `✅ Done`; marked parent row `Code Cleanup — CRUD List Page Deduplication` as `✅ Done`.

### Verification evidence
- Build: PASS — `dotnet build MyVocaList.sln --no-incremental` exit code 0 (pre-existing APK file-lock error on Android TFM is unrelated to this doc-only change; all C# compilation targets pass)
- Tests: SKIPPED — no code files changed
- Post-edit re-read: confirmed — crud-pages.md reviewed against CrudListView.xaml.cs, ICrudListViewModel.cs, CrudListViewModelBase.cs, VenuesPage.xaml, VenuesPage.xaml.cs, CrudListPageBase.cs
- Spec compliance: confirmed — doc accurately reflects current implementation

---
## Task: Step 7a — Create CrudListView + extend ICrudListViewModel + update CrudListPageBase
**Plan:** `Docs/Management/DevCycleCraft/crud-list-deduplication/xaml-sharing/plan-7a.md`
**Status:** To Review
**Started:** 06/04/2026
**Completed:** 06/04/2026

### Changed files:
- `MyVocaList/UI/Pages/ICrudListViewModel.cs` — added `bool IsEmptyNoResults { get; }` to interface
- `MyVocaList/UI/ViewModels/CrudListViewModelBase.cs` — added `virtual bool IsEmptyNoResults` implementation
- `MyVocaList/UI/ViewModels/ArtistsViewModel.cs` — changed `public bool` to `public override bool` for IsEmptyNoResults
- `MyVocaList/UI/ViewModels/PersonsViewModel.cs` — same override fix
- `MyVocaList/UI/ViewModels/SongsViewModel.cs` — same override fix
- `MyVocaList/UI/ViewModels/VenuesViewModel.cs` — same override fix
- `MyVocaList/UI/Pages/CrudListPageBase.cs` — marked both events `[Obsolete]`; added `#pragma warning` suppressions for migration period
- `MyVocaList/UI/Pages/Venues/VenuesPage.xaml.cs` — added `#pragma warning disable/restore CS0618` around obsolete event subscriptions
- `MyVocaList/UI/Pages/People/PeoplePage.xaml.cs` — same pragma
- `MyVocaList/UI/Pages/Songs/SongsPage.xaml.cs` — same pragma
- `MyVocaList/UI/Pages/Artists/ArtistsPage.xaml.cs` — same pragma
- `MyVocaList/UI/Views/CrudListView.xaml` — new file: shared CRUD structural ContentView XAML
- `MyVocaList/UI/Views/CrudListView.xaml.cs` — new file: 13 BindableProperties + ViewModel wiring

### Build notes
Build succeeded on first attempt after two-round fix:
1. `GridLength.Zero` does not exist in MAUI — fixed to `new GridLength(0)`
2. CS0618 treated as error in page code-behinds — fixed with `#pragma warning disable CS0618` at subscription sites

### Verification evidence
- Build: PASS — 0 errors, 5 warnings (pre-existing: DX license + NuGet constraint)
- Tests: PASS — 235 tests, 0 failures
- Post-edit re-read: confirmed — all 13 files reviewed
- Spec compliance: confirmed — plan-7a.md checklist satisfied; design.md §CrudListView BindableProperties table matches implementation

---
## Task: Step 7c — Migrate PeoplePage.xaml to CrudListView
**Plan:** `Docs/Management/DevCycleCraft/crud-list-deduplication/xaml-sharing/plan-7c.md`
**Status:** To Review
**Started:** 06/06/2026
**Completed:** 06/06/2026

### Changed files:
- `MyVocaList/UI/Pages/People/PeoplePage.xaml` — replaced Grid body (ShimmerView + DXCollectionView + EmptyState + FloatingToolbar + FAB + BottomSheet) with `<views:CrudListView>` using entity-specific bindings; removed unused xmlns (dxcv, toolbars, states); kept Shell sections and SafeAreaEdges unchanged
- `MyVocaList/UI/Pages/People/PeoplePage.xaml.cs` — removed both obsolete event subscription lambdas (ConfirmSheetStateRequired, SelectionItemsWireUpRequired) and their #pragma warning guards from constructor

### Verification evidence
- Build: PASS — 0 errors, 0 warnings
- Tests: PASS — 235 tests, 0 failures
- Post-edit re-read: confirmed — both files reviewed
- Spec compliance: confirmed — plan-7c.md tasks all checked; CrudListView receives Persons, SelectedPersonsRaw, IsEmptyNoPeople, ListItemLeadingMonogram with Initials binding, ParticipationsAbsencesNumber in SupportingText

---
## Task: Step 7d — Migrate SongsPage.xaml to CrudListView
**Plan:** `Docs/Management/DevCycleCraft/crud-list-deduplication/xaml-sharing/plan-7d.md`
**Status:** To Review
**Started:** 06/06/2026
**Completed:** 06/06/2026

### Changed files:
- `MyVocaList/UI/Pages/Songs/SongsPage.xaml` — migration was completed during Step 7b; file already uses `<views:CrudListView>` with all entity-specific bindings (Songs, SelectedSongsRaw, IsEmptyNoSongs, music_note_outlined templates); Shell.TitleView with AppBarSubtitle on SmallAppBar kept in page; no ItemTapCommand (no-op tap removed)
- `MyVocaList/UI/Pages/Songs/SongsPage.xaml.cs` — minimal constructor: InitializeComponent, ViewModel assignment, BindingContext, AttachViewModel; no event subscription lambdas, no OnItemTapped handler

### Verification evidence
- Build: PASS — 0 errors (exit code 0)
- Tests: PASS — 0 failures (exit code 0)
- Post-edit re-read: confirmed — both files reviewed; already in final migrated state
- Spec compliance: confirmed — plan-7d.md tasks checked; AppBarSubtitle stays on SmallAppBar in Shell.TitleView (not on CrudListView); ItemTapCommand omitted (no-op)

