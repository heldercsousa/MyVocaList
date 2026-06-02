using MyVocaList.Contracts.Models;
using MyVocaList.Domain.Entity;
using MyVocaList.Domain.RepositoryInterface;
using MyVocaList.Domain.ServicesInterfaces;
using System.Text.RegularExpressions;

namespace MyVocaList.Services;

/// <inheritdoc />
public class PersonService : IPersonService
{
    private readonly IPersonRepository _personRepository;
    private readonly ILogger<PersonService> _logger;

    public int MaxInputLength => 200;
    public int MaxDatabaseLength => 250;
    public int ShowCounterAt => 180;

    public PersonService(IPersonRepository personRepository, ILogger<PersonService> logger)
    {
        _personRepository = personRepository;
        _logger = logger;
    }

    #region Validation

    /// <inheritdoc />
    public (bool isValid, string message) ValidateNameInput(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return (false, "Name is required");

        name = name.Trim();

        if (name.Length > MaxInputLength)
            return (false, $"Name too long. Maximum {MaxInputLength} characters.");

        if (name.Length < 2)
            return (false, "Name too short. Minimum 2 characters.");

        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
            return (false, "Enter first and last name.");

        if (parts[^1].Length < 2)
            return (false, "Last name must have at least 2 characters.");

        return (true, "");
    }

    /// <inheritdoc />
    public (bool isValid, string message) ValidateNameForDatabase(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return (false, "Name is required");

        name = name.Trim();

        if (name.Length > MaxDatabaseLength)
            return (false, $"Name exceeds database limit ({MaxDatabaseLength} characters)");

        var inputValidation = ValidateNameInput(name);
        return inputValidation.isValid ? (true, "") : (false, "Invalid name");
    }

    /// <inheritdoc />
    public (bool isValid, string message) ValidateBirthday(string birthday)
    {
        // Birthday is optional — empty/null is valid
        if (string.IsNullOrWhiteSpace(birthday))
            return (true, "");

        var regex = new Regex(@"^(\d{1,2})/(\d{1,2})$");
        var match = regex.Match(birthday.Trim());

        if (!match.Success)
            return (false, "Use DD/MM format (e.g.: 15/03)");

        if (!int.TryParse(match.Groups[1].Value, out int day) ||
            !int.TryParse(match.Groups[2].Value, out int month))
            return (false, "Invalid date");

        if (day < 1 || day > 31)
            return (false, "Day must be between 1 and 31");

        if (month < 1 || month > 12)
            return (false, "Month must be between 1 and 12");

        if ((month == 2 && day > 29) ||
            ((month == 4 || month == 6 || month == 9 || month == 11) && day > 30))
            return (false, "Invalid date for this month");

        return (true, "");
    }

    /// <inheritdoc />
    public (bool isValid, string message) ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return (true, "");   // Optional

        email = email.Trim();

        var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        if (!emailRegex.IsMatch(email))
            return (false, "Invalid email");

        if (email.Length > 100)
            return (false, "Email too long");

        return (true, "");
    }

    #endregion

    #region CRUD Operations

    /// <inheritdoc />
    public async Task<(bool success, string message, Person? person)> CreatePersonAsync(
        string fullName, string birthday = null, string email = null,
        CancellationToken cancellationToken = default)
    {
        var nameValidation = ValidateNameInput(fullName);
        if (!nameValidation.isValid)
            return (false, nameValidation.message, null);

        var birthdayValidation = ValidateBirthday(birthday);
        if (!birthdayValidation.isValid)
            return (false, birthdayValidation.message, null);

        var emailValidation = ValidateEmail(email);
        if (!emailValidation.isValid)
            return (false, emailValidation.message, null);

        if (!string.IsNullOrWhiteSpace(email))
        {
            var emailTaken = await _personRepository.IsEmailTakenAsync(email.Trim(), cancellationToken: cancellationToken);
            if (emailTaken)
                return (false, "Email already registered to another singer.", null);
        }

        var trimmedName = fullName.Trim();
        var person = new Person(trimmedName)
        {
            BirthdayDayMonth = string.IsNullOrWhiteSpace(birthday) ? null : birthday.Trim(),
            Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim()
        };
        // DB collation (NOCASE_NOACCENT) handles case/accent normalization at query time.

        await _personRepository.AddAsync(person);
        await _personRepository.SaveChangesAsync();

        return (true, $"{trimmedName} registered successfully!", person);
    }

    /// <inheritdoc />
    public async Task<Person?> GetPersonByIdAsync(int id, CancellationToken cancellationToken = default)
        => await _personRepository.GetByIdAsync(id);

    /// <inheritdoc />
    public async Task<Person?> GetPersonByNameAsync(string name, CancellationToken cancellationToken = default)
        => await _personRepository.GetByFullNameAsync(name, cancellationToken);

    /// <inheritdoc />
    public async Task<IEnumerable<Person>> SearchPersonsAsync(string searchTerm, int maxResults = 5, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm) || searchTerm.Length < 2)
            return [];
        return await _personRepository.SearchByNameStartsWithAsync(searchTerm, maxResults, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Person>> SearchPersonsStartsWithAsync(string searchTerm, int maxResults = 3, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm) || searchTerm.Length < 2)
            return [];
        return await _personRepository.SearchByNameStartsWithAsync(searchTerm, maxResults, cancellationToken);
    }

    #endregion

    #region List and Mutation Operations

    /// <inheritdoc />
    public async Task<(IEnumerable<PersonListItemDto> items, int totalCount)> GetPagedPersonsForListAsync(
        int pageNumber, int pageSize, string query = null, CancellationToken cancellationToken = default)
    {
        var (persons, totalCount) = await _personRepository.GetPagedAsync(
            pageNumber, pageSize, query, cancellationToken);

        var dtos = persons.Select(p => new PersonListItemDto
        {
            Id = p.Id,
            FullName = p.FullName,
            BirthdayDayMonth = p.BirthdayDayMonth,
            Email = p.Email,
            Participations = p.Participations,
            Absences = p.Absences
        });

        return (dtos, totalCount);
    }

    /// <inheritdoc />
    public async Task<(bool success, string message)> UpdatePersonAsync(
        int id, string fullName, string birthday = null, string email = null,
        CancellationToken cancellationToken = default)
    {
        var person = await _personRepository.GetByIdAsync(id);
        if (person == null)
            return (false, "Singer not found.");

        var nameValidation = ValidateNameInput(fullName);
        if (!nameValidation.isValid)
            return (false, nameValidation.message);

        var birthdayValidation = ValidateBirthday(birthday);
        if (!birthdayValidation.isValid)
            return (false, birthdayValidation.message);

        var emailValidation = ValidateEmail(email);
        if (!emailValidation.isValid)
            return (false, emailValidation.message);

        if (!string.IsNullOrWhiteSpace(email))
        {
            var emailTaken = await _personRepository.IsEmailTakenAsync(email.Trim(), id, cancellationToken);
            if (emailTaken)
                return (false, "Email already registered to another singer.");
        }

        var trimmedName = fullName.Trim();
        person.FullName = trimmedName;
        person.BirthdayDayMonth = string.IsNullOrWhiteSpace(birthday) ? null : birthday.Trim();
        person.Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();

        await _personRepository.SaveChangesAsync();

        return (true, $"{trimmedName} updated successfully!");
    }

    /// <inheritdoc />
    public async Task<(bool success, string message)> DeletePersonsAsync(
        IEnumerable<int> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        if (idList.Count == 0)
            return (true, "0 singer(s) successfully removed!");

        foreach (var id in idList)
        {
            var person = await _personRepository.GetByIdAsync(id);
            if (person != null)
                await _personRepository.DeleteAsync(person);
        }

        await _personRepository.SaveChangesAsync();
        return (true, $"{idList.Count} singer(s) successfully removed!");
    }

    #endregion

    #region Utilities

    /// <inheritdoc />
    public bool ShouldShowCharacterCounter(int currentLength) => currentLength > ShowCounterAt;

    /// <inheritdoc />
    public (string text, bool isWarning, bool isError) GetCharacterCounterInfo(int currentLength)
    {
        string text = $"{currentLength}/{MaxInputLength}";
        bool isWarning = currentLength > 190;
        bool isError = currentLength >= MaxInputLength;
        return (text, isWarning, isError);
    }

    #endregion
}
