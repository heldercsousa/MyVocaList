using Microsoft.Extensions.DependencyInjection;
using MyVocaList.Contracts.Models;
using MyVocaList.Domain.Constants;
using MyVocaList.Domain.Entity;
using MyVocaList.Domain.Exceptions;
using MyVocaList.Domain.RepositoryInterface;
using MyVocaList.Domain.ServicesInterfaces;
using MyVocaList.Domain.UnitOfWork;
using MyVocaList.Extensions.Strings;
using System.Text.RegularExpressions;

namespace MyVocaList.Services;

/// <inheritdoc />
public class PersonService : IPersonService
{
    private readonly IPersonRepository _personRepository;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<PersonService> _logger;

    public int MaxInputLength => 200;
    public int MaxDatabaseLength => 250;
    public int ShowCounterAt => 180;

    /// <summary>BUG-088 REQ-DUP-01/02/06: shown when the identity (name, email, birthday) is already registered.</summary>
    public const string DuplicateIdentityMessage = "This singer is already registered.";

    /// <summary>BUG-088 REQ-DUP-03: shown when neither an email nor a birthday is given.</summary>
    public const string EmailOrBirthdayRequiredMessage = "Enter an email or a birthday.";

    public PersonService(IPersonRepository personRepository, IUnitOfWork uow, ILogger<PersonService> logger)
    {
        _personRepository = personRepository;
        _uow = uow;
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
    public (bool isValid, string message) ValidateBirthday(int? birthDay, int? birthMonth)
    {
        // Birthday is optional: both null is valid (REQ-BDAY-04, REQ-BDAY-05)
        if (!birthDay.HasValue && !birthMonth.HasValue)
            return (true, "");

        // Exactly one of day or month is an incomplete birthday
        if (!birthDay.HasValue || !birthMonth.HasValue)
            return (false, "Select both day and month");

        if (birthMonth.Value < 1 || birthMonth.Value > 12)
            return (false, "Month must be between 1 and 12");

        // February allows 29 (no year is stored); DaysInMonth is the Domain source of truth
        if (birthDay.Value < 1 || birthDay.Value > BirthdayRules.DaysInMonth(birthMonth.Value))
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
        string fullName, int? birthDay = null, int? birthMonth = null, string email = null,
        CancellationToken cancellationToken = default)
    {
        var nameValidation = ValidateNameInput(fullName);
        if (!nameValidation.isValid)
            return (false, nameValidation.message, null);

        var birthdayValidation = ValidateBirthday(birthDay, birthMonth);
        if (!birthdayValidation.isValid)
            return (false, birthdayValidation.message, null);

        var emailValidation = ValidateEmail(email);
        if (!emailValidation.isValid)
            return (false, emailValidation.message, null);

        // BUG-088 REQ-DUP-03: email or birthday required (checked before any save).
        var contactValidation = ValidateContact(email, birthDay, birthMonth);
        if (!contactValidation.isValid)
            return (false, contactValidation.message, null);

        try
        {
            return await _uow.ExecuteAsync<(bool success, string message, Person? person)>(async sp =>
            {
                // REQ-UOW-28: resolved from the lambda's own scope — never the constructor field.
                var personRepository = sp.GetRequiredService<IPersonRepository>();

                if (!string.IsNullOrWhiteSpace(email))
                {
                    // EF applies Person.Email's ValueConverter (design.md § D3) to this WHERE parameter
                    // too, so an untrimmed check value still matches trimmed stored rows.
                    var emailTaken = await personRepository.IsEmailTakenAsync(email, cancellationToken: cancellationToken);
                    if (emailTaken)
                        return (false, "Email already registered to another singer.", null);
                }

                // BUG-088 REQ-DUP-01: the identity (name + email + birthday day/month) must be new.
                var identityTaken = await personRepository.IdentityExistsAsync(
                    fullName, email, birthDay, birthMonth, null, cancellationToken);
                if (identityTaken)
                    return (false, DuplicateIdentityMessage, null);

                // Person.FullName/Email trimming is enforced by the EF Core ValueConverter configured in
                // PersonConfiguration (design.md § D3) — not here. The birthday is validated above (REQ-BDAY-05).
                var person = new Person(fullName)
                {
                    BirthDay = birthDay,
                    BirthMonth = birthMonth,
                    Email = string.IsNullOrWhiteSpace(email) ? null : email
                };
                // DB collation (NOCASE_NOACCENT) handles case/accent normalization at query time.

                await personRepository.AddAsync(person);
                // SaveChangesAsync deleted — the single save is owned by IUnitOfWork (REQ-UOW-10).

                return (true, $"{fullName.Trim()} registered successfully!", person);
            }, cancellationToken);
        }
        catch (ConstraintViolationException ex)
        {
            // BUG-088 REQ-DUP-06: a race or a constraint the pre-checks missed. Expected business failure → tuple.
            _logger.LogWarning(ex, "Create singer rejected by a database constraint");
            return (false, DuplicateIdentityMessage, null);
        }
    }

    /// <inheritdoc />
    public Task<Person?> GetPersonByIdAsync(int id, CancellationToken cancellationToken = default)
        => _uow.ExecuteReadAsync<Person?>(async sp =>
        {
            // REQ-UOW-37: resolved from the lambda's own scope — never the constructor field.
            var personRepository = sp.GetRequiredService<IPersonRepository>();
            return await personRepository.GetByIdAsync(id);
        }, cancellationToken);

    /// <inheritdoc />
    public Task<Person?> GetPersonByNameAsync(string name, CancellationToken cancellationToken = default)
        => _uow.ExecuteReadAsync<Person?>(async sp =>
        {
            // REQ-UOW-37: resolved from the lambda's own scope — never the constructor field.
            var personRepository = sp.GetRequiredService<IPersonRepository>();
            return await personRepository.GetByFullNameAsync(name, cancellationToken);
        }, cancellationToken);

    /// <inheritdoc />
    public Task<IEnumerable<Person>> SearchPersonsAsync(string searchTerm, int maxResults = SearchConstants.MaxSearchResults, CancellationToken cancellationToken = default)
    {
        // REQ-UOW-41: the short-circuit guard stays OUTSIDE the lambda — a too-short search term
        // makes no database call and must not create a DI scope.
        searchTerm = searchTerm.NormalizeSearchQuery();
        if (searchTerm.Length < SearchConstants.MinimumLocalQueryLength)
            return Task.FromResult<IEnumerable<Person>>([]);

        return _uow.ExecuteReadAsync<IEnumerable<Person>>(async sp =>
        {
            // REQ-UOW-37: resolved from the lambda's own scope — never the constructor field.
            var personRepository = sp.GetRequiredService<IPersonRepository>();
            return await personRepository.SearchByNameStartsWithAsync(searchTerm, maxResults, cancellationToken);
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IEnumerable<Person>> SearchPersonsStartsWithAsync(string searchTerm, int maxResults = SearchConstants.MaxSearchResults, CancellationToken cancellationToken = default)
    {
        // REQ-UOW-41: the short-circuit guard stays OUTSIDE the lambda — a too-short search term
        // makes no database call and must not create a DI scope.
        searchTerm = searchTerm.NormalizeSearchQuery();
        if (searchTerm.Length < SearchConstants.MinimumLocalQueryLength)
            return Task.FromResult<IEnumerable<Person>>([]);

        return _uow.ExecuteReadAsync<IEnumerable<Person>>(async sp =>
        {
            // REQ-UOW-37: resolved from the lambda's own scope — never the constructor field.
            var personRepository = sp.GetRequiredService<IPersonRepository>();
            return await personRepository.SearchByNameStartsWithAsync(searchTerm, maxResults, cancellationToken);
        }, cancellationToken);
    }

    #endregion

    #region List and Mutation Operations

    /// <inheritdoc />
    public Task<(IEnumerable<PersonListItemDto> items, int totalCount)> GetPagedPersonsForListAsync(
        int pageNumber, int pageSize, string query = null, CancellationToken cancellationToken = default)
    {
        query = string.IsNullOrWhiteSpace(query) ? null : query.NormalizeSearchQuery();

        return _uow.ExecuteReadAsync<(IEnumerable<PersonListItemDto> items, int totalCount)>(async sp =>
        {
            // REQ-UOW-37: resolved from the lambda's own scope — never the constructor field.
            var personRepository = sp.GetRequiredService<IPersonRepository>();

            var (persons, totalCount) = await personRepository.GetPagedAsync(
                pageNumber, pageSize, query, cancellationToken);

            var dtos = persons.Select(p => new PersonListItemDto
            {
                Id = p.Id,
                FullName = p.FullName,
                BirthDay = p.BirthDay,
                BirthMonth = p.BirthMonth,
                Email = p.Email,
                Participations = p.Participations,
                Absences = p.Absences
            });

            return (dtos, totalCount);
        }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<(bool success, string message)> UpdatePersonAsync(
        int id, string fullName, int? birthDay, int? birthMonth, string email,
        CancellationToken cancellationToken = default)
    {
        var nameValidation = ValidateNameInput(fullName);
        if (!nameValidation.isValid)
            return (false, nameValidation.message);

        var birthdayValidation = ValidateBirthday(birthDay, birthMonth);
        if (!birthdayValidation.isValid)
            return (false, birthdayValidation.message);

        var emailValidation = ValidateEmail(email);
        if (!emailValidation.isValid)
            return (false, emailValidation.message);

        // BUG-088 REQ-DUP-03: email or birthday required (checked before any save).
        var contactValidation = ValidateContact(email, birthDay, birthMonth);
        if (!contactValidation.isValid)
            return (false, contactValidation.message);

        try
        {
            return await _uow.ExecuteAsync<(bool success, string message)>(async sp =>
            {
                // REQ-UOW-28: resolved from the lambda's own scope — never the constructor field.
                var personRepository = sp.GetRequiredService<IPersonRepository>();

                var person = await personRepository.GetByIdAsync(id);
                if (person == null)
                    return (false, "Singer not found.");

                if (!string.IsNullOrWhiteSpace(email))
                {
                    // EF applies Person.Email's ValueConverter (design.md § D3) to this WHERE parameter
                    // too, so an untrimmed check value still matches trimmed stored rows.
                    var emailTaken = await personRepository.IsEmailTakenAsync(email, id, cancellationToken);
                    if (emailTaken)
                        return (false, "Email already registered to another singer.");
                }

                // BUG-088 REQ-DUP-02: another person (not this one) must not already have the resulting identity.
                var identityTaken = await personRepository.IdentityExistsAsync(
                    fullName, email, birthDay, birthMonth, id, cancellationToken);
                if (identityTaken)
                    return (false, DuplicateIdentityMessage);

                // Person.FullName/Email trimming is enforced by the EF Core ValueConverter configured in
                // PersonConfiguration (design.md § D3) — not here. The birthday is validated above (REQ-BDAY-05).
                person.FullName = fullName;
                person.BirthDay = birthDay;
                person.BirthMonth = birthMonth;
                person.Email = string.IsNullOrWhiteSpace(email) ? null : email;
                // Explicit UpdateAsync: GetByIdAsync's FindAsync only returns an already-tracked
                // instance when one exists in THIS unit of work's local cache. Since each unit of work
                // gets its own freshly-scoped AppDbContext (REQ-UOW-28), a person fetched here was never
                // previously tracked, and the DbContext-wide QueryTrackingBehavior.NoTracking default
                // means FindAsync returns it detached — mutating a detached instance is a silent no-op
                // at SaveChangesAsync time without this call (mirrors ArtistService.UpdateArtistAsync).
                await personRepository.UpdateAsync(person);
                // SaveChangesAsync deleted — the single save is owned by IUnitOfWork (REQ-UOW-10).

                return (true, $"{fullName.Trim()} updated successfully!");
            }, cancellationToken);
        }
        catch (ConstraintViolationException ex)
        {
            // BUG-088 REQ-DUP-06: a race or a constraint the pre-checks missed. Expected business failure → tuple.
            _logger.LogWarning(ex, "Update singer {PersonId} rejected by a database constraint", id);
            return (false, DuplicateIdentityMessage);
        }
    }

    /// <inheritdoc />
    public (bool isValid, string message) ValidateContact(string? email, int? birthDay, int? birthMonth)
    {
        // BUG-088 REQ-DUP-03: identity needs an email or a birthday (day and month both set).
        if (string.IsNullOrWhiteSpace(email) && !birthDay.HasValue && !birthMonth.HasValue)
            return (false, EmailOrBirthdayRequiredMessage);

        return (true, "");
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Person>> GetSameNamePersonsAsync(
        string fullName, string? email, int? birthDay, int? birthMonth, int? excludeId,
        CancellationToken cancellationToken = default)
    {
        // REQ-DUP-09 soft warning: read only, never blocks a save.
        return await _uow.ExecuteReadAsync<IReadOnlyList<Person>>(async sp =>
        {
            // REQ-UOW-37: resolved from the lambda's own scope — never the constructor field.
            var personRepository = sp.GetRequiredService<IPersonRepository>();
            return await personRepository.GetSameNameDifferentIdentityAsync(
                fullName, email, birthDay, birthMonth, excludeId, cancellationToken);
        }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<(bool success, string message)> DeletePersonsAsync(
        IEnumerable<int> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        if (idList.Count == 0)
            return (true, "0 singer(s) successfully removed!");

        return await _uow.ExecuteAsync<(bool success, string message)>(async sp =>
        {
            // REQ-UOW-28: resolved from the lambda's own scope — never the constructor field.
            var personRepository = sp.GetRequiredService<IPersonRepository>();

            foreach (var id in idList)
            {
                var person = await personRepository.GetByIdAsync(id);
                if (person != null)
                    await personRepository.DeleteAsync(person);
            }
            // SaveChangesAsync deleted — the single save is owned by IUnitOfWork (REQ-UOW-10).

            return (true, $"{idList.Count} singer(s) successfully removed!");
        }, cancellationToken);
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
        // Error only when ValidateNameInput would reject the same length (counter threshold
        // alignment, Form Validation Standard) — exactly MaxInputLength is still valid.
        bool isError = currentLength > MaxInputLength;
        return (text, isWarning, isError);
    }

    #endregion
}
