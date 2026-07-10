namespace MyVocaList.Contracts.DTOs.Suggestions;

/// <summary>
/// A single artist autocomplete row. <see cref="LocalId"/> non-null means a local DB record;
/// <see cref="IsRemote"/> true means the row came from an IMusicMetadataProvider.
/// </summary>
/// <param name="LocalId">Local Artist id; non-null => local record.</param>
/// <param name="Name">Display/artist name.</param>
/// <param name="ExternalId">External provider id (remote rows; also local rows that carry one).</param>
/// <param name="ExternalProvider">Provider name (e.g. "MusicBrainz", "Deezer").</param>
/// <param name="IsRemote">True when sourced from a music metadata provider.</param>
/// <param name="IsExactMatch">Collation-equal to the search term (computed at fetch time).</param>
public sealed record ArtistSuggestionDto(
    int? LocalId,
    string Name,
    string? ExternalId,
    string? ExternalProvider,
    bool IsRemote,
    bool IsExactMatch);
