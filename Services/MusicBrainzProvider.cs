using System.Net.Http.Json;
using System.Text.Json.Serialization;
using MyVocaList.Contracts.DTOs;

namespace MyVocaList.Services;

public sealed class MusicBrainzProvider : IMusicMetadataProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MusicBrainzProvider> _logger;

    public string ProviderName => "MusicBrainz";

    public MusicBrainzProvider(HttpClient httpClient, ILogger<MusicBrainzProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<MusicSearchResultDto>> SearchArtistsAsync(string term, CancellationToken ct = default)
    {
        try
        {
            await Task.Delay(1100, ct);
            var url = $"artist?query={Uri.EscapeDataString(term)}&fmt=json";
            var response = await _httpClient.GetFromJsonAsync<MbArtistSearchResponse>(url, ct);
            return response?.Artists?.Take(5)
                .Select(a => new MusicSearchResultDto(a.Id, ProviderName, a.Name, null, null))
                ?? Enumerable.Empty<MusicSearchResultDto>();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MusicBrainz artist search failed for term '{Term}'", term);
            return Enumerable.Empty<MusicSearchResultDto>();
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<MusicSearchResultDto>> SearchSongsAsync(string term, string? artistHint = null, CancellationToken ct = default)
    {
        try
        {
            await Task.Delay(1100, ct);
            var query = $"recording:{Uri.EscapeDataString(term)}";
            if (!string.IsNullOrWhiteSpace(artistHint))
                query += $" AND artist:{Uri.EscapeDataString(artistHint)}";
            var url = $"recording?query={query}&fmt=json";
            var response = await _httpClient.GetFromJsonAsync<MbRecordingSearchResponse>(url, ct);
            return response?.Recordings?.Take(5)
                .Select(r => new MusicSearchResultDto(
                    r.Id,
                    ProviderName,
                    r.ArtistCredit?.FirstOrDefault()?.Name ?? string.Empty,
                    r.Title,
                    null))
                ?? Enumerable.Empty<MusicSearchResultDto>();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MusicBrainz song search failed for term '{Term}'", term);
            return Enumerable.Empty<MusicSearchResultDto>();
        }
    }

    private record MbArtistSearchResponse([property: JsonPropertyName("artists")] List<MbArtist>? Artists);
    private record MbArtist([property: JsonPropertyName("id")] string Id, [property: JsonPropertyName("name")] string Name);

    private record MbRecordingSearchResponse([property: JsonPropertyName("recordings")] List<MbRecording>? Recordings);
    private record MbRecording(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("artist-credit")] List<MbArtistCredit>? ArtistCredit);
    private record MbArtistCredit([property: JsonPropertyName("name")] string Name);
}
