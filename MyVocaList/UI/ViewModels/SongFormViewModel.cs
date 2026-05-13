namespace MyVocaList.UI.ViewModels;

[QueryProperty(nameof(SongIdRaw), "songId")]
[QueryProperty(nameof(ArtistIdRaw), "artistId")]
[QueryProperty(nameof(ArtistName), "artistName")]
[QueryProperty(nameof(SongTitle), "songTitle")]
public partial class SongFormViewModel : ViewModelBase
{
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

    public bool IsEditMode => SongId.HasValue;
    public string PageTitle => IsEditMode ? "Edit Song" : "New Song";

    public SongFormViewModel(
        ISongService songService,
        ISnackbarComponent snackbarService,
        ILogger<SongFormViewModel> logger)
    {
        _songService = songService;
        _snackbarService = snackbarService;
        _logger = logger;

        SaveCommand = new AsyncRelayCommand(SaveAsync);
        CancelCommand = new AsyncRelayCommand(CancelAsync);
    }

    public IAsyncRelayCommand SaveCommand { get; }
    public IAsyncRelayCommand CancelCommand { get; }

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

        IsBusy = true;
        try
        {
            if (IsEditMode)
            {
                var (success, message) = await _songService.UpdateSongAsync(
                    SongId!.Value, title, FeaturedArtists?.Trim());
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
                    ArtistId, title, FeaturedArtists?.Trim());
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
