using MyVocaList.UI.Collections;

namespace MyVocaList.UI.ViewModels;

[QueryProperty(nameof(ArtistIdRaw), "artistId")]
[QueryProperty(nameof(ArtistName), "artistName")]
public partial class SongsViewModel : CrudListViewModelBase<SongListItemDto>
{
    private readonly ICatalogService _catalogService;
    private readonly ISongService _songService;
    private readonly ISnackbarComponent _snackbarService;

    [ObservableProperty] private int _artistId;
    [ObservableProperty] private string _artistName = string.Empty;

    public string ArtistIdRaw { set => ArtistId = int.TryParse(value, out var id) ? id : 0; }

    public SongsViewModel(
        ICatalogService catalogService,
        ISongService songService,
        ISnackbarComponent snackbarService,
        ILogger<SongsViewModel> logger) : base(logger)
    {
        _catalogService = catalogService;
        _songService = songService;
        _snackbarService = snackbarService;

        Songs = [];
        SelectedSongs = [];

        AddSongCommand = new AsyncRelayCommand(NavigateToAddAsync);
        AddToCatalogCommand = new AsyncRelayCommand(AddToCatalogAsync, () => IsCatalogMode);
        RemoveFromCatalogCommand = new AsyncRelayCommand<SongListItemDto>(RemoveFromCatalogAsync, _ => IsCatalogMode);
        GoBackCommand = new AsyncRelayCommand(() => Shell.Current.GoToAsync(".."));
    }

    public ObservableRangeCollection<SongListItemDto> Songs { get; }
    public ObservableRangeCollection<SongListItemDto> SelectedSongs { get; }

    public System.Collections.IList SelectedSongsRaw => SelectedSongs;

    public IAsyncRelayCommand AddSongCommand { get; }
    public IAsyncRelayCommand AddToCatalogCommand { get; }
    public IAsyncRelayCommand<SongListItemDto> RemoveFromCatalogCommand { get; }
    public IAsyncRelayCommand GoBackCommand { get; }

    public bool IsCatalogMode => ArtistId > 0;
    public string AppBarTitle => IsCatalogMode ? ArtistName : "Songs";

    /// <summary>Routes FAB to the correct command based on current mode.</summary>
    public IAsyncRelayCommand PrimaryFabCommand => IsCatalogMode ? AddToCatalogCommand : AddSongCommand;

    /// <summary>Subtitle shows selection count when > 0.</summary>
    public string AppBarSubtitle => SelectedCount > 0 ? $"{SelectedCount} selected" : string.Empty;

    public bool IsEmptyNoSongs => IsEmpty && string.IsNullOrWhiteSpace(SearchText);
    public bool IsEmptyNoResults => IsEmpty && !string.IsNullOrWhiteSpace(SearchText);

    protected override ObservableRangeCollection<SongListItemDto> Items => Songs;
    protected override ObservableRangeCollection<SongListItemDto> SelectedItems => SelectedSongs;

    protected override void OnSelectedCountUpdated(int value)
        => OnPropertyChanged(nameof(AppBarSubtitle));

    partial void OnArtistIdChanged(int value)
    {
        OnPropertyChanged(nameof(IsCatalogMode));
        OnPropertyChanged(nameof(AppBarTitle));
        OnPropertyChanged(nameof(PrimaryFabCommand));
        AddToCatalogCommand.NotifyCanExecuteChanged();
    }

    protected override Task<(IEnumerable<SongListItemDto> items, int totalCount)> FetchPageAsync(
        int page, int pageSize, string query, CancellationToken ct)
        => IsCatalogMode
            ? _catalogService.GetPagedCatalogForArtistAsync(ArtistId, page, pageSize, query, ct)
            : _songService.GetPagedSongsForListAsync(page, pageSize, query, ct);

    protected override Task<(IEnumerable<SongListItemDto> items, int totalCount)> FetchMoreAsync(
        int page, int pageSize, string query, CancellationToken ct = default)
        => IsCatalogMode
            ? _catalogService.GetPagedCatalogForArtistAsync(ArtistId, page, pageSize, query, ct)
            : _songService.GetPagedSongsForListAsync(page, pageSize, query, ct);

    protected override string BuildDeleteConfirmMessage(IList<SongListItemDto> items)
        => items.Count == 1 ? $"Delete '{items[0].Title}'?" : $"Delete {items.Count} songs?";

    protected override async Task ExecuteDeleteAsync(IEnumerable<SongListItemDto> items)
    {
        var ids = items.Select(s => s.Id);
        var (success, message) = await _songService.DeleteSongsAsync(ids);
        if (success)
        {
            await RefreshAsync();
            await _snackbarService.ShowSuccessAsync(message);
        }
        else
        {
            await _snackbarService.ShowErrorAsync(message);
        }
    }

    protected override Task NavigateToAddAsync()
        => Shell.Current.GoToAsync(Routes.SongForm);

    protected override async Task NavigateToEditAsync(SongListItemDto item)
        => await Shell.Current.GoToAsync(
            $"{Routes.SongForm}?songId={item.Id}&artistId={ArtistId}&artistName={Uri.EscapeDataString(ArtistName)}&songTitle={Uri.EscapeDataString(item.Title)}");

    protected override void RaiseEntityEmptyStateProperties()
    {
        OnPropertyChanged(nameof(IsEmptyNoSongs));
        OnPropertyChanged(nameof(IsEmptyNoResults));
    }

    private Task AddToCatalogAsync()
    {
        // TODO Phase 15: open song picker to add to catalog
        return Task.CompletedTask;
    }

    private async Task RemoveFromCatalogAsync(SongListItemDto song)
    {
        if (song == null) return;
        var (success, message) = await _catalogService.RemoveSongFromCatalogAsync(ArtistId, song.Id);
        if (success)
        {
            await RefreshAsync();
            await _snackbarService.ShowSuccessAsync(message);
        }
        else
        {
            await _snackbarService.ShowErrorAsync(message);
        }
    }
}
