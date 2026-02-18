using MyVocaList.Infra.Data;
using Microsoft.EntityFrameworkCore;
using MyVocaList.Infra.Data.Repositories;
using MyVocaList.Domain.Entity;

namespace MyVocaList.Services;

/// <summary>
/// Service responsible only for queue and event operations
/// Person operations delegated to IPersonService
/// </summary>
public class QueueService : IQueueService
{
    private readonly AppDbContext _dbContext; // Direct for migrations
    private readonly IVenueRepository _venueRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IEventParticipationRepository _participationRepository;
    private readonly IPersonService _personService; // New dependency

    public QueueService(
        AppDbContext dbContext,
        IVenueRepository venueRepository,
        IEventRepository eventRepository,
        IEventParticipationRepository participationRepository,
        IPersonService personService)
    {
        _dbContext = dbContext;
        _venueRepository = venueRepository;
        _eventRepository = eventRepository;
        _participationRepository = participationRepository;
        _personService = personService;
    }

    // --- Queue Operations (using PersonService for person operations) ---

    /// <summary>
    /// Adds person to queue - delegates creation to PersonService
    /// </summary>
    public async Task<(bool success, string message, Person? addedDomainPerson)> AddPersonToQueueAsync(
        string fullName, string birthday = null, string email = null)
    {
        try
        {
            // Check if there is an active event
            var activeEvent = await GetActiveEventAsync();
            if (activeEvent == null || !activeEvent.QueueActive)
            {
                return (false, "There is no active queue at the moment", null);
            }

            // Delegate person creation/search to PersonService
            Person? person = null;

            // First try to find existing person
            person = await _personService.GetPersonByNameAsync(fullName);

            if (person == null)
            {
                // If doesn't exist, create new person
                var createResult = await _personService.CreatePersonAsync(fullName, birthday, email);
                if (!createResult.success)
                {
                    return (false, createResult.message, null);
                }
                person = createResult.person;
            }

            return (true, $"{person.FullName} added to queue!", person);
        }
        catch (Exception ex)
        {
            return (false, $"Error adding to queue: {ex.Message}", null);
        }
    }

    /// <summary>
    /// Records participation in active event
    /// </summary>
    public async Task RecordParticipationAsync(int personId, ParticipationStatus status)
    {
        if (personId == 0)
        {
            throw new ArgumentException("Invalid Person ID to record participation.");
        }

        var activeEvent = await GetActiveEventAsync();
        if (activeEvent == null)
        {
            activeEvent = await GetOrCreateDefaultEventAsync();
        }

        var participation = new EventParticipation
        {
            PersonId = personId,
            EventId = activeEvent.Id,
            Timestamp = DateTime.Now,
            Status = status
        };

        await _participationRepository.AddAsync(participation);
        await _participationRepository.SaveChangesAsync();
    }

    // --- Event Management ---

    public async Task<Event?> GetActiveEventAsync()
    {
        return await _eventRepository.GetActiveEventAsync();
    }

    public async Task SetActiveEventAsync(int eventId)
    {
        await _eventRepository.SetActiveEventAsync(eventId);
    }

    public async Task<IEnumerable<Venue>> GetAllEstablishmentsAsync()
    {
        return await _venueRepository.GetAllAsync();
    }

    public async Task<IEnumerable<Event>> GetAllEventsAsync()
    {
        return await _eventRepository.GetAllAsync();
    }

    // --- Private methods ---

    private async Task<Event> GetOrCreateDefaultEventAsync()
    {
        var activeEvent = await _eventRepository.GetActiveEventAsync();
        if (activeEvent == null)
        {
            var defaultVenue = (await _venueRepository.GetAllAsync()).FirstOrDefault();
            if (defaultVenue == null)
            {
                defaultVenue = new Venue { Name = "Default Venue Created Automatically" };
                await _venueRepository.AddAsync(defaultVenue);
                await _venueRepository.SaveChangesAsync();
            }

            activeEvent = new Event
            {
                VenueId = defaultVenue.Id,
                EventDate = DateTime.Today,
                EventName = $"Auto Event {DateTime.Today.ToShortDateString()}",
                QueueActive = true
            };
            await _eventRepository.AddAsync(activeEvent);
            await _eventRepository.SaveChangesAsync();
        }
        return activeEvent;
    }
}
