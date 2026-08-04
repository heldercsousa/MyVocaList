using Microsoft.EntityFrameworkCore;
using MyVocaList.Domain.Entity;
using MyVocaList.Domain.RepositoryInterface;

namespace MyVocaList.Infra.Repository
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
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(eventId, nameof(eventId));

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
            // TODO [BUG-071 / UOW] — out of pattern: SaveChangesAsync embedded in a repository mutator
            // (see MyVocaList.Services/Services/QueueService.cs class-level note for the full defect).
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Checks if there are events associated with a venue
        /// </summary>
        public async Task<bool> HasEventsByVenueAsync(int venueId)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(venueId, nameof(venueId));

            return await _context.Events
                .AnyAsync(e => e.VenueId == venueId);
        }
    }
}
