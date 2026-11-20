using MyVocaList.Contracts.Models;
using MyVocaList.Domain.Constants;
using MyVocaList.Domain.Entity;

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

    // --- BUG-088 identity (REQ-DUP-03, REQ-DUP-09) ---

    /// <summary>REQ-DUP-03: at least one of email or birthday is required. Returns (false, "Enter an email or a birthday.") when both are empty.</summary>
    (bool isValid, string message) ValidateContact(string? email, int? birthDay, int? birthMonth);

    /// <summary>
    /// REQ-DUP-09 soft warning: persons with the same FullName (default collation) whose identity differs
    /// (email or birthday). Excludes excludeId and any exact identity match. Never blocks a save.
    /// </summary>
    Task<IReadOnlyList<Person>> GetSameNamePersonsAsync(
        string fullName, string? email, int? birthDay, int? birthMonth, int? excludeId,
        CancellationToken cancellationToken = default);
}
