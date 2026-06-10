namespace MyVocaList.Contracts.DTOs.Queue;

using MyVocaList.Contracts.Enums;

public record QueueEntryListItemDto(
    int Id,
    int Position,
    string? PersonName,
    string? SongTitle,
    QueueEntryStatus Status,
    int? PerformanceDurationMinutes);
