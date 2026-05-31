using MyVocaList.Domain.ServicesInterfaces;

namespace MyVocaList.UI.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly IYouTubeSearchService _youtubeSearch;
    private readonly ISecureStorageWrapper _secureStorage;
    private readonly ISnackbarComponent _snackbar;
    private readonly ILogger<SettingsViewModel> _logger;

    [ObservableProperty] private string _apiKeyInput = string.Empty;
    [ObservableProperty] private bool _isTestingKey;
    [ObservableProperty] private string _apiKeyStatus = string.Empty;
    [ObservableProperty] private bool _hasApiKeyStatus;

    public SettingsViewModel(
        IYouTubeSearchService youtubeSearch,
        ISecureStorageWrapper secureStorage,
        ISnackbarComponent snackbar,
        ILogger<SettingsViewModel> logger)
    {
        _youtubeSearch = youtubeSearch;
        _secureStorage = secureStorage;
        _snackbar = snackbar;
        _logger = logger;

        SaveApiKeyCommand = new AsyncRelayCommand(SaveApiKeyAsync);
        TestApiKeyCommand = new AsyncRelayCommand(TestApiKeyAsync);
        ClearApiKeyCommand = new AsyncRelayCommand(ClearApiKeyAsync);
    }

    public IAsyncRelayCommand SaveApiKeyCommand { get; }
    public IAsyncRelayCommand TestApiKeyCommand { get; }
    public IAsyncRelayCommand ClearApiKeyCommand { get; }

    public async Task InitializeAsync()
    {
        var stored = await _secureStorage.GetAsync("youtube_api_key");
        ApiKeyInput = stored ?? string.Empty;
    }

    private async Task SaveApiKeyAsync()
    {
        var key = ApiKeyInput.Trim();
        if (string.IsNullOrEmpty(key))
        {
            await ClearApiKeyAsync();
            return;
        }
        await _secureStorage.SetAsync("youtube_api_key", key);
        await _snackbar.ShowSuccessAsync("API key saved");
    }

    private async Task TestApiKeyAsync()
    {
        var key = ApiKeyInput.Trim();
        if (string.IsNullOrEmpty(key))
            return;

        IsTestingKey = true;
        ApiKeyStatus = string.Empty;
        HasApiKeyStatus = false;
        try
        {
            var valid = await _youtubeSearch.ValidateApiKeyAsync(key);
            ApiKeyStatus = valid ? "Key valid — YouTube search is ready." : "Invalid key — check and retry.";
            HasApiKeyStatus = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API key test failed");
            ApiKeyStatus = "Test failed. Check your connection.";
            HasApiKeyStatus = true;
        }
        finally
        {
            IsTestingKey = false;
        }
    }

    private async Task ClearApiKeyAsync()
    {
        _secureStorage.Remove("youtube_api_key");
        ApiKeyInput = string.Empty;
        ApiKeyStatus = string.Empty;
        HasApiKeyStatus = false;
        await _snackbar.ShowSuccessAsync("API key removed");
    }

}
