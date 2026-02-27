using MyVocaList.Contracts.DTOs.List;

namespace MyVocaList.Services
{
    public interface IVenueService
    {
        (bool isValid, string message) ValidateNameInput(string name);
        Task<(bool success, string message)> CreateVenueAsync(string name);
        Task<(bool success, string message)> UpdateVenueAsync(int id, string newName);
        Task<(bool success, string message)> DeleteVenuesAsync(IEnumerable<int> ids);
        bool ShouldShowCharacterCounter(int currentLength);
        (string text, bool isWarning, bool isError) GetCharacterCounterInfo(int currentLength);
        Task<(IEnumerable<VenueListItemDto> items, int totalCount)> GetPagedVenuesForListAsync(
            int pageNumber,
            int pageSize,
            string query = null);
    }
}
