using Microsoft.Extensions.DependencyInjection;
using MyVocaList.Contracts.DTOs;
using MyVocaList.Contracts.DTOs.Suggestions;
using MyVocaList.Domain.Constants;
using MyVocaList.Domain.Entity;
using MyVocaList.Domain.RepositoryInterface;
using MyVocaList.Domain.Resolution;
using MyVocaList.Domain.ServicesInterfaces;
using MyVocaList.Domain.UnitOfWork;
using MyVocaList.Extensions.Strings;

namespace MyVocaList.Services;

/// <inheritdoc />
public class ArtistSuggestionService : IArtistSuggestionService
{
    private const string DeezerProviderName = "Deezer";

    private readonly IArtistRepository _artistRepository;
    private readonly IUnitOfWork _uow;
    private readonly IReadOnlyList<IMusicMetadataProvider> _providers;
    private readonly ISimilarityScorer _scorer;
    private readonly INetworkStatus _networkStatus;
    private readonly ILogger<ArtistSuggestionService> _logger;

    public ArtistSuggestionService(
        IArtistRepository artistRepository,
        IUnitOfWork uow,
        IEnumerable<IMusicMetadataProvider> providers,
        ISimilarityScorer scorer,
        INetworkStatus networkStatus,
        ILogger<ArtistSuggestionService> logger)
    {
        _artistRepository = artistRepository;
        _uow = uow;
        _providers = providers.ToList();
        _scorer = scorer;
        _networkStatus = networkStatus;
        _logger = logger;
    }

    /// <inheritdoc />
    // [AC] REQ-SFT-03/47: guard on the trimmed query OUTSIDE the lambda (no scope, no SQL below 2 chars);
    // the DB candidate pool is re-ranked in memory (ordering only, design § 6.4).
    public async Task<IReadOnlyList<ArtistSuggestionDto>> SearchNameLocalAsync(string query, CancellationToken ct = default)
    {
        var q = query.NormalizeSearchQuery();
        if (q.Length < SearchConstants.MinimumLocalQueryLength)
            return [];

        // REQ-UOW-37/38: repository resolved from the lambda's own scope, never the constructor field.
        var hits = await _uow.ExecuteReadAsync(async sp =>
        {
            var artistRepository = sp.GetRequiredService<IArtistRepository>();
            return await artistRepository.SearchNameCandidatesAsync(q, SimilarityConstants.PoolSize, ct);
        }, ct);

        // OrderByDescending is stable, so equal scores keep the DB (bm25) order.
        return hits
            .OrderByDescending(a => _scorer.RankScore(q, a.Name))
            .Take(SearchConstants.MaxSearchResults)
            .Select(a => new ArtistSuggestionDto(a.Id, a.Name, null, null, false, false))
            .ToList();
    }

    /// <inheritdoc />
    // [AC] REQ-SFT-03/06/08/09/36/47/48/53
    public async Task<ArtistRemoteSearchResult> SearchNameRemoteAsync(string query, CancellationToken ct = default)
    {
        var q = query.NormalizeSearchQuery();
        if (q.Length < SearchConstants.MinimumRemoteQueryLength)
            return new ArtistRemoteSearchResult([], RemoteSearchStatus.Ok);

        // Read live on every call, never cached (REQ-SFT-53).
        if (!_networkStatus.HasInternetAccess)
        {
            _logger.LogInformation("Remote artist search skipped: device has no internet access");
            return new ArtistRemoteSearchResult([], RemoteSearchStatus.Offline);
        }

        var raw = await TryFetchDeezerAsync(q, ct);
        if (raw is null)
            return new ArtistRemoteSearchResult([], RemoteSearchStatus.Unavailable);
        if (raw.Count == 0)
            return new ArtistRemoteSearchResult([], RemoteSearchStatus.Ok);

        var rows = raw.Where(r => !string.IsNullOrWhiteSpace(r.ArtistName)).ToList();
        var names = rows.Select(r => r.ArtistName).Distinct().ToList();

        // No HTTP inside the read scope: the fetch above has completed (REQ-UOW-43). One collated lookup per
        // name — the DB, not C#, decides which Deezer names already exist locally (HARD RULE).
        var existing = names.Count == 0
            ? new HashSet<string>()
            : await _uow.ExecuteReadAsync(async sp =>
            {
                var artistRepository = sp.GetRequiredService<IArtistRepository>();
                var found = new HashSet<string>();
                foreach (var name in names)
                {
                    var matches = await artistRepository.GetByNamesCollatedAsync([name], ct);
                    if (matches is { Count: > 0 })
                        found.Add(name);
                }
                return found;
            }, ct);

        var remote = rows
            .Where(r => !existing.Contains(r.ArtistName))
            .Take(SearchConstants.MaxSearchResults)
            .Select(r => new ArtistSuggestionDto(null, r.ArtistName, r.ExternalId, r.Provider, true, false))
            .ToList();
        return new ArtistRemoteSearchResult(remote, RemoteSearchStatus.Ok);
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    /// <summary>Returns null on any failure/timeout (Unavailable); rethrows caller cancellation.</summary>
    private async Task<IReadOnlyList<MusicSearchResultDto>?> TryFetchDeezerAsync(string q, CancellationToken ct)
    {
        var deezer = _providers.FirstOrDefault(p => p.ProviderName == DeezerProviderName);
        if (deezer is null)
        {
            _logger.LogWarning("Deezer provider is not registered; remote artist search unavailable");
            return null;
        }

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct);
        linked.CancelAfter(SearchConstants.RemoteSearchTimeoutMilliseconds);
        try
        {
            var results = await deezer.SearchArtistsAsync(q, linked.Token);
            return results?.ToList() ?? [];
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Deezer artist search failed or timed out for query '{Query}'", q);
            return null;
        }
    }
}
