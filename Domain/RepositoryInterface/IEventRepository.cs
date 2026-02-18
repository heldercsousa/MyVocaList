using MyVocaList.Domain.Entity;

namespace MyVocaList.Domain.RepositoryInterface
{
    /// <summary>
    /// Repository interface for Event entity operations
    /// </summary>
    public interface IEventRepository : IBaseRepository<Event>
    {
        Task<Event> GetActiveEventAsync();
        Task SetActiveEventAsync(int eventId);
        Task<bool> HasEventsByVenueAsync(int venueId);
    }
}
