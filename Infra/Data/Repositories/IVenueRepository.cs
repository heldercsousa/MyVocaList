using MyVocaList.Domain;

namespace MyVocaList.Infra.Data.Repositories
{
    /// <summary>
    /// Repository interface for Venue entity operations
    /// </summary>
    public interface IVenueRepository : IBaseRepository<Venue>
    {
        /// <summary>
        /// Gets venue by exact name match
        /// </summary>
        Task<Venue?> GetByNameAsync(string name);

        /// <summary>
        /// Searches venues by name starting with the search term
        /// </summary>
        Task<IEnumerable<Venue>> SearchByNameStartsWithAsync(string searchTerm, int maxResults = 10);

        /// <summary>
        /// Searches venues by name containing the search term
        /// </summary>
        Task<IEnumerable<Venue>> SearchByNameContainsAsync(string searchTerm, int maxResults = 10);

        Task<IEnumerable<(Venue venue, bool hasEvents)>> SearchWithHasEventsAsync(string? query);
        Task<IEnumerable<(Venue venue, bool hasEvents)>> GetAllWithHasEventsAsync();
        Task<IEnumerable<(Venue venue, bool hasEvents)>> GetByIdsWithHasEventsAsync(IEnumerable<int> ids);

        /// <summary>
        /// Gets a paginated list of ALL venues with event information flag
        /// Does NOT filter - returns all venues with hasEvents boolean flag
        /// </summary>
        /// <param name="pageNumber">Page number (1-based)</param>
        /// <param name="pageSize">Number of items per page</param>
        /// <param name="query">Optional search query</param>
        /// <returns>Tuple with list of venues (with event flag) and total count</returns>
        Task<(IEnumerable<(Venue venue, bool hasEvents)> items, int totalCount)> GetPagedWithEventInfoAsync(
            int pageNumber,
            int pageSize,
            string? query = null);
    }
}
