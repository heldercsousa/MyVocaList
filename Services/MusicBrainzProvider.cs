using MyVocaList.Contracts.DTOs;
using MyVocaList.Domain.Constants;
using System.Net.Http.Json;

namespace MyVocaList.Services;

/// <summary>MusicBrainz catalog provider (CC0 ids + names). ExternalId is the MBID (REQ-MSRC-02).</summary>
public sealed class MusicBrainzProvider : IMusicMetadataProvider
{
    private const int MaxArtistResults = 5;

    private readonly HttpClient _httpClient;
    private readonly MusicBrainzRateLimiter _rateLimiter;
    private readonly ILogger<MusicBrainzProvider> _logger;

    public MusicBrainzProvider(HttpClient httpClient, MusicBrainzRateLimiter rateLimiter, ILogger<MusicBrainzProvider> logger)
    {
        _httpClient = httpClient;
        _rateLimiter = rateLimiter;
        _logger = logger;
    }

    /// <inheritdoc />
    public string ProviderName => ProviderNames.MusicBrainz;

    /// <summary>Builds <c>MyVocaList/&lt;version&gt; (&lt;contact&gt;)</c>; the bare product token when contact is blank (REQ-MSRC-10).</summary>
    public static string BuildUserAgent(string version, string? contact) =>
        string.IsNullOrWhiteSpace(contact)
            ? $"MyVocaList/{version}"
            : $"MyVocaList/{version} ({contact.Trim()})";

    /// <inheritdoc />
    public async Task<IEnumerable<MusicSearchResultDto>> SearchArtistsAsync(string term, CancellationToken ct = default)
    {
        var query = $"artist:\"{EscapeLucene(term)}\"";
        var body = await GetAsync<MbArtistSearchResponse>($"artist/?query={Uri.EscapeDataString(query)}&fmt=json&limit={MaxArtistResults}", ct);
        return body?.Artists?.Take(MaxArtistResults)
            .Select(a => new MusicSearchResultDto(a.Id, ProviderName, a.Name, SongTitle: null))
            .ToList()
            ?? [];
    }

    /// <inheritdoc />
    public async Task<IEnumerable<MusicSearchResultDto>> SearchSongsAsync(string term, string? artistHint = null, CancellationToken ct = default)
    {
        var query = $"recording:\"{EscapeLucene(term)}\"";
        if (!string.IsNullOrWhiteSpace(artistHint))
            query += $" AND artist:\"{EscapeLucene(artistHint)}\"";
        var body = await GetAsync<MbRecordingSearchResponse>($"recording/?query={Uri.EscapeDataString(query)}&fmt=json&limit={SearchConstants.RemoteSongFetchCount}", ct);
        return body?.Recordings?.Take(SearchConstants.RemoteSongFetchCount)
            .Select(r =>
            {
                var credit = r.ArtistCredit?.FirstOrDefault();
                var album = r.Releases?.FirstOrDefault()?.Title?.Trim();
                return new MusicSearchResultDto(
                    ExternalId: r.Id,
                    Provider: ProviderName,
                    ArtistName: credit?.Artist?.Name ?? credit?.Name ?? string.Empty,
                    SongTitle: r.Title,
                    AlbumTitle: string.IsNullOrEmpty(album) ? null : album,
                    DurationSeconds: r.Length is > 0 ? r.Length / 1000 : null,
                    ExternalArtistId: credit?.Artist?.Id);
            })
            .ToList()
            ?? [];
    }

    private static string EscapeLucene(string term) =>
        term.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private async Task<T?> GetAsync<T>(string url, CancellationToken ct)
    {
        try
        {
            await _rateLimiter.WaitAsync(ct);
            var response = await _httpClient.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
                throw new MusicProviderException($"MusicBrainz returned HTTP {(int)response.StatusCode}");
            return await response.Content.ReadFromJsonAsync<T>(ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (MusicProviderException ex)
        {
            _logger.LogWarning(ex, "MusicBrainz request failed for '{Url}'", url);
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException)
        {
            _logger.LogWarning(ex, "MusicBrainz request failed for '{Url}'", url);
            throw new MusicProviderException("MusicBrainz request failed", ex);
        }
    }
}
