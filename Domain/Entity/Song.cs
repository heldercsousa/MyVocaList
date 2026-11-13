namespace MyVocaList.Domain.Entity;

public class Song
{
    public int Id { get; set; }
    public int ArtistId { get; set; }
    public string Title { get; set; }
    /// <summary>Variant label (Live/Acoustic/Remix); empty string = canonical version.</summary>
    public string Version { get; set; } = string.Empty;
    public string? FeaturedArtists { get; set; }
    public string? Lyrics { get; set; }
    public string? ExternalProvider { get; set; }
    public string? ExternalId { get; set; }
    /// <summary>Absolute https cover URL, or null (REQ-ART-05). Bytes are never stored.</summary>
    public string? ImageUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Artist OriginalArtist { get; set; }
    public ICollection<Catalog> CatalogEntries { get; set; } = [];
}
