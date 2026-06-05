# Task Log — CRUD XAML Sharing

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
