namespace MyVocaList.Contracts.DTOs.List;

public record SongKaraokeUrlDto(
    string VideoId,
    int SongId,
    int PlayCount,
    int? DurationSeconds,
    DateTime? LastUsedAt,
    DateTime AddedAt,
    string? Label,
    bool IsSuggested);
