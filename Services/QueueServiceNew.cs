using MyVocaList.Contracts.Enums;
using MyVocaList.Domain.Entities;
using MyVocaList.Domain.Interfaces;
using MyVocaList.Domain.RepositoryInterface;
using MyVocaList.Domain.ServicesInterfaces;

namespace MyVocaList.Services;

/// <inheritdoc />
public sealed class QueueServiceNew : IQueueServiceNew
{
    private readonly IQueueRepository _queueRepository;
    private readonly IPersonRepository _personRepository;
    private readonly ISongRepository _songRepository;
    private readonly Domain.Interfaces.IEventRepository _eventRepository;
    private readonly ILogger<QueueServiceNew> _logger;

    public QueueServiceNew(
        IQueueRepository queueRepository,
        IPersonRepository personRepository,
        ISongRepository songRepository,
        Domain.Interfaces.IEventRepository eventRepository,
        ILogger<QueueServiceNew> logger)
    {
        _queueRepository = queueRepository;
        _personRepository = personRepository;
        _songRepository = songRepository;
        _eventRepository = eventRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<(bool success, string message)> EnqueueSingerAsync(
        int eventId, int personId, int? songId, CancellationToken ct)
    {
        // Validate event exists
        var @event = await _eventRepository.GetByIdAsync(eventId, ct);
        if (@event == null)
        {
            _logger.LogDebug("Enqueue failed: event {EventId} not found", eventId);
            return (false, "Event not found");
        }

        // Validate person exists
        var person = await _personRepository.GetByIdAsync(personId);
        if (person == null)
        {
            _logger.LogDebug("Enqueue failed: person {PersonId} not found", personId);
            return (false, "Singer not found");
        }

        // Validate song exists if provided
        if (songId.HasValue)
        {
            var song = await _songRepository.GetByIdAsync(songId.Value, ct);
            if (song == null)
            {
                _logger.LogDebug("Enqueue failed: song {SongId} not found", songId);
                return (false, "Song not found");
            }
        }

        // Check person not already enqueued in this event
        var existingEntries = await _queueRepository.GetByEventIdAsync(eventId, ct);
        if (existingEntries.Any(e => e.PersonId == personId && e.Status != QueueEntryStatus.Absent && e.Status != QueueEntryStatus.Cancelled))
        {
            _logger.LogDebug("Enqueue failed: person {PersonId} already enqueued in event {EventId}",
                personId, eventId);
            return (false, "Singer is already in the queue");
        }

        // Create queue entry
        var entry = new QueueEntry
        {
            EventId = eventId,
            PersonId = personId,
            SongId = songId,
            Status = QueueEntryStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow,
            // Required navigation properties — EF will populate on load
            Event = null!,
            Person = null!
        };

        await _queueRepository.AddAsync(entry, ct);

        _logger.LogInformation("Singer {PersonId} enqueued to event {EventId}", personId, eventId);
        return (true, $"{person.FullName} added to queue");
    }

    /// <inheritdoc />
    public async Task<(bool success, string message)> RegisterParticipationAsync(
        int queueEntryId, CancellationToken ct)
    {
        var entry = await _queueRepository.GetByIdAsync(queueEntryId, ct);
        if (entry == null)
        {
            _logger.LogDebug("Participation registration failed: entry {EntryId} not found", queueEntryId);
            return (false, "Queue entry not found");
        }

        if (entry.Status != QueueEntryStatus.Pending)
        {
            _logger.LogDebug("Participation registration failed: entry {EntryId} status {Status} is not Pending",
                queueEntryId, entry.Status);
            return (false, $"Cannot register participation: entry status is {entry.Status}");
        }

        entry.Status = QueueEntryStatus.Performing;
        entry.PerformanceStartTime = DateTime.UtcNow;
        entry.ModifiedAt = DateTime.UtcNow;

        await _queueRepository.UpdateAsync(entry, ct);

        _logger.LogInformation("Participation registered for queue entry {EntryId}", queueEntryId);
        return (true, "Participation registered");
    }

    /// <inheritdoc />
    public async Task<(bool success, string message)> StopPerformanceAsync(
        int queueEntryId, CancellationToken ct)
    {
        var entry = await _queueRepository.GetByIdAsync(queueEntryId, ct);
        if (entry == null)
        {
            _logger.LogDebug("Stop performance failed: entry {EntryId} not found", queueEntryId);
            return (false, "Queue entry not found");
        }

        if (entry.Status != QueueEntryStatus.Performing)
        {
            _logger.LogDebug("Stop performance failed: entry {EntryId} status {Status} is not Performing",
                queueEntryId, entry.Status);
            return (false, $"Cannot stop performance: entry status is {entry.Status}");
        }

        entry.Status = QueueEntryStatus.Completed;
        entry.PerformanceEndTime = DateTime.UtcNow;

        // Calculate duration in minutes
        if (entry.PerformanceStartTime.HasValue)
        {
            var duration = entry.PerformanceEndTime.Value - entry.PerformanceStartTime.Value;
            entry.PerformanceDurationMinutes = (int)duration.TotalMinutes;
        }

        entry.ModifiedAt = DateTime.UtcNow;

        await _queueRepository.UpdateAsync(entry, ct);

        _logger.LogInformation("Performance stopped for queue entry {EntryId}, duration: {Minutes} minutes",
            queueEntryId, entry.PerformanceDurationMinutes);
        return (true, "Performance recorded");
    }

    /// <inheritdoc />
    public async Task<(bool success, string message)> MarkAbsentAsync(
        int queueEntryId, CancellationToken ct)
    {
        var entry = await _queueRepository.GetByIdAsync(queueEntryId, ct);
        if (entry == null)
        {
            _logger.LogDebug("Mark absent failed: entry {EntryId} not found", queueEntryId);
            return (false, "Queue entry not found");
        }

        if (entry.Status != QueueEntryStatus.Pending)
        {
            _logger.LogDebug("Mark absent failed: entry {EntryId} status {Status} is not Pending",
                queueEntryId, entry.Status);
            return (false, $"Cannot mark absent: entry status is {entry.Status}");
        }

        entry.Status = QueueEntryStatus.Absent;
        entry.ModifiedAt = DateTime.UtcNow;

        await _queueRepository.UpdateAsync(entry, ct);

        _logger.LogInformation("Queue entry {EntryId} marked as absent", queueEntryId);
        return (true, "Singer marked as absent");
    }

    /// <inheritdoc />
    public async Task<(bool success, string message)> UpdateSongSelectionAsync(
        int queueEntryId, int? songId, CancellationToken ct)
    {
        var entry = await _queueRepository.GetByIdAsync(queueEntryId, ct);
        if (entry == null)
        {
            _logger.LogDebug("Update song failed: entry {EntryId} not found", queueEntryId);
            return (false, "Queue entry not found");
        }

        // Validate song if provided
        if (songId.HasValue)
        {
            var song = await _songRepository.GetByIdAsync(songId.Value, ct);
            if (song == null)
            {
                _logger.LogDebug("Update song failed: song {SongId} not found", songId);
                return (false, "Song not found");
            }
        }

        entry.SongId = songId;
        entry.ModifiedAt = DateTime.UtcNow;

        await _queueRepository.UpdateAsync(entry, ct);

        _logger.LogInformation("Song selection updated for queue entry {EntryId}", queueEntryId);
        return (true, "Song selection updated");
    }

    /// <inheritdoc />
    public async Task<(bool success, string message)> ReorderQueueAsync(
        int eventId, IEnumerable<(int entryId, int position)> newOrder, CancellationToken ct)
    {
        var orderList = newOrder.ToList();
        if (!orderList.Any())
        {
            _logger.LogDebug("Reorder failed: newOrder is empty");
            return (false, "No entries to reorder");
        }

        // Fetch all entries by ID to update
        var entriesToUpdate = new List<QueueEntry>();
        foreach (var (entryId, position) in orderList)
        {
            var entry = await _queueRepository.GetByIdAsync(entryId, ct);
            if (entry == null || entry.EventId != eventId)
            {
                _logger.LogDebug("Reorder failed: entry {EntryId} not found or belongs to different event", entryId);
                return (false, "One or more entries not found");
            }
            entry.Position = position;
            entry.ModifiedAt = DateTime.UtcNow;
            entriesToUpdate.Add(entry);
        }

        try
        {
            await _queueRepository.ReorderAsync(entriesToUpdate, ct);
            _logger.LogInformation("Queue reordered for event {EventId}, {Count} entries", eventId, entriesToUpdate.Count);
            return (true, "Queue reordered");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Reorder failed for event {EventId}", eventId);
            return (false, "Failed to reorder queue");
        }
    }

    /// <inheritdoc />
    public async Task<QueueEntry?> GetCurrentSingerAsync(int eventId, CancellationToken ct)
    {
        var entries = await _queueRepository.GetByEventIdAsync(eventId, ct);

        // First, check for a performing entry
        var performing = entries.FirstOrDefault(e => e.Status == QueueEntryStatus.Performing);
        if (performing != null)
            return performing;

        // If none performing, return first pending entry
        var nextPending = entries.FirstOrDefault(e => e.Status == QueueEntryStatus.Pending);
        return nextPending;
    }

    /// <inheritdoc />
    public async Task<TimeSpan?> CalculateCompletionEstimateAsync(int eventId, CancellationToken ct)
    {
        var entries = await _queueRepository.GetByEventIdAsync(eventId, ct);

        // Count completed entries with duration data
        var completedWithDuration = entries
            .Where(e => e.Status == QueueEntryStatus.Completed && e.PerformanceDurationMinutes.HasValue)
            .ToList();

        // Need at least 5 completed performances to estimate
        if (completedWithDuration.Count < 5)
            return null;

        // Count pending entries
        var pendingCount = entries.Count(e => e.Status == QueueEntryStatus.Pending);
        if (pendingCount == 0)
            return TimeSpan.Zero;

        // Calculate average duration (safe because we filtered to HasValue above)
        var avgDurationMinutes = completedWithDuration
            .Average(e => (double)(e.PerformanceDurationMinutes ?? 0));

        // Estimate: now + (pending_count * avg_duration)
        var estimatedMinutes = pendingCount * avgDurationMinutes;
        return TimeSpan.FromMinutes(estimatedMinutes);
    }
}
