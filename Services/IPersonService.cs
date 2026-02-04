using MyVocaList.Domain;

namespace MyVocaList.Services
{
    public interface IPersonService
    {
        // Hybrid validation constants
        int MaxInputLength { get; }
        int MaxDatabaseLength { get; }
        int ShowCounterAt { get; }

        // Validation
        (bool isValid, string message) ValidateNameInput(string name);
        (bool isValid, string message) ValidateNameForDatabase(string name);
        (bool isValid, string message) ValidateBirthday(string birthday);
        (bool isValid, string message) ValidateEmail(string email);

        // Registration and search operations
        Task<(bool success, string message, Person? person)> CreatePersonAsync(string fullName, string birthday = null, string email = null);
        Task<Person?> GetPersonByIdAsync(int id);
        Task<Person?> GetPersonByNameAsync(string name);
        Task<IEnumerable<Person>> SearchPersonsAsync(string searchTerm, int maxResults = 5);
        Task<IEnumerable<Person>> SearchPersonsStartsWithAsync(string searchTerm, int maxResults = 3);

        // Utilities
        (string text, bool isWarning, bool isError) GetCharacterCounterInfo(int currentLength);
        bool ShouldShowCharacterCounter(int currentLength);
    }
}
