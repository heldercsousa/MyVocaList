using MyVocaList.Contracts.Enums;
using MyVocaList.Domain.Interfaces;
using MyVocaList.Domain.ServicesInterfaces;

namespace MyVocaList.Services;

/// <inheritdoc />
public sealed class EventService : IEventService
{
    private readonly IEventRepository _eventRepository;
    private readonly ILogger<EventService> _logger;

    private const int MinNameLength = 1;
    private const int MaxNameLength = 100;

    public EventService(IEventRepository eventRepository, ILogger<EventService> logger)
    {
        _eventRepository = eventRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<(bool success, string message, Domain.Entities.Event? @event)> CreateEventAsync(
        int venueId, string name, DateTime scheduledStart, DateTime scheduledEnd,
        string mode, CancellationToken ct)
    {
        // Validation: name
        if (string.IsNullOrWhiteSpace(name))
        {
            _logger.LogDebug("Event creation failed: name is required");
            return (false, "Event name is required", null);
        }

        name = name.Trim();

        if (name.Length < MinNameLength || name.Length > MaxNameLength)
        {
            _logger.LogDebug("Event creation failed: name length {Length} outside range {Min}-{Max}",
                name.Length, MinNameLength, MaxNameLength);
            return (false, $"Name must be {MinNameLength}–{MaxNameLength} characters", null);
        }

        // Validation: scheduled times
        if (scheduledEnd <= scheduledStart)
        {
            _logger.LogDebug("Event creation failed: scheduledEnd {End} not after scheduledStart {Start}",
                scheduledEnd, scheduledStart);
            return (false, "Event end time must be after start time", null);
        }

        // Validation: check for duplicate name on the same date at this venue
        var isDuplicate = await _eventRepository.ExistsByNameAsync(name, ct);
        if (isDuplicate)
        {
            _logger.LogDebug("Event creation failed: duplicate name '{Name}' for venue {VenueId}",
                name, venueId);
            return (false, "An event with this name already exists for this venue", null);
        }

        // Create event
        var @event = new Domain.Entities.Event
        {
            VenueId = venueId,
            Name = name,
            ScheduledStartTime = scheduledStart,
            ScheduledEndTime = scheduledEnd,
            Mode = mode ?? "VideoKaraoke",
            Status = EventStatus.Created,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow,
            // Required navigation properties — EF will populate on load
            Venue = null!,
            QueueEntries = []
        };

        await _eventRepository.AddAsync(@event, ct);

        _logger.LogInformation("Event '{Name}' created for venue {VenueId}", name, venueId);
        return (true, $"Event '{name}' created successfully", @event);
    }

    /// <inheritdoc />
    public async Task<(bool success, string message)> StartEventAsync(int eventId, CancellationToken ct)
    {
        var @event = await _eventRepository.GetByIdAsync(eventId, ct);
        if (@event == null)
        {
            _logger.LogDebug("Event start failed: event {EventId} not found", eventId);
            return (false, "Event not found");
        }

        if (@event.Status != EventStatus.Created)
        {
            _logger.LogDebug("Event start failed: event {EventId} status {Status} is not {Expected}",
                eventId, @event.Status, EventStatus.Created);
            return (false, $"Event cannot be started from {@event.Status} status");
        }

        @event.Status = EventStatus.Started;
        @event.ActualStartTime = DateTime.UtcNow;
        @event.ModifiedAt = DateTime.UtcNow;

        await _eventRepository.UpdateAsync(@event, ct);

        _logger.LogInformation("Event {EventId} started", eventId);
        return (true, "Event started");
    }

    /// <inheritdoc />
    public async Task<(bool success, string message)> PauseEventAsync(int eventId, CancellationToken ct)
    {
        var @event = await _eventRepository.GetByIdAsync(eventId, ct);
        if (@event == null)
        {
            _logger.LogDebug("Event pause failed: event {EventId} not found", eventId);
            return (false, "Event not found");
        }

        if (@event.Status != EventStatus.Started)
        {
            _logger.LogDebug("Event pause failed: event {EventId} status {Status} is not {Expected}",
                eventId, @event.Status, EventStatus.Started);
            return (false, $"Event cannot be paused from {@event.Status} status");
        }

        @event.Status = EventStatus.Paused;
        @event.ModifiedAt = DateTime.UtcNow;

        await _eventRepository.UpdateAsync(@event, ct);

        _logger.LogInformation("Event {EventId} paused", eventId);
        return (true, "Event paused");
    }

    /// <inheritdoc />
    public async Task<(bool success, string message)> ResumeEventAsync(int eventId, CancellationToken ct)
    {
        var @event = await _eventRepository.GetByIdAsync(eventId, ct);
        if (@event == null)
        {
            _logger.LogDebug("Event resume failed: event {EventId} not found", eventId);
            return (false, "Event not found");
        }

        if (@event.Status != EventStatus.Paused)
        {
            _logger.LogDebug("Event resume failed: event {EventId} status {Status} is not {Expected}",
                eventId, @event.Status, EventStatus.Paused);
            return (false, $"Event cannot be resumed from {@event.Status} status");
        }

        @event.Status = EventStatus.Started;
        @event.ModifiedAt = DateTime.UtcNow;

        await _eventRepository.UpdateAsync(@event, ct);

        _logger.LogInformation("Event {EventId} resumed", eventId);
        return (true, "Event resumed");
    }

    /// <inheritdoc />
    public async Task<(bool success, string message)> FinishEventAsync(int eventId, CancellationToken ct)
    {
        var @event = await _eventRepository.GetByIdAsync(eventId, ct);
        if (@event == null)
        {
            _logger.LogDebug("Event finish failed: event {EventId} not found", eventId);
            return (false, "Event not found");
        }

        if (@event.Status != EventStatus.Started && @event.Status != EventStatus.Paused)
        {
            _logger.LogDebug("Event finish failed: event {EventId} status {Status} cannot transition to Finished",
                eventId, @event.Status);
            return (false, $"Event cannot be finished from {@event.Status} status");
        }

        @event.Status = EventStatus.Finished;
        @event.ActualEndTime = DateTime.UtcNow;
        @event.ModifiedAt = DateTime.UtcNow;

        await _eventRepository.UpdateAsync(@event, ct);

        _logger.LogInformation("Event {EventId} finished", eventId);
        return (true, "Event finished");
    }

    /// <inheritdoc />
    public async Task<Domain.Entities.Event?> GetActiveEventAsync(CancellationToken ct)
    {
        // Query for the first event with STARTED or PAUSED status
        var (events, _) = await _eventRepository.GetPagedAsync(1, 1, null, ct);
        var activeEvent = events.FirstOrDefault(e => e.Status == EventStatus.Started || e.Status == EventStatus.Paused);
        return activeEvent;
    }

    /// <inheritdoc />
    public async Task<(bool isValid, string message)> ValidateEventNameAsync(
        int venueId, string name, DateTime scheduledDate, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name))
            return (false, "Event name is required");

        name = name.Trim();

        if (name.Length < MinNameLength || name.Length > MaxNameLength)
            return (false, $"Name must be {MinNameLength}–{MaxNameLength} characters");

        var isDuplicate = await _eventRepository.ExistsByNameAsync(name, ct);
        if (isDuplicate)
            return (false, "An event with this name already exists");

        return (true, "");
    }
}
