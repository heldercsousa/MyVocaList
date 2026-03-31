# VenuesPage MD3 Rebuild Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rebuild VenuesPage to be fully MD3-compliant — SmallAppBar/SearchAppBar in Shell.TitleView, always-on checkbox selection, FloatingToolbar for actions, ListItem components for list rows.

**Architecture:** Shell.TitleView hosts SmallAppBar (default) and SearchAppBar (search mode), swapped via `IsSearchMode` ViewModel flag. Selection is always-on (SelectionMode.Multiple always); tap = toggle selection; navigation to edit goes through FloatingToolbar Edit button (single select). Multi-select mode concept is removed — selection count drives everything. VenueFormPage is unchanged (Option A confirmed — inline labeled buttons, no keyboard conflict).

**Tech Stack:** .NET MAUI 10 · DevExpress MAUI v24.2+ · CommunityToolkit.Mvvm · SmallAppBar / SearchAppBar / ListItem / FloatingToolbar (all existing custom components)

---

## File Map

| File | Change |
|------|--------|
| `MyVocaList/UI/ViewModels/VenuesViewModel.cs` | Remove multi-select mode; add `AppBarTitle`, `IsSearchMode`, `IsScrolled`, `CanDeleteSelected`; simplify selection model |
| `MyVocaList/UI/Pages/Venues/VenuesPage.xaml` | Replace Shell.TitleView Grid with SmallAppBar+SearchAppBar; remove inline search bar; replace card items with ListItem; add FloatingToolbar overlay |
| `MyVocaList/UI/Pages/Venues/VenuesPage.xaml.cs` | Remove long-press/swipe handlers; add SmallAppBar↔SearchAppBar swap; add scroll detection |

---

## Task 1: ViewModel — simplify selection model

**Files:**
- Modify: `MyVocaList/UI/ViewModels/VenuesViewModel.cs`

The current ViewModel has a mode-based multi-select model (off by default, long-press activates).
The new model: selection is always on. Count drives UI. No mode concept.

**What to remove:**
- `_suppressSelectionChangedExit` field
- `[ObservableProperty] private bool _isMultiSelectMode`
- `ShowDefaultTitle`, `ShowMultiSelectToolbar`, `SelectionMode` derived properties
- `EnterMultiSelectMode`, `ExitMultiSelectMode` methods
- `SwipeDeleteCommand`, `CancelSelectionCommand`, `TapCommand` commands
- `partial void OnIsMultiSelectModeChanged`

**What to add:**
- `[ObservableProperty] private bool _isSearchMode` — drives SmallAppBar↔SearchAppBar swap
- `[ObservableProperty] private bool _isScrolled` — drives IsElevated on app bars
- `AppBarTitle` derived property
- `CanDeleteSelected` derived property
- `OpenSearchCommand` — sets `IsSearchMode = true`
- `CloseSearchCommand` — sets `IsSearchMode = false`, clears `SearchText`

**What to change:**
- `SelectionMode` property → always returns `SelectionMode.Multiple`
- `OnSelectedCountChanged` → notify `AppBarTitle`, `CanDeleteSelected`
- `OnIsMultiSelectModeChanged` → delete entirely
- `OnSelectionChanged` → remove `ExitMultiSelectMode` call (just update count)
- `OnBackButtonPressed` logic that calls `ExitMultiSelectMode` → handled in code-behind separately

- [ ] **Step 1: Update VenuesViewModel.cs**

Replace the entire file content with:

```csharp
using MyVocaList.UI.Collections;

namespace MyVocaList.UI.ViewModels
{
    /// <summary>
    /// ViewModel for the Venues list page: paging, search, always-on selection, confirm-delete.
    /// Add navigates to VenueFormPage via FAB. Edit navigates via FloatingToolbar (single select).
    /// </summary>
    public partial class VenuesViewModel : ViewModelBase
    {
        private readonly IVenueService _venueService;
        private readonly ISnackbarComponent _snackbarService;
        private readonly ILogger<VenuesViewModel> _logger;

        private int _currentPage;
        private int _totalCount;
        private string _currentSearchQuery;
        private CancellationTokenSource _searchCts;
        private Func<Task> _pendingConfirmAction;

        private readonly SemaphoreSlim _loadSemaphore = new(1, 1);
        private bool _isLoading;

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

        public VenuesViewModel(
            IVenueService venueService,
            ISnackbarComponent snackbarService,
            ILogger<VenuesViewModel> logger)
        {
            _venueService = venueService;
            _snackbarService = snackbarService;
            _logger = logger;

            Venues = [];
            SelectedVenues = [];

            RefreshCommand = new AsyncRelayCommand(RefreshAsync);
            LoadMoreCommand = new RelayCommand(() => _ = LoadMoreAsync());
            AddVenueCommand = new AsyncRelayCommand(NavigateToAddAsync);
            DeleteSelectedCommand = new RelayCommand(RequestBatchDelete, () => CanDeleteSelected);
            EditSelectedCommand = new AsyncRelayCommand(NavigateToEditAsync, () => CanEditSelected);
            SelectAllCommand = new RelayCommand(ToggleSelectAll);
            ConfirmActionCommand = new AsyncRelayCommand(ExecuteConfirmActionAsync);
            DismissConfirmCommand = new RelayCommand(DismissConfirmSheet);
            OpenSearchCommand = new RelayCommand(() => IsSearchMode = true);
            CloseSearchCommand = new RelayCommand(CloseSearch);
        }

        public ObservableRangeCollection<VenueListItemDto> Venues { get; }
        public ObservableRangeCollection<VenueListItemDto> SelectedVenues { get; }

        /// <summary>Non-generic wrapper for binding to DXCollectionView SelectedItems (requires IList).</summary>
        public System.Collections.IList SelectedVenuesRaw => SelectedVenues;

        public string AppBarTitle => SelectedCount == 0 ? "Venues" : $"{SelectedCount} selected";
        public bool CanEditSelected => SelectedCount == 1;
        public bool CanDeleteSelected => SelectedCount > 0;
        public bool IsAllSelected => Venues.Count > 0 && SelectedCount == Venues.Count;

        // SelectionMode.Multiple is always on — tap toggles selection natively in DXCollectionView.
        public SelectionMode SelectionMode => SelectionMode.Multiple;

        public bool IsEmpty => !IsInitialLoading && Venues.Count == 0;
        public bool IsEmptyNoVenues => IsEmpty && string.IsNullOrWhiteSpace(SearchText);
        public bool IsEmptyNoResults => IsEmpty && !string.IsNullOrWhiteSpace(SearchText);

        public IAsyncRelayCommand RefreshCommand { get; }
        public IRelayCommand LoadMoreCommand { get; }
        public IAsyncRelayCommand AddVenueCommand { get; }
        public IRelayCommand DeleteSelectedCommand { get; }
        public IAsyncRelayCommand EditSelectedCommand { get; }
        public IRelayCommand SelectAllCommand { get; }
        public IAsyncRelayCommand ConfirmActionCommand { get; }
        public IRelayCommand DismissConfirmCommand { get; }
        public IRelayCommand OpenSearchCommand { get; }
        public IRelayCommand CloseSearchCommand { get; }

        partial void OnSearchTextChanged(string value)
        {
            NotifyEmptyStates();
            TriggerSearchDebounce();
        }

        partial void OnSelectedCountChanged(int value)
        {
            OnPropertyChanged(nameof(AppBarTitle));
            OnPropertyChanged(nameof(CanEditSelected));
            OnPropertyChanged(nameof(CanDeleteSelected));
            OnPropertyChanged(nameof(IsAllSelected));
            ((RelayCommand)DeleteSelectedCommand).NotifyCanExecuteChanged();
            ((AsyncRelayCommand)EditSelectedCommand).NotifyCanExecuteChanged();
        }

        partial void OnIsInitialLoadingChanged(bool value) => NotifyEmptyStates();

        public async Task InitializeAsync()
        {
            IsInitialLoading = true;

            // Yield to UI thread so the ShimmerView renders before data fetch begins
            await Task.Yield();

            await LoadFirstPageAsync(CancellationToken.None);
            RunOnUiThread(() => IsInitialLoading = false);
        }

        private async Task LoadFirstPageAsync(CancellationToken cancellationToken)
        {
            var entered = false;
            try
            {
                await _loadSemaphore.WaitAsync(cancellationToken);
                entered = true;

                _currentPage = 1;
                _currentSearchQuery = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim();

                var selectedIds = SelectedVenues.Select(v => v.Id).ToHashSet();

                var (itemsEnumerable, totalCount) = await _venueService.GetPagedVenuesForListAsync(
                    _currentPage, AppPagination.DefaultPageSize, _currentSearchQuery);

                if (cancellationToken.IsCancellationRequested) return;

                _totalCount = totalCount;
                var list = itemsEnumerable.ToList();
                HasMoreItems = totalCount > list.Count;

                RunOnUiThread(() =>
                {
                    Venues.ReplaceRange(list);

                    // Restore selection state by ID after list replacement
                    var restored = Venues.Where(v => selectedIds.Contains(v.Id)).ToList();
                    SelectedVenues.ReplaceRange(restored);
                    SelectedCount = SelectedVenues.Count;
                    NotifyEmptyStates();
                });
            }
            catch (OperationCanceledException)
            {
                // Cancellation requested — silently return
            }
            finally
            {
                if (entered)
                    _loadSemaphore.Release();
            }
        }

        private async Task RefreshAsync()
        {
            RunOnUiThread(() => IsRefreshing = true);
            await LoadFirstPageAsync(CancellationToken.None);
            RunOnUiThread(() => IsRefreshing = false);
        }

        private async Task LoadMoreAsync()
        {
            if (_isLoading || !HasMoreItems)
            {
                RunOnUiThread(() => IsRefreshing = false);
                return;
            }

            _isLoading = true;
            var loadingPage = _currentPage + 1;

            try
            {
                var (itemsEnumerable, totalCount) = await _venueService.GetPagedVenuesForListAsync(
                    loadingPage, AppPagination.DefaultPageSize, _currentSearchQuery);

                _totalCount = totalCount;
                var list = itemsEnumerable.ToList();
                var hasMore = (list.Count + Venues.Count) < _totalCount;
                _currentPage = loadingPage;

                RunOnUiThread(() =>
                {
                    Venues.AddRange(list);
                    HasMoreItems = hasMore;
                    IsRefreshing = false;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load more venues (page {Page})", loadingPage);
                RunOnUiThread(() => IsRefreshing = false);
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void TriggerSearchDebounce()
        {
            try
            {
                _searchCts?.Cancel();
                _searchCts?.Dispose();
            }
            catch { /* ignore disposal races on CancellationTokenSource */ }

            _searchCts = new CancellationTokenSource();
            var token = _searchCts.Token;

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(400, token);
                    if (token.IsCancellationRequested) return;
                    await LoadFirstPageAsync(token);
                }
                catch (OperationCanceledException) { /* ignore */ }
            }, token);
        }

        private Task NavigateToAddAsync() =>
            Shell.Current.GoToAsync(Routes.VenueForm);

        private async Task NavigateToEditAsync()
        {
            var item = SelectedVenues.FirstOrDefault();
            if (item == null) return;

            RunOnUiThread(() =>
            {
                SelectedVenues.ClearRange();
                SelectedCount = 0;
            });
            await Shell.Current.GoToAsync($"{Routes.VenueForm}?venueId={item.Id}&venueName={Uri.EscapeDataString(item.Name)}");
        }

        private void RequestBatchDelete()
        {
            var selectedItems = SelectedVenues.ToList();
            if (selectedItems.Count == 0) return;

            ConfirmMessage = $"Delete {selectedItems.Count} venue(s)?";
            ConfirmActionText = "Delete";
            _pendingConfirmAction = async () =>
            {
                var ids = selectedItems.Select(v => v.Id);
                var (success, message) = await _venueService.DeleteVenuesAsync(ids);
                RunOnUiThread(() =>
                {
                    SelectedVenues.ClearRange();
                    SelectedCount = 0;
                });
                if (success)
                {
                    await RefreshAsync();
                    await _snackbarService.ShowSuccessAsync(message);
                }
                else
                {
                    await _snackbarService.ShowErrorAsync(message);
                }
            };
            ConfirmSheetState = BottomSheetState.HalfExpanded;
        }

        private async Task ExecuteConfirmActionAsync()
        {
            var action = _pendingConfirmAction;
            DismissConfirmSheet();
            if (action != null)
                await action();
        }

        private void DismissConfirmSheet()
        {
            ConfirmSheetState = BottomSheetState.Hidden;
            _pendingConfirmAction = null;
        }

        private void ToggleSelectAll()
        {
            if (IsAllSelected)
            {
                RunOnUiThread(() => SelectedVenues.ClearRange());
                SelectedCount = 0;
                return;
            }
            RunOnUiThread(() => SelectedVenues.ReplaceRange([.. Venues]));
            SelectedCount = Venues.Count;
        }

        public void OnSelectionChanged(int count)
        {
            SelectedCount = count;
        }

        private void CloseSearch()
        {
            IsSearchMode = false;
            SearchText = string.Empty;
        }

        private void NotifyEmptyStates()
        {
            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(IsEmptyNoVenues));
            OnPropertyChanged(nameof(IsEmptyNoResults));
            OnPropertyChanged(nameof(IsAllSelected));
        }
    }
}
```

- [ ] **Step 2: Build to verify no compile errors**

```
dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android
```

Expected: Build succeeded, 0 errors. Fix any before continuing.

- [ ] **Step 3: Commit**

```
git add MyVocaList/UI/ViewModels/VenuesViewModel.cs
git commit -m "refactor(venues): simplify to always-on selection — remove multi-select mode, add AppBarTitle/IsSearchMode"
```

---

## Task 2: VenuesPage XAML — MD3 rebuild

**Files:**
- Modify: `MyVocaList/UI/Pages/Venues/VenuesPage.xaml`

**Key structural changes:**
1. Shell.TitleView: `SmallAppBar` (default) + `SearchAppBar` (search mode), visibility swapped by `IsSearchMode`
2. Content `Grid`: change `RowDefinitions="Auto,*"` → `"*"` (remove search bar row); move `BottomSheet Grid.RowSpan` from 2 to 1
3. Remove inline search `DXBorder`+`TextEdit` (Row 0)
4. Replace card-based `ItemTemplate`/`SelectedItemTemplate` with `ListItem` components
5. Add `FloatingToolbar` + reposition FAB into a bottom row overlay

**SmallAppBar bindings:**
- `Title="{Binding AppBarTitle}"` — shows "Venues" or "N selected"
- `Action1Icon="search_outlined"` — taps `OpenSearchCommand`
- `IsElevated="{Binding IsScrolled}"`
- No `NavigationIcon` (VenuesPage is a root tab, no back button)

**SearchAppBar bindings:**
- `SearchText="{Binding SearchText, Mode=TwoWay}"`
- `Placeholder="Search venues..."`
- `BackCommand="{Binding CloseSearchCommand}"` — clears text + returns to SmallAppBar
- `IsElevated="{Binding IsScrolled}"`

**ListItem usage notes:**
- `Headline="{Binding Name}"` — venue name
- `LeadingContent` = `ListItemLeadingIcon` with `Icon="storefront_outlined"`
- `TrailingContent` = `CheckEdit` with `IsChecked="False"` (ItemTemplate) or `"True"` (SelectedItemTemplate), `InputTransparent="True"`
- `IsSelected="False"` / `"True"` on the `ListItem` (sets SecondaryContainer bg on selected)
- Remove `SwipeContainer` entirely — no swipe delete in new design

**FloatingToolbar + FAB layout:**
- Wrap the content `Grid` content area in a single-cell overlay `Grid`
- `FloatingToolbar` at `VerticalOptions="End" HorizontalOptions="Center" Margin="0,0,0,16"`
- FAB at `VerticalOptions="End" HorizontalOptions="End" Margin="0,0,16,88"` (above toolbar: toolbar height 48 + toolbar bottom margin 16 + gap 24 = 88)
- `DXCollectionView` gets `Margin="0,0,0,80"` so last items aren't hidden under toolbar

**FloatingToolbar slot assignments:**
- Slot 1: `SelectAll` — `done_all_outlined`, always enabled, `IsSelected` = `IsAllSelected`
- Slot 2: `Edit` — `edit_outlined`, `Action2Command="{Binding EditSelectedCommand}"`, `Action2IsSelected="{Binding CanEditSelected}"`
- Slot 3: `Delete` — `delete_outlined`, `Action3Command="{Binding DeleteSelectedCommand}"`, `Action3IsSelected="{Binding CanDeleteSelected}"`

Note: `FloatingToolbar` does not have an `IsEnabled` or opacity concept per slot — use `ActionNIsSelected` for visual feedback only. Commands have `CanExecute` guards in the ViewModel.

- [ ] **Step 1: Replace VenuesPage.xaml**

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    xmlns:dx="http://schemas.devexpress.com/maui"
    xmlns:dxcv="clr-namespace:DevExpress.Maui.CollectionView;assembly=DevExpress.Maui.CollectionView"
    xmlns:dto="clr-namespace:MyVocaList.Contracts.DTOs.List;assembly=MyVocaList.Contracts"
    xmlns:vm="clr-namespace:MyVocaList.UI.ViewModels"
    xmlns:appbars="clr-namespace:MyVocaList.UI.Components.AppBars"
    xmlns:lists="clr-namespace:MyVocaList.UI.Components.Lists"
    xmlns:toolbars="clr-namespace:MyVocaList.UI.Components.Toolbars"
    x:Class="MyVocaList.UI.Pages.Venues.VenuesPage"
    x:Name="page"
    x:DataType="vm:VenuesViewModel"
    Title="Venues"
    BackgroundColor="{StaticResource Surface}"
    SafeAreaEdges="Container">

    <Shell.TitleView>
        <!-- Shell.TitleView accepts one child — wrap both app bars in a Grid so only one is visible at a time -->
        <Grid>
            <!-- SmallAppBar: default view, hidden when search is active -->
            <appbars:SmallAppBar
                x:Name="smallAppBar"
                Title="{Binding AppBarTitle}"
                Action1Icon="search_outlined"
                Action1Command="{Binding OpenSearchCommand}"
                IsElevated="{Binding IsScrolled}"
                IsVisible="{Binding IsSearchMode, Converter={StaticResource InverseBoolConverter}}" />
            <!-- SearchAppBar: active when IsSearchMode=true -->
            <appbars:SearchAppBar
                x:Name="searchAppBar"
                SearchText="{Binding SearchText, Mode=TwoWay}"
                Placeholder="Search venues..."
                BackCommand="{Binding CloseSearchCommand}"
                IsElevated="{Binding IsScrolled}"
                IsVisible="{Binding IsSearchMode}" />
        </Grid>
    </Shell.TitleView>

    <!-- Single-row layout: list + overlay (toolbar + FAB) -->
    <Grid>

        <!-- Venue list with skeleton loading -->
        <dx:ShimmerView IsLoading="{Binding IsInitialLoading}"
                        WaveWidth="0.7"
                        WaveOpacity="0.8">
            <dx:ShimmerView.LoadingView>
                <VerticalStackLayout Spacing="0">
                    <dx:DXBorder BackgroundColor="{dx:ThemeColor SurfaceContainerHighest}"
                                 CornerRadius="0" HeightRequest="56" Margin="0,1" />
                    <dx:DXBorder BackgroundColor="{dx:ThemeColor SurfaceContainerHighest}"
                                 CornerRadius="0" HeightRequest="56" Margin="0,1" />
                    <dx:DXBorder BackgroundColor="{dx:ThemeColor SurfaceContainerHighest}"
                                 CornerRadius="0" HeightRequest="56" Margin="0,1" />
                    <dx:DXBorder BackgroundColor="{dx:ThemeColor SurfaceContainerHighest}"
                                 CornerRadius="0" HeightRequest="56" Margin="0,1" />
                    <dx:DXBorder BackgroundColor="{dx:ThemeColor SurfaceContainerHighest}"
                                 CornerRadius="0" HeightRequest="56" Margin="0,1" />
                    <dx:DXBorder BackgroundColor="{dx:ThemeColor SurfaceContainerHighest}"
                                 CornerRadius="0" HeightRequest="56" Margin="0,1" />
                </VerticalStackLayout>
            </dx:ShimmerView.LoadingView>
            <dx:ShimmerView.Content>
                <dxcv:DXCollectionView x:Name="collectionView"
                       ItemsSource="{Binding Venues}"
                       SelectedItems="{Binding SelectedVenuesRaw}"
                       IsPullToRefreshEnabled="True"
                       IsRefreshing="{Binding IsRefreshing, Mode=TwoWay}"
                       PullToRefreshCommand="{Binding RefreshCommand}"
                       IsLoadMoreEnabled="{Binding HasMoreItems}"
                       LoadMoreCommand="{Binding LoadMoreCommand}"
                       IndicatorColor="{StaticResource Primary}"
                       SelectionMode="Multiple"
                       UseRippleEffect="True"
                       AllowCascadeUpdate="True"
                       ItemSeparatorThickness="0"
                       Margin="0,0,0,80"
                       Scrolled="OnCollectionViewScrolled"
                       SelectionChanged="OnSelectionChanged">

                    <dxcv:DXCollectionView.ItemTemplate>
                        <DataTemplate x:DataType="dto:VenueListItemDto">
                            <lists:ListItem Headline="{Binding Name}"
                                            IsSelected="False">
                                <lists:ListItem.LeadingContent>
                                    <lists:ListItemLeadingIcon Icon="storefront_outlined" />
                                </lists:ListItem.LeadingContent>
                                <lists:ListItem.TrailingContent>
                                    <dx:CheckEdit IsChecked="False"
                                                  InputTransparent="True"
                                                  VerticalOptions="Center" />
                                </lists:ListItem.TrailingContent>
                            </lists:ListItem>
                        </DataTemplate>
                    </dxcv:DXCollectionView.ItemTemplate>

                    <dxcv:DXCollectionView.SelectedItemTemplate>
                        <DataTemplate x:DataType="dto:VenueListItemDto">
                            <lists:ListItem Headline="{Binding Name}"
                                            IsSelected="True">
                                <lists:ListItem.LeadingContent>
                                    <lists:ListItemLeadingIcon Icon="storefront_outlined" />
                                </lists:ListItem.LeadingContent>
                                <lists:ListItem.TrailingContent>
                                    <dx:CheckEdit IsChecked="True"
                                                  CheckedCheckBoxColor="{dx:ThemeColor Primary}"
                                                  InputTransparent="True"
                                                  VerticalOptions="Center" />
                                </lists:ListItem.TrailingContent>
                            </lists:ListItem>
                        </DataTemplate>
                    </dxcv:DXCollectionView.SelectedItemTemplate>
                </dxcv:DXCollectionView>
            </dx:ShimmerView.Content>
        </dx:ShimmerView>

        <!-- Empty state: no venues registered -->
        <VerticalStackLayout IsVisible="{Binding IsEmptyNoVenues}"
                             VerticalOptions="Center"
                             HorizontalOptions="Center"
                             Spacing="8"
                             Margin="32">
            <dx:DXButton Icon="nightlife_outlined"
                         IconColor="{dx:ThemeColor OnSurfaceVariant}"
                         IconWidth="64" IconHeight="64"
                         BackgroundColor="Transparent"
                         InputTransparent="True"
                         WidthRequest="80" HeightRequest="80"
                         HorizontalOptions="Center" />
            <Label Text="No venue registered"
                   FontFamily="RobotoMedium" FontSize="16"
                   TextColor="{dx:ThemeColor OnSurfaceVariant}"
                   HorizontalTextAlignment="Center" />
        </VerticalStackLayout>

        <!-- Empty state: search returned no results -->
        <VerticalStackLayout IsVisible="{Binding IsEmptyNoResults}"
                             VerticalOptions="Center"
                             HorizontalOptions="Center"
                             Spacing="8"
                             Margin="32">
            <dx:DXButton Icon="search_outlined"
                         IconColor="{dx:ThemeColor OnSurfaceVariant}"
                         IconWidth="64" IconHeight="64"
                         BackgroundColor="Transparent"
                         InputTransparent="True"
                         WidthRequest="80" HeightRequest="80"
                         HorizontalOptions="Center" />
            <Label Text="No venue found"
                   FontFamily="RobotoMedium" FontSize="16"
                   TextColor="{dx:ThemeColor OnSurfaceVariant}"
                   HorizontalTextAlignment="Center" />
        </VerticalStackLayout>

        <!-- FAB: bottom-right, above FloatingToolbar -->
        <dx:DXButton Icon="add_outlined"
                     IconColor="{StaticResource OnPrimary}"
                     BackgroundColor="{StaticResource Primary}"
                     PressedBackgroundColor="{StaticResource PrimaryContainer}"
                     WidthRequest="56" HeightRequest="56"
                     CornerRadius="16"
                     HorizontalOptions="End" VerticalOptions="End"
                     Margin="0,0,16,88"
                     Command="{Binding AddVenueCommand}" />

        <!-- FloatingToolbar: centered, above safe area bottom -->
        <toolbars:FloatingToolbar
            HorizontalOptions="Center"
            VerticalOptions="End"
            Margin="0,0,0,16"
            Action1Icon="done_all_outlined"
            Action1Command="{Binding SelectAllCommand}"
            Action1Description="Select all"
            Action1IsSelected="{Binding IsAllSelected}"
            Action2Icon="edit_outlined"
            Action2Command="{Binding EditSelectedCommand}"
            Action2Description="Edit selected"
            Action2IsSelected="{Binding CanEditSelected}"
            Action3Icon="delete_outlined"
            Action3Command="{Binding DeleteSelectedCommand}"
            Action3Description="Delete selected"
            Action3IsSelected="{Binding CanDeleteSelected}" />

        <!-- Confirm delete BottomSheet -->
        <dx:BottomSheet x:Name="confirmSheet"
                        HalfExpandedRatio="0.28"
                        AllowedState="HalfExpanded"
                        IsModal="True"
                        ShowGrabber="True"
                        AllowDismiss="True"
                        BackgroundColor="{StaticResource Surface}"
                        CornerRadius="28"
                        StateChanged="OnConfirmSheetStateChanged">
            <VerticalStackLayout>
                <Label Text="{Binding ConfirmMessage}"
                       FontFamily="RobotoMedium" FontSize="16"
                       TextColor="{StaticResource OnSurface}"
                       HorizontalTextAlignment="Center"
                       Margin="24,20" />
                <BoxView HeightRequest="1" BackgroundColor="{StaticResource OutlineVariant}" />
                <dx:DXButton Content="{Binding ConfirmActionText}"
                             BackgroundColor="Transparent"
                             TextColor="{StaticResource Error}"
                             HorizontalOptions="Fill"
                             HeightRequest="56"
                             Command="{Binding ConfirmActionCommand}" />
                <BoxView HeightRequest="1" BackgroundColor="{StaticResource OutlineVariant}" />
                <dx:DXButton Content="Cancel"
                             BackgroundColor="Transparent"
                             TextColor="{StaticResource Primary}"
                             HorizontalOptions="Fill"
                             HeightRequest="56"
                             Command="{Binding DismissConfirmCommand}" />
            </VerticalStackLayout>
        </dx:BottomSheet>

    </Grid>

</ContentPage>
```

Check `App.xaml` or `MaterialStyles.xaml` for `InverseBoolConverter` — if not present, use code-behind visibility swap instead (see Task 3 fallback).

- [ ] **Step 2: Build**

```
dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android
```

Expected: Build succeeded, 0 errors. Common issues:
- `InverseBoolConverter` not found → see Task 3 fallback
- `storefront_outlined` icon not in DevExpress icon set → replace with `place_outlined` or `home_outlined`
- `ListItemLeadingIcon` namespace not in GlobalUsings → add to XAML xmlns

- [ ] **Step 3: Commit**

```
git add MyVocaList/UI/Pages/Venues/VenuesPage.xaml
git commit -m "feat(venues): MD3 XAML rebuild — SmallAppBar/SearchAppBar, ListItem rows, FloatingToolbar"
```

---

## Task 3: VenuesPage code-behind — update event handlers + swap logic + scroll

**Files:**
- Modify: `MyVocaList/UI/Pages/Venues/VenuesPage.xaml.cs`

**What to remove:**
- `OnItemTapped` — DXCollectionView handles tap natively in Multiple mode
- `OnItemLongPressed` — no long-press behavior
- `OnSwipeDeleteTapped` — swipe delete removed
- `OnSwipeItemShowing` — no swipe in new design
- `OnViewModelPropertyChanged` + `_viewModel.PropertyChanged` subscription — ConfirmSheet state now observed same way; keep but simplify

**What to add:**
- `OnCollectionViewScrolled` — sets `_viewModel.IsScrolled`
- Fallback: if `InverseBoolConverter` is unavailable, handle SmallAppBar/SearchAppBar visibility via `OnViewModelPropertyChanged` watching `IsSearchMode`

**What to keep unchanged:**
- `OnAppearing` — same: assign SelectedItems + call InitializeAsync
- `OnSelectionChanged` — same
- `OnConfirmSheetStateChanged` — same
- `OnBackButtonPressed` — update: remove ExitMultiSelectMode path, keep confirm-sheet path

- [ ] **Step 1: Replace VenuesPage.xaml.cs**

```csharp
namespace MyVocaList.UI.Pages.Venues;

public partial class VenuesPage : ContentPage
{
    private readonly VenuesViewModel _viewModel;

    /// <summary>Exposed for compiled bindings inside DataTemplates that need typed ViewModel access.</summary>
    public VenuesViewModel ViewModel => _viewModel;

    public VenuesPage(VenuesViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;

        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (collectionView != null)
            collectionView.SelectedItems = _viewModel.SelectedVenues;

        _ = _viewModel.InitializeAsync();
    }

    private void OnViewModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(VenuesViewModel.ConfirmSheetState))
        {
            var state = _viewModel.ConfirmSheetState;
            if (state == BottomSheetState.Hidden)
                confirmSheet.Close();
            else
                confirmSheet.Show(state, this); // DX v24.2 overload: Show(BottomSheetState, Page) — verify against installed version if build fails
        }
    }

    private void OnCollectionViewScrolled(object sender, CollectionViewScrolledEventArgs e)
    {
        _viewModel.IsScrolled = e.VerticalOffset > 0;
    }

    protected override bool OnBackButtonPressed()
    {
        if (_viewModel.ConfirmSheetState != BottomSheetState.Hidden)
        {
            _viewModel.ConfirmSheetState = BottomSheetState.Hidden;
            return true;
        }

        if (_viewModel.IsSearchMode)
        {
            _viewModel.CloseSearchCommand.Execute(null);
            return true;
        }

        return false;
    }

    private void OnSelectionChanged(object sender, CollectionViewSelectionChangedEventArgs e)
    {
        var count = (collectionView.SelectedItems as System.Collections.ICollection)?.Count ?? 0;
        _viewModel.OnSelectionChanged(count);
    }

    private void OnConfirmSheetStateChanged(object sender, ValueChangedEventArgs<BottomSheetState> e)
    {
        if (e.NewValue == BottomSheetState.Hidden && _viewModel.ConfirmSheetState != BottomSheetState.Hidden)
            _viewModel.ConfirmSheetState = BottomSheetState.Hidden;
    }
}
```

**Note on `InverseBoolConverter`:** If it doesn't exist, add it to `App.xaml` resources:
```xml
<converters:InverseBoolConverter x:Key="InverseBoolConverter" />
```
And create the class in `MyVocaList/UI/Converters/InverseBoolConverter.cs`:
```csharp
namespace MyVocaList.UI.Converters;

public sealed class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        => value is bool b ? !b : value;

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        => value is bool b ? !b : value;
}
```
Add `xmlns:converters="clr-namespace:MyVocaList.UI.Converters"` to `App.xaml`.

- [ ] **Step 2: Build**

```
dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 3: Commit**

```
git add MyVocaList/UI/Pages/Venues/VenuesPage.xaml.cs
git commit -m "refactor(venues): update code-behind — remove multi-select handlers, add scroll detection"
```

---

## Task 4: Fix-up pass — check converters, icons, GlobalUsings

**Files:**
- Check: `MyVocaList/App.xaml`
- Check: `MyVocaList/GlobalUsings.cs`
- Check/Create: `MyVocaList/UI/Converters/InverseBoolConverter.cs`

- [ ] **Step 1: Check if InverseBoolConverter already exists**

Search the solution:
```
grep -r "InverseBoolConverter" MyVocaList/
```

If found: verify it's registered in `App.xaml` as `{x:Key InverseBoolConverter}`.
If not found: create `MyVocaList/UI/Converters/InverseBoolConverter.cs` and register in `App.xaml` (see Task 3 note above).

- [ ] **Step 2: Verify icon names compile**

`storefront_outlined` must exist in DevExpress MAUI icon set. If build error mentions it, replace with `place_outlined` in both `ItemTemplate` and `SelectedItemTemplate` in VenuesPage.xaml.

- [ ] **Step 3: Verify `lists` namespace compiles**

If `xmlns:lists="clr-namespace:MyVocaList.UI.Components.Lists"` causes errors, check that `ListItem`, `ListItemLeadingIcon` are in that namespace.

- [ ] **Step 4: Final build**

```
dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android
```

Expected: Build succeeded, 0 errors, 0 warnings (except known DevExpress XML warnings).

- [ ] **Step 5: Commit fix-ups if any**

```
git add -p
git commit -m "fix(venues): fix-up converters/icons/namespaces after MD3 rebuild"
```

---

## Task 5: Review

- [ ] **Step 1: Run /project:review**

Verify against the design decisions:
- SmallAppBar in Shell.TitleView showing "Venues" / "N selected"
- Search icon on SmallAppBar triggers SearchAppBar swap
- SearchAppBar back arrow clears search and returns SmallAppBar
- Checkboxes always visible on list items
- Tap = toggle selection
- FloatingToolbar: SelectAll, Edit (enabled =1), Delete (enabled ≥1)
- FAB positioned above toolbar
- Scroll → IsElevated transitions on both app bars
- Confirm sheet still works for batch delete
- Back button: closes search if open, otherwise default Shell behavior

- [ ] **Step 2: Push**

```
git push
```
