using MyVocaList.Contracts.Enums;

namespace MyVocaList.Domain.Entity;

public class Song
{
    public int Id { get; set; }
    public int ArtistId { get; set; }
    public string Title { get; set; }
    public string? Lyrics { get; set; }
    /// <summary>Where the stored lyrics came from; null = not recorded (REQ-SLD-26..28).</summary>
    public LyricsOrigin? LyricsOrigin { get; set; }
    public string? ExternalProvider { get; set; }
    public string? ExternalId { get; set; }
    /// <summary>Absolute https cover URL, or null (REQ-ART-05). Bytes are never stored.</summary>
    public string? ImageUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Artist OriginalArtist { get; set; }
    public ICollection<Catalog> CatalogEntries { get; set; } = [];
}
