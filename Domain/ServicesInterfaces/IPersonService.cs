using MyVocaList.Contracts.Models;
using MyVocaList.Domain.Constants;
using MyVocaList.Domain.Entity;
using MyVocaList.Domain.ReadModels;

namespace MyVocaList.Domain.ServicesInterfaces;

public interface IPersonService
{
    int MaxInputLength { get; }
    int MaxDatabaseLength { get; }
    int ShowCounterAt { get; }

    (bool isValid, string message) ValidateNameInput(string name);
    (bool isValid, string message) ValidateNameForDatabase(string name);
    (bool isValid, string message) ValidateBirthday(int? birthDay, int? birthMonth);
    (bool isValid, string message) ValidateEmail(string email);

    Task<(bool success, string message, Person? person)> CreatePersonAsync(
        string fullName, int? birthDay = null, int? birthMonth = null, string email = null,
        CancellationToken cancellationToken = default);
    Task<Person?> GetPersonByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Person?> GetPersonByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<IEnumerable<Person>> SearchPersonsAsync(string searchTerm, int maxResults = SearchConstants.MaxSearchResults, CancellationToken cancellationToken = default);
    Task<IEnumerable<Person>> SearchPersonsStartsWithAsync(string searchTerm, int maxResults = SearchConstants.MaxSearchResults, CancellationToken cancellationToken = default);

    bool ShouldShowCharacterCounter(int currentLength);
    (string text, bool isWarning, bool isError) GetCharacterCounterInfo(int currentLength);

    // --- New methods added in Person CRUD plan ---

    Task<(IEnumerable<PersonListItemDto> items, int totalCount)> GetPagedPersonsForListAsync(
        int pageNumber, int pageSize, string query = null, CancellationToken cancellationToken = default);

    Task<(bool success, string message)> UpdatePersonAsync(
        int id, string fullName, int? birthDay, int? birthMonth, string email,
        CancellationToken cancellationToken = default);

    Task<(bool success, string message)> DeletePersonsAsync(
        IEnumerable<int> ids, CancellationToken cancellationToken = default);

    // --- BUG-088 identity (REQ-DUP-09, REQ-DUP-13a) ---

    /// <summary>
    /// REQ-DUP-09 soft warning: same-name persons (default collation) for the sheet, at most
    /// PersonService.SameNameListLimit listed, plus the total count. Never blocks a save.
    /// Name-only input (REQ-DUP-13a) includes the exact identity match in the list.
    /// </summary>
    Task<SameNameResult> GetSameNamePersonsAsync(
        string fullName, string? email, int? birthDay, int? birthMonth, int? excludeId,
        CancellationToken cancellationToken = default);
}
