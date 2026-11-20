using MyVocaList.Contracts.DTOs;
using MyVocaList.Domain.Constants;
using MyVocaList.Domain.Rules;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace MyVocaList.Services;

public sealed class DeezerProvider : IMusicMetadataProvider
{
    private const int MaxResults = 5;

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
        var response = await GetAsync<DeezerArtist>($"search/artist?q={Uri.EscapeDataString(term)}", ct);
        return response.Data?.Take(MaxResults)
            .Select(a => new MusicSearchResultDto(
                ExternalId: a.Id.ToString(),
                Provider: ProviderName,
                ArtistName: a.Name,
                SongTitle: null,
                ImageUrl: ImageUrlRules.Normalize(a.PictureMedium)))
            .ToList()
            ?? [];
    }

    /// <inheritdoc />
    /// <remarks><paramref name="artistHint"/> is ignored: the query is plain (REQ-SFT-35).</remarks>
    public async Task<IEnumerable<MusicSearchResultDto>> SearchSongsAsync(string term, string? artistHint = null, CancellationToken ct = default)
    {
        var response = await GetAsync<DeezerTrack>($"search/track?q={Uri.EscapeDataString(term)}", ct);
        return response.Data?.Take(SearchConstants.RemoteSongFetchCount)
            .Select(t => new MusicSearchResultDto(
                ExternalId: t.Id.ToString(),
                Provider: ProviderName,
                ArtistName: t.Artist?.Name ?? string.Empty,
                SongTitle: string.IsNullOrWhiteSpace(t.TitleShort) ? t.Title : t.TitleShort,
                ImageUrl: ImageUrlRules.Normalize(t.Album?.CoverMedium),
                ArtistImageUrl: ImageUrlRules.Normalize(t.Artist?.PictureMedium),
                AlbumTitle: NormaliseAlbumTitle(t.Album?.Title),
                DurationSeconds: NormaliseDuration(t.Duration),
                ExternalArtistId: t.Artist?.Id?.ToString(CultureInfo.InvariantCulture)))
            .ToList()
            ?? [];
    }

    private async Task<DeezerListResponse<T>> GetAsync<T>(string url, CancellationToken ct)
    {
        try
        {
            var response = await _httpClient.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadFromJsonAsync<DeezerListResponse<T>>(ct);
            if (body?.Error is { } error)
                throw new MusicProviderException($"Deezer returned an error: {error.Message} (code {error.Code})");
            return body ?? new DeezerListResponse<T>(null, null);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Deezer request failed for '{Url}'", url);
            throw;
        }
    }

    /// <summary>Trims the album title; null when absent or blank (REQ-ART-01).</summary>
    private static string? NormaliseAlbumTitle(string? raw)
    {
        var value = raw?.Trim();
        return string.IsNullOrEmpty(value) ? null : value;
    }

    /// <summary>Track length in seconds; null when absent or not positive (REQ-LYR-01).</summary>
    private static int? NormaliseDuration(int? seconds) => seconds is > 0 ? seconds : null;

    private record DeezerError(
        [property: JsonPropertyName("message")] string? Message,
        [property: JsonPropertyName("code")] int? Code);
    private record DeezerListResponse<T>(
        [property: JsonPropertyName("data")] List<T>? Data,
        [property: JsonPropertyName("error")] DeezerError? Error);
    private record DeezerArtist(
        [property: JsonPropertyName("id")] long Id,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("picture_medium")] string? PictureMedium);
    private record DeezerTrack(
        [property: JsonPropertyName("id")] long Id,
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("title_short")] string? TitleShort,
        [property: JsonPropertyName("artist")] DeezerTrackArtist? Artist,
        [property: JsonPropertyName("album")] DeezerTrackAlbum? Album,
        [property: JsonPropertyName("duration")] int? Duration);
    private record DeezerTrackArtist(
        [property: JsonPropertyName("id")] long? Id,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("picture_medium")] string? PictureMedium);
    private record DeezerTrackAlbum(
        [property: JsonPropertyName("title")] string? Title,
        [property: JsonPropertyName("cover_medium")] string? CoverMedium);
}
