using MyVocaList.Contracts.DTOs;
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
            .Select(a => new MusicSearchResultDto(a.Id.ToString(), ProviderName, a.Name, null, null))
            .ToList()
            ?? [];
    }

    /// <inheritdoc />
    /// <remarks><paramref name="artistHint"/> is ignored: the query is plain (REQ-SFT-35).</remarks>
    public async Task<IEnumerable<MusicSearchResultDto>> SearchSongsAsync(string term, string? artistHint = null, CancellationToken ct = default)
    {
        var response = await GetAsync<DeezerTrack>($"search/track?q={Uri.EscapeDataString(term)}", ct);
        return response.Data?.Take(MaxResults)
            .Select(t => new MusicSearchResultDto(
                t.Id.ToString(),
                ProviderName,
                t.Artist?.Name ?? string.Empty,
                string.IsNullOrWhiteSpace(t.TitleShort) ? t.Title : t.TitleShort,
                null,
                NormaliseVersion(t.TitleVersion)))
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

    /// <summary>
    /// Trims the Deezer <c>title_version</c> and strips one outer bracket pair, only when the opening
    /// bracket's same-type matching closer is the last character (REQ-SFT-12).
    /// </summary>
    private static string NormaliseVersion(string? raw)
    {
        var value = raw?.Trim() ?? string.Empty;
        if (value.Length < 2) return value;

        var open = value[0];
        var close = open switch { '(' => ')', '[' => ']', _ => '\0' };
        if (close == '\0') return value;

        var depth = 0;
        for (var i = 0; i < value.Length; i++)
        {
            if (value[i] == open) depth++;
            else if (value[i] == close && --depth == 0)
                return i == value.Length - 1 ? value[1..^1].Trim() : value;
        }
        return value;
    }

    private record DeezerError(
        [property: JsonPropertyName("message")] string? Message,
        [property: JsonPropertyName("code")] int? Code);
    private record DeezerListResponse<T>(
        [property: JsonPropertyName("data")] List<T>? Data,
        [property: JsonPropertyName("error")] DeezerError? Error);
    private record DeezerArtist([property: JsonPropertyName("id")] long Id, [property: JsonPropertyName("name")] string Name);
    private record DeezerTrack(
        [property: JsonPropertyName("id")] long Id,
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("title_short")] string? TitleShort,
        [property: JsonPropertyName("title_version")] string? TitleVersion,
        [property: JsonPropertyName("artist")] DeezerTrackArtist? Artist);
    private record DeezerTrackArtist([property: JsonPropertyName("name")] string Name);
}
