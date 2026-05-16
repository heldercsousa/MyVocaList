using MyVocaList.Domain.RepositoryInterface;
using MyVocaList.UI.Collections;

namespace MyVocaList.UI.ViewModels;

[QueryProperty(nameof(Mode), "mode")]
public partial class ArtistsViewModel : ViewModelBase
{
    private readonly IArtistService _artistService;
    private readonly ISnackbarComponent _snackbarService;
    private readonly ILogger<ArtistsViewModel> _logger;

    private int _currentPage;
    private int _totalCount;
    private string _currentSearchQuery;
    private CancellationTokenSource _searchCts;
    private Func<Task> _pendingConfirmAction;

    private readonly SemaphoreSlim _loadSemaphore = new(1, 1);
    private volatile bool _isLoading;

    [ObservableProperty] private ArtistRoleFilter _roleFilter = ArtistRoleFilter.All;
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

    public ArtistsViewModel(
        IArtistService artistService,
        ISnackbarComponent snackbarService,
        ILogger<ArtistsViewModel> logger)
    {
        _artistService = artistService;
        _snackbarService = snackbarService;
        _logger = logger;

        Artists = [];
        SelectedArtists = [];

        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        LoadMoreCommand = new RelayCommand(() => _ = LoadMoreAsync());
        AddArtistCommand = new AsyncRelayCommand(NavigateToAddAsync);
        DeleteSelectedCommand = new RelayCommand(RequestBatchDelete, () => CanDeleteSelected);
        EditSelectedCommand = new AsyncRelayCommand(NavigateToEditAsync, () => CanEditSelected);
        SelectAllCommand = new RelayCommand(ToggleSelectAll);
        ConfirmActionCommand = new AsyncRelayCommand(ExecuteConfirmActionAsync);
        DismissConfirmCommand = new RelayCommand(DismissConfirmSheet);
        OpenSearchCommand = new RelayCommand(() => IsSearchMode = true);
        CloseSearchCommand = new RelayCommand(CloseSearch);
        ViewCatalogCommand = new RelayCommand<ArtistListItemDto>(NavigateToCatalog);
    }

    public ObservableRangeCollection<ArtistListItemDto> Artists { get; }
    public ObservableRangeCollection<ArtistListItemDto> SelectedArtists { get; }

    public System.Collections.IList SelectedArtistsRaw => SelectedArtists;

    public string Mode
    {
        set => RoleFilter = value switch
        {
            "author"    => ArtistRoleFilter.AuthorsOnly,
            "performer" => ArtistRoleFilter.PerformersOnly,
            _           => ArtistRoleFilter.All
        };
    }

    public string AppBarTitle => SelectedCount > 0 ? $"{SelectedCount} selected" : RoleFilter switch
    {
        ArtistRoleFilter.AuthorsOnly    => "Authors",
        ArtistRoleFilter.PerformersOnly => "Performers",
        _                               => "Artists"
    };
    public bool CanEditSelected => SelectedCount == 1;
    public bool CanDeleteSelected => SelectedCount > 0;
    public bool IsAllSelected => Artists.Count > 0 && SelectedCount == Artists.Count;

    public bool IsEmpty => !IsInitialLoading && Artists.Count == 0;
    public bool IsEmptyNoArtists => IsEmpty && string.IsNullOrWhiteSpace(SearchText);
    public bool IsEmptyNoResults => IsEmpty && !string.IsNullOrWhiteSpace(SearchText);

    public IAsyncRelayCommand RefreshCommand { get; }
    public IRelayCommand LoadMoreCommand { get; }
    public IAsyncRelayCommand AddArtistCommand { get; }
    public IRelayCommand DeleteSelectedCommand { get; }
    public IAsyncRelayCommand EditSelectedCommand { get; }
    public IRelayCommand SelectAllCommand { get; }
    public IAsyncRelayCommand ConfirmActionCommand { get; }
    public IRelayCommand DismissConfirmCommand { get; }
    public IRelayCommand OpenSearchCommand { get; }
    public IRelayCommand CloseSearchCommand { get; }
    public IRelayCommand<ArtistListItemDto> ViewCatalogCommand { get; }

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

    partial void OnRoleFilterChanged(ArtistRoleFilter value)
    {
        OnPropertyChanged(nameof(AppBarTitle));
        _ = LoadFirstPageAsync(CancellationToken.None);
    }

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

            var (itemsEnumerable, totalCount) = await _artistService.GetPagedArtistsForListAsync(
                _currentPage, AppPagination.DefaultPageSize, _currentSearchQuery, RoleFilter, cancellationToken);

            if (cancellationToken.IsCancellationRequested) return;

            _totalCount = totalCount;
            var list = itemsEnumerable.ToList();
            HasMoreItems = totalCount > list.Count;

            RunOnUiThread(() =>
            {
                Artists.ReplaceRange(list);
                if (SelectedArtists.Count > 0)
                {
                    SelectedArtists.ClearRange();
                    SelectedCount = 0;
                }
                NotifyEmptyStates();
            });
        }
        catch (OperationCanceledException) { }
        finally
        {
            if (entered) _loadSemaphore.Release();
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
            var (itemsEnumerable, totalCount) = await _artistService.GetPagedArtistsForListAsync(
                loadingPage, AppPagination.DefaultPageSize, _currentSearchQuery, RoleFilter);

            _totalCount = totalCount;
            var list = itemsEnumerable.ToList();
            var hasMore = (list.Count + Artists.Count) < _totalCount;
            _currentPage = loadingPage;

            RunOnUiThread(() =>
            {
                Artists.AddRange(list);
                HasMoreItems = hasMore;
                IsRefreshing = false;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load more artists (page {Page})", loadingPage);
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

    private Task NavigateToAddAsync() =>
        Shell.Current.GoToAsync(Routes.ArtistForm);

    private async Task NavigateToEditAsync()
    {
        var item = SelectedArtists.FirstOrDefault();
        if (item == null) return;

        RunOnUiThread(() =>
        {
            SelectedArtists.ClearRange();
            SelectedCount = 0;
        });
        await Shell.Current.GoToAsync(
            $"{Routes.ArtistForm}?artistId={item.Id}&artistName={Uri.EscapeDataString(item.Name)}");
    }

    private void RequestBatchDelete()
    {
        var selectedItems = SelectedArtists.ToList();
        if (selectedItems.Count == 0) return;

        ConfirmMessage = selectedItems.Count == 1
            ? $"Delete '{selectedItems[0].Name}'?"
            : $"Delete {selectedItems.Count} artists?";
        ConfirmActionText = "Delete";
        _pendingConfirmAction = async () =>
        {
            var ids = selectedItems.Select(a => a.Id);
            var (success, message) = await _artistService.DeleteArtistsAsync(ids);
            RunOnUiThread(() =>
            {
                SelectedArtists.ClearRange();
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
                SelectedArtists.ClearRange();
                SelectedCount = 0;
            });
            return;
        }
        if (Artists.Count == 0) return;
        RunOnUiThread(() =>
        {
            SelectedArtists.ReplaceRange([.. Artists]);
            SelectedCount = Artists.Count;
        });
    }

    public void OnSelectionChanged(int count)
    {
        SelectedCount = count;
    }

    private void NavigateToCatalog(ArtistListItemDto artist)
    {
        if (artist == null) return;
        _ = Shell.Current.GoToAsync(
            $"{Routes.Songs}?artistId={artist.Id}&artistName={Uri.EscapeDataString(artist.Name)}");
    }

    private void CloseSearch()
    {
        IsSearchMode = false;
        SearchText = string.Empty;
    }

    private void NotifyEmptyStates()
    {
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(IsEmptyNoArtists));
        OnPropertyChanged(nameof(IsEmptyNoResults));
        OnPropertyChanged(nameof(IsAllSelected));
    }
}
