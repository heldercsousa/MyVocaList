namespace MyVocaList.Contracts.DTOs.Suggestions;

/// <summary>
/// A single artist autocomplete row. <see cref="LocalId"/> non-null means a local DB record;
/// <see cref="IsRemote"/> true means the row came from an IMusicMetadataProvider.
/// </summary>
/// <param name="LocalId">Local Artist id; non-null => local record.</param>
/// <param name="Name">Display/artist name.</param>
/// <param name="ExternalId">External provider id (remote rows; also local rows that carry one).</param>
/// <param name="ExternalProvider">Provider name (e.g. "Deezer").</param>
/// <param name="IsRemote">True when sourced from a music metadata provider.</param>
/// <param name="ImageUrl">Artist image URL for remote rows; null for local rows (REQ-ART-24).</param>
public sealed record ArtistSuggestionDto(
    int? LocalId,
    string Name,
    string? ExternalId,
    string? ExternalProvider,
    bool IsRemote,
    string? ImageUrl = null);
