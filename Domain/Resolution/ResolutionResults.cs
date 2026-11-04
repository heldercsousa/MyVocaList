namespace MyVocaList.Domain.Resolution;

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
    IReadOnlyList<SongMatch> FuzzyCandidates);

/// <summary>
/// The full resolution outcome for an artist candidate.
/// Resolved before the song so a valid <c>ArtistId</c> is always available (INV-1).
/// </summary>
public sealed record ArtistResolution(
    ResolutionKind Kind,
    /// <summary>Set for <c>ExactExternalMatch</c> and <c>ExactLocalMatch</c> kinds.</summary>
    int? ExactMatchArtistId,
    IReadOnlyList<(int ArtistId, string Name, double Score)> FuzzyCandidates);
