using MyVocaList.Domain;

namespace MyVocaList.Services
{
    /// <summary>
    /// Interface for queue and event operations
    /// Person operations delegated to IPersonService
    /// </summary>
    public interface IQueueService
    {
        // Queue Operations
        Task<(bool success, string message, Pessoa? addedDomainPerson)> AddPersonToQueueAsync(
            string fullName, string birthday = null, string email = null);
        Task RecordParticipationAsync(int personId, ParticipacaoStatus status);

        // Event Management
        Task<Evento?> GetActiveEventAsync();
        Task SetActiveEventAsync(int eventId);
        Task<IEnumerable<Estabelecimento>> GetAllEstablishmentsAsync();
        Task<IEnumerable<Evento>> GetAllEventsAsync();
    }
}
