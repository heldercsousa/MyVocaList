# Code Cleanup — CRUD List Page Deduplication

## Context

Four list pages (Venues, People, Songs, Artists) share nearly identical code-behind files and ViewModels. Every new CRUD list page copies ~300 ViewModel lines and ~70 code-behind lines verbatim, with only the entity type and service call swapped. This plan documents the findings, the recommended approach, and the implementation scope.

---

## What Was Found

### Code-Behind (4 × ~70 lines = ~280 lines total)

All four pages are structurally identical. The only differences:

| Page | Diff |
|------|------|
| VenuesPage | `SelectedVenuesRaw` in `OnAppearing`; `return false` in `OnBackButtonPressed` |
| PeoplePage | `SelectedPersonsRaw`; `return false` |
| SongsPage | `SelectedSongsRaw`; calls `base.OnBackButtonPressed()` instead of `return false`; has empty `OnItemTapped` |
| ArtistsPage | `SelectedArtistsRaw`; `return false` |

Shared methods (byte-for-byte identical):
- `OnViewModelPropertyChanged` — BottomSheet open/close sync
- `OnConfirmSheetStateChanged` — sheet state bidirectional sync
- `OnCollectionViewScrolled` — sets `IsScrolled`
- `OnSelectionChanged` — delegates to `ViewModel.OnSelectionChanged(count)`

### ViewModels (4 × ~320 lines = ~1,280 lines total)

**Identical infrastructure** (private fields, same in all 4):
```
_currentPage, _totalCount, _currentSearchQuery, _searchCts, _pendingConfirmAction,
_loadSemaphore, _isLoading
```

**Identical `[ObservableProperty]` fields** (10/12 fields, same in all 4):
```
_isRefreshing, _searchText, _isSearchMode, _isScrolled, _selectedCount,
_confirmSheetState, _hasMoreItems, _isInitialLoading, _confirmMessage, _confirmActionText
```

**Identical commands** (9/11 commands, same in all 4):
`RefreshCommand, LoadMoreCommand, DeleteSelectedCommand, EditSelectedCommand,
SelectAllCommand, ConfirmActionCommand, DismissConfirmCommand, OpenSearchCommand, CloseSearchCommand`

**Identical source-generated partial hooks** (all 4):
- `OnSearchTextChanged` → `NotifyEmptyStates()` + `TriggerSearchDebounce()`
- `OnSelectedCountChanged` → raises 4 property notifications + CanExecute
- `OnIsInitialLoadingChanged` → `NotifyEmptyStates()`

**Identical methods** (all 4, bodies identical or structurally equivalent):
`InitializeAsync, RefreshAsync, LoadMoreAsync, TriggerSearchDebounce,
ExecuteConfirmActionAsync, DismissConfirmSheet, OnSelectionChanged, CloseSearch`

**Entity-specific parts** (cannot be unified without abstraction):
- `LoadFirstPageAsync` — service call differs
- `LoadMoreAsync` — service call differs
- `RequestBatchDelete` — service call + message differ
- `NotifyEmptyStates` — entity-specific empty state property names differ
- `ToggleSelectAll` / `NavigateToEditAsync` — entity-specific collection references

---

## Recommended Approach

### Principle

Use **interface + abstract base class** for both layers. The composition preference (CLAUDE.md) is honoured: the abstract base holds *mechanism*; entity-specific *data and service calls* stay in the concrete class. Do NOT use a single mega-base that forces all behaviour into one type.

---

### Layer 1 — Code-Behind: `CrudListPageBase` (abstract ContentPage)

Extract a non-generic abstract base class in C# only (no XAML):

```csharp
// MyVocaList/UI/Pages/CrudListPageBase.cs
public abstract partial class CrudListPageBase : ContentPage
{
    // Subclass provides the ViewModel reference via this contract
    protected abstract ICrudListViewModel ListViewModel { get; }
    // Subclass provides the selected items IList for DXCollectionView wiring
    protected abstract System.Collections.IList SelectedItemsRaw { get; }

    protected CrudListPageBase()
    {
        // subscribe in subclass constructor after InitializeComponent()
    }

    protected void AttachViewModel()
    {
        ListViewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ICrudListViewModel.ConfirmSheetState))
        {
            var state = ListViewModel.ConfirmSheetState;
            if (state == BottomSheetState.Hidden) confirmSheet.Close();
            else confirmSheet.Show(state, this);
        }
    }
    // + OnConfirmSheetStateChanged, OnCollectionViewScrolled, OnSelectionChanged
    // + OnAppearing (calls SelectedItemsRaw wiring + InitializeAsync)
    // + OnBackButtonPressed (sheet → search → base call)
}
```

Each page subclass becomes ~15 lines:
```csharp
public partial class VenuesPage : CrudListPageBase
{
    protected override ICrudListViewModel ListViewModel => _viewModel;
    protected override IList SelectedItemsRaw => _viewModel.SelectedVenuesRaw;
    // constructor + nothing else
}
```

**XAML change**: Replace root element `<ContentPage …>` with `<pages:CrudListPageBase …>` (one line change per XAML file). The `x:DataType` stays on the concrete ViewModel — no XAML data-binding changes.

---

### Layer 2 — ViewModel: `ICrudListViewModel` + `CrudListViewModelBase<TItem>`

#### Interface (`ICrudListViewModel`)
Exposes all common properties and commands needed by the code-behind:
```
ConfirmSheetState, IsSearchMode, CloseSearchCommand, OnSelectionChanged(int)
```

#### Abstract Base (`CrudListViewModelBase<TItem>`)

Holds all shared observable properties, infrastructure fields, and methods.

**CommunityToolkit.Mvvm constraint**: `[ObservableProperty]` is declared in the base; the source generator places `partial void OnXxxChanged` hooks in the base class. This is fine because the hooks are identical in all 4 ViewModels (they call `NotifyEmptyStates()` + `TriggerSearchDebounce()` — both provided by the base).

```csharp
public abstract partial class CrudListViewModelBase<TItem> : ViewModelBase, ICrudListViewModel
{
    // === Infrastructure (moved from all 4 VMs) ===
    private int _currentPage;
    private int _totalCount;
    private string _currentSearchQuery;
    private CancellationTokenSource _searchCts;
    private Func<Task> _pendingConfirmAction;
    private readonly SemaphoreSlim _loadSemaphore = new(1, 1);
    private volatile bool _isLoading;

    // === [ObservableProperty] fields (identical across all 4) ===
    [ObservableProperty] private bool _isRefreshing;
    [ObservableProperty] private string _searchText = string.Empty;
    // ... 8 more ...

    // === Common commands ===
    public IAsyncRelayCommand RefreshCommand { get; }
    public IRelayCommand LoadMoreCommand { get; }
    // ... 7 more identical commands ...

    // === Abstract contract (entity-specific) ===
    protected abstract ObservableRangeCollection<TItem> Items { get; }
    protected abstract ObservableRangeCollection<TItem> SelectedItems { get; }
    protected abstract Task<(IEnumerable<TItem> items, int totalCount)> FetchPageAsync(
        int page, int pageSize, string query, CancellationToken ct);
    protected abstract Task<(IEnumerable<TItem> items, int totalCount)> FetchMoreAsync(
        int page, int pageSize, string query);
    protected abstract Task DeleteAsync(IEnumerable<TItem> items);
    protected abstract string BuildDeleteConfirmMessage(IList<TItem> items);
    protected abstract Task NavigateToAddAsync();
    protected abstract Task NavigateToEditAsync(TItem item);
    protected abstract void RaiseEmptyStateProperties(); // calls OnPropertyChanged for entity-specific names

    // === Shared methods (moved from all 4 VMs) ===
    // InitializeAsync, RefreshAsync, LoadMoreAsync, TriggerSearchDebounce,
    // ExecuteConfirmActionAsync, DismissConfirmSheet, ToggleSelectAll,
    // OnSelectionChanged, CloseSearch
    // partial void OnSearchTextChanged, OnSelectedCountChanged, OnIsInitialLoadingChanged
}
```

Concrete ViewModel becomes ~80 lines (down from ~320):
```csharp
public partial class VenuesViewModel : CrudListViewModelBase<VenueListItemDto>
{
    protected override ObservableRangeCollection<VenueListItemDto> Items => Venues;
    protected override ObservableRangeCollection<VenueListItemDto> SelectedItems => SelectedVenues;

    protected override Task<(IEnumerable<VenueListItemDto>, int)> FetchPageAsync(...) =>
        _venueService.GetPagedVenuesForListAsync(...);

    protected override void RaiseEmptyStateProperties()
    {
        OnPropertyChanged(nameof(IsEmptyNoVenues));
        OnPropertyChanged(nameof(IsEmptyNoResults));
        // ...
    }
    // + entity-specific: AppBarTitle, IsEmptyNoVenues, AddVenueCommand, NavigateToAddAsync
}
```

---

## What This Does NOT Change

- XAML item templates (these are intentionally different — different DTOs, icons, fields)
- Entity-specific AppBarTitle computed property
- Entity-specific empty state property names (IsEmptyNoVenues vs IsEmptyNoArtists)
- Entity-specific navigation (routes are different)
- Songs/Artists extra commands and features (filter chips, ViewCatalog, GoBack)

---

## BACKLOG Entry (to add)

Table: **Dev Cycle Craft**
Name: `Code cleanup — CRUD list page deduplication`
Status: `💡 Pending` → promote to `📋 Spec` when this plan is approved
Notes: See this document. Affects 4 code-behind files + 4 ViewModels + new shared files.

---

## Files Affected

### New files
- `MyVocaList/UI/Pages/CrudListPageBase.cs`
- `MyVocaList/UI/Pages/ICrudListViewModel.cs`
- `MyVocaList/UI/ViewModels/CrudListViewModelBase.cs` (partial, abstract, generic)

### Modified files
- `MyVocaList/UI/Pages/Venues/VenuesPage.xaml` — root element → `CrudListPageBase`
- `MyVocaList/UI/Pages/Venues/VenuesPage.xaml.cs` — inherit from base, ~15 lines remain
- `MyVocaList/UI/Pages/People/PeoplePage.xaml` — same
- `MyVocaList/UI/Pages/People/PeoplePage.xaml.cs` — same
- `MyVocaList/UI/Pages/Songs/SongsPage.xaml` — same
- `MyVocaList/UI/Pages/Songs/SongsPage.xaml.cs` — same + keep OnItemTapped
- `MyVocaList/UI/Pages/Artists/ArtistsPage.xaml` — same
- `MyVocaList/UI/Pages/Artists/ArtistsPage.xaml.cs` — same
- `MyVocaList/UI/ViewModels/VenuesViewModel.cs` — ~80 lines remain
- `MyVocaList/UI/ViewModels/PersonsViewModel.cs` — ~80 lines remain
- `MyVocaList/UI/ViewModels/SongsViewModel.cs` — ~90 lines (extra commands)
- `MyVocaList/UI/ViewModels/ArtistsViewModel.cs` — ~100 lines (filter + extra commands)
- `MyVocaList.Tests/` — update any unit tests that mock the 4 ViewModels

---

## Estimation

| Layer | Current lines | After | Saved |
|-------|--------------|-------|-------|
| 4 code-behinds | ~280 | ~60 | ~220 |
| 4 ViewModels | ~1,280 | ~360 | ~920 |
| New shared files | 0 | ~250 | — |
| **Net** | ~1,560 | ~670 | **~890 lines (-57%)** |

---

## Verification

1. `dotnet build` — 0 errors after each file change (incremental rule: one XAML at a time)
2. `dotnet test` — all existing tests pass; ViewModel tests may need constructor updates if base absorbs the constructor params
3. Manual smoke test on emulator: each page loads, search works, select-all works, delete confirmation sheet opens/closes, FAB navigates to form
