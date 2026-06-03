using System.Net.Http.Json;
using System.Text.Json;
using MyVocaList.Contracts.DTOs;
using MyVocaList.Domain.ServicesInterfaces;
using NuGet.Versioning;

namespace MyVocaList.Services;

public sealed class VersionCheckService : IVersionCheckService
{
    private const string ClientName = "version-check";
    private const string ManifestUrl =
        "https://raw.githubusercontent.com/heldercsousa/MyVocaList/main/version-manifest.json";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _currentVersion;
    private readonly string _platformKey;
    private readonly ILogger<VersionCheckService> _logger;

    /// <param name="httpClientFactory">HTTP client factory — must have a named client "version-check" registered.</param>
    /// <param name="currentVersion">The running app version string (e.g. "1.2.3").</param>
    /// <param name="platformKey">"android" or "ios" — used to select the store URL from the manifest.</param>
    /// <param name="logger">Logger for diagnostic warnings on fail-open paths.</param>
    public VersionCheckService(
        IHttpClientFactory httpClientFactory,
        string currentVersion,
        string platformKey,
        ILogger<VersionCheckService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _currentVersion = currentVersion;
        _platformKey = platformKey;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken ct = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            var client = _httpClientFactory.CreateClient(ClientName);
            var manifest = await client.GetFromJsonAsync<VersionManifestJson>(
                ManifestUrl,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                cts.Token);

            if (manifest is null)
            {
                _logger.LogWarning("Version manifest returned null — fail-open");
                return UpdateCheckResult.UpToDate;
            }

            if (!NuGetVersion.TryParse(_currentVersion, out var current) ||
                !NuGetVersion.TryParse(manifest.LatestVersion, out var latest) ||
                !NuGetVersion.TryParse(manifest.MinRequiredVersion, out var minRequired))
            {
                _logger.LogWarning("Version parse failed — fail-open");
                return UpdateCheckResult.UpToDate;
            }

            var storeUrl = manifest.StoreUrls?.GetValueOrDefault(_platformKey, string.Empty) ?? string.Empty;

            if (current < minRequired)
                return new UpdateCheckResult(false, false, true, storeUrl, manifest.LatestVersion, manifest.UpdateMessage ?? string.Empty);

            if (current < latest)
                return new UpdateCheckResult(false, true, false, storeUrl, manifest.LatestVersion, manifest.UpdateMessage ?? string.Empty);

            return UpdateCheckResult.UpToDate;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Version manifest fetch failed — fail-open");
            return UpdateCheckResult.UpToDate;
        }
    }

    private sealed class VersionManifestJson
    {
        public string LatestVersion { get; set; } = string.Empty;
        public string MinRequiredVersion { get; set; } = string.Empty;
        public Dictionary<string, string>? StoreUrls { get; set; }
        public string? UpdateMessage { get; set; }
    }
}
