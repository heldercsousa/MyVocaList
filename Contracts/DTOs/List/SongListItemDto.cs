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
    string? FeaturedArtists,
    string? ExternalProvider,
    string? Version = null,
    string? ImageUrl = null)
{
    /// <summary>
    /// Non-empty parts of artist, version and "feat. &lt;featured artists&gt;", joined by " · " (REQ-ART-20).
    /// </summary>
    public string SupportingText
    {
        get
        {
            var featured = string.IsNullOrWhiteSpace(FeaturedArtists) ? null : "feat. " + FeaturedArtists.Trim();
            return string.Join(" · ", new[] { OriginalArtistName?.Trim(), Version?.Trim(), featured }
                .Where(part => !string.IsNullOrEmpty(part)));
        }
    }
}
