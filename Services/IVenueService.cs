using MyVocaList.Contracts.DTOs.List;
using MyVocaList.Domain;

namespace MyVocaList.Services
{
    public interface IVenueService
    {
        (bool isValid, string message) ValidateNameInput(string name);
        Task<(bool success, string message, Estabelecimento? venue)> CreateVenueAsync(string name);
        Task<(bool success, string message)> UpdateVenueAsync(int id, string newName);

        // Old method, kept for compatibility and single deletion scenarios
        Task<(bool success, string message)> DeleteVenueAsync(int id);

        // NEW METHOD for batch deletion
        Task<(bool success, string message)> DeleteVenuesAsync(IEnumerable<int> ids);

        Task<IEnumerable<Estabelecimento>> GetAllVenuesAsync();
        Task<Estabelecimento?> GetVenueByIdAsync(int id);
        bool ShouldShowCharacterCounter(int currentLength);
        (string text, bool isWarning, bool isError) GetCharacterCounterInfo(int currentLength);

        Task<IEnumerable<EstabelecimentoListItemDto>> GetAllVenuesForListAsync();
        Task<IEnumerable<EstabelecimentoListItemDto>> SearchVenuesForListAsync(string query);

        /// <summary>
        /// Gets a paginated list of venues for display
        /// </summary>
        /// <param name="pageNumber">Page number (1-based)</param>
        /// <param name="pageSize">Number of items per page</param>
        /// <param name="query">Optional search query</param>
        /// <returns>Tuple with list of DTOs and total count</returns>
        Task<(IEnumerable<EstabelecimentoListItemDto> items, int totalCount)> GetPagedVenuesForListAsync(
            int pageNumber,
            int pageSize,
            string? query = null);
    }
}
