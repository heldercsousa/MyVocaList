using MyVocaList.Domain;
using MyVocaList.Infra.Data;
using Microsoft.EntityFrameworkCore;
using MyVocaList.Infra.Data.Repositories;

namespace MyVocaList.Services;

/// <summary>
/// Service responsible only for queue and event operations
/// Person operations delegated to IPersonService
/// </summary>
public class QueueService : IQueueService
{
    private readonly AppDbContext _dbContext; // Direct for migrations
    private readonly IEstabelecimentoRepository _venueRepository;
    private readonly IEventoRepository _eventRepository;
    private readonly IParticipacaoEventoRepository _participationRepository;
    private readonly IPersonService _personService; // New dependency

    public QueueService(
        AppDbContext dbContext,
        IEstabelecimentoRepository venueRepository,
        IEventoRepository eventRepository,
        IParticipacaoEventoRepository participationRepository,
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
    public async Task<(bool success, string message, Pessoa? addedDomainPerson)> AddPersonToQueueAsync(
        string fullName, string birthday = null, string email = null)
    {
        try
        {
            // Check if there is an active event
            var activeEvent = await GetActiveEventAsync();
            if (activeEvent == null || !activeEvent.FilaAtiva)
            {
                return (false, "There is no active queue at the moment", null);
            }

            // Delegate person creation/search to PersonService
            Pessoa? person = null;

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

            return (true, $"{person.NomeCompleto} added to queue!", person);
        }
        catch (Exception ex)
        {
            return (false, $"Error adding to queue: {ex.Message}", null);
        }
    }

    /// <summary>
    /// Records participation in active event
    /// </summary>
    public async Task RecordParticipationAsync(int personId, ParticipacaoStatus status)
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

        var participation = new ParticipacaoEvento
        {
            PessoaId = personId,
            EventoId = activeEvent.Id,
            Timestamp = DateTime.Now,
            Status = status
        };

        await _participationRepository.AddAsync(participation);
        await _participationRepository.SaveChangesAsync();
    }

    // --- Event Management ---

    public async Task<Evento?> GetActiveEventAsync()
    {
        return await _eventRepository.GetActiveEventAsync();
    }

    public async Task SetActiveEventAsync(int eventId)
    {
        await _eventRepository.SetActiveEventAsync(eventId);
    }

    public async Task<IEnumerable<Estabelecimento>> GetAllEstablishmentsAsync()
    {
        return await _venueRepository.GetAllAsync();
    }

    public async Task<IEnumerable<Evento>> GetAllEventsAsync()
    {
        return await _eventRepository.GetAllAsync();
    }

    // --- Private methods ---

    private async Task<Evento> GetOrCreateDefaultEventAsync()
    {
        var activeEvent = await _eventRepository.GetActiveEventAsync();
        if (activeEvent == null)
        {
            var defaultVenue = (await _venueRepository.GetAllAsync()).FirstOrDefault();
            if (defaultVenue == null)
            {
                defaultVenue = new Estabelecimento { Nome = "Default Venue Created Automatically" };
                await _venueRepository.AddAsync(defaultVenue);
                await _venueRepository.SaveChangesAsync();
            }

            activeEvent = new Evento
            {
                EstabelecimentoId = defaultVenue.Id,
                DataEvento = DateTime.Today,
                NomeEvento = $"Auto Event {DateTime.Today.ToShortDateString()}",
                FilaAtiva = true
            };
            await _eventRepository.AddAsync(activeEvent);
            await _eventRepository.SaveChangesAsync();
        }
        return activeEvent;
    }
}
