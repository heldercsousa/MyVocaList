using MyVocaList.UI.Collections;

namespace MyVocaList.UI.ViewModels;

public abstract partial class CrudListViewModelBase<TItem> : ViewModelBase, ICrudListViewModel
{
    private int _currentPage;
    private int _totalCount;
    private string _currentSearchQuery;
    private CancellationTokenSource _searchCts;
    private Func<Task> _pendingConfirmAction;
    private readonly SemaphoreSlim _loadSemaphore = new(1, 1);
    private volatile bool _isLoading;
    private readonly ILogger _logger;

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
    protected abstract void RaiseEntityEmptyStateProperties();

    protected virtual void OnAfterLoad(IReadOnlyList<TItem> items) { }

    public bool IsEmpty => !IsInitialLoading && Items.Count == 0;
    public virtual bool IsEmptyNoResults => IsEmpty && !string.IsNullOrWhiteSpace(SearchText);
    public bool CanEditSelected => SelectedCount == 1;
    public bool CanDeleteSelected => SelectedCount > 0;
    public bool IsAllSelected => Items.Count > 0 && SelectedCount == Items.Count;

    public IAsyncRelayCommand RefreshCommand { get; }
    public IRelayCommand LoadMoreCommand { get; }
    public IRelayCommand DeleteSelectedCommand { get; }
    public IAsyncRelayCommand EditSelectedCommand { get; }
    public IRelayCommand SelectAllCommand { get; }
    public IAsyncRelayCommand ConfirmActionCommand { get; }
    public IRelayCommand DismissConfirmCommand { get; }
    public IRelayCommand OpenSearchCommand { get; }
    public IRelayCommand CloseSearchCommand { get; }

    protected CrudListViewModelBase(ILogger logger)
    {
        _logger = logger;
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        LoadMoreCommand = new RelayCommand(() => _ = LoadMoreAsync());
        DeleteSelectedCommand = new RelayCommand(RequestBatchDelete, () => CanDeleteSelected);
        EditSelectedCommand = new AsyncRelayCommand(ExecuteEditSelectedAsync, () => CanEditSelected);
        SelectAllCommand = new RelayCommand(ToggleSelectAll);
        ConfirmActionCommand = new AsyncRelayCommand(ExecuteConfirmActionAsync);
        DismissConfirmCommand = new RelayCommand(DismissConfirmSheet);
        OpenSearchCommand = new RelayCommand(() => IsSearchMode = true);
        CloseSearchCommand = new RelayCommand(CloseSearch);
    }

    partial void OnSearchTextChanged(string value)
    {
        NotifyEmptyStates();
        TriggerSearchDebounce();
    }

    partial void OnSelectedCountChanged(int value)
    {
        OnPropertyChanged(nameof(CanEditSelected));
        OnPropertyChanged(nameof(CanDeleteSelected));
        OnPropertyChanged(nameof(IsAllSelected));
        DeleteSelectedCommand.NotifyCanExecuteChanged();
        EditSelectedCommand.NotifyCanExecuteChanged();
        OnSelectedCountUpdated(value);
    }

    protected virtual void OnSelectedCountUpdated(int value) { }

    partial void OnIsInitialLoadingChanged(bool value) => NotifyEmptyStates();

    public async Task InitializeAsync()
    {
        IsInitialLoading = true;
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

            var (itemsEnumerable, totalCount) = await FetchPageAsync(
                _currentPage, AppPagination.DefaultPageSize, _currentSearchQuery, cancellationToken);

            if (cancellationToken.IsCancellationRequested) return;

            _totalCount = totalCount;
            var list = itemsEnumerable.ToList();
            HasMoreItems = totalCount > list.Count;

            RunOnUiThread(() =>
            {
                Items.ReplaceRange(list);
                if (SelectedItems.Count > 0)
                {
                    SelectedItems.ClearRange();
                    SelectedCount = 0;
                }
                NotifyEmptyStates();
            });

            OnAfterLoad(list);
        }
        catch (OperationCanceledException) { }
        finally
        {
            if (entered) _loadSemaphore.Release();
        }
    }

    protected async Task RefreshAsync()
    {
        RunOnUiThread(() => IsRefreshing = true);
        await LoadFirstPageAsync(CancellationToken.None);
        RunOnUiThread(() => IsRefreshing = false);
    }

    // Reload without triggering IsInitialLoading — use in response to filter changes.
    protected Task ReloadAsync() => LoadFirstPageAsync(CancellationToken.None);

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
            var (itemsEnumerable, totalCount) = await FetchMoreAsync(
                loadingPage, AppPagination.DefaultPageSize, _currentSearchQuery);

            _totalCount = totalCount;
            var list = itemsEnumerable.ToList();
            var hasMore = (list.Count + Items.Count) < _totalCount;
            _currentPage = loadingPage;

            RunOnUiThread(() =>
            {
                Items.AddRange(list);
                HasMoreItems = hasMore;
                IsRefreshing = false;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load more items (page {Page})", loadingPage);
            RunOnUiThread(() => IsRefreshing = false);
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void TriggerSearchDebounce()
    {
        try { _searchCts?.Cancel(); _searchCts?.Dispose(); }
        catch { /* ignore disposal races */ }

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
            catch (OperationCanceledException) { }
        }, token);
    }

    private async Task ExecuteEditSelectedAsync()
    {
        var item = SelectedItems.FirstOrDefault();
        if (item == null) return;

        RunOnUiThread(() =>
        {
            SelectedItems.ClearRange();
            SelectedCount = 0;
        });
        await NavigateToEditAsync(item);
    }

    private void RequestBatchDelete()
    {
        var selectedItems = SelectedItems.ToList();
        if (selectedItems.Count == 0) return;

        ConfirmMessage = BuildDeleteConfirmMessage(selectedItems);
        ConfirmActionText = "Delete";
        _pendingConfirmAction = async () =>
        {
            await ExecuteDeleteAsync(selectedItems);
            RunOnUiThread(() =>
            {
                SelectedItems.ClearRange();
                SelectedCount = 0;
            });
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
                SelectedItems.ClearRange();
                SelectedCount = 0;
            });
            return;
        }
        if (Items.Count == 0) return;
        RunOnUiThread(() =>
        {
            SelectedItems.ReplaceRange([.. Items]);
            SelectedCount = Items.Count;
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
        OnPropertyChanged(nameof(IsAllSelected));
        RaiseEntityEmptyStateProperties();
    }
}
