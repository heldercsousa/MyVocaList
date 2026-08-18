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
    // Mergeable field names (N4): ArtistId is intentionally excluded.
    private static readonly IReadOnlySet<string> MergeableFields =
        new HashSet<string>(StringComparer.Ordinal) { "Title", "FeaturedArtists", "Lyrics", "Version" };

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
                var exists = await songRepository.ExistsByTitleVersionForArtistAsync(
                    resolvedArtistId.Value, candidate.Title, candidate.Version, ct);

                if (exists)
                {
                    // Load the song to get its id and HasManualEdits flag
                    var pool = await songRepository.GetFuzzyCandidatePoolAsync(
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
                    return new SongResolution(ResolutionKind.FuzzyCandidates, null, scored, [], false);
                }
            }

            _logger.LogDebug("Song '{Title}' — NoMatch", candidate.Title);
            return new SongResolution(ResolutionKind.NoMatch, null, [], [], false);
        }, ct);

    /// <inheritdoc />
    // [AC] REQ-UOW-22: the deepest chain in the pilot — SongResolutionService.CommitAsync →
    // ArtistResolutionService.CommitAsync → ArtistService.CreateArtistAsync. The AsyncLocal ambient
    // scope published here is JOINED by both nested levels, so all three share ONE DI scope, ONE
    // AppDbContext and ONE explicit transaction: a fault at the song write rolls the already-flushed
    // artist row back with it.
    // [AC] REQ-UOW-24: save-skip is decided exactly ONCE, by this outermost ExecuteAsync. A nested
    // failure tuple (e.g. SongService.CreateSongAsync's title validation) propagates out through this
    // method's own returned tuple, whose leading bool is false — so nothing is committed.
    public Task<(bool success, string message, Song? song)> CommitAsync(
        SongCandidate candidate,
        ResolutionChoice choice,
        int? targetSongId,
        IReadOnlyCollection<string>? acceptedFields,
        CancellationToken ct = default)
        => _uow.ExecuteAsync<(bool success, string message, Song? song)>(async sp =>
        {
            // [AC] REQ-UOW-28: the repository AND both nested services are resolved from the
            // lambda's own scope — never the constructor fields. A surviving _artistResolution
            // reference here would silently defeat the join across levels 2–3 while still
            // compiling and passing.
            var songRepository = sp.GetRequiredService<ISongRepository>();
            var songService = sp.GetRequiredService<ISongService>();
            var artistResolution = sp.GetRequiredService<IArtistResolutionService>();

            switch (choice)
            {
                case ResolutionChoice.CreateNew:
                    {
                        var artistId = await ResolveOrCreateArtistIdAsync(artistResolution, candidate, ct);
                        if (artistId <= 0)
                            return (false, "Failed to resolve or create artist", null);

                        var (success, message, song) = await songService.CreateSongAsync(
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

                        var artistId = await ResolveOrCreateArtistIdAsync(artistResolution, candidate, ct);
                        if (artistId <= 0)
                            return (false, "Failed to resolve or create artist", null);

                        var (success, message, song) = await songService.CreateSongWithUrlsAsync(
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

                        var song = await songRepository.GetByIdAsync(targetSongId.Value, ct);
                        if (song is null)
                            return (false, $"Song {targetSongId} not found", null);

                        ApplyUpdate(song, candidate, acceptedFields);

                        // Persist external identity if not already set
                        var newExternalId = !string.IsNullOrWhiteSpace(candidate.ExternalId) ? candidate.ExternalId : null;
                        var newExternalProvider = !string.IsNullOrWhiteSpace(candidate.ExternalProvider) ? candidate.ExternalProvider : null;

                        var (updateSuccess, updateMessage) = await songService.UpdateSongAsync(
                            song.Id, song.Title, song.FeaturedArtists, song.Lyrics,
                            song.HasManualEdits,
                            newExternalId, newExternalProvider, song.Version, ct: ct);

                        if (!updateSuccess)
                            return (false, updateMessage, null);

                        // Reload to return the updated entity
                        var updated = await songRepository.GetByIdAsync(targetSongId.Value, ct);
                        return (true, updateMessage, updated);
                    }

                case ResolutionChoice.AttachExternalId:
                    {
                        if (targetSongId is null)
                            return (false, "targetSongId is required for AttachExternalId", null);

                        var song = await songRepository.GetByIdAsync(targetSongId.Value, ct);
                        if (song is null)
                            return (false, $"Song {targetSongId} not found", null);

                        if (string.IsNullOrWhiteSpace(song.ExternalId))
                        {
                            var (updateSuccess, updateMessage) = await songService.UpdateSongAsync(
                                song.Id, song.Title, song.FeaturedArtists, song.Lyrics,
                                song.HasManualEdits,
                                candidate.ExternalId, candidate.ExternalProvider, song.Version, ct: ct);

                            if (!updateSuccess)
                                return (false, updateMessage, null);
                        }

                        var attached = await songRepository.GetByIdAsync(targetSongId.Value, ct);
                        return (true, "External identity attached", attached);
                    }

                default:
                    return (false, $"Unsupported resolution choice: {choice}", null);
            }
        }, ct);

    // ── Private helpers ───────────────────────────────────────────────────

    /// <summary>
    /// REQ-UOW-28: takes the <see cref="IArtistResolutionService"/> as a parameter rather than
    /// reading the constructor field. This helper runs inside <see cref="CommitAsync"/>'s
    /// <c>ExecuteAsync</c> lambda, so it must use the instance resolved from that lambda's own
    /// scope — a <c>_artistResolution</c> read here would be the same defect as one written
    /// inline in the lambda body, merely one call frame away.
    /// </summary>
    private static async Task<int> ResolveOrCreateArtistIdAsync(
        IArtistResolutionService artistResolutionService, SongCandidate candidate, CancellationToken ct)
    {
        var artistResolution = await artistResolutionService.ResolveAsync(candidate.Artist, ct);

        if (artistResolution.Kind is ResolutionKind.ExactExternalMatch or ResolutionKind.ExactLocalMatch)
            return artistResolution.ExactMatchArtistId!.Value;

        // Artist must be created (NoMatch case during commit). ArtistResolutionService.CommitAsync
        // JOINS this method's ambient write scope (level 2 of the 3-level chain) and flushes so the
        // generated artist id materialises — still inside this unit of work's open transaction.
        var (success, _, artistId) = await artistResolutionService.CommitAsync(
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
