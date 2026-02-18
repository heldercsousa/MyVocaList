using Microsoft.EntityFrameworkCore;
using MyVocaList.Infra.Utils;
using MyVocaList.Domain.Entity;
using MyVocaList.Domain.RepositoryInterface;

namespace MyVocaList.Infra.Repository
{
    /// <summary>
    /// Repository implementation for EventParticipation entity operations
    /// </summary>
    public class EventParticipationRepository : BaseRepository<EventParticipation>, IEventParticipationRepository
    {
        public EventParticipationRepository(AppDbContext context) : base(context) { }

        public async Task<IEnumerable<EventParticipation>> GetParticipationsByPersonIdAndEventIdAsync(int personId, int eventId)
        {
            Guard.AgainstNegativeOrZero(personId, nameof(personId));
            Guard.AgainstNegativeOrZero(eventId, nameof(eventId));

            return await _dbSet
                .Where(ep => ep.PersonId == personId && ep.EventId == eventId)
                .ToListAsync();
        }
    }
}
