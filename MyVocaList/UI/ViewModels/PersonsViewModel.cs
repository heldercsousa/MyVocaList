using MyVocaList.Domain.Entity;
using MyVocaList.UI.Collections;

namespace MyVocaList.UI.ViewModels;

/// <summary>
/// ViewModel for the Singers list page: paging, search, always-on selection, confirm-delete.
/// Add navigates to PersonFormPage via FAB. Edit navigates via FloatingToolbar (single select).
/// </summary>
public partial class PersonsViewModel : ViewModelBase
{
    private readonly IPersonService _personService;
    private readonly ISnackbarComponent _snackbarService;
    private readonly ILogger<PersonsViewModel> _logger;

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

    public PersonsViewModel(
        IPersonService personService,
        ISnackbarComponent snackbarService,
        ILogger<PersonsViewModel> logger)
    {
        _personService = personService;
        _snackbarService = snackbarService;
        _logger = logger;

        Persons = [];
        SelectedPersons = [];

        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        LoadMoreCommand = new RelayCommand(() => _ = LoadMoreAsync());
        AddPersonCommand = new AsyncRelayCommand(NavigateToAddAsync);
        DeleteSelectedCommand = new RelayCommand(RequestBatchDelete, () => CanDeleteSelected);
        EditSelectedCommand = new AsyncRelayCommand(NavigateToEditAsync, () => CanEditSelected);
        SelectAllCommand = new RelayCommand(ToggleSelectAll);
        ConfirmActionCommand = new AsyncRelayCommand(ExecuteConfirmActionAsync);
        DismissConfirmCommand = new RelayCommand(DismissConfirmSheet);
        OpenSearchCommand = new RelayCommand(() => IsSearchMode = true);
        CloseSearchCommand = new RelayCommand(CloseSearch);
    }

    public ObservableRangeCollection<PersonListItemDto> Persons { get; }
    public ObservableRangeCollection<PersonListItemDto> SelectedPersons { get; }

    /// <summary>Non-generic wrapper for binding to DXCollectionView SelectedItems (requires IList).</summary>
    public System.Collections.IList SelectedPersonsRaw => SelectedPersons;

    public string AppBarTitle => SelectedCount == 0 ? "Singers" : $"{SelectedCount} selected";
    public bool CanEditSelected => SelectedCount == 1;
    public bool CanDeleteSelected => SelectedCount > 0;
    public bool IsAllSelected => Persons.Count > 0 && SelectedCount == Persons.Count;

    public bool IsEmpty => !IsInitialLoading && Persons.Count == 0;
    public bool IsEmptyNoPersons => IsEmpty && string.IsNullOrWhiteSpace(SearchText);
    public bool IsEmptyNoResults => IsEmpty && !string.IsNullOrWhiteSpace(SearchText);

    public IAsyncRelayCommand RefreshCommand { get; }
    public IRelayCommand LoadMoreCommand { get; }
    public IAsyncRelayCommand AddPersonCommand { get; }
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

            var (itemsEnumerable, totalCount) = await _personService.GetPagedPersonsForListAsync(
                _currentPage, AppPagination.DefaultPageSize, _currentSearchQuery, cancellationToken);

            if (cancellationToken.IsCancellationRequested) return;

            _totalCount = totalCount;
            var list = itemsEnumerable.ToList();
            HasMoreItems = totalCount > list.Count;

            RunOnUiThread(() =>
            {
                Persons.ReplaceRange(list);
                if (SelectedPersons.Count > 0)
                {
                    SelectedPersons.ClearRange();
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
            var (itemsEnumerable, totalCount) = await _personService.GetPagedPersonsForListAsync(
                loadingPage, AppPagination.DefaultPageSize, _currentSearchQuery);

            _totalCount = totalCount;
            var list = itemsEnumerable.ToList();
            var hasMore = (list.Count + Persons.Count) < _totalCount;
            _currentPage = loadingPage;

            RunOnUiThread(() =>
            {
                Persons.AddRange(list);
                HasMoreItems = hasMore;
                IsRefreshing = false;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load more singers (page {Page})", loadingPage);
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
            catch (OperationCanceledException) { /* ignore */ }
        }, token);
    }

    private Task NavigateToAddAsync() =>
        Shell.Current?.GoToAsync(Routes.PersonForm) ?? Task.CompletedTask;

    private async Task NavigateToEditAsync()
    {
        var item = SelectedPersons.FirstOrDefault();
        if (item == null) return;

        RunOnUiThread(() =>
        {
            SelectedPersons.ClearRange();
            SelectedCount = 0;
        });

        var name = Uri.EscapeDataString(item.FullName);
        var birthday = Uri.EscapeDataString(item.BirthdayDayMonth ?? string.Empty);
        var email = Uri.EscapeDataString(item.Email ?? string.Empty);

        await (Shell.Current?.GoToAsync(
            $"{Routes.PersonForm}?personId={item.Id}&personName={name}&personBirthday={birthday}&personEmail={email}") ?? Task.CompletedTask);
    }

    private void RequestBatchDelete()
    {
        var selectedItems = SelectedPersons.ToList();
        if (selectedItems.Count == 0) return;

        ConfirmMessage = $"Delete {selectedItems.Count} singer(s)?";
        ConfirmActionText = "Delete";
        _pendingConfirmAction = async () =>
        {
            var ids = selectedItems.Select(p => p.Id);
            var (success, message) = await _personService.DeletePersonsAsync(ids);
            RunOnUiThread(() =>
            {
                SelectedPersons.ClearRange();
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
                SelectedPersons.ClearRange();
                SelectedCount = 0;
            });
            return;
        }
        if (Persons.Count == 0) return;
        RunOnUiThread(() =>
        {
            SelectedPersons.ReplaceRange([.. Persons]);
            SelectedCount = Persons.Count;
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
        OnPropertyChanged(nameof(IsEmptyNoPersons));
        OnPropertyChanged(nameof(IsEmptyNoResults));
        OnPropertyChanged(nameof(IsAllSelected));
    }
}
