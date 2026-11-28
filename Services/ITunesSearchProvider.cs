using System.Globalization;
using System.Net.Http.Json;
using MyVocaList.Contracts.DTOs;
using MyVocaList.Domain.Constants;

namespace MyVocaList.Services;

/// <summary>
/// Song-title suggestions from the keyless iTunes Search API, in its popularity order (REQ-ITS-01..03).
/// Title suggestions only: artist search is not served here and returns empty.
/// </summary>
public sealed class ITunesSearchProvider : IMusicMetadataProvider
{
    private const int SongLimit = 25;   // REQ-ITS-01 amended 2026-11-28: fetch 25 rows so the re-rank can reach them
    private const string SmallArtworkSize = "100x100bb";
    private const string SuggestionArtworkSize = "250x250bb";

    private readonly HttpClient _httpClient;
    private readonly ITunesRequestBudget _budget;
    private readonly ILogger<ITunesSearchProvider> _logger;

    public ITunesSearchProvider(HttpClient httpClient, ITunesRequestBudget budget, ILogger<ITunesSearchProvider> logger)
    {
        _httpClient = httpClient;
        _budget = budget;
        _logger = logger;
    }

    /// <inheritdoc />
    public string ProviderName => ProviderNames.ITunes;

    /// <inheritdoc />
    public Task<IEnumerable<MusicSearchResultDto>> SearchArtistsAsync(string term, CancellationToken ct = default) =>
        Task.FromResult<IEnumerable<MusicSearchResultDto>>([]);

    /// <inheritdoc />
    public async Task<IEnumerable<MusicSearchResultDto>> SearchSongsAsync(string term, string? artistHint = null, CancellationToken ct = default)
    {
        if (!_budget.TryTake())
        {
            _logger.LogInformation("iTunes title search skipped: call budget of {Max} per minute reached", ITunesRequestBudget.MaxCallsPerMinute);
            return [];
        }

        var url = $"search?entity=song&limit={SongLimit}&term={Uri.EscapeDataString(term)}";
        var body = await _httpClient.GetFromJsonAsync<ITunesSearchResponse>(url, ct);
        return body?.Results?
            .Where(r => r.TrackId is not null && !string.IsNullOrWhiteSpace(r.TrackName))
            .Select(r => new MusicSearchResultDto(
                ExternalId: r.TrackId!.Value.ToString(CultureInfo.InvariantCulture),
                Provider: ProviderName,
                ArtistName: r.ArtistName ?? string.Empty,
                SongTitle: r.TrackName,
                ImageUrl: r.ArtworkUrl100?.Replace(SmallArtworkSize, SuggestionArtworkSize, StringComparison.Ordinal),
                AlbumTitle: string.IsNullOrWhiteSpace(r.CollectionName) ? null : r.CollectionName,
                DurationSeconds: r.TrackTimeMillis is > 0 ? r.TrackTimeMillis / 1000 : null))
            .ToList()
            ?? [];
    }

    private sealed record ITunesSearchResponse(List<ITunesTrack>? Results);

    private sealed record ITunesTrack(
        long? TrackId,
        string? TrackName,
        string? ArtistName,
        string? CollectionName,
        string? ArtworkUrl100,
        int? TrackTimeMillis);
}
