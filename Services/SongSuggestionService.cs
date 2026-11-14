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
    private const string DeezerProviderName = "Deezer";

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
        if (q.Length < SearchConstants.MinimumRemoteQueryLength)
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

        var raw = await TryFetchDeezerAsync(q, ct);
        if (raw is null)
            return new SongRemoteSearchResult([], [], RemoteSearchStatus.Unavailable);
        if (raw.Count == 0)
            return new SongRemoteSearchResult([], [], RemoteSearchStatus.Ok);

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
            "Remote title search '{Query}': {Raw} from Deezer, {Promoted} matched the library, {Remote} remote options",
            q, raw.Count, promoted.Count, remote.Count);
        return new SongRemoteSearchResult(promoted, remote, RemoteSearchStatus.Ok);
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    // Promoted rows also come through here, so they carry the stored images and no album (REQ-ART-06).
    private static SongSuggestionDto MapLocal(SongSearchHit hit) => new(
        hit.SongId, hit.Title, hit.ArtistName, hit.ArtistId, hit.ExternalId, hit.ExternalProvider,
        false, hit.Version ?? string.Empty, hit.ArtistName, hit.ImageUrl, hit.ArtistImageUrl, null);

    /// <summary>Returns null on any failure/timeout (Unavailable); rethrows caller cancellation.</summary>
    private async Task<IReadOnlyList<MusicSearchResultDto>?> TryFetchDeezerAsync(string q, CancellationToken ct)
    {
        var deezer = _providers.FirstOrDefault(p => p.ProviderName == DeezerProviderName);
        if (deezer is null)
        {
            _logger.LogWarning("Deezer provider is not registered; remote title search unavailable");
            return null;
        }

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct);
        linked.CancelAfter(SearchConstants.RemoteSearchTimeoutMilliseconds);
        try
        {
            var results = await deezer.SearchSongsAsync(q, null, linked.Token);
            return results?.ToList() ?? [];
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Deezer title search failed or timed out for query '{Query}'", q);
            return null;
        }
    }

    // Identity first (external id), then collated (title, artist, version). Each triple is looked up on its
    // own so a returned hit maps to its row without any C#-side text comparison (HARD RULE).
    private static async Task<(List<SongSearchHit> Promoted, List<MusicSearchResultDto> Remaining)> DedupAgainstLibraryAsync(
        IServiceProvider sp, IReadOnlyList<MusicSearchResultDto> raw, CancellationToken ct)
    {
        var songRepository = sp.GetRequiredService<ISongRepository>();
        var promoted = new List<SongSearchHit>();

        var ids = raw.Select(r => r.ExternalId).Where(i => !string.IsNullOrEmpty(i)).Distinct().ToList();
        IReadOnlyList<SongSearchHit> byIdentity = ids.Count > 0
            ? await songRepository.FindByExternalIdsAsync(DeezerProviderName, ids, ct)
            : [];
        promoted.AddRange(byIdentity);
        var matchedIds = byIdentity.Select(h => h.ExternalId).ToHashSet();

        var remaining = new List<MusicSearchResultDto>();
        foreach (var row in raw)
        {
            if (matchedIds.Contains(row.ExternalId))
                continue;

            if (string.IsNullOrWhiteSpace(row.SongTitle))
            {
                remaining.Add(row);
                continue;
            }

            var triple = (row.SongTitle, row.ArtistName, row.Version ?? string.Empty);
            var hits = await songRepository.FindByTitleAndArtistNamesAsync([triple], ct);
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
            return new SongSuggestionDto(
                null, r.SongTitle ?? string.Empty, r.ArtistName, artist?.Id, r.ExternalId, r.Provider,
                true, r.Version ?? string.Empty, artist?.Name, r.ImageUrl, r.ArtistImageUrl, r.AlbumTitle,
                r.DurationSeconds);
        }).ToList();
    }
}
