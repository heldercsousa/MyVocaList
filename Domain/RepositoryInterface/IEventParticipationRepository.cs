using MyVocaList.Domain.Entity;

namespace MyVocaList.Domain.RepositoryInterface
{
    /// <summary>
    /// Repository interface for EventParticipation entity operations
    /// </summary>
    public interface IEventParticipationRepository : IBaseRepository<EventParticipation>
    {
        Task<IEnumerable<EventParticipation>> GetParticipationsByPersonIdAndEventIdAsync(int personId, int eventId);
    }
}
