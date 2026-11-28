using MyVocaList.Domain.Constants;
using MyVocaList.UI.Collections;
using MyVocaList.UI.Services;

namespace MyVocaList.UI.ViewModels;

public abstract partial class CrudListViewModelBase<TItem> : ViewModelBase, ICrudListViewModel
{
    private int _currentPage;
    private int _totalCount;
    private string _currentSearchQuery;
    private CancellationTokenSource _searchCts;
    private Func<Task> _pendingConfirmAction;
    private volatile bool _isLoading;
    // Bumped (on the UI thread) by every load that shows the shimmer; only the newest one may end it (REQ-DLH-05).
    private int _loadGeneration;
    private int _queryGeneration; // BUG-089: bumped by every first-page load (search, reload, refresh)
    private readonly ILogger _logger;

    /// <summary>True once a load has applied its rows, i.e. the first load completed. Set on the UI thread.</summary>
    protected bool HasCompletedFirstLoad { get; private set; }

    [ObservableProperty] private string _appBarNavigationIcon = "arrow_back_outlined";
    [ObservableProperty] private ICommand _appBarNavigationCommand;

    [ObservableProperty] private bool _isRefreshing;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private bool _isScrolled;
    [ObservableProperty] private int _selectedCount;
    [ObservableProperty] private SheetState _confirmSheetState = SheetState.Hidden;
    [ObservableProperty] private bool _hasMoreItems = true;
    /// <summary>
    /// True while the list is replacing its rows: first load, later visit, debounced search, filter reload. CrudListView
    /// shows the shimmer in place of the rows until the newest such load applies them (REQ-DLH-05). Pull-to-refresh
    /// and load-more never set it.
    /// </summary>
    [ObservableProperty] private bool _isInitialLoading = true;
    [ObservableProperty] private string _confirmMessage = string.Empty;
    [ObservableProperty] private string _confirmActionText = "Delete";
    [ObservableProperty] private bool _isSelectionMode;
    [ObservableProperty] private SheetState _itemActionsSheetState = SheetState.Hidden;
    [ObservableProperty] private bool _isItemMenuOpen;

    private IReadOnlyList<ItemMenuAction> _itemMenuActions;

    /// <summary>The item the item actions sheet or the item overflow menu acts on; <c>default</c> when both are closed.</summary>
    protected TItem ItemActionsTarget { get; private set; }

    /// <summary>Page-specific overflow menu items. Built once, on first read, by <see cref="BuildItemMenuActions"/>.</summary>
    public IReadOnlyList<ItemMenuAction> ItemMenuActions => _itemMenuActions ??= BuildItemMenuActions();

    /// <summary>Override in a derived ViewModel to add page-specific items between Edit and Delete. Default: none.</summary>
    protected virtual IReadOnlyList<ItemMenuAction> BuildItemMenuActions() => [];

    /// <summary>Typed helper for derived ViewModels: wraps a typed action so it receives the target list item.</summary>
    protected static ItemMenuAction MenuAction(string text, string icon, Func<TItem, Task> execute) =>
        new(text, icon, item => execute((TItem)item));

    private IReadOnlyList<MenuItemModel> _itemMenuItems;

    /// <summary>The item menu entries in display order: Edit, the page-specific actions, Delete. Built once, on first read.</summary>
    public IReadOnlyList<MenuItemModel> ItemMenuItems => _itemMenuItems ??= BuildItemMenuItems();

    private IReadOnlyList<MenuItemModel> BuildItemMenuItems()
    {
        var items = new List<MenuItemModel> { new("Edit", "edit_outlined", EditItemCommand) };
        items.AddRange(ItemMenuActions.Select(a => new MenuItemModel(a.Text, a.Icon, ExecuteItemMenuActionCommand, a)));
        items.Add(new MenuItemModel("Delete", "delete_outlined", DeleteItemCommand));
        return items;
    }

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
    public bool CanDeleteSelected => SelectedCount > 0;
    public bool IsAllSelected => Items.Count > 0 && SelectedCount == Items.Count;

    public IAsyncRelayCommand RefreshCommand { get; }
    public IRelayCommand LoadMoreCommand { get; }
    public IRelayCommand DeleteSelectedCommand { get; }
    public IRelayCommand SelectAllCommand { get; }
    public IAsyncRelayCommand ConfirmActionCommand { get; }
    public IRelayCommand DismissConfirmCommand { get; }
    public IAsyncRelayCommand<object> ItemTapCommand { get; }
    public IRelayCommand<object> ItemLongPressCommand { get; }
    public IRelayCommand<object> ToggleItemSelectionCommand { get; }
    public IAsyncRelayCommand EditItemCommand { get; }
    public IRelayCommand DeleteItemCommand { get; }
    public IRelayCommand DismissItemActionsCommand { get; }
    public ICommand OpenItemMenuCommand { get; }
    public ICommand DismissItemMenuCommand { get; }
    public ICommand ExecuteItemMenuActionCommand { get; }

    protected CrudListViewModelBase(ILogger logger)
    {
        _logger = logger;
        _appBarNavigationCommand = new Command(() => { });
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        LoadMoreCommand = new RelayCommand(() => _ = LoadMoreAsync());
        DeleteSelectedCommand = new RelayCommand(() => RequestDelete(SelectedItems.ToList()), () => CanDeleteSelected);
        SelectAllCommand = new RelayCommand(ToggleSelectAll);
        ConfirmActionCommand = new AsyncRelayCommand(ExecuteConfirmActionAsync);
        DismissConfirmCommand = new RelayCommand(DismissConfirmSheet);
        ItemTapCommand = new AsyncRelayCommand<object>(OnItemTappedAsync);
        ItemLongPressCommand = new RelayCommand<object>(OnItemLongPressed);
        ToggleItemSelectionCommand = new RelayCommand<object>(ToggleItemSelection);
        EditItemCommand = new AsyncRelayCommand(EditItemAsync);
        DeleteItemCommand = new RelayCommand(DeleteItem);
        DismissItemActionsCommand = new RelayCommand(CloseItemActions);
        OpenItemMenuCommand = new RelayCommand<object>(OpenItemMenu);
        DismissItemMenuCommand = new RelayCommand(CloseItemActions);
        ExecuteItemMenuActionCommand = new AsyncRelayCommand<ItemMenuAction>(ExecuteItemMenuActionAsync);
    }

    // TwoWay write-back from the popup (outside tap, own close) and VM-driven closes both land here.
    // Closing always clears the target; Edit/Delete/page actions capture it before closing.
    partial void OnIsItemMenuOpenChanged(bool value)
    {
        if (!value)
            ItemActionsTarget = default;
    }

    partial void OnSearchTextChanged(string value)
    {
        NotifyEmptyStates();
        TriggerSearchDebounce();
    }

    partial void OnSelectedCountChanged(int value)
    {
        OnPropertyChanged(nameof(CanDeleteSelected));
        OnPropertyChanged(nameof(IsAllSelected));
        DeleteSelectedCommand.NotifyCanExecuteChanged();
        IsSelectionMode = value > 0;
        OnSelectedCountUpdated(value);
    }

    protected virtual void OnSelectedCountUpdated(int value) { }

    partial void OnIsInitialLoadingChanged(bool value) => NotifyEmptyStates();

    public async Task InitializeAsync()
    {
        // PHASE2-INSTRUMENTATION: remove after page-load-frozen is closed.
        var initSw = System.Diagnostics.Stopwatch.StartNew();

        // REQ-DLH-05: the first load and every later visit replace the rows, so both show the shimmer until the
        // new rows apply; REQ-DLH-01: the debug-only hold applies to each of them.
        var generation = BeginShimmeredLoad();
        await Task.Yield();
        await LoadFirstPageAsync(CancellationToken.None, generation);

        // PHASE2-INSTRUMENTATION: remove after page-load-frozen is closed.
        initSw.Stop();
        _logger.LogInformation("[PageLoad] {ViewModel} initAsync={Ms}ms", GetType().Name, initSw.ElapsedMilliseconds);
    }

    /// <summary>
    /// Fetches the first page and replaces the rows. <paramref name="shimmerGeneration"/> comes from
    /// <see cref="BeginShimmeredLoad"/> for a load that shows the shimmer (REQ-DLH-05); null is pull-to-refresh, which
    /// neither shows the shimmer nor takes the debug hold (REQ-DLH-05, REQ-DLH-01).
    /// </summary>
    private async Task LoadFirstPageAsync(CancellationToken cancellationToken, int? shimmerGeneration = null)
    {
        var applied = false;
        // BUG-089: every first-page load (search, reload, refresh) starts a new query generation. A load-more or a
        // refresh that started under an older generation must not change the rows.
        var queryGeneration = Interlocked.Increment(ref _queryGeneration);
        try
        {
            _currentPage = 1;
            _currentSearchQuery = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim();

            // REQ-DLH-01: the hold starts with the fetch and is awaited after it, so the shimmer lasts max(fetch, hold)
            // and the UI thread never waits. Zero duration (every build but Debug, and every unit test) = completed task.
            var hold = shimmerGeneration.HasValue ? LoadingHold.WaitAsync(cancellationToken) : Task.CompletedTask;

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

            await hold;

            // A superseded shimmered load (a newer search or reload started) applies nothing: the newest load owns the rows and the shimmer.
            if (cancellationToken.IsCancellationRequested || IsSuperseded(shimmerGeneration) || IsStaleQuery(queryGeneration)) return;

            _totalCount = totalCount;
            var hasMore = totalCount > list.Count;

            RunOnUiThread(() =>
            {
                // BUG-089 B8: re-check on the UI thread, where the rows are applied; a newer load may have started since the check above.
                if (IsSuperseded(shimmerGeneration) || IsStaleQuery(queryGeneration)) return;

                Items.ReplaceRange(list);
                HasMoreItems = hasMore;
                if (SelectedItems.Count > 0)
                {
                    SelectedItems.ClearRange();
                    SelectedCount = 0;
                }
                // REQ-DLH-05: the newest shimmered load ends the shimmer in the same block that applies its rows.
                if (shimmerGeneration.HasValue && shimmerGeneration.Value == CurrentLoadGeneration)
                    IsInitialLoading = false;
                HasCompletedFirstLoad = true;
                NotifyEmptyStates();
            });
            applied = true;

            OnAfterLoad(list);
        }
        catch (OperationCanceledException) { }
        finally
        {
            // A shimmered load that ends without applying its rows (cancelled or failed) ends the shimmer only while it is still the newest.
            if (!applied && shimmerGeneration.HasValue)
                RunOnUiThread(() =>
                {
                    if (shimmerGeneration.Value == CurrentLoadGeneration)
                        IsInitialLoading = false;
                });
        }
    }

    /// <summary>
    /// Starts a load that replaces the rows and shows the shimmer (REQ-DLH-05). Returns its generation; only the newest
    /// generation may end the shimmer. Called on the UI thread.
    /// </summary>
    private int BeginShimmeredLoad()
    {
        var generation = Interlocked.Increment(ref _loadGeneration);
        RunOnUiThread(() => IsInitialLoading = true);
        return generation;
    }

    private int CurrentLoadGeneration => Volatile.Read(ref _loadGeneration);

    private bool IsSuperseded(int? shimmerGeneration) =>
        shimmerGeneration.HasValue && shimmerGeneration.Value != CurrentLoadGeneration;

    // BUG-089: true once a newer first-page load has started after the given query generation.
    private bool IsStaleQuery(int queryGeneration) => Volatile.Read(ref _queryGeneration) != queryGeneration;

    protected async Task RefreshAsync()
    {
        RunOnUiThread(() => IsRefreshing = true);
        await LoadFirstPageAsync(CancellationToken.None);
        RunOnUiThread(() => IsRefreshing = false);
    }

    // Filter reload: shows the shimmer until its rows apply (REQ-DLH-05). Called on the UI thread.
    protected Task ReloadAsync() => LoadFirstPageAsync(CancellationToken.None, BeginShimmeredLoad());

    private async Task LoadMoreAsync()
    {
        if (_isLoading || !HasMoreItems)
        {
            RunOnUiThread(() => IsRefreshing = false);
            return;
        }

        _isLoading = true;
        var loadingPage = 0;

        try
        {
            loadingPage = _currentPage + 1;

            // BUG-089: the page belongs to the query captured here; a newer first-page load makes it stale.
            var queryGeneration = Volatile.Read(ref _queryGeneration);
            var query = _currentSearchQuery;

            // SQLITE-WORKAROUND: same offload as LoadFirstPageAsync — SQLite query + lazy
            // projection enumeration must not run on the UI thread.
            var (list, totalCount) = await Task.Run(async () =>
            {
                var (itemsEnumerable, total) = await FetchMoreAsync(
                    loadingPage, AppPagination.DefaultPageSize, query);
                return (itemsEnumerable.ToList(), total);
            });

            if (IsStaleQuery(queryGeneration))
            {
                RunOnUiThread(() => IsRefreshing = false);
                return;
            }

            _totalCount = totalCount;
            var hasMore = (list.Count + Items.Count) < _totalCount;
            _currentPage = loadingPage;

            RunOnUiThread(() =>
            {
                if (IsStaleQuery(queryGeneration)) // BUG-089: re-check on the UI thread, where the rows are appended
                {
                    IsRefreshing = false;
                    return;
                }

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
        // Cancel only: the old task may still use its token, and its own finally disposes the source.
        try { _searchCts?.Cancel(); } catch (ObjectDisposedException) { /* already released by its task */ }

        var cts = new CancellationTokenSource();
        _searchCts = cts;
        var token = cts.Token;

        // REQ-DLH-05: the shimmer shows from the keystroke until the newest search applies its rows.
        var generation = BeginShimmeredLoad();

        // B9: the task is not given the token, so it always runs and its finally releases the last source
        // (a cancelled-before-start task would skip the finally). Every source is disposed there.
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(SearchConstants.AutocompleteDebounceMilliseconds, token);
                if (token.IsCancellationRequested) return;
                await LoadFirstPageAsync(token, generation);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                // B5: the full detail goes to the log only; the current rows stay. The base has no user-message mechanism.
                _logger.LogError(ex, "CRUD list search failed");
            }
            finally
            {
                if (ReferenceEquals(_searchCts, cts)) _searchCts = null;
                cts.Dispose();
            }
        });
    }

    private void RequestDelete(IList<TItem> items)
    {
        if (items.Count == 0) return;

        ConfirmMessage = BuildDeleteConfirmMessage(items);
        ConfirmActionText = "Delete";
        _pendingConfirmAction = async () =>
        {
            await ExecuteDeleteAsync(items);
            RunOnUiThread(() =>
            {
                SelectedItems.ClearRange();
                SelectedCount = 0;
            });
        };
        ConfirmSheetState = SheetState.HalfExpanded;
    }

    private async Task ExecuteConfirmActionAsync()
    {
        var action = _pendingConfirmAction;
        DismissConfirmSheet();
        if (action != null)
            // Offload the SQLite delete (+ TransactionLogInterceptor JSON work) to the
            // thread pool. Concrete deletes end with a reload; offloading here keeps
            // that reload off the UI thread too.
            await Task.Run(action);
    }

    private void DismissConfirmSheet()
    {
        ConfirmSheetState = SheetState.Hidden;
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

    /// <inheritdoc />
    public void OnSelectionChanged(int count)
    {
        SelectedCount = count;
    }

    /// <inheritdoc />
    public bool HandleBack()
    {
        if (IsItemMenuOpen)
        {
            CloseItemActions();
            return true;
        }

        if (ItemActionsSheetState != SheetState.Hidden)
        {
            CloseItemActions();
            return true;
        }

        if (ConfirmSheetState != SheetState.Hidden)
        {
            DismissConfirmSheet();
            return true;
        }

        if (IsSelectionMode)
        {
            RunOnUiThread(() =>
            {
                SelectedItems.ClearRange();
                SelectedCount = 0;
            });
            return true;
        }

        return false;
    }

    private Task OnItemTappedAsync(object parameter)
    {
        // With the menu open, a tap only dismisses it (no scrim; the tap may reach the row beneath) (REQ-OVFM-15).
        if (IsItemMenuOpen)
        {
            CloseItemActions();
            return Task.CompletedTask;
        }

        if (IsSelectionMode || parameter is not TItem item)
            return Task.CompletedTask;

        return NavigateToEditAsync(item);
    }

    private void OnItemLongPressed(object parameter)
    {
        // With the menu open, a long-press only dismisses it; it must not start selection (REQ-OVFM-16).
        if (IsItemMenuOpen)
        {
            CloseItemActions();
            return;
        }

        if (parameter is not TItem item) return;

        if (!IsSelectionMode)
            BeginSelection(item);
        else
            OpenItemActions(item);
    }

    private void ToggleItemSelection(object parameter)
    {
        if (parameter is not TItem item) return;

        if (!IsSelectionMode)
        {
            BeginSelection(item);
            return;
        }

        RunOnUiThread(() =>
        {
            if (!SelectedItems.Remove(item))
                SelectedItems.Add(item);
            SelectedCount = SelectedItems.Count;
        });
    }

    private void BeginSelection(TItem item)
    {
        // Set first so the view switches selection to Multiple before the item is added.
        IsSelectionMode = true;
        RunOnUiThread(() =>
        {
            SelectedItems.ReplaceRange([item]);
            SelectedCount = 1;
        });
    }

    private void OpenItemActions(TItem item)
    {
        ItemActionsTarget = item;
        ItemActionsSheetState = SheetState.HalfExpanded;
    }

    private void OpenItemMenu(object parameter)
    {
        if (IsSelectionMode || parameter is not TItem item) return;

        ItemActionsTarget = item;
        IsItemMenuOpen = true;
    }

    private void CloseItemActions()
    {
        ItemActionsSheetState = SheetState.Hidden;
        IsItemMenuOpen = false;
        ItemActionsTarget = default;
    }

    private async Task ExecuteItemMenuActionAsync(ItemMenuAction action)
    {
        var target = ItemActionsTarget;
        CloseItemActions();
        if (target is not null && action is not null)
            await action.Execute(target);
    }

    private async Task EditItemAsync()
    {
        var target = ItemActionsTarget;
        CloseItemActions();
        if (target is not null)
            await NavigateToEditAsync(target);
    }

    private void DeleteItem()
    {
        var target = ItemActionsTarget;
        CloseItemActions();
        if (target is not null)
            RequestDelete([target]);
    }

    private void NotifyEmptyStates()
    {
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(IsAllSelected));
        RaiseEntityEmptyStateProperties();
    }
}
