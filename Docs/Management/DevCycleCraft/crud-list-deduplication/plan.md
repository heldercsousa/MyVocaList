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

### Design principles applied

Two C# patterns govern the choice between events and abstract methods here:

| Scenario | Recommended mechanism | Reason |
|---|---|---|
| Base CANNOT access the resource (MAUI XAML constraint) | **Event** | Derived class owns the XAML element; base emits intent, derived acts |
| Base MUST call entity-specific logic | **Abstract method** | Compiler-enforced, no-subscribe-means-broken risk eliminated |
| Derived class may optionally extend base behavior | **`protected virtual` method with no-op base** | Opt-in extension without forcing override |

---

### The MAUI Partial Class Constraint (critical prerequisite)

In MAUI, changing a page's base class from `ContentPage` to a custom abstract class is supported: change the XAML root element and both partial declarations match. **However**, `x:Name` fields (`confirmSheet`, `collectionView`) are generated as `private` in the XAML-generated partial of the *derived* class. The base class has zero visibility into them.

This means: **`CrudListPageBase` cannot call `confirmSheet.Close()` or `collectionView.SelectedItems = ...` directly.** Any approach that puts these calls in the base (abstract properties returning DX types, etc.) introduces coupling between the base and MAUI/DX UI element types. Events solve this cleanly — the base emits intent, the derived class acts on its own XAML elements.

A secondary benefit: XAML event bindings (`Scrolled="OnCollectionViewScrolled"`) find inherited methods via normal C# method resolution. The base can declare these handlers; XAML wires them up correctly through the derived class.

---

### Layer 1 — Code-Behind: `CrudListPageBase` + Events

```csharp
// MyVocaList/UI/Pages/CrudListPageBase.cs
public abstract class CrudListPageBase : ContentPage
{
    // === Required contract (compiler-enforced) ===
    protected abstract ICrudListViewModel ListViewModel { get; }

    // === Events for UI-element operations the base cannot perform ===
    // Derived subscribes in constructor; wires up its own XAML x:Name elements.
    protected event EventHandler<BottomSheetState> ConfirmSheetStateRequired;
    protected event EventHandler SelectionItemsWireUpRequired;

    // Called by derived constructor after InitializeComponent()
    protected void AttachViewModel()
        => ListViewModel.PropertyChanged += OnViewModelPropertyChanged;

    private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ICrudListViewModel.ConfirmSheetState))
            ConfirmSheetStateRequired?.Invoke(this, ListViewModel.ConfirmSheetState);
    }

    // Bidirectional sync: sheet closed by gesture → sync back to VM
    protected void OnConfirmSheetStateChanged(object sender, ValueChangedEventArgs<BottomSheetState> e)
    {
        if (e.NewValue == BottomSheetState.Hidden &&
            ListViewModel.ConfirmSheetState != BottomSheetState.Hidden)
            ListViewModel.ConfirmSheetState = BottomSheetState.Hidden;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        SelectionItemsWireUpRequired?.Invoke(this, EventArgs.Empty);
        _ = ListViewModel.InitializeAsync();
    }

    // XAML event bindings in derived pages resolve these via inheritance:
    protected void OnCollectionViewScrolled(object sender, DXCollectionViewScrolledEventArgs e)
        => ListViewModel.IsScrolled = e.Offset > 0;

    protected void OnSelectionChanged(object sender, CollectionViewSelectionChangedEventArgs e)
    {
        // sender IS the collectionView — no x:Name reference needed
        var count = (sender as DXCollectionView)?.SelectedItems?.Count ?? 0;
        ListViewModel.OnSelectionChanged(count);
    }

    protected override bool OnBackButtonPressed()
    {
        if (ListViewModel.ConfirmSheetState != BottomSheetState.Hidden)
        {
            ListViewModel.ConfirmSheetState = BottomSheetState.Hidden;
            return true;
        }
        if (ListViewModel.IsSearchMode)
        {
            ListViewModel.CloseSearchCommand.Execute(null);
            return true;
        }
        return base.OnBackButtonPressed();
    }
}
```

Each derived page constructor wires its own XAML elements to the base events:
```csharp
public partial class VenuesPage : CrudListPageBase
{
    private readonly VenuesViewModel _viewModel;
    protected override ICrudListViewModel ListViewModel => _viewModel;

    public VenuesPage(VenuesViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
        AttachViewModel();

        // Wire base events to this page's own XAML x:Name elements
        ConfirmSheetStateRequired += (_, state) =>
        {
            if (state == BottomSheetState.Hidden) confirmSheet.Close();
            else confirmSheet.Show(state, this);
        };
        SelectionItemsWireUpRequired += (_, _) =>
        {
            if (collectionView != null)
                collectionView.SelectedItems = _viewModel.SelectedVenuesRaw;
        };
    }
}
```

**XAML change per page**: root element `<ContentPage …>` → `<pages:CrudListPageBase …>`. The `x:DataType` remains on the concrete ViewModel; no data-binding changes needed.

**Why events beat abstract properties here**: An abstract `protected abstract BottomSheet ConfirmSheet { get; }` would force a DX type into the base's API surface, coupling it to DevExpress. Events keep the base ignorant of DX types entirely. When the future Blazor Hybrid migration arrives, the base doesn't change.

---

### Layer 2 — ViewModel: `ICrudListViewModel` + `CrudListViewModelBase<TItem>`

#### Interface (`ICrudListViewModel`)
```csharp
public interface ICrudListViewModel
{
    BottomSheetState ConfirmSheetState { get; set; }
    bool IsSearchMode { get; }
    bool IsScrolled { get; set; }
    IRelayCommand CloseSearchCommand { get; }
    Task InitializeAsync();
    void OnSelectionChanged(int count);
}
```

#### Abstract Base (`CrudListViewModelBase<TItem>`)

**CommunityToolkit.Mvvm source generator note**: `[ObservableProperty]` on fields in a `partial abstract` generic class is fully supported. The source generator emits the `partial void OnXxxChanged` hooks in the same class (the base). Since all four VMs have identical hook implementations (`OnSearchTextChanged`, `OnSelectedCountChanged`, `OnIsInitialLoadingChanged`), they all live in the base — no override needed.

```csharp
public abstract partial class CrudListViewModelBase<TItem> : ViewModelBase, ICrudListViewModel
{
    // === Infrastructure (private, moved from all 4 VMs) ===
    private int _currentPage;
    private int _totalCount;
    private string _currentSearchQuery;
    private CancellationTokenSource _searchCts;
    private Func<Task> _pendingConfirmAction;
    private readonly SemaphoreSlim _loadSemaphore = new(1, 1);
    private volatile bool _isLoading;

    // === [ObservableProperty] fields — identical in all 4 ===
    [ObservableProperty] private bool _isRefreshing;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private bool _isSearchMode;
    [ObservableProperty] private bool _isScrolled;
    [ObservableProperty] private int _selectedCount;
    [ObservableProperty] private BottomSheetState _confirmSheetState = BottomSheetState.Hidden;
    [ObservableProperty] private bool _hasMoreItems = true;
    [ObservableProperty] private bool _isInitialLoading = true;
    [ObservableProperty] private string _confirmMessage = string.Empty;
    [ObservableProperty] private string _confirmActionText = "Delete";

    // === Abstract contract — REQUIRED, compiler-enforced ===
    protected abstract ObservableRangeCollection<TItem> Items { get; }
    protected abstract ObservableRangeCollection<TItem> SelectedItems { get; }

    protected abstract Task<(IEnumerable<TItem> items, int totalCount)> FetchPageAsync(
        int page, int pageSize, string query, CancellationToken ct);

    protected abstract Task<(IEnumerable<TItem> items, int totalCount)> FetchMoreAsync(
        int page, int pageSize, string query, CancellationToken ct = default);

    protected abstract Task ExecuteDeleteAsync(IEnumerable<TItem> items);
    protected abstract string BuildDeleteConfirmMessage(IList<TItem> items);
    protected abstract Task NavigateToAddAsync();
    protected abstract Task NavigateToEditAsync(TItem item);

    // Raises entity-specific empty state property names (IsEmptyNoVenues, etc.)
    // Abstract = compiler-enforced; every entity has these, events would allow "forgetting" silently.
    protected abstract void RaiseEntityEmptyStateProperties();

    // === Optional extension hooks — virtual with no-op base ===
    // Override in derived class to add behavior after a page load completes.
    protected virtual void OnAfterLoad(IReadOnlyList<TItem> items) { }

    // === Shared methods (identical across all 4 VMs) ===
    // InitializeAsync, RefreshAsync, LoadFirstPageAsync, LoadMoreAsync,
    // TriggerSearchDebounce, ExecuteConfirmActionAsync, DismissConfirmSheet,
    // ToggleSelectAll, OnSelectionChanged, CloseSearch, NotifyEmptyStates
    // plus partial void hooks: OnSearchTextChanged, OnSelectedCountChanged, OnIsInitialLoadingChanged
}
```

**Why abstract methods (not events) for the ViewModel**:
- `RaiseEntityEmptyStateProperties` is REQUIRED — forgetting to subscribe an event silently breaks empty states with no build error. An abstract method is a build-time contract.
- `FetchPageAsync`, `DeleteAsync` etc. are required by the base algorithm — the Template Method pattern is the correct fit.
- INPC notifications from `CrudListViewModelBase<TItem>` propagate correctly through inheritance. A composed `CrudListController<TItem>` would require forwarding all `PropertyChanged` events manually — more boilerplate than it saves.

Concrete ViewModel becomes ~80 lines:
```csharp
public partial class VenuesViewModel : CrudListViewModelBase<VenueListItemDto>
{
    private readonly IVenueService _venueService;
    // ... snackbar, logger

    protected override ObservableRangeCollection<VenueListItemDto> Items => Venues;
    protected override ObservableRangeCollection<VenueListItemDto> SelectedItems => SelectedVenues;

    public ObservableRangeCollection<VenueListItemDto> Venues { get; } = [];
    public ObservableRangeCollection<VenueListItemDto> SelectedVenues { get; } = [];
    public System.Collections.IList SelectedVenuesRaw => SelectedVenues;

    public string AppBarTitle => SelectedCount == 0 ? "Venues" : $"{SelectedCount} selected";
    public bool IsEmptyNoVenues => IsEmpty && string.IsNullOrWhiteSpace(SearchText);
    public bool IsEmptyNoResults => IsEmpty && !string.IsNullOrWhiteSpace(SearchText);

    protected override Task<(IEnumerable<VenueListItemDto>, int)> FetchPageAsync(...) =>
        _venueService.GetPagedVenuesForListAsync(...);

    protected override void RaiseEntityEmptyStateProperties()
    {
        OnPropertyChanged(nameof(IsEmptyNoVenues));
        OnPropertyChanged(nameof(IsEmptyNoResults));
    }

    public IAsyncRelayCommand AddVenueCommand { get; }
    protected override Task NavigateToAddAsync() => Shell.Current.GoToAsync(Routes.VenueForm);
    // ... NavigateToEditAsync, BuildDeleteConfirmMessage, ExecuteDeleteAsync
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
