using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using MyVocaList.Contracts.DTOs;

namespace MyVocaList.Services;

/// <summary>
/// LRCLIB lyrics source (<c>https://lrclib.net/api/get</c>): plain lyrics of a title and artist (REQ-LYR-01, 02, 03, 07).
/// The relative URI resolves against the typed client's <c>BaseAddress</c>; the User-Agent is set at registration (REQ-LYR-15).
/// </summary>
public sealed class LrclibLyricsProvider : ILyricsProvider
{
    private const int MinDurationSeconds = 1;
    private const int MaxDurationSeconds = 3600;

    private readonly HttpClient _httpClient;
    private readonly ILogger<LrclibLyricsProvider> _logger;

    public LrclibLyricsProvider(HttpClient httpClient, ILogger<LrclibLyricsProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string?> FetchLyricsAsync(LyricsQuery query, CancellationToken ct = default)
    {
        var url = BuildUrl(query);
        try
        {
            var response = await _httpClient.GetAsync(url, ct);
            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadFromJsonAsync<LrclibGetResponse>(ct);
            if (body is null || body.Instrumental == true)
                return null;

            return string.IsNullOrWhiteSpace(body.PlainLyrics) ? null : body.PlainLyrics;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LRCLIB request failed for '{Url}'", url);
            throw;
        }
    }

    /// <summary>
    /// Builds <c>api/get</c> with every value escaped. <c>album_name</c> only when not blank;
    /// <c>duration</c> only when 1 to 3600 seconds (REQ-LYR-01).
    /// </summary>
    private static string BuildUrl(LyricsQuery query)
    {
        var parts = new List<string>
        {
            $"track_name={Uri.EscapeDataString(query.Title)}",
            $"artist_name={Uri.EscapeDataString(query.ArtistName)}",
        };

        if (!string.IsNullOrWhiteSpace(query.AlbumTitle))
            parts.Add($"album_name={Uri.EscapeDataString(query.AlbumTitle)}");

        if (query.DurationSeconds is >= MinDurationSeconds and <= MaxDurationSeconds)
            parts.Add($"duration={query.DurationSeconds.Value.ToString(CultureInfo.InvariantCulture)}");

        return "api/get?" + string.Join('&', parts);
    }

    private record LrclibGetResponse(
        [property: JsonPropertyName("plainLyrics")] string? PlainLyrics,
        [property: JsonPropertyName("instrumental")] bool? Instrumental);
}
