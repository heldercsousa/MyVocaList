using MyVocaList.Domain.Entities;

namespace MyVocaList.Domain.ServicesInterfaces;

/// <summary>
/// Service for queue operations: enqueue, participation tracking, performance recording, reordering.
/// </summary>
public interface IQueueServiceNew
{
    /// <summary>Adds a singer to the queue for an event.</summary>
    /// <returns>(true, message) on success; (false, reason) on failure.</returns>
    Task<(bool success, string message)> EnqueueSingerAsync(
        int eventId, int personId, int? songId, CancellationToken ct);

    /// <summary>Transitions a queue entry from PENDING to PERFORMING (starts performance).</summary>
    Task<(bool success, string message)> RegisterParticipationAsync(
        int queueEntryId, CancellationToken ct);

    /// <summary>Transitions a queue entry from PERFORMING to COMPLETED (stops performance, records duration).</summary>
    Task<(bool success, string message)> StopPerformanceAsync(
        int queueEntryId, CancellationToken ct);

    /// <summary>Transitions a queue entry from PENDING to ABSENT.</summary>
    Task<(bool success, string message)> MarkAbsentAsync(
        int queueEntryId, CancellationToken ct);

    /// <summary>Updates the song selection for a queue entry.</summary>
    Task<(bool success, string message)> UpdateSongSelectionAsync(
        int queueEntryId, int? songId, CancellationToken ct);

    /// <summary>Reorders the queue for an event with new positions.</summary>
    /// <param name="eventId">The event ID.</param>
    /// <param name="newOrder">List of (entryId, newPosition) tuples.</param>
    Task<(bool success, string message)> ReorderQueueAsync(
        int eventId, IEnumerable<(int entryId, int position)> newOrder, CancellationToken ct);

    /// <summary>Gets the currently performing singer, or the next pending singer, or null.</summary>
    Task<QueueEntry?> GetCurrentSingerAsync(int eventId, CancellationToken ct);

    /// <summary>Calculates estimated completion time based on average performance duration.</summary>
    /// <remarks>Returns null if fewer than 5 performances recorded; returns null if no pending entries.</remarks>
    Task<TimeSpan?> CalculateCompletionEstimateAsync(int eventId, CancellationToken ct);
}
