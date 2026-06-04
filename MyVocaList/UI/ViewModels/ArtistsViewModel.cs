using MyVocaList.Domain.RepositoryInterface;
using MyVocaList.UI.Collections;

namespace MyVocaList.UI.ViewModels;

[QueryProperty(nameof(Mode), "mode")]
public partial class ArtistsViewModel : CrudListViewModelBase<ArtistListItemDto>
{
    private readonly IArtistService _artistService;
    private readonly ISnackbarComponent _snackbarService;

    [ObservableProperty] private ArtistRoleFilter _roleFilter = ArtistRoleFilter.All;
    [ObservableProperty] private System.Collections.IList _selectedRoleFilters = new List<object>();

    public ArtistsViewModel(
        IArtistService artistService,
        ISnackbarComponent snackbarService,
        ILogger<ArtistsViewModel> logger) : base(logger)
    {
        _artistService = artistService;
        _snackbarService = snackbarService;

        Artists = [];
        SelectedArtists = [];

        AddArtistCommand = new AsyncRelayCommand(NavigateToAddAsync);
        ViewCatalogCommand = new RelayCommand<ArtistListItemDto>(NavigateToCatalog);
        GoBackCommand = new AsyncRelayCommand(GoBackAsync);
    }

    public ObservableRangeCollection<ArtistListItemDto> Artists { get; }
    public ObservableRangeCollection<ArtistListItemDto> SelectedArtists { get; }

    public System.Collections.IList SelectedArtistsRaw => SelectedArtists;

    public IAsyncRelayCommand AddArtistCommand { get; }
    public IRelayCommand<ArtistListItemDto> ViewCatalogCommand { get; }
    public IAsyncRelayCommand GoBackCommand { get; }

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

    public bool IsEmptyNoArtists => IsEmpty && string.IsNullOrWhiteSpace(SearchText);
    public bool IsEmptyNoResults => IsEmpty && !string.IsNullOrWhiteSpace(SearchText);

    protected override ObservableRangeCollection<ArtistListItemDto> Items => Artists;
    protected override ObservableRangeCollection<ArtistListItemDto> SelectedItems => SelectedArtists;

    protected override void OnSelectedCountUpdated(int value)
        => OnPropertyChanged(nameof(AppBarTitle));

    partial void OnRoleFilterChanged(ArtistRoleFilter value)
    {
        OnPropertyChanged(nameof(AppBarTitle));
        _ = ReloadAsync();
    }

    partial void OnSelectedRoleFiltersChanged(System.Collections.IList value)
    {
        var selected = value?.Cast<string>().ToHashSet(StringComparer.Ordinal) ?? [];
        RoleFilter = (selected.Contains("Authors"), selected.Contains("Performers")) switch
        {
            (true, false) => ArtistRoleFilter.AuthorsOnly,
            (false, true) => ArtistRoleFilter.PerformersOnly,
            _ => ArtistRoleFilter.All
        };
    }

    protected override Task<(IEnumerable<ArtistListItemDto> items, int totalCount)> FetchPageAsync(
        int page, int pageSize, string query, CancellationToken ct)
        => _artistService.GetPagedArtistsForListAsync(page, pageSize, query, RoleFilter, ct);

    protected override Task<(IEnumerable<ArtistListItemDto> items, int totalCount)> FetchMoreAsync(
        int page, int pageSize, string query, CancellationToken ct = default)
        => _artistService.GetPagedArtistsForListAsync(page, pageSize, query, RoleFilter, ct);

    protected override string BuildDeleteConfirmMessage(IList<ArtistListItemDto> items)
        => items.Count == 1 ? $"Delete '{items[0].Name}'?" : $"Delete {items.Count} artists?";

    protected override async Task ExecuteDeleteAsync(IEnumerable<ArtistListItemDto> items)
    {
        var ids = items.Select(a => a.Id);
        var (success, message) = await _artistService.DeleteArtistsAsync(ids);
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
        => Shell.Current.GoToAsync(Routes.ArtistForm);

    protected override async Task NavigateToEditAsync(ArtistListItemDto item)
        => await Shell.Current.GoToAsync(
            $"{Routes.ArtistForm}?artistId={item.Id}&artistName={Uri.EscapeDataString(item.Name)}");

    protected override void RaiseEntityEmptyStateProperties()
    {
        OnPropertyChanged(nameof(IsEmptyNoArtists));
        OnPropertyChanged(nameof(IsEmptyNoResults));
    }

    private Task GoBackAsync() => Shell.Current.GoToAsync("..");

    private void NavigateToCatalog(ArtistListItemDto artist)
    {
        if (artist == null) return;
        _ = Shell.Current.GoToAsync(
            $"{Routes.Songs}?artistId={artist.Id}&artistName={Uri.EscapeDataString(artist.Name)}");
    }
}
