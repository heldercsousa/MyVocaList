using MyVocaList.Contracts.DTOs.Venues;
using MyVocaList.Contracts.DTOs.List;

namespace MyVocaList.Domain.ServicesInterfaces
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
        Task<VenueEditDto?> GetVenueForEditAsync(int id, CancellationToken ct = default);
        Task<(bool success, string message, VenueField field)> SaveVenueAsync(VenueSaveRequest request, CancellationToken ct = default);
        (bool isValid, string message, VenueField field) ValidateAddressInput(VenueAddress address);
    }
}
