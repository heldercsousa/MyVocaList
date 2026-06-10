namespace MyVocaList.Contracts.DTOs.Event;

using MyVocaList.Contracts.Enums;

public record EventListItemDto(
    int Id,
    string Name,
    string? VenueName,
    DateTime? ScheduledStartTime,
    EventStatus Status,
    string Mode);
