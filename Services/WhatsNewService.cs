using System.Text.Json;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using MyVocaList.Contracts.DTOs;
using MyVocaList.Domain.ServicesInterfaces;

namespace MyVocaList.Services;

public sealed class WhatsNewService : IWhatsNewService
{
    private const string LastSeenKey = "last_seen_version";
    private const string ReleasesFileName = "releases.json";

    private readonly IPreferences _preferences;
    private readonly IAppInfo _appInfo;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<WhatsNewService> _logger;

    public WhatsNewService(
        IPreferences preferences,
        IAppInfo appInfo,
        IFileSystem fileSystem,
        ILogger<WhatsNewService> logger)
    {
        _preferences = preferences;
        _appInfo = appInfo;
        _fileSystem = fileSystem;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ReleaseEntry?> GetCurrentReleaseAsync(CancellationToken ct = default)
    {
        var entries = await LoadEntriesAsync(ct);
        return entries?.FirstOrDefault(e => e.Version == _appInfo.VersionString);
    }

    /// <inheritdoc />
    public async Task<ReleaseEntry?> GetPendingReleaseAsync(CancellationToken ct = default)
    {
        var current = _appInfo.VersionString;
        var lastSeen = _preferences.Get(LastSeenKey, (string?)null);

        if (lastSeen is null)
        {
            MarkCurrentVersionSeen();
            return null;
        }

        if (lastSeen == current)
            return null;

        var entries = await LoadEntriesAsync(ct);
        return entries?.FirstOrDefault(e => e.Version == current);
    }

    /// <inheritdoc />
    public void MarkCurrentVersionSeen()
        => _preferences.Set(LastSeenKey, _appInfo.VersionString);

    private async Task<IReadOnlyList<ReleaseEntry>?> LoadEntriesAsync(CancellationToken ct)
    {
        try
        {
            await using var stream = await _fileSystem.OpenAppPackageFileAsync(ReleasesFileName);
            var entries = await JsonSerializer.DeserializeAsync<List<ReleaseEntryJson>>(stream,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, ct);
            return entries?
                .Select(e => new ReleaseEntry(e.Version, e.Date,
                    (e.Highlights ?? []).AsReadOnly(),
                    (e.Fixes ?? []).AsReadOnly()))
                .ToList()
                .AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load {File} — skipping What's New", ReleasesFileName);
            return null;
        }
    }

    private sealed class ReleaseEntryJson
    {
        public string Version { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public List<string>? Highlights { get; set; }
        public List<string>? Fixes { get; set; }
    }
}
