using MyVocaList.Contracts.DTOs;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace MyVocaList.Services;

public sealed class DeezerProvider : IMusicMetadataProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DeezerProvider> _logger;

    public string ProviderName => "Deezer";

    public DeezerProvider(HttpClient httpClient, ILogger<DeezerProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<MusicSearchResultDto>> SearchArtistsAsync(string term, CancellationToken ct = default)
    {
        try
        {
            var url = $"search/artist?q={Uri.EscapeDataString(term)}";
            var response = await _httpClient.GetFromJsonAsync<DeezerListResponse<DeezerArtist>>(url, ct);
            return response?.Data?.Take(5)
                .Select(a => new MusicSearchResultDto(a.Id.ToString(), ProviderName, a.Name, null, null))
                ?? Enumerable.Empty<MusicSearchResultDto>();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Deezer artist search failed for term '{Term}'", term);
            return Enumerable.Empty<MusicSearchResultDto>();
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<MusicSearchResultDto>> SearchSongsAsync(string term, string? artistHint = null, CancellationToken ct = default)
    {
        try
        {
            var query = $"track:\"{Uri.EscapeDataString(term)}\"";
            if (!string.IsNullOrWhiteSpace(artistHint))
                query += $" artist:\"{Uri.EscapeDataString(artistHint)}\"";
            var url = $"search/track?q={query}";
            var response = await _httpClient.GetFromJsonAsync<DeezerListResponse<DeezerTrack>>(url, ct);
            return response?.Data?.Take(5)
                .Select(t => new MusicSearchResultDto(t.Id.ToString(), ProviderName, t.Artist.Name, t.Title, null))
                ?? Enumerable.Empty<MusicSearchResultDto>();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Deezer song search failed for term '{Term}'", term);
            return Enumerable.Empty<MusicSearchResultDto>();
        }
    }

    private record DeezerListResponse<T>([property: JsonPropertyName("data")] List<T>? Data);
    private record DeezerArtist([property: JsonPropertyName("id")] long Id, [property: JsonPropertyName("name")] string Name);
    private record DeezerTrack(
        [property: JsonPropertyName("id")] long Id,
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("artist")] DeezerTrackArtist Artist);
    private record DeezerTrackArtist([property: JsonPropertyName("name")] string Name);
}
