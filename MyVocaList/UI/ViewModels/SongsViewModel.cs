using MyVocaList.UI.Collections;

namespace MyVocaList.UI.ViewModels;

[QueryProperty(nameof(ArtistIdRaw), "artistId")]
[QueryProperty(nameof(ArtistName), "artistName")]
public partial class SongsViewModel : ViewModelBase
{
    private readonly ISongService _songService;
    private readonly ISnackbarComponent _snackbarService;
    private readonly ILogger<SongsViewModel> _logger;

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
    [ObservableProperty] private int _artistId;
    [ObservableProperty] private string _artistName = string.Empty;

    public string ArtistIdRaw { set => ArtistId = int.TryParse(value, out var id) ? id : 0; }

    public SongsViewModel(
        ISongService songService,
        ISnackbarComponent snackbarService,
        ILogger<SongsViewModel> logger)
    {
        _songService = songService;
        _snackbarService = snackbarService;
        _logger = logger;

        Songs = [];
        SelectedSongs = [];

        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        LoadMoreCommand = new RelayCommand(() => _ = LoadMoreAsync());
        AddSongCommand = new AsyncRelayCommand(NavigateToAddAsync);
        DeleteSelectedCommand = new RelayCommand(RequestBatchDelete, () => CanDeleteSelected);
        EditSelectedCommand = new AsyncRelayCommand(NavigateToEditAsync, () => CanEditSelected);
        SelectAllCommand = new RelayCommand(ToggleSelectAll);
        ConfirmActionCommand = new AsyncRelayCommand(ExecuteConfirmActionAsync);
        DismissConfirmCommand = new RelayCommand(DismissConfirmSheet);
        OpenSearchCommand = new RelayCommand(() => IsSearchMode = true);
        CloseSearchCommand = new RelayCommand(CloseSearch);
    }

    public ObservableRangeCollection<SongListItemDto> Songs { get; }
    public ObservableRangeCollection<SongListItemDto> SelectedSongs { get; }

    public System.Collections.IList SelectedSongsRaw => SelectedSongs;

    /// <summary>Title bar always shows the artist name; subtitle shows selection count when > 0.</summary>
    public string AppBarSubtitle => SelectedCount > 0 ? $"{SelectedCount} selected" : string.Empty;

    public bool CanEditSelected => SelectedCount == 1;
    public bool CanDeleteSelected => SelectedCount > 0;
    public bool IsAllSelected => Songs.Count > 0 && SelectedCount == Songs.Count;

    public bool IsEmpty => !IsInitialLoading && Songs.Count == 0;
    public bool IsEmptyNoSongs => IsEmpty && string.IsNullOrWhiteSpace(SearchText);
    public bool IsEmptyNoResults => IsEmpty && !string.IsNullOrWhiteSpace(SearchText);

    public IAsyncRelayCommand RefreshCommand { get; }
    public IRelayCommand LoadMoreCommand { get; }
    public IAsyncRelayCommand AddSongCommand { get; }
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
        OnPropertyChanged(nameof(AppBarSubtitle));
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

            var (itemsEnumerable, totalCount) = await _songService.GetPagedSongsForListAsync(
                ArtistId, _currentPage, AppPagination.DefaultPageSize, _currentSearchQuery, cancellationToken);

            if (cancellationToken.IsCancellationRequested) return;

            _totalCount = totalCount;
            var list = itemsEnumerable.ToList();
            HasMoreItems = totalCount > list.Count;

            RunOnUiThread(() =>
            {
                Songs.ReplaceRange(list);
                if (SelectedSongs.Count > 0)
                {
                    SelectedSongs.ClearRange();
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
            var (itemsEnumerable, totalCount) = await _songService.GetPagedSongsForListAsync(
                ArtistId, loadingPage, AppPagination.DefaultPageSize, _currentSearchQuery);

            _totalCount = totalCount;
            var list = itemsEnumerable.ToList();
            var hasMore = (list.Count + Songs.Count) < _totalCount;
            _currentPage = loadingPage;

            RunOnUiThread(() =>
            {
                Songs.AddRange(list);
                HasMoreItems = hasMore;
                IsRefreshing = false;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load more songs (page {Page})", loadingPage);
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
        Shell.Current.GoToAsync($"{Routes.SongForm}?artistId={ArtistId}&artistName={Uri.EscapeDataString(ArtistName)}");

    private async Task NavigateToEditAsync()
    {
        var item = SelectedSongs.FirstOrDefault();
        if (item == null) return;

        RunOnUiThread(() =>
        {
            SelectedSongs.ClearRange();
            SelectedCount = 0;
        });
        await Shell.Current.GoToAsync(
            $"{Routes.SongForm}?songId={item.Id}&artistId={ArtistId}&artistName={Uri.EscapeDataString(ArtistName)}&songTitle={Uri.EscapeDataString(item.Title)}");
    }

    private void RequestBatchDelete()
    {
        var selectedItems = SelectedSongs.ToList();
        if (selectedItems.Count == 0) return;

        ConfirmMessage = selectedItems.Count == 1
            ? $"Delete '{selectedItems[0].Title}'?"
            : $"Delete {selectedItems.Count} songs?";
        ConfirmActionText = "Delete";
        _pendingConfirmAction = async () =>
        {
            var ids = selectedItems.Select(s => s.Id);
            var (success, message) = await _songService.DeleteSongsAsync(ids);
            RunOnUiThread(() =>
            {
                SelectedSongs.ClearRange();
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
                SelectedSongs.ClearRange();
                SelectedCount = 0;
            });
            return;
        }
        if (Songs.Count == 0) return;
        RunOnUiThread(() =>
        {
            SelectedSongs.ReplaceRange([.. Songs]);
            SelectedCount = Songs.Count;
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
        OnPropertyChanged(nameof(IsEmptyNoSongs));
        OnPropertyChanged(nameof(IsEmptyNoResults));
        OnPropertyChanged(nameof(IsAllSelected));
    }
}
