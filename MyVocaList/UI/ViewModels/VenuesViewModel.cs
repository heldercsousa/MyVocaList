using MyVocaList.UI.Collections;

namespace MyVocaList.UI.ViewModels
{
    /// <summary>
    /// ViewModel for the Venues list page: paging, search, multi-select, confirm-delete.
    /// Add/Edit navigates to VenueFormPage.
    /// </summary>
    public partial class VenuesViewModel : ViewModelBase
    {
        private readonly IVenueService _venueService;
        private readonly ISnackbarService _snackbarService;
        private readonly ILogger<VenuesViewModel> _logger;

        private int _currentPage;
        private int _totalCount;
        private string _currentSearchQuery;
        private CancellationTokenSource _searchCts;
        private Func<Task> _pendingConfirmAction;

        private readonly SemaphoreSlim _loadSemaphore = new(1, 1);
        private bool _suppressSelectionChangedExit;
        private bool _isLoading;

        [ObservableProperty] private bool _isRefreshing;
        [ObservableProperty] private string _searchText = string.Empty;
        [ObservableProperty] private bool _isMultiSelectMode;
        [ObservableProperty] private int _selectedCount;
        [ObservableProperty] private BottomSheetState _confirmSheetState = BottomSheetState.Hidden;
        [ObservableProperty] private bool _hasMoreItems = true;
        [ObservableProperty] private bool _isInitialLoading = true;
        [ObservableProperty] private string _confirmMessage = string.Empty;
        [ObservableProperty] private string _confirmActionText = "Delete";

        public VenuesViewModel(
            IVenueService venueService,
            ISnackbarService snackbarService,
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
            SwipeDeleteCommand = new RelayCommand<VenueListItemDto>(item => RequestSwipeDelete(item));
            DeleteSelectedCommand = new RelayCommand(RequestBatchDelete);
            EditSelectedCommand = new AsyncRelayCommand(NavigateToEditAsync);
            CancelSelectionCommand = new RelayCommand(ExitMultiSelectMode);
            TapCommand = new RelayCommand<VenueListItemDto>(OnItemTapped);
            SelectAllCommand = new RelayCommand(ToggleSelectAll);
            ConfirmActionCommand = new AsyncRelayCommand(ExecuteConfirmActionAsync);
            DismissConfirmCommand = new RelayCommand(DismissConfirmSheet);
        }

        public ObservableRangeCollection<VenueListItemDto> Venues { get; }
        public ObservableRangeCollection<VenueListItemDto> SelectedVenues { get; }

        /// <summary>Non-generic wrapper for binding to DXCollectionView SelectedItems (requires IList).</summary>
        public System.Collections.IList SelectedVenuesRaw => SelectedVenues;

        public string SelectedCountText => $"{SelectedCount} selected";
        public bool CanEditSelected => SelectedCount == 1;
        public bool ShowDefaultTitle => !IsMultiSelectMode;
        public bool ShowMultiSelectToolbar => IsMultiSelectMode;

        public SelectionMode SelectionMode =>
            IsMultiSelectMode ? SelectionMode.Multiple : SelectionMode.None;

        public bool IsAllSelected => IsMultiSelectMode && Venues.Count > 0 && SelectedCount == Venues.Count;

        public bool IsEmpty => !IsInitialLoading && Venues.Count == 0;
        public bool IsEmptyNoVenues => IsEmpty && string.IsNullOrWhiteSpace(SearchText);
        public bool IsEmptyNoResults => IsEmpty && !string.IsNullOrWhiteSpace(SearchText);

        public IAsyncRelayCommand RefreshCommand { get; }
        public IRelayCommand LoadMoreCommand { get; }
        public IAsyncRelayCommand AddVenueCommand { get; }
        public IRelayCommand<VenueListItemDto> SwipeDeleteCommand { get; }
        public IRelayCommand DeleteSelectedCommand { get; }
        public IAsyncRelayCommand EditSelectedCommand { get; }
        public IRelayCommand CancelSelectionCommand { get; }
        public IRelayCommand<VenueListItemDto> TapCommand { get; }
        public IRelayCommand SelectAllCommand { get; }
        public IAsyncRelayCommand ConfirmActionCommand { get; }
        public IRelayCommand DismissConfirmCommand { get; }

        partial void OnSearchTextChanged(string value)
        {
            NotifyEmptyStates();
            TriggerSearchDebounce();
        }

        partial void OnIsMultiSelectModeChanged(bool value)
        {
            OnPropertyChanged(nameof(SelectionMode));
            OnPropertyChanged(nameof(ShowDefaultTitle));
            OnPropertyChanged(nameof(ShowMultiSelectToolbar));
            OnPropertyChanged(nameof(IsAllSelected));
        }

        partial void OnSelectedCountChanged(int value)
        {
            OnPropertyChanged(nameof(SelectedCountText));
            OnPropertyChanged(nameof(CanEditSelected));
            OnPropertyChanged(nameof(IsAllSelected));
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

        private void OnItemTapped(VenueListItemDto item)
        {
            if (item == null) return;
            _ = Shell.Current.GoToAsync(
                $"{Routes.VenueForm}?venueId={item.Id}&venueName={Uri.EscapeDataString(item.Name)}");
        }

        private Task NavigateToAddAsync() =>
            Shell.Current.GoToAsync(Routes.VenueForm);

        private async Task NavigateToEditAsync()
        {
            var item = SelectedVenues.FirstOrDefault();
            if (item == null) return;

            ExitMultiSelectMode();
            await Shell.Current.GoToAsync($"{Routes.VenueForm}?venueId={item.Id}&venueName={Uri.EscapeDataString(item.Name)}");
        }

        private void RequestSwipeDelete(VenueListItemDto item)
        {
            ConfirmMessage = $"Delete \"{item.Name}\"?";
            ConfirmActionText = "Delete";
            _pendingConfirmAction = async () =>
            {
                var (success, message) = await _venueService.DeleteVenuesAsync([item.Id]);
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
                ExitMultiSelectMode();
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

        public void EnterMultiSelectMode(VenueListItemDto initialItem)
        {
            IsMultiSelectMode = true;
            _suppressSelectionChangedExit = true;
            RunOnUiThread(() =>
            {
                SelectedVenues.ClearRange();
                SelectedVenues.AddRange([initialItem]);
                _suppressSelectionChangedExit = false;
            });
            SelectedCount = 1;
        }

        public void ExitMultiSelectMode()
        {
            IsMultiSelectMode = false;
            RunOnUiThread(() => SelectedVenues.ClearRange());
            SelectedCount = 0;
        }

        private void ToggleSelectAll()
        {
            if (IsAllSelected)
            {
                // Deselect all but remain in multi-select mode (user stays in selection context)
                _suppressSelectionChangedExit = true;
                RunOnUiThread(() =>
                {
                    SelectedVenues.ClearRange();
                    _suppressSelectionChangedExit = false;
                });
                SelectedCount = 0;
                return;
            }
            IsMultiSelectMode = true;
            _suppressSelectionChangedExit = true;
            RunOnUiThread(() =>
            {
                SelectedVenues.ReplaceRange([.. Venues]);
                _suppressSelectionChangedExit = false;
            });
            SelectedCount = Venues.Count;
        }

        public void OnSelectionChanged(int count)
        {
            SelectedCount = count;
            if (IsMultiSelectMode && count == 0 && !_suppressSelectionChangedExit)
                ExitMultiSelectMode();
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
