using Microsoft.Extensions.DependencyInjection;
using MyVocaList.Domain.RepositoryInterface;
using MyVocaList.Domain.Resolution;
using MyVocaList.Domain.ServicesInterfaces;
using MyVocaList.Domain.UnitOfWork;

namespace MyVocaList.Services;

/// <inheritdoc />
public class ArtistResolutionService : IArtistResolutionService
{
    private readonly IArtistRepository _artistRepository;
    private readonly IArtistService _artistService;
    private readonly ISimilarityScorer _scorer;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ArtistResolutionService> _logger;

    public ArtistResolutionService(
        IArtistRepository artistRepository,
        IArtistService artistService,
        ISimilarityScorer scorer,
        IUnitOfWork uow,
        ILogger<ArtistResolutionService> logger)
    {
        _artistRepository = artistRepository;
        _artistService = artistService;
        _scorer = scorer;
        _uow = uow;
        _logger = logger;
    }

    /// <inheritdoc />
    // [AC] REQ-UOW-34: read-only method — ExecuteReadAsync never publishes an ambient scope, and
    // JOINS an ambient write scope when one is already open (the lookup-before-persist case).
    public Task<ArtistResolution> ResolveAsync(ArtistCandidate candidate, CancellationToken ct = default)
        => _uow.ExecuteReadAsync<ArtistResolution>(async sp =>
        {
            // REQ-UOW-28: resolved from the lambda's own scope — never the constructor field.
            var artistRepository = sp.GetRequiredService<IArtistRepository>();

            // 1. External-id match
            if (!string.IsNullOrWhiteSpace(candidate.ExternalProvider) &&
                !string.IsNullOrWhiteSpace(candidate.ExternalId))
            {
                var byExternal = await artistRepository.GetByExternalIdAsync(
                    candidate.ExternalId, candidate.ExternalProvider, ct);

                if (byExternal is not null)
                {
                    _logger.LogDebug("Artist resolved via external id {Provider}/{Id} → artist {ArtistId}",
                        candidate.ExternalProvider, candidate.ExternalId, byExternal.Id);
                    return new ArtistResolution(ResolutionKind.ExactExternalMatch, byExternal.Id, []);
                }
            }

            // 2. Exact-name match under collation
            if (!string.IsNullOrWhiteSpace(candidate.Name))
            {
                var byName = await artistRepository.GetByNameAsync(candidate.Name.Trim(), ct);
                if (byName is not null)
                {
                    _logger.LogDebug("Artist resolved via exact name '{Name}' → artist {ArtistId}",
                        candidate.Name, byName.Id);
                    return new ArtistResolution(ResolutionKind.ExactLocalMatch, byName.Id, []);
                }
            }

            // 3. Fuzzy: derive prefix token and retrieve bounded pool
            var prefixToken = DerivePrefixToken(candidate.Name);
            if (string.IsNullOrEmpty(prefixToken))
            {
                _logger.LogDebug("Artist '{Name}' has empty prefix token — returning NoMatch", candidate.Name);
                return new ArtistResolution(ResolutionKind.NoMatch, null, []);
            }

            var pool = await artistRepository.GetFuzzyCandidatePoolAsync(
                prefixToken, SimilarityConstants.PoolSize, ct) ?? [];

            var scored = pool
                .Select(a => (ArtistId: a.Id, a.Name, Score: _scorer.Score(candidate.Name, a.Name)))
                .Where(x => x.Score >= SimilarityConstants.DefaultThreshold)
                .OrderByDescending(x => x.Score)
                .Select(x => (x.ArtistId, x.Name, x.Score))
                .ToList();

            if (scored.Count > 0)
            {
                _logger.LogDebug("Artist '{Name}' has {Count} fuzzy candidate(s)", candidate.Name, scored.Count);
                return new ArtistResolution(ResolutionKind.FuzzyCandidates, null, scored);
            }

            _logger.LogDebug("Artist '{Name}' — NoMatch", candidate.Name);
            return new ArtistResolution(ResolutionKind.NoMatch, null, []);
        }, ct);

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Derives the first whitespace-delimited token of <paramref name="name"/>,
    /// capped at <see cref="SimilarityConstants.PrefixTokenMaxLen"/> characters.
    /// Returns an empty string for null/whitespace input.
    /// </summary>
    internal static string DerivePrefixToken(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        var trimmed = name.Trim();
        var spaceIdx = trimmed.IndexOf(' ');
        var token = spaceIdx < 0 ? trimmed : trimmed[..spaceIdx];

        return token.Length > SimilarityConstants.PrefixTokenMaxLen
            ? token[..SimilarityConstants.PrefixTokenMaxLen]
            : token;
    }
}
