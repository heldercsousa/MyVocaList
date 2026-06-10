namespace MyVocaList.Contracts.DTOs.Queue;

using MyVocaList.Contracts.Enums;

public record QueueEntryDto(
    int Id,
    int EventId,
    int PersonId,
    int? SongId,
    int Position,
    QueueEntryStatus Status,
    DateTime? PerformanceStartTime,
    DateTime? PerformanceEndTime,
    int? PerformanceDurationMinutes,
    DateTime CreatedAt,
    DateTime ModifiedAt,
    PersonDto? Person,
    SongDto? Song);

public record PersonDto(int Id, string Name);

public record SongDto(int Id, string Title);
