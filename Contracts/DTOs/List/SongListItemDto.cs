using MyVocaList.Contracts.DTOs.Suggestions;

namespace MyVocaList.Contracts.DTOs.List;

/// <summary>
/// One row of the Songs list (plain and catalog mode). <see cref="ImageUrl"/> is the row image:
/// the song's image, else the album cover of its stored release, else its original artist's image, else
/// null (placeholder) (REQ-ART-19, REQ-MSRC-16).
/// </summary>
public record SongListItemDto(
    int Id,
    string Title,
    int OriginalArtistId,
    string? OriginalArtistName,
    string? ExternalProvider,
    string? SongImageUrl = null,
    string? ArtistImageUrl = null,
    string? ReleaseMbid = null)
{
    /// <summary>The row image reference, resolved by <see cref="SongImageResolver"/>; null shows the placeholder.</summary>
    public string? ImageUrl => SongImageResolver.Resolve(SongImageUrl, ReleaseMbid, ArtistImageUrl);

    /// <summary>
    /// The original artist name, trimmed (REQ-RMV-09: the Songs-list supporting text is the artist only).
    /// </summary>
    public string SupportingText => OriginalArtistName?.Trim() ?? string.Empty;
}
