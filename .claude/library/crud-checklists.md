# CRUD Page Design Laws — ViewModel + Code-Behind checklists (list page)

> Section file split from `crud-pages.md` on 2026-07-14 (token-scoped reads). Index + never-miss rules: `crud-pages.md`.

## ViewModel Checklist (list page)

Every list ViewModel shall have:

| Property / Command | Purpose |
|--------------------|---------|
| `ObservableRangeCollection<TDto> Items` | The list data |
| `ObservableRangeCollection<TDto> SelectedItems` | Selected rows |
| `IList SelectedItemsRaw` | Non-generic wrapper for DXCollectionView binding |
| `bool IsInitialLoading` | Drives `ShimmerView` |
| `bool IsRefreshing` | Drives pull-to-refresh |
| `bool HasMoreItems` | Drives load-more |
| `string SearchText` | Bound to `SearchAppBar` (if search is present) |
| `bool IsSearchMode` | Controls SmallAppBar ↔ SearchAppBar swap (if search is present) |
| `bool IsScrolled` | Drives `IsElevated` on both app bars |
| `int SelectedCount` | Updated by `OnSelectionChanged` in code-behind |
| `string AppBarTitle` | Derived: entity name or "N selected" |
| `bool CanEditSelected` | `SelectedCount == 1` (if edit is supported) |
| `bool CanDeleteSelected` | `SelectedCount > 0` (if delete is supported) |
| `BottomSheetState ConfirmSheetState` | Drives `ConfirmSheet` component (if destructive action exists) |
| `RefreshCommand`, `LoadMoreCommand`, `AddCommand` | Standard list commands |
| `EditSelectedCommand`, `DeleteSelectedCommand`, `SelectAllCommand` | Action commands (adapt to page needs) |
| `OpenSearchCommand`, `CloseSearchCommand` | Search commands (omit if no search) |

> **CrudListView binding notes:**
> - `bool IsEmptyNoResults` is part of `ICrudListViewModel` — CrudListView binds to it directly via BindingContext; no BindableProperty on the page is needed.
> - `bool IsEmptyNoItems` is **not** on the interface — it must be passed from the page via `CrudListView.IsEmptyNoItems` BindableProperty because the property name differs per entity (`IsEmptyNoVenues`, `IsEmptyNoArtists`, etc.).

---

## Code-Behind Checklist (list page)

Every list page code-behind shall have:

```csharp
public partial class MyEntityPage : CrudListPageBase
{
    private readonly MyEntityViewModel _viewModel;

    /// <summary>Exposed for compiled bindings inside DataTemplates.</summary>
    public MyEntityViewModel ViewModel => _viewModel;

    protected override ICrudListViewModel ListViewModel => _viewModel;

    public MyEntityPage(MyEntityViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
        AttachViewModel();   // subscribe PropertyChanged for OnBackButtonPressed logic
    }
}
```

`OnCollectionViewScrolled`, `OnSelectionChanged`, `OnConfirmSheetStateChanged`, and the SelectedItems wire-up are all handled internally by `CrudListView`. Do not add them to the page code-behind.

`OnAppearing` and `OnBackButtonPressed` are handled by `CrudListPageBase` — do not override them unless adding page-specific logic on top.

---
