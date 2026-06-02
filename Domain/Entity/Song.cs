namespace MyVocaList.Domain.Entity;

public class Song
{
    public int Id { get; set; }
    public int ArtistId { get; set; }
    public string Title { get; set; }
    public string? FeaturedArtists { get; set; }
    public string? Lyrics { get; set; }
    public string? ExternalProvider { get; set; }
    public string? ExternalId { get; set; }
    public bool HasManualEdits { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Artist OriginalArtist { get; set; }
    public ICollection<Catalog> CatalogEntries { get; set; } = [];
}
