using MyVocaList.Contracts.DTOs.List;
using MyVocaList.Domain.Entity;

namespace MyVocaList.Services
{
    public interface IVenueService
    {
        (bool isValid, string message) ValidateNameInput(string name);
        Task<(bool success, string message, Venue? venue)> CreateVenueAsync(string name);
        Task<(bool success, string message)> UpdateVenueAsync(int id, string newName);

        // Old method, kept for compatibility and single deletion scenarios
        Task<(bool success, string message)> DeleteVenueAsync(int id);

        // NEW METHOD for batch deletion
        Task<(bool success, string message)> DeleteVenuesAsync(IEnumerable<int> ids);

        Task<IEnumerable<Venue>> GetAllVenuesAsync();
        Task<Venue?> GetVenueByIdAsync(int id);
        bool ShouldShowCharacterCounter(int currentLength);
        (string text, bool isWarning, bool isError) GetCharacterCounterInfo(int currentLength);

        Task<IEnumerable<VenueListItemDto>> GetAllVenuesForListAsync();
        Task<IEnumerable<VenueListItemDto>> SearchVenuesForListAsync(string query);

        /// <summary>
        /// Gets a paginated list of venues for display
        /// </summary>
        /// <param name="pageNumber">Page number (1-based)</param>
        /// <param name="pageSize">Number of items per page</param>
        /// <param name="query">Optional search query</param>
        /// <returns>Tuple with list of DTOs and total count</returns>
        Task<(IEnumerable<VenueListItemDto> items, int totalCount)> GetPagedVenuesForListAsync(
            int pageNumber,
            int pageSize,
            string? query = null);
    }
}
