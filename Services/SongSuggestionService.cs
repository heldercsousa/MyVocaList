using Microsoft.Extensions.DependencyInjection;
using MyVocaList.Contracts.DTOs;
using MyVocaList.Contracts.DTOs.List;
using MyVocaList.Contracts.DTOs.Suggestions;
using MyVocaList.Domain.Constants;
using MyVocaList.Domain.Entity;
using MyVocaList.Domain.ReadModels;
using MyVocaList.Domain.RepositoryInterface;
using MyVocaList.Domain.Resolution;
using MyVocaList.Domain.ServicesInterfaces;
using MyVocaList.Domain.UnitOfWork;
using MyVocaList.Extensions.Strings;

namespace MyVocaList.Services;

/// <inheritdoc />
public class SongSuggestionService : ISongSuggestionService
{
    // Remote title suggestions come from iTunes (REQ-ITS-01); MusicBrainz serves only the post-pick MBID lookup (REQ-ITS-11).
    private const string MusicProviderName = ProviderNames.ITunes;
    private const string MusicBrainzProviderName = ProviderNames.MusicBrainz;

    private readonly ISongRepository _songRepository;
    private readonly IArtistRepository _artistRepository;
    private readonly IUnitOfWork _uow;
    private readonly IReadOnlyList<IMusicMetadataProvider> _providers;
    private readonly ISimilarityScorer _scorer;
    private readonly INetworkStatus _networkStatus;
    private readonly ILogger<SongSuggestionService> _logger;

    public SongSuggestionService(
        ISongRepository songRepository,
        IArtistRepository artistRepository,
        IUnitOfWork uow,
        IEnumerable<IMusicMetadataProvider> providers,
        ISimilarityScorer scorer,
        INetworkStatus networkStatus,
        ILogger<SongSuggestionService> logger)
    {
        _songRepository = songRepository;
        _artistRepository = artistRepository;
        _uow = uow;
        _providers = providers.ToList();
        _scorer = scorer;
        _networkStatus = networkStatus;
        _logger = logger;
    }

    /// <inheritdoc />
    // [AC] REQ-SFT-03/04/30/32: guard on the trimmed query; DB relevance order is re-ranked in memory
    // (ordering only, design § 6.4) over the bounded pool.
    public async Task<IReadOnlyList<SongSuggestionDto>> SearchTitleLocalAsync(string query, CancellationToken ct = default)
    {
        var q = query.NormalizeSearchQuery();
        if (q.Length < SearchConstants.MinimumLocalQueryLength)
            return [];

        var hits = await _uow.ExecuteReadAsync(async sp =>
        {
            var songRepository = sp.GetRequiredService<ISongRepository>();
            return await songRepository.SearchTitleCandidatesAsync(q, SimilarityConstants.PoolSize, ct);
        }, ct);

        // OrderByDescending is stable, so equal scores keep the DB (bm25) order.
        return hits
            .OrderByDescending(h => Math.Max(
                _scorer.RankScore(q, h.Title),
                _scorer.RankScore(q, h.Title + " " + h.ArtistName)))
            .Take(SearchConstants.MaxSearchResults)
            .Select(MapLocal)
            .ToList();
    }

    /// <inheritdoc />
    // [AC] REQ-SFT-03/05/06/08/09/12/13/35/36/53
    public async Task<SongRemoteSearchResult> SearchTitleRemoteAsync(
        string query, IReadOnlyCollection<int> shownLocalSongIds, CancellationToken ct = default)
    {
        var q = query.NormalizeSearchQuery();
        if (q.Length < SearchConstants.SongTitleRemoteMinimumLength)
            return new SongRemoteSearchResult([], [], RemoteSearchStatus.Ok);

        // Any failure of the remote path degrades to Unavailable with a logged warning, so the caller
        // shows the note instead of losing the whole option list to an exception (REQ-SFT-06).
        try
        {
            return await SearchTitleRemoteCoreAsync(q, shownLocalSongIds, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Remote title search failed for query '{Query}'", q);
            return new SongRemoteSearchResult([], [], RemoteSearchStatus.Unavailable);
        }
    }

    private async Task<SongRemoteSearchResult> SearchTitleRemoteCoreAsync(
        string q, IReadOnlyCollection<int> shownLocalSongIds, CancellationToken ct)
    {
        // Read live on every call, never cached (REQ-SFT-53).
        if (!_networkStatus.HasInternetAccess)
        {
            _logger.LogInformation("Remote title search skipped: device has no internet access");
            return new SongRemoteSearchResult([], [], RemoteSearchStatus.Offline);
        }

        var fetched = await TryFetchRemoteAsync(q, ct);
        if (fetched is null)
            return new SongRemoteSearchResult([], [], RemoteSearchStatus.Unavailable);
        if (fetched.Count == 0)
            return new SongRemoteSearchResult([], [], RemoteSearchStatus.Ok);

        // REQ-ITS-14: fewer than 3 rows → one retry with the last word dropped; retry rows follow the first answer's rows.
        if (fetched.Count < SearchConstants.SongTitleRetryBelowRowCount && SongTitleMatcher.DropLastWord(q) is { } shorter)
        {
            var retry = await TryFetchRemoteAsync(shorter, ct);
            if (retry is not null)
                fetched = [.. fetched, .. retry];
        }

        // REQ-ITS-02: one row per (title, artist) pair; REQ-ITS-16: coverage, title fit, bracket suffix, then iTunes order.
        var distinct = DistinctByTitleAndArtistName(fetched.Take(SearchConstants.RemoteSongFetchCount));
        var raw = SongTitleMatcher.RankRows(q, distinct, r => r.SongTitle ?? string.Empty, r => r.ArtistName ?? string.Empty);

        // No HTTP inside the read scope: the fetch above has completed (REQ-UOW-43).
        var (promotedHits, remaining) = await _uow.ExecuteReadAsync(
            async sp => await DedupAgainstLibraryAsync(sp, raw, ct), ct);

        var shown = new HashSet<int>(shownLocalSongIds);
        var promoted = promotedHits
            .DistinctBy(h => h.SongId)
            .Where(h => !shown.Contains(h.SongId))
            .Select(MapLocal)
            .ToList();

        var top = remaining.Take(SearchConstants.MaxSearchResults).ToList();
        var remote = await ResolveLocalArtistsAsync(top, ct);
        _logger.LogInformation(
            "Remote title search '{Query}': {Raw} from iTunes, {Promoted} matched the library, {Remote} remote options",
            q, fetched.Count, promoted.Count, remote.Count);
        return new SongRemoteSearchResult(promoted, remote, RemoteSearchStatus.Ok);
    }

    /// <inheritdoc />
    // [AC] REQ-ITS-11: MusicBrainz ids for the artist and the release of a picked iTunes row, looked up after the pick.
    // A row matches when its title and artist text equal the pick exactly (no rewriting, HARD RULE). Any failure or
    // timeout gives no ids (failure-silent); caller cancellation is rethrown.
    public async Task<(string? ArtistMbid, string? ReleaseMbid)> ResolveMusicBrainzIdsAsync(
        string title, string artistName, CancellationToken ct = default)
    {
        var provider = _providers.FirstOrDefault(p => p.ProviderName == MusicBrainzProviderName);
        if (provider is null || string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(artistName))
            return (null, null);

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct);
        // The limiter's wait (and one 503 retry) happens inside the call, so the bound adds room for it.
        linked.CancelAfter(SearchConstants.RemoteSearchTimeoutMilliseconds + 2 * MusicBrainzRateLimiter.MinimumIntervalMs);
        try
        {
            var rows = await provider.SearchSongsAsync(title, artistName, linked.Token);
            var match = rows.FirstOrDefault(r =>
                string.Equals(r.SongTitle, title, StringComparison.Ordinal)
                && string.Equals(r.ArtistName, artistName, StringComparison.Ordinal)
                && !string.IsNullOrEmpty(r.ExternalArtistId));
            return match is null ? (null, null) : (match.ExternalArtistId, match.ReleaseMbid);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogInformation(ex, "MusicBrainz id lookup after a pick failed for '{Title}'; no ids stored", title);
            return (null, null);
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    // [AC] REQ-ITS-02 (supersedes the artist-id grouping of REQ-SLD-06/11 for iTunes rows, which carry no artist id):
    // one row per (title, artist name) pair, first occurrence kept, in the provider's popularity order. Ordinal
    // in-memory comparison of the provider's own text, display-only (HARD RULE: no rewriting).
    private static List<MusicSearchResultDto> DistinctByTitleAndArtistName(IEnumerable<MusicSearchResultDto> rows)
    {
        var seen = new HashSet<(string Title, string Artist)>();
        var result = new List<MusicSearchResultDto>();
        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.SongTitle) || string.IsNullOrWhiteSpace(row.ArtistName)
                || seen.Add((row.SongTitle, row.ArtistName)))
                result.Add(row);
        }

        return result;
    }

    // Promoted rows also come through here, so they carry the stored images and no album (REQ-ART-06).
    private static SongSuggestionDto MapLocal(SongSearchHit hit) => new(
        hit.SongId, hit.Title, hit.ArtistName, hit.ArtistId, hit.ExternalId, hit.ExternalProvider,
        false, hit.ArtistName, hit.ImageUrl, hit.ArtistImageUrl, null);

    /// <summary>Returns null on any failure/timeout (Unavailable); rethrows caller cancellation.</summary>
    private async Task<IReadOnlyList<MusicSearchResultDto>?> TryFetchRemoteAsync(string q, CancellationToken ct)
    {
        var provider = _providers.FirstOrDefault(p => p.ProviderName == MusicProviderName);
        if (provider is null)
        {
            _logger.LogWarning("iTunes provider is not registered; remote title search unavailable");
            return null;
        }

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct);
        linked.CancelAfter(SearchConstants.RemoteSearchTimeoutMilliseconds);
        try
        {
            var results = await provider.SearchSongsAsync(q, null, linked.Token);
            return results?.ToList() ?? [];
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "iTunes title search failed or timed out for query '{Query}'", q);
            return null;
        }
    }

    // Identity first (external id), then collated (title, artist) (REQ-RMV-08). Each pair is looked up on its
    // own so a returned hit maps to its row without any C#-side text comparison (HARD RULE).
    private static async Task<(List<SongSearchHit> Promoted, List<MusicSearchResultDto> Remaining)> DedupAgainstLibraryAsync(
        IServiceProvider sp, IReadOnlyList<MusicSearchResultDto> raw, CancellationToken ct)
    {
        var songRepository = sp.GetRequiredService<ISongRepository>();
        var promoted = new List<SongSearchHit>();

        // iTunes rows carry no external identity (REQ-ITS-10), so the match is the collated (title, artist) pair only.
        var remaining = new List<MusicSearchResultDto>();
        foreach (var row in raw)
        {
            if (string.IsNullOrWhiteSpace(row.SongTitle))
            {
                remaining.Add(row);
                continue;
            }

            var pair = (row.SongTitle, row.ArtistName);
            var hits = await songRepository.FindByTitleAndArtistNamesAsync([pair], ct);
            if (hits.Count == 0)
                remaining.Add(row);
            else
                promoted.Add(hits[0]);
        }

        return (promoted, remaining);
    }

    private async Task<List<SongSuggestionDto>> ResolveLocalArtistsAsync(
        List<MusicSearchResultDto> rows, CancellationToken ct)
    {
        var names = rows.Select(r => r.ArtistName).Where(n => !string.IsNullOrWhiteSpace(n)).Distinct().ToList();

        // One collated lookup per distinct name: the DB, not C#, decides which stored artist matches.
        var resolved = names.Count == 0
            ? new Dictionary<string, Artist>()
            : await _uow.ExecuteReadAsync(async sp =>
            {
                var artistRepository = sp.GetRequiredService<IArtistRepository>();
                var map = new Dictionary<string, Artist>();
                foreach (var name in names)
                {
                    var found = await artistRepository.GetByNamesCollatedAsync([name], ct);
                    if (found.Count > 0)
                        map[name] = found[0];
                }
                return map;
            }, ct);

        return rows.Select(r =>
        {
            Artist? artist = null;
            if (r.ArtistName is not null)
                resolved.TryGetValue(r.ArtistName, out artist);
            // iTunes track ids are not stored (REQ-ITS-10 and Apple terms): an iTunes row carries no external identity.
            var isITunes = r.Provider == ProviderNames.ITunes;
            return new SongSuggestionDto(
                null, r.SongTitle ?? string.Empty, r.ArtistName, artist?.Id,
                isITunes ? null : r.ExternalId, isITunes ? null : r.Provider,
                true, artist?.Name, r.ImageUrl, r.ArtistImageUrl, r.AlbumTitle,
                r.DurationSeconds, r.ExternalArtistId, r.ReleaseMbid);
        }).ToList();
    }
}
