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
/// <param name="Version">Version label (local: stored; remote: normalised Deezer version); empty when none (REQ-SFT-12).</param>
/// <param name="LocalArtistName">Stored spelling of the local artist; null when the artist is not local (REQ-SFT-13).</param>
/// <param name="ImageUrl">Song image URL (album cover); null when none (REQ-ART-06).</param>
/// <param name="ArtistImageUrl">Artist image URL; null when none (REQ-ART-06).</param>
/// <param name="AlbumTitle">Album title; set on remote rows only, null when none (REQ-ART-07).</param>
/// <param name="DurationSeconds">Track duration in seconds; set on remote rows only, null when none (REQ-LYR-01).</param>
public sealed record SongSuggestionDto(
    int? LocalId,
    string Title,
    string ArtistName,
    int? LocalArtistId,
    string? ExternalId,
    string? ExternalProvider,
    bool IsRemote,
    string Version = "",
    string? LocalArtistName = null,
    string? ImageUrl = null,
    string? ArtistImageUrl = null,
    string? AlbumTitle = null,
    int? DurationSeconds = null)
{
    /// <summary>The image a row shows: the song image, else the artist image, else null (REQ-ART-06).</summary>
    public string? RowImageUrl => ImageUrl ?? ArtistImageUrl;
}
