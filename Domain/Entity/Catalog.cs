namespace MyVocaList.Domain.Entity;

public class Catalog
{
    public int ArtistId { get; set; }
    public int SongId { get; set; }

    public Artist Artist { get; set; }
    public Song Song { get; set; }
}
