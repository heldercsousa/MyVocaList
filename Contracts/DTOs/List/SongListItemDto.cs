namespace MyVocaList.Contracts.DTOs.List;

/// <summary>
/// One row of the Songs list (plain and catalog mode). <see cref="ImageUrl"/> is the row image:
/// the song's image, else its original artist's image, else null (placeholder) (REQ-ART-19).
/// </summary>
public record SongListItemDto(
    int Id,
    string Title,
    int OriginalArtistId,
    string? OriginalArtistName,
    string? ExternalProvider,
    string? ImageUrl = null)
{
    /// <summary>
    /// The original artist name, trimmed (REQ-RMV-09: the Songs-list supporting text is the artist only).
    /// </summary>
    public string SupportingText => OriginalArtistName?.Trim() ?? string.Empty;
}
