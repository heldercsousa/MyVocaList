using MyVocaList.Contracts.DTOs.Venues;
using MyVocaList.Domain.Entity;

namespace MyVocaList.Domain.RepositoryInterface
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

        Task<IEnumerable<(Venue venue, int eventCount)>> SearchWithHasEventsAsync(string? query);
        Task<IEnumerable<(Venue venue, int eventCount)>> GetAllWithHasEventsAsync();
        Task<IEnumerable<(Venue venue, int eventCount)>> GetByIdsWithHasEventsAsync(IEnumerable<int> ids);

        /// <summary>
        /// Gets a paginated list of ALL venues with event information flag
        /// Does NOT filter - returns all venues with hasEvents boolean flag
        /// </summary>
        /// <param name="pageNumber">Page number (1-based)</param>
        /// <param name="pageSize">Number of items per page</param>
        /// <param name="query">Optional search query</param>
        /// <returns>Tuple with list of venues (with event flag) and total count</returns>
        Task<(IEnumerable<(Venue venue, int eventCount)> items, int totalCount)> GetPagedWithEventInfoAsync(
            int pageNumber,
            int pageSize,
            string? query = null);

        /// <summary>
        /// Checks whether another venue has an equal name and all six address fields equal (case and accent
        /// insensitive under the database collation; a blank field equals only a blank field, so two empty
        /// addresses match). The caller passes a normalised name and address (REQ-VEN-41 to 44).
        /// </summary>
        /// <param name="name">Trimmed venue name to look for</param>
        /// <param name="address">Normalised address to look for; may be empty</param>
        /// <param name="excludeId">Id of the venue being edited, or null when adding</param>
        /// <param name="ct">Cancellation token</param>
        Task<bool> NameAndAddressExistsAsync(string name, VenueAddress address, int? excludeId, CancellationToken ct = default);

        /// <summary>
        /// Gets every stored local image reference of venues (<c>ImageUrl</c> starting with <c>local:</c>),
        /// used by the start-up orphan sweep (REQ-VEN-04)
        /// </summary>
        Task<List<string>> GetLocalImageReferencesAsync(CancellationToken ct = default);
    }
}
