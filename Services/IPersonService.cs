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
        Task<(bool success, string message, Pessoa? person)> CreatePersonAsync(string fullName, string birthday = null, string email = null);
        Task<Pessoa?> GetPersonByIdAsync(int id);
        Task<Pessoa?> GetPersonByNameAsync(string name);
        Task<IEnumerable<Pessoa>> SearchPersonsAsync(string searchTerm, int maxResults = 5);
        Task<IEnumerable<Pessoa>> SearchPersonsStartsWithAsync(string searchTerm, int maxResults = 3);

        // Utilities
        (string text, bool isWarning, bool isError) GetCharacterCounterInfo(int currentLength);
        bool ShouldShowCharacterCounter(int currentLength);
    }
}
