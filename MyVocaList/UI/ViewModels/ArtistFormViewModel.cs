using MyVocaList.Contracts.DTOs;

namespace MyVocaList.UI.ViewModels;

[QueryProperty(nameof(ArtistIdRaw), "artistId")]
[QueryProperty(nameof(ArtistName), "artistName")]
public partial class ArtistFormViewModel : ViewModelBase
{
    private readonly IArtistService _artistService;
    private readonly IMusicMetadataService _musicMetadataService;
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
    [ObservableProperty] private string _apiSearchText = string.Empty;
    [ObservableProperty] private IEnumerable<MusicSearchResultDto> _apiResults = [];
    [ObservableProperty] private bool _isApiSearching;
    [ObservableProperty] private string _apiStatusMessage = string.Empty;
    [ObservableProperty] private IEnumerable<ArtistListItemDto> _duplicateSuggestions = [];
    [ObservableProperty] private bool _hasApiResults;
    [ObservableProperty] private bool _hasDuplicateSuggestions;
    [ObservableProperty] private bool _hasApiStatusMessage;

    public string SelectedExternalId { get; private set; } = string.Empty;
    public string SelectedProvider { get; private set; } = string.Empty;

    public bool IsEditMode => ArtistId.HasValue;
    public string PageTitle => IsEditMode ? "Edit Artist" : "New Artist";

    public ArtistFormViewModel(
        IArtistService artistService,
        IMusicMetadataService musicMetadataService,
        ISnackbarComponent snackbarService,
        ILogger<ArtistFormViewModel> logger)
    {
        _artistService = artistService;
        _musicMetadataService = musicMetadataService;
        _snackbarService = snackbarService;
        _logger = logger;

        SaveCommand = new AsyncRelayCommand(SaveAsync);
        CancelCommand = new AsyncRelayCommand(CancelAsync);
        SearchApiCommand = new AsyncRelayCommand(SearchApiAsync);
        SelectApiResultCommand = new RelayCommand<MusicSearchResultDto>(SelectApiResult);
        SelectDuplicateCommand = new AsyncRelayCommand<ArtistListItemDto>(SelectDuplicateAsync);
    }

    public IAsyncRelayCommand SaveCommand { get; }
    public IAsyncRelayCommand CancelCommand { get; }
    public IAsyncRelayCommand SearchApiCommand { get; }
    public IRelayCommand<MusicSearchResultDto> SelectApiResultCommand { get; }
    public IAsyncRelayCommand<ArtistListItemDto> SelectDuplicateCommand { get; }

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

    private async Task SearchApiAsync()
    {
        var term = ApiSearchText?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(term)) { ApiResults = []; HasApiResults = false; DuplicateSuggestions = []; HasDuplicateSuggestions = false; ApiStatusMessage = string.Empty; HasApiStatusMessage = false; return; }

        IsApiSearching = true;
        ApiStatusMessage = string.Empty;
        try
        {
            var results = await _musicMetadataService.SearchArtistsAsync(term);
            var list = results.Take(5).ToList();
            ApiResults = list;
            HasApiResults = list.Count > 0;
            ApiStatusMessage = HasApiResults ? string.Empty : "No results found";
            HasApiStatusMessage = !HasApiResults;

            var local = await _artistService.SearchArtistsByNameAsync(term, maxResults: 5);
            var localList = local.ToList();
            DuplicateSuggestions = localList;
            HasDuplicateSuggestions = localList.Count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API artist search failed for term {Term}", term);
            ApiStatusMessage = "Search failed";
            HasApiStatusMessage = true;
            ApiResults = [];
            HasApiResults = false;
        }
        finally
        {
            IsApiSearching = false;
        }
    }

    private void SelectApiResult(MusicSearchResultDto result)
    {
        if (result is null) return;
        ArtistName = result.ArtistName;
        SelectedExternalId = result.ExternalId;
        SelectedProvider = result.Provider;
        ApiResults = [];
        ApiStatusMessage = string.Empty;
    }

    private Task SelectDuplicateAsync(ArtistListItemDto artist)
    {
        if (artist is null) return Task.CompletedTask;
        return Shell.Current.GoToAsync($"..?artistId={artist.Id}&artistName={Uri.EscapeDataString(artist.Name)}");
    }

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
