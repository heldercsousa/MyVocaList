using MyVocaList.UI.Collections;

namespace MyVocaList.UI.ViewModels;

public abstract partial class CrudListViewModelBase<TItem> : ViewModelBase, ICrudListViewModel
{
    private int _currentPage;
    private int _totalCount;
    private string _currentSearchQuery;
    private CancellationTokenSource _searchCts;
    private Func<Task> _pendingConfirmAction;
    // Static: all CRUD list ViewModels share one effectively-singleton AppDbContext
    // (MAUI has no per-page scope), so at most one DB load may run at a time app-wide.
    // SQLITE-WORKAROUND: remove this gate when SQLite is replaced (INFRA_MSSQL) —
    // see constraints-registry.md § EF Core / SQLite and DevCycleCraft/page-load-frozen/plan.md.
    private static readonly SemaphoreSlim DbLoadGate = new(1, 1);
    private volatile bool _isLoading;
    private bool _hasLoadedOnce;
    private readonly ILogger _logger;

    [ObservableProperty] private string _appBarNavigationIcon = "arrow_back_outlined";
    [ObservableProperty] private ICommand _appBarNavigationCommand;

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
        _appBarNavigationCommand = new Command(() => { });
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
        // PHASE2-INSTRUMENTATION: remove after page-load-frozen is closed.
        var initSw = System.Diagnostics.Stopwatch.StartNew();

        if (_hasLoadedOnce)
        {
            // Shell-cached page revisit: data is already present — refresh silently
            // without touching IsInitialLoading (no shimmer subtree swap on revisit).
            // Residual: the silent ReplaceRange Reset is still visible (accepted — see plan.md T3).
            await LoadFirstPageAsync(CancellationToken.None);
        }
        else
        {
            IsInitialLoading = true;
            await Task.Yield();
            await LoadFirstPageAsync(CancellationToken.None);
            RunOnUiThread(() => IsInitialLoading = false);
        }

        // PHASE2-INSTRUMENTATION: remove after page-load-frozen is closed.
        initSw.Stop();
        _logger.LogInformation("[PageLoad] {ViewModel} initAsync={Ms}ms", GetType().Name, initSw.ElapsedMilliseconds);
    }

    private async Task LoadFirstPageAsync(CancellationToken cancellationToken)
    {
        var entered = false;
        try
        {
            await DbLoadGate.WaitAsync(cancellationToken);
            entered = true;

            _currentPage = 1;
            _currentSearchQuery = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim();

            // SQLITE-WORKAROUND: Microsoft.Data.Sqlite completes async methods synchronously
            // on the calling thread — offload fetch AND enumeration (services return lazy
            // projections) to the thread pool so the query never runs on the UI thread.
            // Re-evaluate when SQLite is replaced (INFRA_MSSQL); a truly async provider
            // does not need this offload.
            // PHASE2-INSTRUMENTATION: remove after page-load-frozen is closed.
            var fetchSw = System.Diagnostics.Stopwatch.StartNew();
            var (list, totalCount) = await Task.Run(async () =>
            {
                var (itemsEnumerable, total) = await FetchPageAsync(
                    _currentPage, AppPagination.DefaultPageSize, _currentSearchQuery, cancellationToken);
                return (itemsEnumerable.ToList(), total);
            }, cancellationToken);
            fetchSw.Stop();
            _logger.LogInformation("[PageLoad] {ViewModel} fetch={Ms}ms", GetType().Name, fetchSw.ElapsedMilliseconds);
            // PHASE2-INSTRUMENTATION end

            if (cancellationToken.IsCancellationRequested) return;

            _totalCount = totalCount;
            var hasMore = totalCount > list.Count;

            RunOnUiThread(() =>
            {
                Items.ReplaceRange(list);
                HasMoreItems = hasMore;
                if (SelectedItems.Count > 0)
                {
                    SelectedItems.ClearRange();
                    SelectedCount = 0;
                }
                NotifyEmptyStates();
            });

            // Set AFTER the RunOnUiThread block — this point is reached only when the
            // fetch completed without cancellation and the list was applied successfully.
            // Cancellation paths (IsCancellationRequested early return and OperationCanceledException
            // catch) do NOT set this flag, so a cancelled first load retries the shimmer cycle.
            _hasLoadedOnce = true;

            OnAfterLoad(list);
        }
        catch (OperationCanceledException) { }
        finally
        {
            if (entered) DbLoadGate.Release();
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
        var entered = false;
        var loadingPage = 0;

        try
        {
            await DbLoadGate.WaitAsync();
            entered = true;

            // Read the page number AFTER the gate: a first-page load (search/refresh)
            // holding the gate may reset _currentPage before this load-more runs.
            loadingPage = _currentPage + 1;

            // SQLITE-WORKAROUND: same offload as LoadFirstPageAsync — SQLite query + lazy
            // projection enumeration must not run on the UI thread.
            var (list, totalCount) = await Task.Run(async () =>
            {
                var (itemsEnumerable, total) = await FetchMoreAsync(
                    loadingPage, AppPagination.DefaultPageSize, _currentSearchQuery);
                return (itemsEnumerable.ToList(), total);
            });

            _totalCount = totalCount;
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
            if (entered) DbLoadGate.Release();
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
            // Offload the SQLite delete (+ TransactionLogInterceptor JSON work) to the
            // thread pool. Do NOT hold DbLoadGate here — concrete deletes end with a
            // reload that acquires the gate internally (would deadlock).
            await Task.Run(action);
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
