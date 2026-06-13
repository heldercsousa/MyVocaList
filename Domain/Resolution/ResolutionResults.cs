namespace MyVocaList.Domain.Resolution;

/// <summary>
/// Describes a single field difference between the API-sourced candidate and the existing local record.
/// Populated only when the target has <c>HasManualEdits = true</c>.
/// Mergeable fields: Title, FeaturedArtists, Lyrics, Version — ArtistId is never mergeable here.
/// </summary>
public sealed record FieldDiff(
    string Field,
    string? ApiValue,
    string? CurrentValue);

/// <summary>A fuzzy candidate match for a song, scored by <c>ISimilarityScorer</c>.</summary>
public sealed record SongMatch(
    int SongId,
    string Title,
    string Version,
    string ArtistName,
    double Score);

/// <summary>
/// The full resolution outcome for a song candidate.
/// The UI acts on this object to present the appropriate confirmation sheet.
/// </summary>
public sealed record SongResolution(
    ResolutionKind Kind,
    /// <summary>Set for <c>ExactExternalMatch</c> and <c>ExactLocalMatch</c> kinds.</summary>
    int? ExactMatchSongId,
    /// <summary>Populated when <c>Kind == FuzzyCandidates</c>.</summary>
    IReadOnlyList<SongMatch> FuzzyCandidates,
    /// <summary>Populated when the target record has <c>HasManualEdits = true</c>.</summary>
    IReadOnlyList<FieldDiff> FieldDiffs,
    bool TargetHasManualEdits);

/// <summary>
/// The full resolution outcome for an artist candidate.
/// Resolved before the song so a valid <c>ArtistId</c> is always available (INV-1).
/// </summary>
public sealed record ArtistResolution(
    ResolutionKind Kind,
    /// <summary>Set for <c>ExactExternalMatch</c> and <c>ExactLocalMatch</c> kinds.</summary>
    int? ExactMatchArtistId,
    IReadOnlyList<(int ArtistId, string Name, double Score)> FuzzyCandidates);
