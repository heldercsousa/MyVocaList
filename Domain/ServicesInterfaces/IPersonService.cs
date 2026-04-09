using MyVocaList.Contracts.Models;
using MyVocaList.Domain.Entity;

namespace MyVocaList.Domain.ServicesInterfaces;

public interface IPersonService
{
    int MaxInputLength { get; }
    int MaxDatabaseLength { get; }
    int ShowCounterAt { get; }

    (bool isValid, string message) ValidateNameInput(string name);
    (bool isValid, string message) ValidateNameForDatabase(string name);
    (bool isValid, string message) ValidateBirthday(string birthday);
    (bool isValid, string message) ValidateEmail(string email);

    Task<(bool success, string message, Person? person)> CreatePersonAsync(
        string fullName, string birthday = null, string email = null,
        CancellationToken cancellationToken = default);
    Task<Person?> GetPersonByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Person?> GetPersonByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<IEnumerable<Person>> SearchPersonsAsync(string searchTerm, int maxResults = 5, CancellationToken cancellationToken = default);
    Task<IEnumerable<Person>> SearchPersonsStartsWithAsync(string searchTerm, int maxResults = 3, CancellationToken cancellationToken = default);

    bool ShouldShowCharacterCounter(int currentLength);
    (string text, bool isWarning, bool isError) GetCharacterCounterInfo(int currentLength);

    // --- New methods added in Person CRUD plan ---

    Task<(IEnumerable<PersonListItemDto> items, int totalCount)> GetPagedPersonsForListAsync(
        int pageNumber, int pageSize, string query = null, CancellationToken cancellationToken = default);

    Task<(bool success, string message)> UpdatePersonAsync(
        int id, string fullName, string birthday = null, string email = null,
        CancellationToken cancellationToken = default);

    Task<(bool success, string message)> DeletePersonsAsync(
        IEnumerable<int> ids, CancellationToken cancellationToken = default);
}
