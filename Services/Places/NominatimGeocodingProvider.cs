using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyVocaList.Services.Places;

/// <summary>
/// Nominatim (OpenStreetMap) geocoding over a typed <see cref="HttpClient"/> (REQ-VEN-25..28, 34).
/// Sets no headers, throttles nothing and caches nothing: the User-Agent comes from DI, the rate gate and the cache
/// are applied by <c>PlaceLookupGate</c>.
/// </summary>
public sealed class NominatimGeocodingProvider : IGeocodingProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NominatimGeocodingProvider> _logger;

    public NominatimGeocodingProvider(HttpClient httpClient, ILogger<NominatimGeocodingProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<GeocodingRecord?> ReverseAsync(double latitude, double longitude, string language, CancellationToken ct = default)
    {
        var lat = latitude.ToString("F6", CultureInfo.InvariantCulture);
        var lon = longitude.ToString("F6", CultureInfo.InvariantCulture);
        var url = $"reverse?format=jsonv2&lat={lat}&lon={lon}&zoom=18&addressdetails=1&accept-language={Uri.EscapeDataString(language)}&extratags=1";

        var place = await GetAsync<NominatimPlace>(url, ct);
        // An error member means nothing is at this point (REQ-VEN-27); presence is tested, not the value.
        if (place is null || place.Error.ValueKind != JsonValueKind.Undefined)
            return null;
        return ToRecord(place);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<GeocodingRecord>> SearchAsync(string text, string language, int limit, CancellationToken ct = default)
    {
        var url = $"search?format=jsonv2&q={Uri.EscapeDataString(text)}&limit={limit.ToString(CultureInfo.InvariantCulture)}&addressdetails=1&accept-language={Uri.EscapeDataString(language)}&extratags=1";

        var places = await GetAsync<List<NominatimPlace>>(url, ct);
        if (places is null)
            return [];
        return places.Select(ToRecord).OfType<GeocodingRecord>().ToList();
    }

    private async Task<T?> GetAsync<T>(string url, CancellationToken ct)
    {
        try
        {
            var response = await _httpClient.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<T>(ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Nominatim request failed for '{Url}'", url);
            throw;
        }
    }

    /// <summary>Maps one answer; null when lat or lon is missing, not a string, unparsable or not finite.</summary>
    private static GeocodingRecord? ToRecord(NominatimPlace place)
    {
        if (!TryParseCoordinate(place.Lat, out var latitude) || !TryParseCoordinate(place.Lon, out var longitude))
            return null;

        var address = new Dictionary<string, string>(StringComparer.Ordinal);
        if (place.Address.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in place.Address.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.String && property.Value.GetString() is { } value)
                    address[property.Name] = value;
            }
        }

        var extraTags = new Dictionary<string, string>(StringComparer.Ordinal);
        if (place.ExtraTags.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in place.ExtraTags.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.String && property.Value.GetString() is { } value)
                    extraTags[property.Name] = value;
            }
        }

        return new GeocodingRecord(
            latitude,
            longitude,
            AsString(place.Name),
            AsString(place.Category),
            AsString(place.DisplayName),
            address,
            extraTags);
    }

    private static bool TryParseCoordinate(JsonElement value, out double result)
    {
        result = 0;
        return value.ValueKind == JsonValueKind.String
            && double.TryParse(value.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out result)
            && double.IsFinite(result);
    }

    private static string? AsString(JsonElement value) =>
        value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    // Fields are JsonElement so a missing member is ValueKind.Undefined and a non-string value is ignored, never a parse failure.
    private sealed record NominatimPlace(
        [property: JsonPropertyName("lat")] JsonElement Lat,
        [property: JsonPropertyName("lon")] JsonElement Lon,
        [property: JsonPropertyName("name")] JsonElement Name,
        [property: JsonPropertyName("category")] JsonElement Category,
        [property: JsonPropertyName("display_name")] JsonElement DisplayName,
        [property: JsonPropertyName("address")] JsonElement Address,
        [property: JsonPropertyName("error")] JsonElement Error,
        [property: JsonPropertyName("extratags")] JsonElement ExtraTags);
}
