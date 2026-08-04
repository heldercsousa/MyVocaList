using MyVocaList.Contracts.Enums;
using MyVocaList.Domain.Interfaces;
using MyVocaList.Domain.ServicesInterfaces;

namespace MyVocaList.Services;

// TODO [BUG-071 / UOW] — SHARED WINDOW-SCOPED DbContext: OUT OF PATTERN.
// This code mutates entities on the app-lifetime AppDbContext (MAUI creates no
// per-page scope), so it can leave an entity tracked and later throw
// "another instance with the same key value for {'Id'} is already being tracked",
// and a throw here can poison the shared context for every other feature.
// Fix by applying the unit-of-work pattern being established for the Venue,
// Artist, Person and Song CRUDs — do NOT patch this locally.
// Spec: Docs/Management/cross-cutting/read-model-notracking-guidelines/changes/
//   2026-08-03-dbcontext-lifetime-unit-of-work-pattern-maui-has-no-per-page-scope/
// Tracked by: .../changes/2026-08-04-apply-the-unit-of-work-pattern-to-queue-and-event-entities-deferred/
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

    // TODO [BUG-071 / UOW] — out of pattern: mutates the shared window-scope DbContext (see class-level note).
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

        if (name.Trim().Length < MinNameLength || name.Trim().Length > MaxNameLength)
        {
            _logger.LogDebug("Event creation failed: name length {Length} outside range {Min}-{Max}",
                name.Trim().Length, MinNameLength, MaxNameLength);
            return (false, $"Name must be {MinNameLength}–{MaxNameLength} characters", null);
        }

        // Validation: scheduled times
        if (scheduledEnd <= scheduledStart)
        {
            _logger.LogDebug("Event creation failed: scheduledEnd {End} not after scheduledStart {Start}",
                scheduledEnd, scheduledStart);
            return (false, "Event end time must be after start time", null);
        }

        // Event.Name trimming is enforced by the EF Core ValueConverter configured in
        // QueueManagementEventConfiguration (design.md § D3) — not here. EF applies the same
        // converter to this WHERE parameter, so an untrimmed check value still matches trimmed
        // stored rows.
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

    // TODO [BUG-071 / UOW] — out of pattern: mutates the shared window-scope DbContext (see class-level note).
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

    // TODO [BUG-071 / UOW] — out of pattern: mutates the shared window-scope DbContext (see class-level note).
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

    // TODO [BUG-071 / UOW] — out of pattern: mutates the shared window-scope DbContext (see class-level note).
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

    // TODO [BUG-071 / UOW] — out of pattern: mutates the shared window-scope DbContext (see class-level note).
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
