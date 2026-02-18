using Microsoft.EntityFrameworkCore;
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
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(personId, nameof(personId));
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(eventId, nameof(eventId));

            return await _dbSet
                .Where(ep => ep.PersonId == personId && ep.EventId == eventId)
                .ToListAsync();
        }
    }
}
