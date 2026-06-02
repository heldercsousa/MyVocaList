namespace MyVocaList.Domain.Entity;

public class Artist
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string? ExternalProvider { get; set; }
    public string? ExternalId { get; set; }
    public bool HasManualEdits { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<Song> OriginalSongs { get; set; } = [];
    public ICollection<Catalog> CatalogEntries { get; set; } = [];
}
