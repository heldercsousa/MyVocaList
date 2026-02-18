using MyVocaList.Domain.Entity;

namespace MyVocaList.Services
{
    /// <summary>
    /// Interface for queue and event operations
    /// Person operations delegated to IPersonService
    /// </summary>
    public interface IQueueService
    {
        // Queue Operations
        Task<(bool success, string message, Person? addedDomainPerson)> AddPersonToQueueAsync(
            string fullName, string birthday = null, string email = null);
        Task RecordParticipationAsync(int personId, ParticipationStatus status);

        // Event Management
        Task<Event?> GetActiveEventAsync();
        Task SetActiveEventAsync(int eventId);
        Task<IEnumerable<Venue>> GetAllEstablishmentsAsync();
        Task<IEnumerable<Event>> GetAllEventsAsync();
    }
}
