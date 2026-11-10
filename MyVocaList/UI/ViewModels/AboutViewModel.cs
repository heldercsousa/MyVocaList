using System.Reflection;

namespace MyVocaList.UI.ViewModels;

public sealed partial class AboutViewModel : ViewModelBase
{
    private readonly IWhatsNewService _whatsNewService;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasReleaseNotes))]
    private ReleaseEntry? _currentRelease;

    [ObservableProperty]
    private BuildGroup? _selectedGroup;

    public string Version { get; }
    public string BuildLine { get; }
    public bool HasBuildDetails { get; }
    public string BranchText { get; }
    public bool IsDirty { get; }
    public IReadOnlyList<BuildGroup> Groups { get; }
    public string Since { get; } = $"Since {AppConstants.FoundedYear}";
    public bool HasReleaseNotes => CurrentRelease is not null;
    public string Copyright => FormatCopyright(AppConstants.FoundedYear, DateTime.Now.Year);

    internal static string FormatCopyright(int foundedYear, int currentYear) =>
        currentYear > foundedYear
            ? $"© {foundedYear}–{currentYear} Helder Sousa"
            : $"© {foundedYear} Helder Sousa";

    public AboutViewModel(IWhatsNewService whatsNewService)
    {
        _whatsNewService = whatsNewService;

        var assembly = typeof(AboutViewModel).Assembly;
        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        var timestamp = assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == "BuildTimestampUtc")?.Value;
        var identity = BuildInfo.ParseIdentity(informational, timestamp, TimeZoneInfo.Local);

        using var stream = assembly.GetManifestResourceStream(BuildInfo.DetailsResourceName);
        var details = BuildInfo.ReadDetails(stream, identity.ShortSha);

        using var statusStream = assembly.GetManifestResourceStream(BuildInfo.StatusResourceName);
        var status = BuildInfo.ReadStatus(statusStream);

        Version = $"v{identity.Version}";
        BuildLine = $"Build {identity.ShortSha} · {identity.BuiltAtLocal}";
        HasBuildDetails = details is not null;
        BranchText = details is null ? "" : $"Branch: {details.Branch}";
        IsDirty = details?.IsDirty ?? false;
        Groups = BuildInfo.ToGroups(details, status);

        var index = BuildInfo.DefaultGroupIndex(Groups);
        SelectedGroup = index >= 0 ? Groups[index] : null;
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        CurrentRelease = await _whatsNewService.GetCurrentReleaseAsync(ct);
    }
}
