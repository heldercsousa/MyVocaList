namespace MyVocaList.Domain.Resolution;

/// <summary>
/// Carries everything needed to resolve an artist, regardless of whether the candidate
/// originated from manual entry or a third-party API result.
/// </summary>
public sealed record ArtistCandidate(
    string Name,
    string? ExternalProvider,
    string? ExternalId);

/// <summary>
/// Carries everything needed to resolve a song, regardless of whether the candidate
/// originated from manual entry or a third-party API result.
/// The artist must be resolved before the song (INV-1).
/// </summary>
public sealed record SongCandidate(
    string Title,
    /// <summary>Empty string denotes the canonical version; non-empty = deliberate variant (Live/Acoustic/Remix).</summary>
    string Version,
    string? FeaturedArtists,
    string? Lyrics,
    ArtistCandidate Artist,
    string? ExternalProvider,
    string? ExternalId);
