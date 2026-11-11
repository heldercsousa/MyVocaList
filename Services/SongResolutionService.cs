using Microsoft.Extensions.DependencyInjection;
using MyVocaList.Domain.Entity;
using MyVocaList.Domain.RepositoryInterface;
using MyVocaList.Domain.Resolution;
using MyVocaList.Domain.ServicesInterfaces;
using MyVocaList.Domain.UnitOfWork;

namespace MyVocaList.Services;

/// <inheritdoc />
public class SongResolutionService : ISongResolutionService
{
    private readonly ISongRepository _songRepository;
    private readonly IArtistResolutionService _artistResolution;
    private readonly ISongService _songService;
    private readonly ISimilarityScorer _scorer;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<SongResolutionService> _logger;

    public SongResolutionService(
        ISongRepository songRepository,
        IArtistResolutionService artistResolution,
        ISongService songService,
        ISimilarityScorer scorer,
        IUnitOfWork uow,
        ILogger<SongResolutionService> logger)
    {
        _songRepository = songRepository;
        _artistResolution = artistResolution;
        _songService = songService;
        _scorer = scorer;
        _uow = uow;
        _logger = logger;
    }

    /// <inheritdoc />
    // [AC] REQ-UOW-34: read-only method — ExecuteReadAsync never publishes an ambient scope, and
    // JOINS an ambient write scope when one is already open. The nested
    // IArtistResolutionService.ResolveAsync is itself an ExecuteReadAsync, so this is the read-join
    // path exercised by real production code.
    public Task<SongResolution> ResolveAsync(SongCandidate candidate, CancellationToken ct = default)
        => _uow.ExecuteReadAsync<SongResolution>(async sp =>
        {
            // [AC] REQ-UOW-28: BOTH the repository AND the nested service are resolved from the
            // lambda's own scope — never the constructor fields.
            var songRepository = sp.GetRequiredService<ISongRepository>();
            var artistResolutionService = sp.GetRequiredService<IArtistResolutionService>();

            // INV-1: resolve artist first
            var artistResolution = await artistResolutionService.ResolveAsync(candidate.Artist, ct);
            int? resolvedArtistId = artistResolution.Kind is ResolutionKind.ExactExternalMatch or ResolutionKind.ExactLocalMatch
                ? artistResolution.ExactMatchArtistId
                : null;

            // 1. External-id match (independent of artist resolution)
            if (!string.IsNullOrWhiteSpace(candidate.ExternalProvider) &&
                !string.IsNullOrWhiteSpace(candidate.ExternalId))
            {
                var byExternal = await songRepository.GetByExternalIdAsync(
                    candidate.ExternalId, candidate.ExternalProvider, ct);

                if (byExternal is not null)
                {
                    _logger.LogDebug("Song resolved via external id {Provider}/{Id} → song {SongId}",
                        candidate.ExternalProvider, candidate.ExternalId, byExternal.Id);

                    return new SongResolution(
                        ResolutionKind.ExactExternalMatch,
                        byExternal.Id,
                        []);
                }
            }

            // Exact local and fuzzy require a known artist id (INV-1)
            if (resolvedArtistId is null)
            {
                _logger.LogDebug("Song '{Title}' — artist unresolved (Kind={Kind}); returning NoMatch",
                    candidate.Title, artistResolution.Kind);
                return new SongResolution(ResolutionKind.NoMatch, null, []);
            }

            // 2. Exact local match
            if (!string.IsNullOrWhiteSpace(candidate.Title))
            {
                // REQ-SFT-42: one collated lookup (accent/case-insensitive) that returns the stored id.
                var exactId = await songRepository.FindIdByTitleVersionForArtistAsync(
                    resolvedArtistId.Value, candidate.Title, candidate.Version, null, ct);

                if (exactId is not null)
                {
                    _logger.LogDebug("Song resolved via exact local match → song {SongId}", exactId.Value);
                    return new SongResolution(ResolutionKind.ExactLocalMatch, exactId.Value, []);
                }
            }

            // 3. Fuzzy (BUG-085, REQ-SFT-23): same-artist trigram pool over the WHOLE typed title
            if (!string.IsNullOrWhiteSpace(candidate.Title))
            {
                var pool = await songRepository.SearchTitleCandidatesForArtistAsync(
                    resolvedArtistId.Value, candidate.Title, SimilarityConstants.PoolSize, ct) ?? [];

                var scored = pool
                    .Select(h => new
                    {
                        Hit = h,
                        Score = _scorer.Score(candidate.Title, h.Title)
                    })
                    .Where(x => x.Score >= SimilarityConstants.DefaultThreshold)
                    .OrderByDescending(x => x.Score)
                    .Select(x => new SongMatch(
                        x.Hit.SongId,
                        x.Hit.Title,
                        x.Hit.Version,
                        x.Hit.ArtistName,
                        x.Score))
                    .ToList();

                if (scored.Count > 0)
                {
                    _logger.LogDebug("Song '{Title}' has {Count} fuzzy candidate(s)", candidate.Title, scored.Count);
                    return new SongResolution(ResolutionKind.FuzzyCandidates, null, scored);
                }
            }

            _logger.LogDebug("Song '{Title}' — NoMatch", candidate.Title);
            return new SongResolution(ResolutionKind.NoMatch, null, []);
        }, ct);
}
