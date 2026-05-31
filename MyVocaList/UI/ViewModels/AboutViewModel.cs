namespace MyVocaList.UI.ViewModels;

public sealed partial class AboutViewModel : ViewModelBase
{
    private readonly IWhatsNewService _whatsNewService;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasReleaseNotes))]
    private ReleaseEntry? _currentRelease;

    public string Version { get; } = $"v{AppInfo.VersionString}";
    public string Since { get; } = $"Since {AppConstants.FoundedYear}";
    public bool HasReleaseNotes => CurrentRelease is not null;

    public AboutViewModel(IWhatsNewService whatsNewService)
    {
        _whatsNewService = whatsNewService;
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        CurrentRelease = await _whatsNewService.GetCurrentReleaseAsync(ct);
    }
}
