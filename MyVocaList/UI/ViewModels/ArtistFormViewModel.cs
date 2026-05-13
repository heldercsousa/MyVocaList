namespace MyVocaList.UI.ViewModels;

[QueryProperty(nameof(ArtistIdRaw), "artistId")]
[QueryProperty(nameof(ArtistName), "artistName")]
public partial class ArtistFormViewModel : ViewModelBase
{
    private readonly IArtistService _artistService;
    private readonly ISnackbarComponent _snackbarService;
    private readonly ILogger<ArtistFormViewModel> _logger;

    public string ArtistIdRaw { set => ArtistId = int.TryParse(value, out var id) ? id : null; }

    [ObservableProperty] private int? _artistId;
    [ObservableProperty] private string _artistName = string.Empty;
    [ObservableProperty] private bool _nameHasError;
    [ObservableProperty] private string _nameErrorText = string.Empty;
    [ObservableProperty] private string _characterCounterText = string.Empty;
    [ObservableProperty] private bool _showCharacterCounter;
    [ObservableProperty] private bool _isCharacterCounterWarning;
    [ObservableProperty] private bool _isCharacterCounterError;
    [ObservableProperty] private bool _isBusy;

    public bool IsEditMode => ArtistId.HasValue;
    public string PageTitle => IsEditMode ? "Edit Artist" : "New Artist";

    public ArtistFormViewModel(
        IArtistService artistService,
        ISnackbarComponent snackbarService,
        ILogger<ArtistFormViewModel> logger)
    {
        _artistService = artistService;
        _snackbarService = snackbarService;
        _logger = logger;

        SaveCommand = new AsyncRelayCommand(SaveAsync);
        CancelCommand = new AsyncRelayCommand(CancelAsync);
    }

    public IAsyncRelayCommand SaveCommand { get; }
    public IAsyncRelayCommand CancelCommand { get; }

    partial void OnArtistIdChanged(int? value)
    {
        OnPropertyChanged(nameof(IsEditMode));
        OnPropertyChanged(nameof(PageTitle));
    }

    partial void OnArtistNameChanged(string value)
    {
        ClearError();
        UpdateCharacterCounter(value?.Length ?? 0);
    }

    private async Task SaveAsync()
    {
        var name = ArtistName?.Trim() ?? string.Empty;

        var validation = _artistService.ValidateNameInput(name);
        if (!validation.isValid)
        {
            NameHasError = true;
            NameErrorText = validation.message;
            return;
        }

        IsBusy = true;
        try
        {
            if (IsEditMode)
            {
                var (success, message) = await _artistService.UpdateArtistAsync(ArtistId!.Value, name);
                if (success)
                {
                    await _snackbarService.ShowSuccessAsync("Artist updated");
                    await Shell.Current.GoToAsync("..");
                }
                else
                {
                    NameHasError = true;
                    NameErrorText = message;
                }
            }
            else
            {
                var (success, message, _) = await _artistService.CreateArtistAsync(name);
                if (success)
                {
                    await _snackbarService.ShowSuccessAsync("Artist created");
                    await Shell.Current.GoToAsync("..");
                }
                else
                {
                    NameHasError = true;
                    NameErrorText = message;
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
        NameHasError = false;
        NameErrorText = string.Empty;
    }

    private void UpdateCharacterCounter(int length)
    {
        ShowCharacterCounter = _artistService.ShouldShowCharacterCounter(length);
        if (ShowCharacterCounter)
        {
            var (text, isWarning, isError) = _artistService.GetCharacterCounterInfo(length);
            CharacterCounterText = text;
            IsCharacterCounterWarning = isWarning;
            IsCharacterCounterError = isError;
        }
    }
}
