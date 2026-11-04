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

            // 3. Fuzzy
            var prefixToken = DerivePrefixToken(candidate.Title);
            if (!string.IsNullOrEmpty(prefixToken))
            {
                var pool = await songRepository.GetFuzzyCandidatePoolAsync(
                    resolvedArtistId.Value, prefixToken, SimilarityConstants.PoolSize, ct) ?? [];

                var scored = pool
                    .Select(s => new
                    {
                        Song = s,
                        Score = _scorer.Score(candidate.Title, s.Title)
                    })
                    .Where(x => x.Score >= SimilarityConstants.DefaultThreshold)
                    .OrderByDescending(x => x.Score)
                    .Select(x => new SongMatch(
                        x.Song.Id,
                        x.Song.Title,
                        x.Song.Version,
                        x.Song.OriginalArtist?.Name ?? string.Empty,
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

    /// <summary>
    /// Derives the first whitespace-delimited token of <paramref name="title"/>,
    /// capped at <see cref="SimilarityConstants.PrefixTokenMaxLen"/> characters.
    /// Returns empty string for null/whitespace input (design §4 N1).
    /// </summary>
    internal static string DerivePrefixToken(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return string.Empty;

        var trimmed = title.Trim();
        var spaceIdx = trimmed.IndexOf(' ');
        var token = spaceIdx < 0 ? trimmed : trimmed[..spaceIdx];

        return token.Length > SimilarityConstants.PrefixTokenMaxLen
            ? token[..SimilarityConstants.PrefixTokenMaxLen]
            : token;
    }
}
