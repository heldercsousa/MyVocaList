namespace MyVocaList.UI.ViewModels;

[QueryProperty(nameof(SongIdRaw), "songId")]
[QueryProperty(nameof(ArtistIdRaw), "artistId")]
[QueryProperty(nameof(ArtistName), "artistName")]
[QueryProperty(nameof(SongTitle), "songTitle")]
public partial class SongFormViewModel : ViewModelBase
{
    private readonly IArtistService _artistService;
    private readonly ISongService _songService;
    private readonly ISnackbarComponent _snackbarService;
    private readonly ILogger<SongFormViewModel> _logger;

    public string SongIdRaw { set => SongId = int.TryParse(value, out var id) ? id : null; }
    public string ArtistIdRaw { set => ArtistId = int.TryParse(value, out var id) ? id : 0; }

    [ObservableProperty] private int? _songId;
    [ObservableProperty] private int _artistId;
    [ObservableProperty] private string _artistName = string.Empty;
    [ObservableProperty] private string _songTitle = string.Empty;
    [ObservableProperty] private string _featuredArtists = string.Empty;
    [ObservableProperty] private bool _titleHasError;
    [ObservableProperty] private string _titleErrorText = string.Empty;
    [ObservableProperty] private string _characterCounterText = string.Empty;
    [ObservableProperty] private bool _showCharacterCounter;
    [ObservableProperty] private bool _isCharacterCounterWarning;
    [ObservableProperty] private bool _isCharacterCounterError;
    [ObservableProperty] private bool _isBusy;

    // Artist autocomplete
    [ObservableProperty] private string _artistSearchText = string.Empty;
    [ObservableProperty] private int? _selectedArtistId;
    [ObservableProperty] private string? _selectedArtistName;
    [ObservableProperty] private bool _isArtistLocked;
    [ObservableProperty] private IEnumerable<AutocompleteSuggestion> _artistSuggestions = [];
    [ObservableProperty] private bool _artistHasError;
    [ObservableProperty] private string _artistErrorText = string.Empty;

    // Lyrics
    [ObservableProperty] private string? _lyrics;

    public bool IsEditMode => SongId.HasValue;
    public string PageTitle => IsEditMode ? "Edit Song" : "New Song";

    public SongFormViewModel(
        IArtistService artistService,
        ISongService songService,
        ISnackbarComponent snackbarService,
        ILogger<SongFormViewModel> logger)
    {
        _artistService = artistService;
        _songService = songService;
        _snackbarService = snackbarService;
        _logger = logger;

        SaveCommand = new AsyncRelayCommand(SaveAsync);
        CancelCommand = new AsyncRelayCommand(CancelAsync);
        SearchArtistsCommand = new AsyncRelayCommand<string>(SearchArtistsAsync);
        SelectArtistCommand = new RelayCommand<AutocompleteSuggestion>(SelectArtist);
    }

    public IAsyncRelayCommand SaveCommand { get; }
    public IAsyncRelayCommand CancelCommand { get; }
    public IAsyncRelayCommand<string> SearchArtistsCommand { get; }
    public IRelayCommand<AutocompleteSuggestion> SelectArtistCommand { get; }

    partial void OnSongIdChanged(int? value)
    {
        OnPropertyChanged(nameof(IsEditMode));
        OnPropertyChanged(nameof(PageTitle));
    }

    partial void OnSongTitleChanged(string value)
    {
        ClearError();
        UpdateCharacterCounter(value?.Length ?? 0);
    }

    partial void OnArtistIdChanged(int value)
    {
        if (value > 0)
        {
            SelectedArtistId = value;
            ArtistSearchText = ArtistName;
        }
    }

    partial void OnArtistNameChanged(string value)
    {
        if (SelectedArtistId.HasValue && SelectedArtistId.Value > 0)
            ArtistSearchText = value;
    }

    private async Task SearchArtistsAsync(string term)
    {
        if (string.IsNullOrWhiteSpace(term)) { ArtistSuggestions = []; return; }
        var results = await _artistService.SearchArtistsByNameAsync(term, maxResults: 5);
        ArtistSuggestions = results.Select(a =>
            new AutocompleteSuggestion(a.Name, a.CatalogCountText, a)).ToList();
    }

    private void SelectArtist(AutocompleteSuggestion suggestion)
    {
        if (suggestion?.Data is not ArtistListItemDto artist) return;
        SelectedArtistId = artist.Id;
        SelectedArtistName = artist.Name;
        ArtistSearchText = artist.Name;
        ArtistSuggestions = [];
        ArtistHasError = false;
        ArtistErrorText = string.Empty;
    }

    private async Task SaveAsync()
    {
        var title = SongTitle?.Trim() ?? string.Empty;

        var validation = _songService.ValidateTitleInput(title);
        if (!validation.isValid)
        {
            TitleHasError = true;
            TitleErrorText = validation.message;
            return;
        }

        if (!SelectedArtistId.HasValue || SelectedArtistId.Value == 0)
        {
            ArtistHasError = true;
            ArtistErrorText = "Artist is required";
            return;
        }

        IsBusy = true;
        try
        {
            if (IsEditMode)
            {
                var (success, message) = await _songService.UpdateSongAsync(
                    SongId!.Value, title, FeaturedArtists?.Trim(), Lyrics?.Trim(), true);
                if (success)
                {
                    await _snackbarService.ShowSuccessAsync("Song updated");
                    await Shell.Current.GoToAsync("..");
                }
                else
                {
                    TitleHasError = true;
                    TitleErrorText = message;
                }
            }
            else
            {
                var (success, message, _) = await _songService.CreateSongAsync(
                    SelectedArtistId.Value, title, FeaturedArtists?.Trim(), Lyrics?.Trim());
                if (success)
                {
                    await _snackbarService.ShowSuccessAsync("Song created");
                    await Shell.Current.GoToAsync("..");
                }
                else
                {
                    TitleHasError = true;
                    TitleErrorText = message;
                }
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private Task CancelAsync() => Shell.Current.GoToAsync("..");

    private void ClearError()
    {
        TitleHasError = false;
        TitleErrorText = string.Empty;
    }

    private void UpdateCharacterCounter(int length)
    {
        ShowCharacterCounter = _songService.ShouldShowCharacterCounter(length);
        if (ShowCharacterCounter)
        {
            var (text, isWarning, isError) = _songService.GetCharacterCounterInfo(length);
            CharacterCounterText = text;
            IsCharacterCounterWarning = isWarning;
            IsCharacterCounterError = isError;
        }
    }
}
