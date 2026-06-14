using MyVocaList.Domain.Entity;
using MyVocaList.Domain.RepositoryInterface;
using MyVocaList.Domain.Resolution;
using MyVocaList.Domain.ServicesInterfaces;

namespace MyVocaList.Services;

/// <inheritdoc />
public class SongResolutionService : ISongResolutionService
{
    // Mergeable field names (N4): ArtistId is intentionally excluded.
    private static readonly IReadOnlySet<string> MergeableFields =
        new HashSet<string>(StringComparer.Ordinal) { "Title", "FeaturedArtists", "Lyrics", "Version" };

    private readonly ISongRepository _songRepository;
    private readonly IArtistResolutionService _artistResolution;
    private readonly ISongService _songService;
    private readonly ISimilarityScorer _scorer;
    private readonly ILogger<SongResolutionService> _logger;

    public SongResolutionService(
        ISongRepository songRepository,
        IArtistResolutionService artistResolution,
        ISongService songService,
        ISimilarityScorer scorer,
        ILogger<SongResolutionService> logger)
    {
        _songRepository = songRepository;
        _artistResolution = artistResolution;
        _songService = songService;
        _scorer = scorer;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<SongResolution> ResolveAsync(SongCandidate candidate, CancellationToken ct = default)
    {
        // INV-1: resolve artist first
        var artistResolution = await _artistResolution.ResolveAsync(candidate.Artist, ct);
        int? resolvedArtistId = artistResolution.Kind is ResolutionKind.ExactExternalMatch or ResolutionKind.ExactLocalMatch
            ? artistResolution.ExactMatchArtistId
            : null;

        // 1. External-id match (independent of artist resolution)
        if (!string.IsNullOrWhiteSpace(candidate.ExternalProvider) &&
            !string.IsNullOrWhiteSpace(candidate.ExternalId))
        {
            var byExternal = await _songRepository.GetByExternalIdAsync(
                candidate.ExternalId, candidate.ExternalProvider, ct);

            if (byExternal is not null)
            {
                _logger.LogDebug("Song resolved via external id {Provider}/{Id} → song {SongId}",
                    candidate.ExternalProvider, candidate.ExternalId, byExternal.Id);

                var diffs = byExternal.HasManualEdits
                    ? ComputeFieldDiffs(candidate, byExternal)
                    : [];

                return new SongResolution(
                    ResolutionKind.ExactExternalMatch,
                    byExternal.Id,
                    [],
                    diffs,
                    byExternal.HasManualEdits);
            }
        }

        // Exact local and fuzzy require a known artist id (INV-1)
        if (resolvedArtistId is null)
        {
            _logger.LogDebug("Song '{Title}' — artist unresolved (Kind={Kind}); returning NoMatch",
                candidate.Title, artistResolution.Kind);
            return new SongResolution(ResolutionKind.NoMatch, null, [], [], false);
        }

        // 2. Exact local match
        if (!string.IsNullOrWhiteSpace(candidate.Title))
        {
            var exists = await _songRepository.ExistsByTitleVersionForArtistAsync(
                resolvedArtistId.Value, candidate.Title, candidate.Version, ct);

            if (exists)
            {
                // Load the song to get its id and HasManualEdits flag
                var pool = await _songRepository.GetFuzzyCandidatePoolAsync(
                    resolvedArtistId.Value, DerivePrefixToken(candidate.Title), SimilarityConstants.PoolSize, ct) ?? [];

                var exactMatch = pool.FirstOrDefault(s =>
                    string.Equals(s.Title, candidate.Title, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(s.Version, candidate.Version, StringComparison.OrdinalIgnoreCase));

                if (exactMatch is not null)
                {
                    var diffs = exactMatch.HasManualEdits
                        ? ComputeFieldDiffs(candidate, exactMatch)
                        : [];

                    _logger.LogDebug("Song resolved via exact local match → song {SongId}", exactMatch.Id);
                    return new SongResolution(
                        ResolutionKind.ExactLocalMatch,
                        exactMatch.Id,
                        [],
                        diffs,
                        exactMatch.HasManualEdits);
                }

                // Exists check passed but we couldn't load via pool — still return ExactLocalMatch with no id
                _logger.LogWarning(
                    "ExistsByTitleVersionForArtist returned true but no match found in pool for '{Title}' v='{Version}'",
                    candidate.Title, candidate.Version);
                return new SongResolution(ResolutionKind.ExactLocalMatch, null, [], [], false);
            }
        }

        // 3. Fuzzy
        var prefixToken = DerivePrefixToken(candidate.Title);
        if (!string.IsNullOrEmpty(prefixToken))
        {
            var pool = await _songRepository.GetFuzzyCandidatePoolAsync(
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
                return new SongResolution(ResolutionKind.FuzzyCandidates, null, scored, [], false);
            }
        }

        _logger.LogDebug("Song '{Title}' — NoMatch", candidate.Title);
        return new SongResolution(ResolutionKind.NoMatch, null, [], [], false);
    }

    /// <inheritdoc />
    public async Task<(bool success, string message, Song? song)> CommitAsync(
        SongCandidate candidate,
        ResolutionChoice choice,
        int? targetSongId,
        IReadOnlyCollection<string>? acceptedFields,
        CancellationToken ct = default)
    {
        switch (choice)
        {
            case ResolutionChoice.CreateNew:
            {
                var artistId = await ResolveOrCreateArtistIdAsync(candidate, ct);
                if (artistId <= 0)
                    return (false, "Failed to resolve or create artist", null);

                var (success, message, song) = await _songService.CreateSongAsync(
                    artistId, candidate.Title,
                    candidate.FeaturedArtists, candidate.Lyrics,
                    candidate.ExternalId, candidate.ExternalProvider, ct);

                return (success, message, song);
            }

            case ResolutionChoice.CreateNewVersion:
            {
                // AC-1.2: non-empty Version required
                if (string.IsNullOrWhiteSpace(candidate.Version))
                    return (false, "A non-empty Version is required when saving as a new version", null);

                var artistId = await ResolveOrCreateArtistIdAsync(candidate, ct);
                if (artistId <= 0)
                    return (false, "Failed to resolve or create artist", null);

                var (success, message, song) = await _songService.CreateSongWithUrlsAsync(
                    artistId, candidate.Title, candidate.Version,
                    candidate.FeaturedArtists, candidate.Lyrics,
                    candidate.ExternalId, candidate.ExternalProvider,
                    [], ct);

                return (success, message, song);
            }

            case ResolutionChoice.UpdateExisting:
            {
                if (targetSongId is null)
                    return (false, "targetSongId is required for UpdateExisting", null);

                var song = await _songRepository.GetByIdAsync(targetSongId.Value, ct);
                if (song is null)
                    return (false, $"Song {targetSongId} not found", null);

                ApplyUpdate(song, candidate, acceptedFields);

                // Persist external identity if not already set
                var newExternalId = !string.IsNullOrWhiteSpace(candidate.ExternalId) ? candidate.ExternalId : null;
                var newExternalProvider = !string.IsNullOrWhiteSpace(candidate.ExternalProvider) ? candidate.ExternalProvider : null;

                var (updateSuccess, updateMessage) = await _songService.UpdateSongAsync(
                    song.Id, song.Title, song.FeaturedArtists, song.Lyrics,
                    song.HasManualEdits,
                    newExternalId, newExternalProvider, ct);

                if (!updateSuccess)
                    return (false, updateMessage, null);

                // Reload to return the updated entity
                var updated = await _songRepository.GetByIdAsync(targetSongId.Value, ct);
                return (true, updateMessage, updated);
            }

            case ResolutionChoice.AttachExternalId:
            {
                if (targetSongId is null)
                    return (false, "targetSongId is required for AttachExternalId", null);

                var song = await _songRepository.GetByIdAsync(targetSongId.Value, ct);
                if (song is null)
                    return (false, $"Song {targetSongId} not found", null);

                if (string.IsNullOrWhiteSpace(song.ExternalId))
                {
                    var (updateSuccess, updateMessage) = await _songService.UpdateSongAsync(
                        song.Id, song.Title, song.FeaturedArtists, song.Lyrics,
                        song.HasManualEdits,
                        candidate.ExternalId, candidate.ExternalProvider, ct);

                    if (!updateSuccess)
                        return (false, updateMessage, null);
                }

                var attached = await _songRepository.GetByIdAsync(targetSongId.Value, ct);
                return (true, "External identity attached", attached);
            }

            default:
                return (false, $"Unsupported resolution choice: {choice}", null);
        }
    }

    // ── Private helpers ───────────────────────────────────────────────────

    private async Task<int> ResolveOrCreateArtistIdAsync(SongCandidate candidate, CancellationToken ct)
    {
        var artistResolution = await _artistResolution.ResolveAsync(candidate.Artist, ct);

        if (artistResolution.Kind is ResolutionKind.ExactExternalMatch or ResolutionKind.ExactLocalMatch)
            return artistResolution.ExactMatchArtistId!.Value;

        // Artist must be created (NoMatch case during commit)
        var (success, _, artistId) = await _artistResolution.CommitAsync(
            candidate.Artist, ResolutionChoice.CreateNew, null, ct);

        return success ? artistId : 0;
    }

    /// <summary>
    /// Applies update fields to the in-memory <paramref name="song"/> entity.
    /// If the song has manual edits, only fields listed in <paramref name="acceptedFields"/> are applied (AC-4.2).
    /// If no manual edits, all non-empty candidate fields overwrite existing ones (AC-4.1).
    /// Only mergeable fields (Title, FeaturedArtists, Lyrics, Version) are ever mutated here.
    /// </summary>
    private static void ApplyUpdate(Song song, SongCandidate candidate, IReadOnlyCollection<string>? acceptedFields)
    {
        if (song.HasManualEdits && acceptedFields is not null)
        {
            // AC-4.2: apply only accepted fields, restricted to mergeable set
            if (acceptedFields.Contains("Title") && MergeableFields.Contains("Title") &&
                !string.IsNullOrWhiteSpace(candidate.Title))
                song.Title = candidate.Title.Trim();

            if (acceptedFields.Contains("FeaturedArtists") && MergeableFields.Contains("FeaturedArtists"))
                song.FeaturedArtists = candidate.FeaturedArtists?.Trim();

            if (acceptedFields.Contains("Lyrics") && MergeableFields.Contains("Lyrics"))
                song.Lyrics = candidate.Lyrics;

            if (acceptedFields.Contains("Version") && MergeableFields.Contains("Version") &&
                !string.IsNullOrWhiteSpace(candidate.Version))
                song.Version = candidate.Version.Trim();
        }
        else if (!song.HasManualEdits)
        {
            // AC-4.1: overwrite non-empty API fields
            if (!string.IsNullOrWhiteSpace(candidate.Title))
                song.Title = candidate.Title.Trim();

            // FeaturedArtists and Lyrics: overwrite with candidate value (may be null/empty = clear)
            song.FeaturedArtists = candidate.FeaturedArtists?.Trim();
            song.Lyrics = candidate.Lyrics;

            if (!string.IsNullOrWhiteSpace(candidate.Version))
                song.Version = candidate.Version.Trim();
        }
        // else: song.HasManualEdits && acceptedFields == null → accept all non-empty (same as no-manual-edits)
    }

    /// <summary>
    /// Computes field diffs between the API candidate and the existing song.
    /// Restricted to the mergeable field set (N4): Title, FeaturedArtists, Lyrics, Version.
    /// </summary>
    private static IReadOnlyList<FieldDiff> ComputeFieldDiffs(SongCandidate candidate, Song target)
    {
        var diffs = new List<FieldDiff>();

        if (!string.Equals(candidate.Title?.Trim(), target.Title?.Trim(), StringComparison.OrdinalIgnoreCase))
            diffs.Add(new FieldDiff("Title", candidate.Title, target.Title));

        if (!string.Equals(candidate.FeaturedArtists?.Trim(), target.FeaturedArtists?.Trim(), StringComparison.OrdinalIgnoreCase))
            diffs.Add(new FieldDiff("FeaturedArtists", candidate.FeaturedArtists, target.FeaturedArtists));

        if (!string.Equals(candidate.Lyrics, target.Lyrics, StringComparison.Ordinal))
            diffs.Add(new FieldDiff("Lyrics", candidate.Lyrics, target.Lyrics));

        if (!string.Equals(candidate.Version?.Trim(), target.Version?.Trim(), StringComparison.OrdinalIgnoreCase))
            diffs.Add(new FieldDiff("Version", candidate.Version, target.Version));

        return diffs;
    }

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
