namespace MyVocaList.Contracts.DTOs.Event;

using MyVocaList.Contracts.Enums;

public record EventDto(
    int Id,
    int VenueId,
    string Name,
    DateTime? ScheduledStartTime,
    DateTime? ScheduledEndTime,
    DateTime? ActualStartTime,
    DateTime? ActualEndTime,
    EventStatus Status,
    string Mode,
    DateTime CreatedAt,
    DateTime ModifiedAt);
