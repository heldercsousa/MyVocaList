namespace MyVocaList.Contracts.DTOs.Suggestions;

/// <summary>
/// A single song-title autocomplete row. <see cref="LocalId"/> non-null means a registered song;
/// <see cref="LocalArtistId"/> non-null means the suggestion's artist exists locally.
/// </summary>
/// <param name="LocalId">Local Song id; non-null => local record.</param>
/// <param name="Title">Song title.</param>
/// <param name="ArtistName">Artist name shown as supporting text.</param>
/// <param name="LocalArtistId">Non-null when the suggestion's artist exists locally.</param>
/// <param name="ExternalId">External provider song id (remote rows).</param>
/// <param name="ExternalProvider">Provider name.</param>
/// <param name="IsRemote">True when sourced from a music metadata provider.</param>
public sealed record SongSuggestionDto(
    int? LocalId,
    string Title,
    string ArtistName,
    int? LocalArtistId,
    string? ExternalId,
    string? ExternalProvider,
    bool IsRemote);
