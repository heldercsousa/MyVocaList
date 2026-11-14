using System.Text.Json;
using MyVocaList.Contracts.DTOs.Venues;
using MyVocaList.Domain.Constants;
using MyVocaList.Domain.ServicesInterfaces;

namespace MyVocaList.Services.Places;

/// <summary>
/// Resolves a looked-up place's picture address (REQ-VEN-38, 39, 40). Resolution order: the Wikidata logo statement P154,
/// else the image statement P18 (one request to the Wikidata claims API); then the Commons file name; then the image address.
/// Only the Wikidata step sends a request. Offline, an empty source, a timeout, an HTTP error or a malformed answer give null;
/// no expected failure throws. Caller cancellation propagates.
/// </summary>
public sealed class VenuePictureLookupService : IVenuePictureLookupService
{
    private const string CommonsFilePathPrefix = "https://commons.wikimedia.org/wiki/Special:FilePath/";
    private const string CommonsFilePathSuffix = "?width=512";
    private const string LogoProperty = "P154";
    private const string ImageProperty = "P18";

    private readonly HttpClient _httpClient;
    private readonly INetworkStatus _network;
    private readonly ILogger<VenuePictureLookupService> _logger;

    public VenuePictureLookupService(HttpClient httpClient, INetworkStatus network, ILogger<VenuePictureLookupService> logger)
    {
        _httpClient = httpClient;
        _network = network;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Uri?> ResolveAsync(VenuePictureSource source, CancellationToken ct = default)
    {
        if (source.WikidataId is null && source.CommonsFile is null && source.ImageUrl is null)
            return null;

        if (!_network.HasInternetAccess)
        {
            _logger.LogInformation("Venue picture skipped: no internet access");
            return null;
        }

        if (source.WikidataId is not null)
        {
            var (answered, fileName) = await ReadWikidataFileNameAsync(source.WikidataId, ct);
            if (!answered)
                return null;
            if (fileName is not null)
                return CommonsFileUri(fileName);
        }

        if (source.CommonsFile is not null)
            return CommonsFileUri(source.CommonsFile);

        if (source.ImageUrl is not null && Uri.TryCreate(source.ImageUrl, UriKind.Absolute, out var imageUri))
            return imageUri;

        return null;
    }

    /// <summary>One Wikidata claims request. <c>Answered</c> is false when the request failed; <c>FileName</c> is null when no picture statement exists.</summary>
    private async Task<(bool Answered, string? FileName)> ReadWikidataFileNameAsync(string entityId, CancellationToken ct)
    {
        using var bound = CancellationTokenSource.CreateLinkedTokenSource(ct);
        bound.CancelAfter(SearchConstants.PlaceLookupTimeoutMilliseconds);
        try
        {
            var url = $"w/api.php?action=wbgetclaims&entity={Uri.EscapeDataString(entityId)}&format=json";
            using var response = await _httpClient.GetAsync(url, bound.Token);
            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync(bound.Token);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: bound.Token);

            return (true, FirstFileName(document.RootElement, LogoProperty) ?? FirstFileName(document.RootElement, ImageProperty));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is OperationCanceledException or HttpRequestException or JsonException)
        {
            _logger.LogWarning("Venue picture lookup failed for {WikidataId}: {Reason}", entityId, ex.GetType().Name);
            return (false, null);
        }
    }

    /// <summary>The file name of the first statement of <paramref name="property"/>; null when absent, blank or not a string.</summary>
    private static string? FirstFileName(JsonElement root, string property)
    {
        if (!TryGet(root, "claims", out var claims) || !TryGet(claims, property, out var statements))
            return null;
        if (statements.ValueKind != JsonValueKind.Array || statements.GetArrayLength() == 0)
            return null;
        if (!TryGet(statements[0], "mainsnak", out var snak)
            || !TryGet(snak, "datavalue", out var datavalue)
            || !TryGet(datavalue, "value", out var value))
            return null;

        return value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(value.GetString())
            ? value.GetString()
            : null;
    }

    private static bool TryGet(JsonElement element, string name, out JsonElement child)
    {
        if (element.ValueKind == JsonValueKind.Object)
            return element.TryGetProperty(name, out child);

        child = default;
        return false;
    }

    /// <summary>Commons file address at width 512: spaces become underscores, then the name is escaped.</summary>
    private static Uri? CommonsFileUri(string fileName)
    {
        var name = fileName.Trim().Replace(' ', '_');
        if (name.Length == 0)
            return null;

        return new Uri($"{CommonsFilePathPrefix}{Uri.EscapeDataString(name)}{CommonsFilePathSuffix}", UriKind.Absolute);
    }
}
