namespace MyVocaList.Domain.Entity;

public class SongKaraokeUrl
{
    public string VideoId { get; set; }
    public int SongId { get; set; }
    public int PlayCount { get; set; }
    public int? DurationSeconds { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public DateTime AddedAt { get; set; }
    public string? Label { get; set; }

    public Song Song { get; set; }
}
