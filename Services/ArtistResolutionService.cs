using MyVocaList.Domain.Entity;
using MyVocaList.Domain.RepositoryInterface;
using MyVocaList.Domain.Resolution;
using MyVocaList.Domain.ServicesInterfaces;
using MyVocaList.Domain.Resolution;

namespace MyVocaList.Services;

/// <inheritdoc />
public class ArtistResolutionService : IArtistResolutionService
{
    private readonly IArtistRepository _artistRepository;
    private readonly IArtistService _artistService;
    private readonly ISimilarityScorer _scorer;
    private readonly ILogger<ArtistResolutionService> _logger;

    public ArtistResolutionService(
        IArtistRepository artistRepository,
        IArtistService artistService,
        ISimilarityScorer scorer,
        ILogger<ArtistResolutionService> logger)
    {
        _artistRepository = artistRepository;
        _artistService = artistService;
        _scorer = scorer;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ArtistResolution> ResolveAsync(ArtistCandidate candidate, CancellationToken ct = default)
    {
        // 1. External-id match
        if (!string.IsNullOrWhiteSpace(candidate.ExternalProvider) &&
            !string.IsNullOrWhiteSpace(candidate.ExternalId))
        {
            var byExternal = await _artistRepository.GetByExternalIdAsync(
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
            var byName = await _artistRepository.GetByNameAsync(candidate.Name.Trim(), ct);
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

        var pool = await _artistRepository.GetFuzzyCandidatePoolAsync(
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
    }

    /// <inheritdoc />
    public async Task<(bool success, string message, int artistId)> CommitAsync(
        ArtistCandidate candidate,
        ResolutionChoice choice,
        int? targetArtistId,
        CancellationToken ct = default)
    {
        switch (choice)
        {
            case ResolutionChoice.UpdateExisting:
            case ResolutionChoice.AttachExternalId:
            {
                if (targetArtistId is null)
                    return (false, "targetArtistId is required for UpdateExisting/AttachExternalId", 0);

                var artist = await _artistRepository.GetByIdAsync(targetArtistId.Value, ct);
                if (artist is null)
                    return (false, $"Artist {targetArtistId} not found", 0);

                // Attach external identity if absent
                if (!string.IsNullOrWhiteSpace(candidate.ExternalProvider) &&
                    !string.IsNullOrWhiteSpace(candidate.ExternalId) &&
                    string.IsNullOrWhiteSpace(artist.ExternalId))
                {
                    artist.ExternalProvider = candidate.ExternalProvider;
                    artist.ExternalId = candidate.ExternalId;
                    artist.UpdatedAt = DateTime.UtcNow;
                    await _artistRepository.UpdateAsync(artist, ct);
                    await _artistRepository.SaveChangesAsync(ct);
                }

                return (true, "Artist resolved", targetArtistId.Value);
            }

            case ResolutionChoice.CreateNew:
            {
                var (success, message, created) = await _artistService.CreateArtistAsync(candidate.Name, ct: ct);
                if (!success || created is null)
                    return (false, message, 0);

                // Set external identity on the newly created artist if provided
                if (!string.IsNullOrWhiteSpace(candidate.ExternalProvider) &&
                    !string.IsNullOrWhiteSpace(candidate.ExternalId))
                {
                    created.ExternalProvider = candidate.ExternalProvider;
                    created.ExternalId = candidate.ExternalId;
                    created.UpdatedAt = DateTime.UtcNow;
                    await _artistRepository.UpdateAsync(created, ct);
                    await _artistRepository.SaveChangesAsync(ct);
                }

                return (true, message, created.Id);
            }

            default:
                return (false, $"Unsupported resolution choice: {choice}", 0);
        }
    }

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
