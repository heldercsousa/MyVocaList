using MyVocaList.Domain.Entity;
using MyVocaList.Domain.RepositoryInterface;
using System.Text.RegularExpressions;

namespace MyVocaList.Services
{
    /// <summary>
    /// Service for business operations related to persons
    /// </summary>
    public class PersonService : IPersonService
    {
        private readonly IPersonRepository _personRepository;
        private readonly ITextNormalizationService _textNormalizer;

        // Hybrid validation constants
        public int MaxInputLength => 200;  // Input limit (UX friendly)
        public int MaxDatabaseLength => 250;     // Database limit (extra security)
        public int ShowCounterAt => 180;   // When to show counter

        public PersonService(
            IPersonRepository personRepository,
            ITextNormalizationService textNormalizer)
        {
            _personRepository = personRepository;
            _textNormalizer = textNormalizer;
        }

        #region Validations

        public (bool isValid, string message) ValidateNameInput(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return (false, "Name is required");
            }

            name = name.Trim();

            if (name.Length > MaxInputLength)
            {
                return (false, $"Name too long. Maximum {MaxInputLength} characters.");
            }

            if (name.Length < 2)
            {
                return (false, "Name too short. Minimum 2 characters.");
            }

            string[] parts = name.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                return (false, "Enter first and last name.");
            }

            if (parts[parts.Length - 1].Length < 2)
            {
                return (false, "Last name must have at least 2 characters.");
            }

            return (true, "");
        }

        public (bool isValid, string message) ValidateNameForDatabase(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return (false, "Name is required");
            }

            name = name.Trim();

            if (name.Length > MaxDatabaseLength)
            {
                return (false, $"Name exceeds database limit ({MaxDatabaseLength} characters)");
            }

            // Use basic validation
            var inputValidation = ValidateNameInput(name);
            return inputValidation.isValid ? (true, "") : (false, "Invalid name");
        }

        public (bool isValid, string message) ValidateBirthday(string birthday)
        {
            if (string.IsNullOrWhiteSpace(birthday))
            {
                return (false, "Birthday is required");
            }

            // Accepted format: DD/MM
            var regex = new Regex(@"^(\d{1,2})/(\d{1,2})$");
            var match = regex.Match(birthday.Trim());

            if (!match.Success)
            {
                return (false, "Use DD/MM format (e.g.: 15/03)");
            }

            if (!int.TryParse(match.Groups[1].Value, out int day) ||
                !int.TryParse(match.Groups[2].Value, out int month))
            {
                return (false, "Invalid date");
            }

            if (day < 1 || day > 31)
            {
                return (false, "Day must be between 1 and 31");
            }

            if (month < 1 || month > 12)
            {
                return (false, "Month must be between 1 and 12");
            }

            // Basic validation of days per month
            if ((month == 2 && day > 29) ||
                ((month == 4 || month == 6 || month == 9 || month == 11) && day > 30))
            {
                return (false, "Invalid date for this month");
            }

            return (true, "");
        }

        public (bool isValid, string message) ValidateEmail(string email)
        {
            // Email is optional
            if (string.IsNullOrWhiteSpace(email))
            {
                return (true, ""); // Valid if empty (optional)
            }

            email = email.Trim();

            // Basic email validation
            var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
            if (!emailRegex.IsMatch(email))
            {
                return (false, "Invalid email");
            }

            if (email.Length > 100)
            {
                return (false, "Email too long");
            }

            return (true, "");
        }

        #endregion

        #region Registration and Search Operations

        public async Task<(bool success, string message, Person? person)> CreatePersonAsync(
            string fullName, string birthday = null, string email = null)
        {
            try
            {
                // Create new person
                var person = new Person(fullName)
                {
                    BirthdayDayMonth = birthday?.Trim(),
                    Email = email?.Trim()
                };

                // Set normalized name using utility
                var normalizedName = _textNormalizer.NormalizeName(fullName);
                person.SetNormalizedName(normalizedName);

                // Save to repository
                await _personRepository.AddAsync(person);
                await _personRepository.SaveChangesAsync();

                return (true, $"{fullName} registered successfully!", person);
            }
            catch (Exception ex)
            {
                return (false, $"Error registering person: {ex.Message}", null);
            }
        }

        public async Task<Person?> GetPersonByIdAsync(int id)
        {
            return await _personRepository.GetByIdAsync(id);
        }

        public async Task<Person?> GetPersonByNameAsync(string name)
        {
            return await _personRepository.GetByFullNameAsync(name);
        }

        public async Task<IEnumerable<Person>> SearchPersonsAsync(string searchTerm, int maxResults = 5)
        {
            if (string.IsNullOrWhiteSpace(searchTerm) || searchTerm.Length < 2)
            {
                return new List<Person>();
            }

            return await _personRepository.SearchByNameAsync(searchTerm, maxResults);
        }

        public async Task<IEnumerable<Person>> SearchPersonsStartsWithAsync(string searchTerm, int maxResults = 3)
        {
            if (string.IsNullOrWhiteSpace(searchTerm) || searchTerm.Length < 2)
            {
                return new List<Person>();
            }

            return await _personRepository.SearchByNameStartsWithAsync(searchTerm, maxResults);
        }

        #endregion

        #region Utilities

        public bool ShouldShowCharacterCounter(int currentLength)
        {
            return currentLength > ShowCounterAt;
        }

        public (string text, bool isWarning, bool isError) GetCharacterCounterInfo(int currentLength)
        {
            string text = $"{currentLength}/{MaxInputLength}";
            bool isWarning = currentLength > 190;
            bool isError = currentLength >= MaxInputLength;

            return (text, isWarning, isError);
        }

        #endregion
    }
}
