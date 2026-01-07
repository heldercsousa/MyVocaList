using MyVocaList.Domain;
using Microsoft.EntityFrameworkCore;
using MyVocaList.Infra.Utils;

namespace MyVocaList.Infra.Data.Repositories
{
    /// <summary>
    /// Repository implementation for Event entity operations
    /// </summary>
    public class EventRepository : BaseRepository<Event>, IEventRepository
    {
        public EventRepository(AppDbContext context) : base(context) { }

        public async Task<Event> GetActiveEventAsync()
        {
            return await _dbSet.Include(e => e.Venue) // Include the Venue
                               .FirstOrDefaultAsync(e => e.QueueActive);
        }

        public async Task SetActiveEventAsync(int eventId)
        {
            Guard.AgainstNegativeOrZero(eventId, nameof(eventId));

            var currentActive = await _dbSet.FirstOrDefaultAsync(e => e.QueueActive);
            if (currentActive != null && currentActive.Id != eventId) // Avoid deactivating if already active
            {
                currentActive.QueueActive = false;
                _dbSet.Update(currentActive);
            }

            var newActive = await _dbSet.FindAsync(eventId);
            if (newActive != null)
            {
                newActive.QueueActive = true;
                _dbSet.Update(newActive);
            }
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Checks if there are events associated with a venue
        /// </summary>
        public async Task<bool> HasEventsByVenueAsync(int venueId)
        {
            Guard.AgainstNegativeOrZero(venueId, nameof(venueId));

            return await _context.Events
                .AnyAsync(e => e.VenueId == venueId);
        }
    }
}
