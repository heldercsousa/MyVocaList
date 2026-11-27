namespace MyVocaList.Contracts.DTOs.List;

public record SongKaraokeUrlDto(
    string VideoId,
    int SongId,
    int PlayCount,
    int? DurationSeconds,
    DateTime? LastUsedAt,
    DateTime AddedAt,
    string? Label,
    bool IsSuggested,
    string? Title = null,
    string? ThumbnailUrl = null)
{
    /// <summary>REQ-MSRC-14: the video title, or the video id when no title is known (offline, failure, not filled yet).</summary>
    public string DisplayTitle => string.IsNullOrWhiteSpace(Title) ? VideoId : Title;

    /// <summary>REQ-MSRC-14: true when a thumbnail address is known.</summary>
    public bool HasThumbnail => !string.IsNullOrWhiteSpace(ThumbnailUrl);
}
