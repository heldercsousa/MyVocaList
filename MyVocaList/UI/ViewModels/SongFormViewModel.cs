using CommunityToolkit.Mvvm.Messaging;
using MyVocaList.Contracts.Messages;
using MyVocaList.UI.Collections;

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
    private readonly ISongKaraokeUrlService _karaokeUrlService;
    private readonly ISecureStorageWrapper _secureStorage;
    private readonly IMessenger _messenger;

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

    // YouTube URLs section
    [ObservableProperty] private ObservableRangeCollection<SongKaraokeUrlDto> _karaokeUrls = [];
    private readonly HashSet<string> _addedVideoIds = [];
    [ObservableProperty] private bool _hasYouTubeApiKey;
    [ObservableProperty] private string _pasteUrlInput = string.Empty;
    [ObservableProperty] private string _pasteUrlError = string.Empty;
    [ObservableProperty] private bool _hasPasteUrlError;
    [ObservableProperty] private bool _canLaunchYouTubeSearch;

    public string SelectedExternalId { get; private set; } = string.Empty;
    public string SelectedProvider { get; private set; } = string.Empty;

    public bool IsEditMode => SongId.HasValue;
    public string PageTitle => IsEditMode ? "Edit Song" : "New Song";

    public SongFormViewModel(
        IArtistService artistService,
        ISongService songService,
        ISnackbarComponent snackbarService,
        ILogger<SongFormViewModel> logger,
        ISongKaraokeUrlService karaokeUrlService,
        ISecureStorageWrapper secureStorage,
        IMessenger messenger)
    {
        _artistService = artistService;
        _songService = songService;
        _snackbarService = snackbarService;
        _logger = logger;
        _karaokeUrlService = karaokeUrlService;
        _secureStorage = secureStorage;
        _messenger = messenger;

        SaveCommand = new AsyncRelayCommand(SaveAsync);
        CancelCommand = new AsyncRelayCommand(CancelAsync);
        SearchArtistsCommand = new AsyncRelayCommand<string>(SearchArtistsAsync);
        SelectArtistCommand = new RelayCommand<AutocompleteSuggestion>(SelectArtist);
        NavigateToSongPickerCommand = new AsyncRelayCommand(NavigateToSongPickerAsync);
        NavigateToYouTubeSearchCommand = new AsyncRelayCommand(NavigateToYouTubeSearchAsync);
        AddFromPasteCommand = new AsyncRelayCommand(AddFromPasteAsync);
        RemoveUrlCommand = new AsyncRelayCommand<SongKaraokeUrlDto>(RemoveUrlAsync);
        GoToSettingsCommand = new AsyncRelayCommand(async () => await Shell.Current.GoToAsync("//settings"));
    }

    public IAsyncRelayCommand SaveCommand { get; }
    public IAsyncRelayCommand CancelCommand { get; }
    public IAsyncRelayCommand<string> SearchArtistsCommand { get; }
    public IRelayCommand<AutocompleteSuggestion> SelectArtistCommand { get; }
    public IAsyncRelayCommand NavigateToSongPickerCommand { get; }
    public IAsyncRelayCommand NavigateToYouTubeSearchCommand { get; }
    public IAsyncRelayCommand AddFromPasteCommand { get; }
    public IAsyncRelayCommand<SongKaraokeUrlDto> RemoveUrlCommand { get; }
    public IAsyncRelayCommand GoToSettingsCommand { get; }

    partial void OnSongIdChanged(int? value)
    {
        OnPropertyChanged(nameof(IsEditMode));
        OnPropertyChanged(nameof(PageTitle));
        if (value.HasValue)
            _ = LoadKaraokeUrlsAsync();
    }

    partial void OnSongTitleChanged(string value)
    {
        ClearError();
        UpdateCharacterCounter(value?.Length ?? 0);
        UpdateCanLaunchYouTubeSearch();
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
        UpdateCanLaunchYouTubeSearch();
    }

    private async Task SearchArtistsAsync(string term)
    {
        if (string.IsNullOrWhiteSpace(term)) { ArtistSuggestions = []; return; }
        var results = await _artistService.SearchArtistsByNameAsync(term, maxResults: 5);
        RunOnUiThread(() =>
            ArtistSuggestions = results.Select(a =>
                new AutocompleteSuggestion(a.Name, a.CatalogCountText, a)).ToList());
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
            ArtistErrorText = string.IsNullOrWhiteSpace(ArtistSearchText)
                ? "Artist is required"
                : "Search and select an artist from the list";
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

    public async Task RefreshApiKeyFlagAsync()
    {
        var key = await _secureStorage.GetAsync("youtube_api_key");
        HasYouTubeApiKey = !string.IsNullOrWhiteSpace(key);
    }

    private async Task LoadKaraokeUrlsAsync(CancellationToken ct = default)
    {
        if (!SongId.HasValue) return;
        var apiKey = await _secureStorage.GetAsync("youtube_api_key");
        HasYouTubeApiKey = !string.IsNullOrWhiteSpace(apiKey);
        var urls = await _karaokeUrlService.GetUrlsForSongAsync(SongId.Value, ct);
        foreach (var u in urls) _addedVideoIds.Add(u.VideoId);
        RunOnUiThread(() => KaraokeUrls.ReplaceRange(urls));
    }

    private async Task NavigateToSongPickerAsync()
    {
        _messenger.Register<SongPickedMessage>(this, (_, msg) =>
        {
            SongTitle = msg.Result.SongTitle ?? string.Empty;
            FeaturedArtists = msg.Result.FeaturedArtists ?? string.Empty;
            SelectedExternalId = msg.Result.ExternalId;
            SelectedProvider = msg.Result.Provider;

            // Auto-fill artist if not yet selected
            if ((!SelectedArtistId.HasValue || SelectedArtistId.Value == 0)
                && !string.IsNullOrEmpty(msg.Result.ArtistName))
            {
                ArtistSearchText = msg.Result.ArtistName;
            }

            _messenger.Unregister<SongPickedMessage>(this);
        });
        await Shell.Current.GoToAsync(Routes.SongPicker);
    }

    private async Task NavigateToYouTubeSearchAsync()
    {
        _messenger.Register<YouTubeVideoPickedMessage>(this, (_, msg) =>
        {
            if (!SongId.HasValue) return;
            var rawUrl = $"https://youtu.be/{msg.Result.VideoId}";
            _ = AddUrlFromPickerAsync(rawUrl, msg.Result.VideoId);
            _messenger.Unregister<YouTubeVideoPickedMessage>(this);
        });
        await Shell.Current.GoToAsync(Routes.YouTubeSearch);
    }

    private async Task AddUrlFromPickerAsync(string rawUrl, string videoId, CancellationToken ct = default)
    {
        if (IsVideoIdAdded(videoId)) return;
        var (success, message, dto) = await _karaokeUrlService.AddUrlAsync(SongId!.Value, rawUrl, ct: ct);
        if (success && dto is not null)
        {
            _addedVideoIds.Add(videoId);
            RunOnUiThread(() => KaraokeUrls.Add(dto));
            await _snackbarService.ShowSuccessAsync("URL added");
        }
        else
        {
            await _snackbarService.ShowErrorAsync(message);
        }
    }

    public bool IsVideoIdAdded(string videoId) => _addedVideoIds.Contains(videoId)
        || KaraokeUrls.Any(u => u.VideoId == videoId);

    private async Task AddFromPasteAsync(CancellationToken ct = default)
    {
        var raw = PasteUrlInput?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(raw)) return;

        if (!SongId.HasValue)
        {
            PasteUrlError = "Save the song first before adding URLs";
            HasPasteUrlError = true;
            return;
        }

        var (success, message, dto) = await _karaokeUrlService.AddUrlAsync(SongId.Value, raw, ct: ct);
        if (success && dto is not null)
        {
            RunOnUiThread(() =>
            {
                KaraokeUrls.Add(dto);
                PasteUrlInput = string.Empty;
                PasteUrlError = string.Empty;
                HasPasteUrlError = false;
            });
        }
        else
        {
            PasteUrlError = message;
            HasPasteUrlError = true;
        }
    }

    private async Task RemoveUrlAsync(SongKaraokeUrlDto dto, CancellationToken ct = default)
    {
        if (dto is null || !SongId.HasValue) return;

        var songId = SongId.Value;

        // Commit-first: delete from DB immediately so undo can re-insert cleanly
        var (success, message) = await _karaokeUrlService.RemoveUrlAsync(songId, dto.VideoId, ct);
        if (!success)
        {
            await _snackbarService.ShowErrorAsync(message);
            return;
        }

        _addedVideoIds.Remove(dto.VideoId);
        RunOnUiThread(() => KaraokeUrls.Remove(dto));

        // Show snackbar; if UNDO tapped, re-insert via AddUrlAsync
        await _snackbarService.ShowWithUndoAsync("URL removed", "UNDO", async () =>
        {
            var rawUrl = $"https://youtu.be/{dto.VideoId}";
            var (reAddSuccess, _, reAdded) = await _karaokeUrlService.AddUrlAsync(songId, rawUrl);
            if (reAddSuccess && reAdded is not null)
            {
                _addedVideoIds.Add(dto.VideoId);
                RunOnUiThread(() => KaraokeUrls.Add(reAdded));
            }
        });
    }

    [RelayCommand]
    private async Task LaunchYouTubeSearch()
    {
        var title = SongTitle?.Trim() ?? string.Empty;
        var artist = ArtistName?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(artist))
            return;

        var query = $"karaoke {title} {artist}";
        var encodedQuery = Uri.EscapeDataString(query);
        var youtubeUri = new Uri($"https://www.youtube.com/results?search_query={encodedQuery}");

        if (!await Launcher.TryOpenAsync(youtubeUri))
            await Browser.OpenAsync(youtubeUri, BrowserLaunchMode.SystemPreferred);
    }

    private void UpdateCanLaunchYouTubeSearch()
    {
        CanLaunchYouTubeSearch = !string.IsNullOrWhiteSpace(SongTitle)
                             && !string.IsNullOrWhiteSpace(ArtistName);
    }
}
