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

    /// <summary>REQ-MSRC-14: the YouTube video title (oEmbed); null until fetched.</summary>
    public string? Title { get; set; }

    /// <summary>REQ-MSRC-14: the YouTube thumbnail address (oEmbed); null until fetched.</summary>
    public string? ThumbnailUrl { get; set; }

    public Song Song { get; set; }
}
