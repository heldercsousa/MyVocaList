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
        private volatile bool _isLoading;

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
            DeleteSelectedCommand.NotifyCanExecuteChanged();
            EditSelectedCommand.NotifyCanExecuteChanged();
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

                var (itemsEnumerable, totalCount) = await _venueService.GetPagedVenuesForListAsync(
                    _currentPage, AppPagination.DefaultPageSize, _currentSearchQuery);

                if (cancellationToken.IsCancellationRequested) return;

                _totalCount = totalCount;
                var list = itemsEnumerable.ToList();
                HasMoreItems = totalCount > list.Count;

                RunOnUiThread(() =>
                {
                    Venues.ReplaceRange(list);
                    if (SelectedVenues.Count > 0)
                    {
                        SelectedVenues.ClearRange();
                        SelectedCount = 0;
                    }
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
                RunOnUiThread(() =>
                {
                    SelectedVenues.ClearRange();
                    SelectedCount = 0;
                });
                return;
            }
            if (Venues.Count == 0) return;
            RunOnUiThread(() =>
            {
                SelectedVenues.ReplaceRange([.. Venues]);
                SelectedCount = Venues.Count;
            });
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
