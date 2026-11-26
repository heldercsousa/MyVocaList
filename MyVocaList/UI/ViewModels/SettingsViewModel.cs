namespace MyVocaList.UI.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    /// <summary>Secure-storage key of the removed YouTube Data API key setting (REQ-MSRC-04).</summary>
    private const string LegacyYouTubeKeyName = "youtube_api_key";

    private readonly ISecureStorageWrapper _secureStorage;
    private readonly ILogger<SettingsViewModel> _logger;

    public SettingsViewModel(ISecureStorageWrapper secureStorage, ILogger<SettingsViewModel> logger)
    {
        _secureStorage = secureStorage;
        _logger = logger;
    }

    /// <summary>Removes a previously stored YouTube API key; the setting no longer exists.</summary>
    public Task InitializeAsync()
    {
        // BUG-020: keystore access can throw; this runs from an async-void OnAppearing, so never let it escape.
        try
        {
            _secureStorage.Remove(LegacyYouTubeKeyName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to remove the legacy YouTube API key from secure storage");
        }

        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task NavigateToFeedbackAsync()
        => await Shell.Current.GoToAsync(Routes.Feedback);
}
