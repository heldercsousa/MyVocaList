using MyVocaList.Contracts.Enums;
using MyVocaList.Domain.Entities;

namespace MyVocaList.Domain.ServicesInterfaces;

/// <summary>
/// Service for event lifecycle management (create, state transitions, queries).
/// </summary>
public interface IEventService
{
    /// <summary>Creates a new event with the given details.</summary>
    /// <param name="venueId">The venue ID for this event.</param>
    /// <param name="name">The event name (1–100 characters).</param>
    /// <param name="scheduledStart">The scheduled start time.</param>
    /// <param name="scheduledEnd">The scheduled end time (must be after scheduledStart).</param>
    /// <param name="mode">The event mode (e.g., "VideoKaraoke", "Bandoke").</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>(true, message, event) on success; (false, reason, null) on failure.</returns>
    Task<(bool success, string message, Event? @event)> CreateEventAsync(
        int venueId, string name, DateTime scheduledStart, DateTime scheduledEnd,
        string mode, CancellationToken ct);

    /// <summary>Transitions event from CREATED to STARTED, setting ActualStartTime.</summary>
    Task<(bool success, string message)> StartEventAsync(int eventId, CancellationToken ct);

    /// <summary>Transitions event from STARTED to PAUSED.</summary>
    Task<(bool success, string message)> PauseEventAsync(int eventId, CancellationToken ct);

    /// <summary>Transitions event from PAUSED back to STARTED.</summary>
    Task<(bool success, string message)> ResumeEventAsync(int eventId, CancellationToken ct);

    /// <summary>Transitions event from STARTED or PAUSED to FINISHED, setting ActualEndTime.</summary>
    Task<(bool success, string message)> FinishEventAsync(int eventId, CancellationToken ct);

    /// <summary>Retrieves the first event with status STARTED or PAUSED (active event).</summary>
    Task<Event?> GetActiveEventAsync(CancellationToken ct);

    /// <summary>Validates event name for a given venue and scheduled date.</summary>
    /// <returns>(isValid, message) tuple.</returns>
    Task<(bool isValid, string message)> ValidateEventNameAsync(
        int venueId, string name, DateTime scheduledDate, CancellationToken ct);
}
