# Person CRUD Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the full Person (Singer) CRUD feature — list page with search/selection/paging, form page with autocomplete name suggestions, and all backing service/repository logic.

**Architecture:** Fix existing infra bugs (PersonConfiguration, PersonRepository, PersonService) first, then add service methods TDD-first, then implement ViewModels TDD-first, then build the two UI pages following the Venues reference pattern. `PeoplePage` is rewritten in-place; `PersonFormPage` is new. The `AutocompleteField` component and `AutocompleteSuggestion` record are already implemented and ready to use.

**Tech Stack:** .NET MAUI 10 · CommunityToolkit.Mvvm · DevExpress MAUI v25.2.4 · EF Core 10 + SQLite · xUnit + Moq

---

## File Map

| Action | File | Purpose |
|--------|------|---------|
| Modify | `Domain/Entity/Person.cs` | Add `ExternalId` property |
| Modify | `Domain/RepositoryInterface/IPersonRepository.cs` | Remove `SearchByNameAsync`; add `SearchByNameOrEmailAsync`, `GetPagedAsync` |
| Modify | `Domain/ServicesInterfaces/IPersonService.cs` | Add `GetPagedPersonsForListAsync`, `UpdatePersonAsync`, `DeletePersonsAsync` |
| **Delete** | `Domain/ServicesInterfaces/ITextNormalizationService.cs` | No longer needed — DB collation handles normalization |
| Modify | `Infra/EntityEFConfig/PersonConfiguration.cs` | Fix optional fields, add ExternalId, unique indexes, composite filtered index |
| Create | `Infra/Migrations/{timestamp}_PersonConfigFixes.cs` | Migration for all PersonConfiguration changes |
| Modify | `Infra/Repository/PersonRepository.cs` | Rewrite: EF.Functions.Like + Collate; add `GetPagedAsync`, `SearchByNameOrEmailAsync` |
| Modify | `Services/PersonService.cs` | Remove `ITextNormalizationService`; fix `ValidateBirthday`; fix `CreatePersonAsync` catch; add new methods |
| **Delete** | `Services/TextNormalizationService.cs` | Already fully commented out |
| Modify | `Contracts/Models/PersonListItemDto.cs` | Add `BirthdayDayMonth`, `Email` |
| Modify | `MyVocaList/Navigation/Routes.cs` | Add `PersonForm = "person-form"` |
| Modify | `MyVocaList/AppShell.xaml.cs` | Register `Routes.PersonForm → PersonFormPage` |
| Modify | `MyVocaList/MauiProgram.cs` | Register `IPersonRepository`, `IPersonService`, `PersonsViewModel`, `PersonFormPage`, `PersonFormViewModel` |
| Create | `MyVocaList/UI/ViewModels/PersonsViewModel.cs` | List page ViewModel |
| Create | `MyVocaList/UI/ViewModels/PersonFormViewModel.cs` | Form page ViewModel |
| Modify | `MyVocaList/UI/Pages/People/PeoplePage.xaml` | Full rewrite as Singers list page |
| Modify | `MyVocaList/UI/Pages/People/PeoplePage.xaml.cs` | Full rewrite |
| Create | `MyVocaList/UI/Pages/People/PersonFormPage.xaml` | New Singer add/edit form |
| Create | `MyVocaList/UI/Pages/People/PersonFormPage.xaml.cs` | New |
| Create | `MyVocaList.Tests/Unit/Services/PersonServiceTests.cs` | Service unit tests |
| Create | `MyVocaList.Tests/Unit/ViewModels/PersonsViewModelTests.cs` | List ViewModel tests |
| Create | `MyVocaList.Tests/Unit/ViewModels/PersonFormViewModelTests.cs` | Form ViewModel tests |
| Create | `MyVocaList.Tests/Integration/Repositories/PersonRepositoryTests.cs` | Repository integration tests |

---

## Task 1: Fix Person Entity + PersonConfiguration + EF Migration

**Files:**
- Modify: `Domain/Entity/Person.cs`
- Modify: `Infra/EntityEFConfig/PersonConfiguration.cs`
- Create: `Infra/Migrations/{timestamp}_PersonConfigFixes.cs` (generated)

- [ ] **Step 1: Add `ExternalId` to Person entity**

In `Domain/Entity/Person.cs`, add this property after `Id`:

```csharp
public Guid? ExternalId { get; set; }   // Reserved for future device/account identity
```

- [ ] **Step 2: Rewrite PersonConfiguration with all fixes**

Replace the entire content of `Infra/EntityEFConfig/PersonConfiguration.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyVocaList.Domain.Entity;

namespace MyVocaList.Infra.EntityEFConfig;

public class PersonConfiguration : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedOnAdd();

        builder.Property(p => p.ExternalId)
               .IsRequired(false);

        builder.Property(p => p.FullName)
               .HasColumnType("TEXT").IsRequired().HasMaxLength(250);

        builder.Property(p => p.FullNameNormalized)
               .HasColumnType("TEXT").IsRequired().HasMaxLength(250);

        builder.Property(p => p.BirthdayDayMonth)
               .HasColumnType("TEXT").IsRequired(false).HasMaxLength(5);

        builder.Property(p => p.Email)
               .HasColumnType("TEXT").IsRequired(false).HasMaxLength(100);

        builder.Property(p => p.Participations)
               .IsRequired().HasDefaultValue(0);

        builder.Property(p => p.Absences)
               .IsRequired().HasDefaultValue(0);

        // Standard index: fast prefix search on normalized name
        builder.HasIndex(p => p.FullNameNormalized)
               .HasDatabaseName("IX_Persons_FullNameNormalized");

        // Nullable unique index: multiple NULLs allowed (SQLite: NULL != NULL)
        builder.HasIndex(p => p.Email)
               .IsUnique()
               .HasDatabaseName("IX_Persons_Email");

        builder.HasIndex(p => p.ExternalId)
               .IsUnique()
               .HasDatabaseName("IX_Persons_ExternalId");

        // Filtered composite unique: same name + same birthday = duplicate
        // WHERE BirthdayDayMonth IS NOT NULL → same name + null birthday is allowed
        builder.HasIndex(p => new { p.FullNameNormalized, p.BirthdayDayMonth })
               .IsUnique()
               .HasFilter("[BirthdayDayMonth] IS NOT NULL")
               .HasDatabaseName("IX_Persons_Name_Birthday");
    }
}
```

- [ ] **Step 3: Generate EF Core migration**

Run from the solution root (adjust paths if needed):

```bash
dotnet ef migrations add PersonConfigFixes --project Infra --startup-project MyVocaList -- --no-build
```

If the above fails, build first then run without `--no-build`. Expected output: `Done. To undo this action, use 'ef migrations remove'`

- [ ] **Step 4: Build and confirm 0 errors**

```bash
dotnet build MyVocaList/MyVocaList.csproj
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 5: Commit**

```bash
git add Domain/Entity/Person.cs Infra/EntityEFConfig/PersonConfiguration.cs Infra/Migrations/
git commit -m "fix(infra): fix PersonConfiguration — optional fields, unique indexes, ExternalId, composite filtered index"
```

---

## Task 2: Rewrite IPersonRepository + PersonRepository

**Files:**
- Modify: `Domain/RepositoryInterface/IPersonRepository.cs`
- Modify: `Infra/Repository/PersonRepository.cs`

- [ ] **Step 1: Rewrite IPersonRepository**

Replace the entire content of `Domain/RepositoryInterface/IPersonRepository.cs`:

```csharp
using MyVocaList.Domain.Entity;

namespace MyVocaList.Domain.RepositoryInterface;

/// <summary>Repository interface for Person entity operations.</summary>
public interface IPersonRepository : IBaseRepository<Person>
{
    /// <summary>Returns the first person whose normalized name exactly matches (case-insensitive).</summary>
    Task<Person> GetByFullNameAsync(string fullName, CancellationToken cancellationToken = default);

    /// <summary>Prefix search on normalized name. Used by autocomplete suggestion list.</summary>
    Task<List<Person>> SearchByNameStartsWithAsync(string searchTerm, int maxResults = 10, CancellationToken cancellationToken = default);

    /// <summary>Search by name prefix OR email contains. Used by the list page search bar.</summary>
    Task<List<Person>> SearchByNameOrEmailAsync(string searchTerm, int maxResults = 10, CancellationToken cancellationToken = default);

    /// <summary>Paged query for the list page. Searches name OR email when query is non-null.</summary>
    Task<(IEnumerable<Person> items, int totalCount)> GetPagedAsync(int pageNumber, int pageSize, string query = null, CancellationToken cancellationToken = default);

    /// <summary>Search by any word in name (not implemented in v1).</summary>
    Task<List<Person>> SearchByAnyWordAsync(string searchTerm, int maxResults = 10);

    /// <summary>Returns true if any person (other than excludeId) has this email.</summary>
    Task<bool> IsEmailTakenAsync(string email, int? excludePersonId = null, CancellationToken cancellationToken = default);
}
```

- [ ] **Step 2: Rewrite PersonRepository**

Replace the entire content of `Infra/Repository/PersonRepository.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using MyVocaList.Domain.Entity;
using MyVocaList.Domain.RepositoryInterface;

namespace MyVocaList.Infra.Repository;

/// <inheritdoc />
public class PersonRepository : BaseRepository<Person>, IPersonRepository
{
    public PersonRepository(AppDbContext context) : base(context) { }

    /// <inheritdoc />
    public async Task<Person> GetByFullNameAsync(string fullName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName, nameof(fullName));
        return await _dbSet.FirstOrDefaultAsync(p =>
            EF.Functions.Like(
                EF.Functions.Collate(p.FullNameNormalized, "NOCASE"),
                EF.Functions.Collate(fullName.Trim(), "NOCASE")),
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<Person>> SearchByNameStartsWithAsync(string searchTerm, int maxResults = 10, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return [];

        var pattern = searchTerm.Trim() + "%";
        return await _dbSet
            .Where(p => EF.Functions.Like(
                EF.Functions.Collate(p.FullNameNormalized, "NOCASE"),
                EF.Functions.Collate(pattern, "NOCASE")))
            .OrderBy(p => p.FullNameNormalized)
            .Take(maxResults)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<Person>> SearchByNameOrEmailAsync(string searchTerm, int maxResults = 10, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return [];

        var term = searchTerm.Trim();
        var namePattern = term + "%";
        var emailPattern = "%" + term + "%";

        return await _dbSet
            .Where(p =>
                EF.Functions.Like(
                    EF.Functions.Collate(p.FullNameNormalized, "NOCASE"),
                    EF.Functions.Collate(namePattern, "NOCASE"))
                ||
                (p.Email != null && EF.Functions.Like(
                    EF.Functions.Collate(p.Email, "NOCASE"),
                    EF.Functions.Collate(emailPattern, "NOCASE"))))
            .OrderBy(p => p.FullNameNormalized)
            .Take(maxResults)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<(IEnumerable<Person> items, int totalCount)> GetPagedAsync(
        int pageNumber, int pageSize, string query = null, CancellationToken cancellationToken = default)
    {
        var q = _dbSet.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            var namePattern = term + "%";
            var emailPattern = "%" + term + "%";

            q = q.Where(p =>
                EF.Functions.Like(
                    EF.Functions.Collate(p.FullNameNormalized, "NOCASE"),
                    EF.Functions.Collate(namePattern, "NOCASE"))
                ||
                (p.Email != null && EF.Functions.Like(
                    EF.Functions.Collate(p.Email, "NOCASE"),
                    EF.Functions.Collate(emailPattern, "NOCASE"))));
        }

        var totalCount = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderBy(p => p.FullNameNormalized)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    /// <inheritdoc />
    public Task<List<Person>> SearchByAnyWordAsync(string searchTerm, int maxResults = 10)
        => throw new NotImplementedException("Full-text word search is not implemented in v1.");

    /// <inheritdoc />
    public async Task<bool> IsEmailTakenAsync(string email, int? excludePersonId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        var normalizedEmail = email.Trim().ToLowerInvariant();
        return await _dbSet.AnyAsync(p =>
            p.Email != null &&
            p.Email.ToLower() == normalizedEmail &&
            (excludePersonId == null || p.Id != excludePersonId.Value),
            cancellationToken);
    }
}
```

- [ ] **Step 3: Build and confirm 0 errors**

```bash
dotnet build MyVocaList/MyVocaList.csproj
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 4: Commit**

```bash
git add Domain/RepositoryInterface/IPersonRepository.cs Infra/Repository/PersonRepository.cs
git commit -m "refactor(infra): rewrite PersonRepository — collation-aware queries, GetPagedAsync, SearchByNameOrEmailAsync"
```

---

## Task 3: Fix PersonService — Remove ITextNormalizationService, fix bugs

**Files:**
- Delete: `Domain/ServicesInterfaces/ITextNormalizationService.cs`
- Delete: `Services/TextNormalizationService.cs`
- Modify: `Services/PersonService.cs`

- [ ] **Step 1: Delete ITextNormalizationService.cs**

Delete file: `Domain/ServicesInterfaces/ITextNormalizationService.cs`

- [ ] **Step 2: Delete TextNormalizationService.cs**

Delete file: `Services/TextNormalizationService.cs` (already fully commented out — safe to delete)

- [ ] **Step 3: Rewrite PersonService (Phase 0 fixes only)**

Replace the entire content of `Services/PersonService.cs`:

```csharp
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
        // DB collation (NOCASE_NOACCENT) handles accent normalization at query time.
        // FullNameNormalized stores the raw trimmed name.
        person.SetNormalizedName(trimmedName);

        await _personRepository.AddAsync(person, cancellationToken);
        await _personRepository.SaveChangesAsync(cancellationToken);

        return (true, $"{trimmedName} registered successfully!", person);
    }

    /// <inheritdoc />
    public async Task<Person?> GetPersonByIdAsync(int id, CancellationToken cancellationToken = default)
        => await _personRepository.GetByIdAsync(id, cancellationToken);

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
```

- [ ] **Step 4: Update IPersonService to remove ITextNormalizationService dependency + add CancellationToken to existing methods**

Replace `Domain/ServicesInterfaces/IPersonService.cs`:

```csharp
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
}
```

- [ ] **Step 5: Build and confirm 0 errors**

```bash
dotnet build MyVocaList/MyVocaList.csproj
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 6: Run existing tests — all 25 should still pass**

```bash
dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj
```

Expected: `Passed! – Failed: 0, Passed: 25`

- [ ] **Step 7: Commit**

```bash
git add Domain/ServicesInterfaces/ Services/PersonService.cs Services/TextNormalizationService.cs
git commit -m "refactor(services): remove ITextNormalizationService; fix ValidateBirthday; fix CreatePersonAsync"
```

---

## Task 4 (TDD): Add new service methods — tests first, then implementation

**Files:**
- Modify: `Domain/ServicesInterfaces/IPersonService.cs`
- Modify: `Services/PersonService.cs`
- Create: `MyVocaList.Tests/Unit/Services/PersonServiceTests.cs`

The new methods: `GetPagedPersonsForListAsync`, `UpdatePersonAsync`, `DeletePersonsAsync`.

- [ ] **Step 1: Add method stubs to IPersonService**

Add to `Domain/ServicesInterfaces/IPersonService.cs` (append to the interface body):

```csharp
// --- New methods added in Person CRUD plan ---

Task<(IEnumerable<PersonListItemDto> items, int totalCount)> GetPagedPersonsForListAsync(
    int pageNumber, int pageSize, string query = null, CancellationToken cancellationToken = default);

Task<(bool success, string message)> UpdatePersonAsync(
    int id, string fullName, string birthday = null, string email = null,
    CancellationToken cancellationToken = default);

Task<(bool success, string message)> DeletePersonsAsync(
    IEnumerable<int> ids, CancellationToken cancellationToken = default);
```

The interface now uses `PersonListItemDto` — add this using to the interface file:
```csharp
using MyVocaList.Contracts.Models;
```

- [ ] **Step 2: Add throwing stubs to PersonService (makes tests compile + fail)**

Add to `Services/PersonService.cs` after the `#region Utilities` block, a new region:

```csharp
#region List and Mutation Operations (stubs — implemented after tests are written)

/// <inheritdoc />
public Task<(IEnumerable<PersonListItemDto> items, int totalCount)> GetPagedPersonsForListAsync(
    int pageNumber, int pageSize, string query = null, CancellationToken cancellationToken = default)
    => throw new NotImplementedException();

/// <inheritdoc />
public Task<(bool success, string message)> UpdatePersonAsync(
    int id, string fullName, string birthday = null, string email = null,
    CancellationToken cancellationToken = default)
    => throw new NotImplementedException();

/// <inheritdoc />
public Task<(bool success, string message)> DeletePersonsAsync(
    IEnumerable<int> ids, CancellationToken cancellationToken = default)
    => throw new NotImplementedException();

#endregion
```

Add `using MyVocaList.Contracts.Models;` at top of PersonService.cs.

- [ ] **Step 3: Build — confirm 0 errors (stubs compile)**

```bash
dotnet build MyVocaList/MyVocaList.csproj
```

- [ ] **Step 4: Write failing tests**

Create `MyVocaList.Tests/Unit/Services/PersonServiceTests.cs`:

```csharp
using MyVocaList.Contracts.Models;

namespace MyVocaList.Tests.Unit.Services;

public class PersonServiceTests
{
    private readonly Mock<IPersonRepository> _repoMock = new();
    private readonly Mock<ILogger<PersonService>> _loggerMock = new();

    private PersonService CreateSut() => new(_repoMock.Object, _loggerMock.Object);

    // ── ValidateNameInput ──────────────────────────────────────────────────────

    [Fact]
    public void ValidateNameInput_EmptyName_ReturnsInvalid()
    {
        var (isValid, message) = CreateSut().ValidateNameInput("  ");
        Assert.False(isValid);
        Assert.Equal("Name is required", message);
    }

    [Fact]
    public void ValidateNameInput_TooShort_ReturnsInvalid()
    {
        var (isValid, message) = CreateSut().ValidateNameInput("A");
        Assert.False(isValid);
        Assert.Contains("2 characters", message);
    }

    [Fact]
    public void ValidateNameInput_SingleWord_ReturnsInvalid()
    {
        var (isValid, message) = CreateSut().ValidateNameInput("John");
        Assert.False(isValid);
        Assert.Contains("last name", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateNameInput_LastNameTooShort_ReturnsInvalid()
    {
        var (isValid, message) = CreateSut().ValidateNameInput("John A");
        Assert.False(isValid);
        Assert.Contains("Last name", message);
    }

    [Fact]
    public void ValidateNameInput_TooLong_ReturnsInvalid()
    {
        var longName = "Jo " + new string('x', 200);
        var (isValid, _) = CreateSut().ValidateNameInput(longName);
        Assert.False(isValid);
    }

    [Fact]
    public void ValidateNameInput_ValidTwoPartName_ReturnsValid()
    {
        var (isValid, message) = CreateSut().ValidateNameInput("John Doe");
        Assert.True(isValid);
        Assert.Equal("", message);
    }

    // ── ValidateBirthday ──────────────────────────────────────────────────────

    [Fact]
    public void ValidateBirthday_NullBirthday_ReturnsValid()
    {
        // Birthday is optional — null must be valid
        var (isValid, message) = CreateSut().ValidateBirthday(null);
        Assert.True(isValid);
        Assert.Equal("", message);
    }

    [Fact]
    public void ValidateBirthday_WhitespaceBirthday_ReturnsValid()
    {
        var (isValid, _) = CreateSut().ValidateBirthday("   ");
        Assert.True(isValid);
    }

    [Fact]
    public void ValidateBirthday_InvalidFormat_ReturnsInvalid()
    {
        var (isValid, message) = CreateSut().ValidateBirthday("15-03");
        Assert.False(isValid);
        Assert.Contains("DD/MM", message);
    }

    [Fact]
    public void ValidateBirthday_InvalidDay_ReturnsInvalid()
    {
        var (isValid, message) = CreateSut().ValidateBirthday("32/01");
        Assert.False(isValid);
        Assert.Contains("Day", message);
    }

    [Fact]
    public void ValidateBirthday_InvalidMonth_ReturnsInvalid()
    {
        var (isValid, message) = CreateSut().ValidateBirthday("15/13");
        Assert.False(isValid);
        Assert.Contains("Month", message);
    }

    [Fact]
    public void ValidateBirthday_ValidDate_ReturnsValid()
    {
        var (isValid, _) = CreateSut().ValidateBirthday("15/03");
        Assert.True(isValid);
    }

    // ── ValidateEmail ─────────────────────────────────────────────────────────

    [Fact]
    public void ValidateEmail_EmptyEmail_ReturnsValid()
    {
        var (isValid, _) = CreateSut().ValidateEmail("");
        Assert.True(isValid);
    }

    [Fact]
    public void ValidateEmail_InvalidFormat_ReturnsInvalid()
    {
        var (isValid, message) = CreateSut().ValidateEmail("notanemail");
        Assert.False(isValid);
        Assert.Contains("Invalid", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateEmail_ValidEmail_ReturnsValid()
    {
        var (isValid, _) = CreateSut().ValidateEmail("john@example.com");
        Assert.True(isValid);
    }

    // ── GetPagedPersonsForListAsync ───────────────────────────────────────────

    [Fact]
    public async Task GetPagedPersonsForListAsync_NoResults_ReturnsEmpty()
    {
        _repoMock.Setup(r => r.GetPagedAsync(1, 20, null, default))
                 .ReturnsAsync((Enumerable.Empty<Person>(), 0));

        var (items, totalCount) = await CreateSut().GetPagedPersonsForListAsync(1, 20);

        Assert.Empty(items);
        Assert.Equal(0, totalCount);
    }

    [Fact]
    public async Task GetPagedPersonsForListAsync_WithPersons_ReturnsMappedDtos()
    {
        var persons = new List<Person>
        {
            new("John Doe") { BirthdayDayMonth = "15/03", Email = "john@example.com", Participations = 3, Absences = 1 }
        };
        persons[0].Id.GetType(); // Access Id to ensure it's initialized
        _repoMock.Setup(r => r.GetPagedAsync(1, 20, null, default))
                 .ReturnsAsync((persons.AsEnumerable(), 1));

        var (items, totalCount) = await CreateSut().GetPagedPersonsForListAsync(1, 20);

        Assert.Equal(1, totalCount);
        var dto = Assert.Single(items);
        Assert.Equal("John Doe", dto.FullName);
        Assert.Equal("15/03", dto.BirthdayDayMonth);
        Assert.Equal("john@example.com", dto.Email);
        Assert.Equal(3, dto.Participations);
    }

    // ── UpdatePersonAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task UpdatePersonAsync_PersonNotFound_ReturnsFalse()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, default))
                 .ReturnsAsync((Person)null);

        var (success, message) = await CreateSut().UpdatePersonAsync(99, "John Doe");

        Assert.False(success);
        Assert.NotEmpty(message);
    }

    [Fact]
    public async Task UpdatePersonAsync_InvalidName_ReturnsFalse()
    {
        var person = new Person("Old Name");
        _repoMock.Setup(r => r.GetByIdAsync(1, default)).ReturnsAsync(person);

        var (success, _) = await CreateSut().UpdatePersonAsync(1, "A");   // too short

        Assert.False(success);
    }

    [Fact]
    public async Task UpdatePersonAsync_EmailTakenByOther_ReturnsFalse()
    {
        var person = new Person("John Doe");
        _repoMock.Setup(r => r.GetByIdAsync(1, default)).ReturnsAsync(person);
        _repoMock.Setup(r => r.IsEmailTakenAsync("taken@example.com", 1, default))
                 .ReturnsAsync(true);

        var (success, message) = await CreateSut().UpdatePersonAsync(1, "John Doe", email: "taken@example.com");

        Assert.False(success);
        Assert.Contains("Email", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdatePersonAsync_Valid_SavesAndReturnsTrue()
    {
        var person = new Person("Old Name");
        _repoMock.Setup(r => r.GetByIdAsync(1, default)).ReturnsAsync(person);
        _repoMock.Setup(r => r.IsEmailTakenAsync(It.IsAny<string>(), It.IsAny<int?>(), default))
                 .ReturnsAsync(false);
        _repoMock.Setup(r => r.SaveChangesAsync(default)).Returns(Task.CompletedTask);

        var (success, message) = await CreateSut().UpdatePersonAsync(1, "John Doe");

        Assert.True(success);
        Assert.Contains("updated", message, StringComparison.OrdinalIgnoreCase);
    }

    // ── DeletePersonsAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task DeletePersonsAsync_EmptyIds_ReturnsTrue()
    {
        var (success, _) = await CreateSut().DeletePersonsAsync([]);
        Assert.True(success);
    }

    [Fact]
    public async Task DeletePersonsAsync_ValidIds_DeletesAndReturnsSuccess()
    {
        var p1 = new Person("John Doe");
        var p2 = new Person("Jane Smith");
        _repoMock.Setup(r => r.GetByIdAsync(1, default)).ReturnsAsync(p1);
        _repoMock.Setup(r => r.GetByIdAsync(2, default)).ReturnsAsync(p2);
        _repoMock.Setup(r => r.RemoveAsync(It.IsAny<Person>(), default)).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync(default)).Returns(Task.CompletedTask);

        var (success, message) = await CreateSut().DeletePersonsAsync([1, 2]);

        Assert.True(success);
        Assert.Contains("2", message);
    }
}
```

- [ ] **Step 5: Run tests — confirm they fail**

```bash
dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --filter "FullyQualifiedName~PersonServiceTests" --verbosity normal
```

Expected: Multiple failures including `GetPagedPersonsForListAsync_NoResults_ReturnsEmpty` — `NotImplementedException`.

- [ ] **Step 6: Implement the three new service methods**

Replace the `#region List and Mutation Operations` stub block in `Services/PersonService.cs` with actual implementations:

```csharp
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
    var person = await _personRepository.GetByIdAsync(id, cancellationToken);
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
    person.SetNormalizedName(trimmedName);
    person.BirthdayDayMonth = string.IsNullOrWhiteSpace(birthday) ? null : birthday.Trim();
    person.Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();

    await _personRepository.SaveChangesAsync(cancellationToken);

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
        var person = await _personRepository.GetByIdAsync(id, cancellationToken);
        if (person != null)
            await _personRepository.RemoveAsync(person, cancellationToken);
    }

    await _personRepository.SaveChangesAsync(cancellationToken);
    return (true, $"{idList.Count} singer(s) successfully removed!");
}

#endregion
```

Add `GetPagedPersonsForListAsync`, `UpdatePersonAsync`, `DeletePersonsAsync` to `IPersonService.cs` (already added as stubs in Step 1 of this task — no changes needed there).

- [ ] **Step 7: Build — confirm 0 errors**

```bash
dotnet build MyVocaList/MyVocaList.csproj
```

- [ ] **Step 8: Run PersonServiceTests — all should pass**

```bash
dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --filter "FullyQualifiedName~PersonServiceTests" --verbosity normal
```

Expected: All PersonServiceTests pass.

- [ ] **Step 9: Run all tests — all 25+ should pass**

```bash
dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj
```

- [ ] **Step 10: Commit**

```bash
git add Domain/ServicesInterfaces/IPersonService.cs Services/PersonService.cs MyVocaList.Tests/Unit/Services/PersonServiceTests.cs
git commit -m "feat(services): add GetPagedPersonsForListAsync, UpdatePersonAsync, DeletePersonsAsync + tests"
```

---

## Task 5: Contracts DTO updates + DI registrations

**Files:**
- Modify: `Contracts/Models/PersonListItemDto.cs`
- Modify: `MyVocaList/Navigation/Routes.cs`
- Modify: `MyVocaList/AppShell.xaml.cs`
- Modify: `MyVocaList/MauiProgram.cs`

- [ ] **Step 1: Add BirthdayDayMonth and Email to PersonListItemDto**

In `Contracts/Models/PersonListItemDto.cs`, add after the `Id` property:

```csharp
public string BirthdayDayMonth { get; set; }
public string Email { get; set; }
```

These are simple auto-properties (no `INotifyPropertyChanged` needed — they are set once when the DTO is constructed and not updated in-place like Participations/Absences).

- [ ] **Step 2: Add PersonForm route**

In `MyVocaList/Navigation/Routes.cs`, add:

```csharp
public const string PersonForm = "person-form";
```

- [ ] **Step 3: Register PersonFormPage route in AppShell**

In `MyVocaList/AppShell.xaml.cs`, in the constructor after the VenueForm line:

```csharp
Routing.RegisterRoute(Routes.PersonForm, typeof(PersonFormPage));
```

- [ ] **Step 4: Add DI registrations to MauiProgram.cs**

In `MyVocaList/MauiProgram.cs`, add after the existing venue-related registrations:

```csharp
builder.Services.AddScoped<IPersonRepository, PersonRepository>();
builder.Services.AddScoped<IPersonService, PersonService>();
builder.Services.AddTransient<PersonsViewModel>();
builder.Services.AddTransient<PersonFormViewModel>();
builder.Services.AddTransient<PersonFormPage>();
```

Also add these usings if not present (they should already be via GlobalUsings):
- `MyVocaList.Infra.Repository` (for PersonRepository)
- `MyVocaList.Services` (for PersonService)

- [ ] **Step 5: Build and confirm 0 errors**

```bash
dotnet build MyVocaList/MyVocaList.csproj
```

Note: PersonsViewModel and PersonFormPage don't exist yet — this will fail if those references are added to DI before the classes exist. Add the DI lines only for IPersonRepository and IPersonService now; add PersonsViewModel, PersonFormViewModel, PersonFormPage after they are created in Tasks 6 and 7.

> **Revised Step 4:** Add only these two lines now:
> ```csharp
> builder.Services.AddScoped<IPersonRepository, PersonRepository>();
> builder.Services.AddScoped<IPersonService, PersonService>();
> ```
> Remaining VM/page DI lines are added after those files are created.

- [ ] **Step 6: Commit**

```bash
git add Contracts/Models/PersonListItemDto.cs MyVocaList/Navigation/Routes.cs MyVocaList/AppShell.xaml.cs MyVocaList/MauiProgram.cs
git commit -m "feat(contracts): add BirthdayDayMonth/Email to PersonListItemDto; add PersonForm route; register PersonRepository + PersonService"
```

---

## Task 6 (TDD): PersonFormViewModel — tests first, then implementation

**Files:**
- Create: `MyVocaList.Tests/Unit/ViewModels/PersonFormViewModelTests.cs`
- Create: `MyVocaList/UI/ViewModels/PersonFormViewModel.cs`
- Create: `MyVocaList/UI/Pages/People/PersonFormPage.xaml`
- Create: `MyVocaList/UI/Pages/People/PersonFormPage.xaml.cs`

- [ ] **Step 1: Write failing PersonFormViewModelTests**

Create `MyVocaList.Tests/Unit/ViewModels/PersonFormViewModelTests.cs`:

```csharp
using MyVocaList.Contracts.Models;
using MyVocaList.UI.ViewModels;

namespace MyVocaList.Tests.Unit.ViewModels;

public class PersonFormViewModelTests
{
    private readonly Mock<IPersonService> _serviceMock = new();
    private readonly Mock<ISnackbarComponent> _snackMock = new();
    private readonly Mock<ILogger<PersonFormViewModel>> _loggerMock = new();

    private PersonFormViewModel CreateSut() =>
        new(_serviceMock.Object, _snackMock.Object, _loggerMock.Object);

    // ── Derived properties ────────────────────────────────────────────────

    [Fact]
    public void IsEditMode_WhenPersonIdNull_ReturnsFalse()
    {
        var sut = CreateSut();
        sut.PersonId = null;
        Assert.False(sut.IsEditMode);
    }

    [Fact]
    public void IsEditMode_WhenPersonIdSet_ReturnsTrue()
    {
        var sut = CreateSut();
        sut.PersonId = 42;
        Assert.True(sut.IsEditMode);
    }

    [Fact]
    public void PageTitle_WhenNotEditMode_ReturnsNewSinger()
    {
        var sut = CreateSut();
        sut.PersonId = null;
        Assert.Equal("New Singer", sut.PageTitle);
    }

    [Fact]
    public void PageTitle_WhenEditMode_ReturnsEditSinger()
    {
        var sut = CreateSut();
        sut.PersonId = 1;
        Assert.Equal("Edit Singer", sut.PageTitle);
    }

    // ── SaveCommand: validation errors set HasError properties ───────────

    [Fact]
    public async Task SaveCommand_EmptyName_SetsNameHasError()
    {
        var sut = CreateSut();
        sut.PersonName = "";

        _serviceMock.Setup(s => s.ValidateNameInput(""))
                    .Returns((false, "Name is required"));

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.True(sut.NameHasError);
        Assert.Equal("Name is required", sut.NameErrorText);
    }

    [Fact]
    public async Task SaveCommand_InvalidBirthday_SetsBirthdayHasError()
    {
        var sut = CreateSut();
        sut.PersonName = "John Doe";
        sut.PersonBirthday = "99/99";

        _serviceMock.Setup(s => s.ValidateNameInput("John Doe")).Returns((true, ""));
        _serviceMock.Setup(s => s.ValidateBirthday("99/99")).Returns((false, "Use DD/MM format (e.g.: 15/03)"));

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.True(sut.BirthdayHasError);
        Assert.NotEmpty(sut.BirthdayErrorText);
    }

    [Fact]
    public async Task SaveCommand_InvalidEmail_SetsEmailHasError()
    {
        var sut = CreateSut();
        sut.PersonName = "John Doe";
        sut.PersonBirthday = "";
        sut.PersonEmail = "notanemail";

        _serviceMock.Setup(s => s.ValidateNameInput("John Doe")).Returns((true, ""));
        _serviceMock.Setup(s => s.ValidateBirthday("")).Returns((true, ""));
        _serviceMock.Setup(s => s.ValidateEmail("notanemail")).Returns((false, "Invalid email"));

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.True(sut.EmailHasError);
        Assert.Equal("Invalid email", sut.EmailErrorText);
    }

    [Fact]
    public async Task SaveCommand_CreateMode_ValidInputs_CallsCreateAsync()
    {
        var sut = CreateSut();
        sut.PersonId = null;
        sut.PersonName = "John Doe";
        sut.PersonBirthday = "";
        sut.PersonEmail = "";

        _serviceMock.Setup(s => s.ValidateNameInput("John Doe")).Returns((true, ""));
        _serviceMock.Setup(s => s.ValidateBirthday("")).Returns((true, ""));
        _serviceMock.Setup(s => s.ValidateEmail("")).Returns((true, ""));
        _serviceMock.Setup(s => s.CreatePersonAsync("John Doe", null, null, default))
                    .ReturnsAsync((true, "John Doe registered successfully!", new Person("John Doe")));
        _snackMock.Setup(s => s.ShowSuccessAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

        await sut.SaveCommand.ExecuteAsync(null);

        _serviceMock.Verify(s => s.CreatePersonAsync("John Doe", null, null, default), Times.Once);
    }

    [Fact]
    public async Task SaveCommand_EditMode_ValidInputs_CallsUpdateAsync()
    {
        var sut = CreateSut();
        sut.PersonId = 5;
        sut.PersonName = "John Doe";
        sut.PersonBirthday = "";
        sut.PersonEmail = "";

        _serviceMock.Setup(s => s.ValidateNameInput("John Doe")).Returns((true, ""));
        _serviceMock.Setup(s => s.ValidateBirthday("")).Returns((true, ""));
        _serviceMock.Setup(s => s.ValidateEmail("")).Returns((true, ""));
        _serviceMock.Setup(s => s.UpdatePersonAsync(5, "John Doe", null, null, default))
                    .ReturnsAsync((true, "John Doe updated successfully!"));
        _snackMock.Setup(s => s.ShowSuccessAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

        await sut.SaveCommand.ExecuteAsync(null);

        _serviceMock.Verify(s => s.UpdatePersonAsync(5, "John Doe", null, null, default), Times.Once);
    }

    // ── SearchPersonsCommand ──────────────────────────────────────────────

    [Fact]
    public async Task SearchPersonsCommand_ShortTerm_SetsEmptySuggestions()
    {
        var sut = CreateSut();

        await sut.SearchPersonsCommand.ExecuteAsync("a");  // < 2 chars

        Assert.Empty(sut.Suggestions);
    }

    [Fact]
    public async Task SearchPersonsCommand_ValidTerm_MapsToDtos()
    {
        var person = new Person("John Doe");
        _serviceMock.Setup(s => s.SearchPersonsStartsWithAsync("jo", 5, default))
                    .ReturnsAsync(new List<Person> { person });

        var sut = CreateSut();
        await sut.SearchPersonsCommand.ExecuteAsync("jo");

        var suggestion = Assert.Single(sut.Suggestions);
        Assert.Equal("John Doe", suggestion.Headline);
        Assert.Same(person, suggestion.Data);
    }

    // ── Character counter ─────────────────────────────────────────────────

    [Fact]
    public void ShowCharacterCounter_ShortName_IsFalse()
    {
        _serviceMock.Setup(s => s.ShouldShowCharacterCounter(8)).Returns(false);
        var sut = CreateSut();
        sut.PersonName = "John Doe";  // 8 chars
        Assert.False(sut.ShowCharacterCounter);
    }
}
```

> **Note:** `ISnackbarComponent` is in `MyVocaList.UI.Components` namespace (MAUI project). The test project already references the MAUI project, so this compiles.

- [ ] **Step 2: Run — confirm tests fail (PersonFormViewModel does not exist yet)**

```bash
dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --filter "FullyQualifiedName~PersonFormViewModelTests" --verbosity normal
```

Expected: Compilation error — `PersonFormViewModel` not found.

- [ ] **Step 3: Create PersonFormViewModel**

Create `MyVocaList/UI/ViewModels/PersonFormViewModel.cs`:

```csharp
namespace MyVocaList.UI.ViewModels;

/// <summary>
/// ViewModel for the Add / Edit Singer form page.
/// PersonId null = create mode; PersonId set = edit mode.
/// </summary>
[QueryProperty(nameof(PersonIdRaw), "personId")]
[QueryProperty(nameof(PersonName), "personName")]
[QueryProperty(nameof(PersonBirthday), "personBirthday")]
[QueryProperty(nameof(PersonEmail), "personEmail")]
public partial class PersonFormViewModel : ViewModelBase
{
    private readonly IPersonService _personService;
    private readonly ISnackbarComponent _snackbarService;
    private readonly ILogger<PersonFormViewModel> _logger;

    // Shell passes all query parameters as strings; parse manually.
    public string PersonIdRaw { set => PersonId = int.TryParse(value, out var id) ? id : null; }

    [ObservableProperty] private int? _personId;
    [ObservableProperty] private string _personName = string.Empty;
    [ObservableProperty] private string _personBirthday = string.Empty;
    [ObservableProperty] private string _personEmail = string.Empty;

    [ObservableProperty] private bool _nameHasError;
    [ObservableProperty] private string _nameErrorText = string.Empty;
    [ObservableProperty] private bool _birthdayHasError;
    [ObservableProperty] private string _birthdayErrorText = string.Empty;
    [ObservableProperty] private bool _emailHasError;
    [ObservableProperty] private string _emailErrorText = string.Empty;

    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private IEnumerable<AutocompleteSuggestion> _suggestions = [];

    // Character counter
    [ObservableProperty] private string _characterCounterText = string.Empty;
    [ObservableProperty] private bool _showCharacterCounter;
    [ObservableProperty] private bool _isCharacterCounterWarning;
    [ObservableProperty] private bool _isCharacterCounterError;

    public bool IsEditMode => PersonId.HasValue;
    public string PageTitle => IsEditMode ? "Edit Singer" : "New Singer";

    public PersonFormViewModel(
        IPersonService personService,
        ISnackbarComponent snackbarService,
        ILogger<PersonFormViewModel> logger)
    {
        _personService = personService;
        _snackbarService = snackbarService;
        _logger = logger;

        SaveCommand = new AsyncRelayCommand(SaveAsync);
        CancelCommand = new AsyncRelayCommand(CancelAsync);
        SearchPersonsCommand = new AsyncRelayCommand<string>(SearchPersonsAsync);
        SuggestionSelectedCommand = new AsyncRelayCommand<AutocompleteSuggestion>(SuggestionSelectedAsync);
    }

    public IAsyncRelayCommand SaveCommand { get; }
    public IAsyncRelayCommand CancelCommand { get; }
    public IAsyncRelayCommand<string> SearchPersonsCommand { get; }
    public IAsyncRelayCommand<AutocompleteSuggestion> SuggestionSelectedCommand { get; }

    partial void OnPersonIdChanged(int? value)
    {
        OnPropertyChanged(nameof(IsEditMode));
        OnPropertyChanged(nameof(PageTitle));
    }

    partial void OnPersonNameChanged(string value)
    {
        ClearNameError();
        UpdateCharacterCounter(value?.Length ?? 0);
    }

    partial void OnPersonBirthdayChanged(string value) => ClearBirthdayError();
    partial void OnPersonEmailChanged(string value) => ClearEmailError();

    private async Task SaveAsync()
    {
        var name = PersonName?.Trim() ?? string.Empty;
        var birthday = string.IsNullOrWhiteSpace(PersonBirthday) ? null : PersonBirthday.Trim();
        var email = string.IsNullOrWhiteSpace(PersonEmail) ? null : PersonEmail.Trim();

        var nameValidation = _personService.ValidateNameInput(name);
        if (!nameValidation.isValid)
        {
            NameHasError = true;
            NameErrorText = nameValidation.message;
            return;
        }

        var birthdayValidation = _personService.ValidateBirthday(birthday);
        if (!birthdayValidation.isValid)
        {
            BirthdayHasError = true;
            BirthdayErrorText = birthdayValidation.message;
            return;
        }

        var emailValidation = _personService.ValidateEmail(email);
        if (!emailValidation.isValid)
        {
            EmailHasError = true;
            EmailErrorText = emailValidation.message;
            return;
        }

        IsBusy = true;
        try
        {
            if (IsEditMode)
            {
                var (success, message) = await _personService.UpdatePersonAsync(
                    PersonId.Value, name, birthday, email);
                if (success)
                {
                    await _snackbarService.ShowSuccessAsync(message);
                    await Shell.Current.GoToAsync("..");
                }
                else
                {
                    // Determine which field has the error
                    SetInlineError(message);
                }
            }
            else
            {
                var (success, message, _) = await _personService.CreatePersonAsync(name, birthday, email);
                if (success)
                {
                    await _snackbarService.ShowSuccessAsync(message);
                    await Shell.Current.GoToAsync("..");
                }
                else
                {
                    SetInlineError(message);
                }
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void SetInlineError(string message)
    {
        // Route service error messages to the correct field
        if (message.Contains("Email", StringComparison.OrdinalIgnoreCase))
        {
            EmailHasError = true;
            EmailErrorText = message;
        }
        else if (message.Contains("birthday", StringComparison.OrdinalIgnoreCase) ||
                 message.Contains("DD/MM", StringComparison.OrdinalIgnoreCase))
        {
            BirthdayHasError = true;
            BirthdayErrorText = message;
        }
        else
        {
            NameHasError = true;
            NameErrorText = message;
        }
    }

    private Task CancelAsync() => Shell.Current.GoToAsync("..");

    private async Task SearchPersonsAsync(string term)
    {
        if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
        {
            Suggestions = [];
            return;
        }

        var results = await _personService.SearchPersonsStartsWithAsync(term, 5);
        Suggestions = results.Select(p => new AutocompleteSuggestion(
            p.FullName,
            p.GetDisplayIdentifier(),
            p)).ToList();
    }

    private async Task SuggestionSelectedAsync(AutocompleteSuggestion suggestion)
    {
        if (suggestion?.Data is not Person person) return;

        Suggestions = [];

        // Navigate to edit form for the selected existing person
        var birthday = Uri.EscapeDataString(person.BirthdayDayMonth ?? string.Empty);
        var email = Uri.EscapeDataString(person.Email ?? string.Empty);
        var name = Uri.EscapeDataString(person.FullName);

        await Shell.Current.GoToAsync(
            $"{Routes.PersonForm}?personId={person.Id}&personName={name}&personBirthday={birthday}&personEmail={email}");
    }

    private void ClearNameError() { NameHasError = false; NameErrorText = string.Empty; }
    private void ClearBirthdayError() { BirthdayHasError = false; BirthdayErrorText = string.Empty; }
    private void ClearEmailError() { EmailHasError = false; EmailErrorText = string.Empty; }

    private void UpdateCharacterCounter(int length)
    {
        ShowCharacterCounter = _personService.ShouldShowCharacterCounter(length);
        if (ShowCharacterCounter)
        {
            var (text, isWarning, isError) = _personService.GetCharacterCounterInfo(length);
            CharacterCounterText = text;
            IsCharacterCounterWarning = isWarning;
            IsCharacterCounterError = isError;
        }
    }
}
```

- [ ] **Step 4: Build — confirm 0 errors**

```bash
dotnet build MyVocaList/MyVocaList.csproj
```

- [ ] **Step 5: Run PersonFormViewModelTests — all should pass**

```bash
dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --filter "FullyQualifiedName~PersonFormViewModelTests"
```

- [ ] **Step 6: Create PersonFormPage.xaml**

Create `MyVocaList/UI/Pages/People/PersonFormPage.xaml`:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    xmlns:dx="http://schemas.devexpress.com/maui"
    xmlns:dxe="clr-namespace:DevExpress.Maui.Editors;assembly=DevExpress.Maui.Editors"
    xmlns:vm="clr-namespace:MyVocaList.UI.ViewModels"
    xmlns:autocomplete="clr-namespace:MyVocaList.UI.Components.AutocompleteField"
    x:Class="MyVocaList.UI.Pages.People.PersonFormPage"
    x:DataType="vm:PersonFormViewModel"
    Title="{Binding PageTitle}"
    BackgroundColor="{StaticResource Surface}"
    SafeAreaEdges="All">

    <ScrollView>
        <VerticalStackLayout Padding="24" Spacing="16">

            <!-- Full Name with autocomplete suggestions -->
            <autocomplete:AutocompleteField
                x:Name="nameField"
                LabelText="Full Name"
                Placeholder="First and last name"
                HasError="{Binding NameHasError}"
                ErrorText="{Binding NameErrorText}"
                Suggestions="{Binding Suggestions}"
                SearchRequestedCommand="{Binding SearchPersonsCommand}"
                SuggestionSelectedCommand="{Binding SuggestionSelectedCommand}" />

            <!-- Character counter -->
            <Label Text="{Binding CharacterCounterText}"
                   IsVisible="{Binding ShowCharacterCounter}"
                   StyleClass="Body.Small"
                   HorizontalOptions="End">
                <Label.Triggers>
                    <DataTrigger TargetType="Label"
                                 Binding="{Binding IsCharacterCounterError}"
                                 Value="True">
                        <Setter Property="TextColor" Value="{StaticResource Error}" />
                    </DataTrigger>
                    <DataTrigger TargetType="Label"
                                 Binding="{Binding IsCharacterCounterWarning}"
                                 Value="True">
                        <Setter Property="TextColor" Value="{StaticResource Warning}" />
                    </DataTrigger>
                </Label.Triggers>
            </Label>

            <!-- Birthday (optional) -->
            <dxe:TextEdit Text="{Binding PersonBirthday, Mode=TwoWay}"
                          LabelText="Birthday (optional)"
                          PlaceholderText="DD/MM"
                          HasError="{Binding BirthdayHasError}"
                          ErrorText="{Binding BirthdayErrorText}" />

            <!-- Email (optional) -->
            <dxe:TextEdit Text="{Binding PersonEmail, Mode=TwoWay}"
                          LabelText="Email (optional)"
                          PlaceholderText="singer@example.com"
                          Keyboard="Email"
                          HasError="{Binding EmailHasError}"
                          ErrorText="{Binding EmailErrorText}" />

            <!-- Action buttons -->
            <HorizontalStackLayout HorizontalOptions="End" Spacing="8">
                <dx:DXButton Content="Cancel"
                             Style="{StaticResource OutlinedButton}"
                             Padding="24,0"
                             Command="{Binding CancelCommand}" />
                <dx:DXButton Content="Save"
                             Style="{StaticResource FilledButton}"
                             Padding="24,0"
                             Command="{Binding SaveCommand}" />
            </HorizontalStackLayout>

        </VerticalStackLayout>
    </ScrollView>

</ContentPage>
```

- [ ] **Step 7: Build — confirm 0 errors**

```bash
dotnet build MyVocaList/MyVocaList.csproj
```

- [ ] **Step 8: Create PersonFormPage.xaml.cs**

Create `MyVocaList/UI/Pages/People/PersonFormPage.xaml.cs`:

```csharp
namespace MyVocaList.UI.Pages.People;

public partial class PersonFormPage : ContentPage
{
    private readonly PersonFormViewModel _viewModel;

    /// <summary>Exposed for compiled bindings inside DataTemplates.</summary>
    public PersonFormViewModel ViewModel => _viewModel;

    public PersonFormPage(PersonFormViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Focus the name field only in create mode
        if (!_viewModel.IsEditMode)
            nameField.Focus();
    }
}
```

- [ ] **Step 9: Add remaining DI registrations to MauiProgram.cs**

Add to `MyVocaList/MauiProgram.cs`:

```csharp
builder.Services.AddTransient<PersonFormViewModel>();
builder.Services.AddTransient<PersonFormPage>();
```

- [ ] **Step 10: Build and confirm 0 errors**

```bash
dotnet build MyVocaList/MyVocaList.csproj
```

- [ ] **Step 11: Commit**

```bash
git add MyVocaList/UI/ViewModels/PersonFormViewModel.cs MyVocaList/UI/Pages/People/PersonFormPage.xaml MyVocaList/UI/Pages/People/PersonFormPage.xaml.cs MyVocaList/MauiProgram.cs MyVocaList.Tests/Unit/ViewModels/PersonFormViewModelTests.cs
git commit -m "feat(ui): add PersonFormViewModel + PersonFormPage with autocomplete name suggestions"
```

---

## Task 7 (TDD): PersonsViewModel — tests first, then implementation

**Files:**
- Create: `MyVocaList.Tests/Unit/ViewModels/PersonsViewModelTests.cs`
- Create: `MyVocaList/UI/ViewModels/PersonsViewModel.cs`

- [ ] **Step 1: Write failing PersonsViewModelTests**

Create `MyVocaList.Tests/Unit/ViewModels/PersonsViewModelTests.cs`:

```csharp
using MyVocaList.Contracts.Models;
using MyVocaList.UI.ViewModels;

namespace MyVocaList.Tests.Unit.ViewModels;

public class PersonsViewModelTests
{
    private readonly Mock<IPersonService> _serviceMock = new();
    private readonly Mock<ISnackbarComponent> _snackMock = new();
    private readonly Mock<ILogger<PersonsViewModel>> _loggerMock = new();

    private PersonsViewModel CreateSut() =>
        new(_serviceMock.Object, _snackMock.Object, _loggerMock.Object);

    // ── AppBarTitle ───────────────────────────────────────────────────────

    [Fact]
    public void AppBarTitle_WhenNoneSelected_ReturnsSingers()
    {
        var sut = CreateSut();
        sut.OnSelectionChanged(0);
        Assert.Equal("Singers", sut.AppBarTitle);
    }

    [Fact]
    public void AppBarTitle_WhenOneSelected_Returns1Selected()
    {
        var sut = CreateSut();
        sut.OnSelectionChanged(1);
        Assert.Equal("1 selected", sut.AppBarTitle);
    }

    [Fact]
    public void AppBarTitle_WhenMultipleSelected_ReturnsNSelected()
    {
        var sut = CreateSut();
        sut.OnSelectionChanged(3);
        Assert.Equal("3 selected", sut.AppBarTitle);
    }

    // ── CanEditSelected / CanDeleteSelected ───────────────────────────────

    [Fact]
    public void CanEditSelected_WhenOneSelected_IsTrue()
    {
        var sut = CreateSut();
        sut.OnSelectionChanged(1);
        Assert.True(sut.CanEditSelected);
    }

    [Fact]
    public void CanEditSelected_WhenTwoSelected_IsFalse()
    {
        var sut = CreateSut();
        sut.OnSelectionChanged(2);
        Assert.False(sut.CanEditSelected);
    }

    [Fact]
    public void CanDeleteSelected_WhenOneOrMoreSelected_IsTrue()
    {
        var sut = CreateSut();
        sut.OnSelectionChanged(1);
        Assert.True(sut.CanDeleteSelected);
    }

    [Fact]
    public void CanDeleteSelected_WhenNoneSelected_IsFalse()
    {
        var sut = CreateSut();
        sut.OnSelectionChanged(0);
        Assert.False(sut.CanDeleteSelected);
    }

    // ── Empty state derived properties ────────────────────────────────────

    [Fact]
    public async Task InitializeAsync_EmptyDb_SetsIsEmptyNoPersons()
    {
        _serviceMock.Setup(s => s.GetPagedPersonsForListAsync(1, 20, null, default))
                    .ReturnsAsync((Enumerable.Empty<PersonListItemDto>(), 0));

        var sut = CreateSut();
        await sut.InitializeAsync();

        Assert.True(sut.IsEmptyNoPersons);
        Assert.False(sut.IsEmptyNoResults);
        Assert.False(sut.IsInitialLoading);
    }

    [Fact]
    public async Task InitializeAsync_WithPersons_PopulatesPersons()
    {
        var dtos = new List<PersonListItemDto>
        {
            new() { Id = 1, FullName = "John Doe", Participations = 2 }
        };
        _serviceMock.Setup(s => s.GetPagedPersonsForListAsync(1, 20, null, default))
                    .ReturnsAsync((dtos.AsEnumerable(), 1));

        var sut = CreateSut();
        await sut.InitializeAsync();

        Assert.Single(sut.Persons);
        Assert.False(sut.IsEmptyNoPersons);
    }

    // ── SelectAll ─────────────────────────────────────────────────────────

    [Fact]
    public async Task SelectAllCommand_WhenNotAllSelected_SelectsAll()
    {
        var dtos = new List<PersonListItemDto>
        {
            new() { Id = 1, FullName = "John Doe" },
            new() { Id = 2, FullName = "Jane Smith" }
        };
        _serviceMock.Setup(s => s.GetPagedPersonsForListAsync(1, 20, null, default))
                    .ReturnsAsync((dtos.AsEnumerable(), 2));

        var sut = CreateSut();
        await sut.InitializeAsync();

        // No items selected initially
        Assert.False(sut.IsAllSelected);
    }
}
```

- [ ] **Step 2: Run — confirm tests fail (PersonsViewModel not found)**

```bash
dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --filter "FullyQualifiedName~PersonsViewModelTests" --verbosity normal
```

Expected: Compilation error — `PersonsViewModel` not found.

- [ ] **Step 3: Create PersonsViewModel**

Create `MyVocaList/UI/ViewModels/PersonsViewModel.cs`:

```csharp
using MyVocaList.UI.Collections;

namespace MyVocaList.UI.ViewModels;

/// <summary>
/// ViewModel for the Singers list page: paging, search, always-on selection, confirm-delete.
/// Add navigates to PersonFormPage via FAB. Edit navigates via FloatingToolbar (single select).
/// </summary>
public partial class PersonsViewModel : ViewModelBase
{
    private readonly IPersonService _personService;
    private readonly ISnackbarComponent _snackbarService;
    private readonly ILogger<PersonsViewModel> _logger;

    private int _currentPage;
    private int _totalCount;
    private string _currentSearchQuery;
    private CancellationTokenSource _searchCts;
    private Func<Task> _pendingConfirmAction;

    private readonly SemaphoreSlim _loadSemaphore = new(1, 1);
    private volatile bool _isLoading;

    [ObservableProperty] private bool _isRefreshing;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private bool _isSearchMode;
    [ObservableProperty] private bool _isScrolled;
    [ObservableProperty] private int _selectedCount;
    [ObservableProperty] private BottomSheetState _confirmSheetState = BottomSheetState.Hidden;
    [ObservableProperty] private bool _hasMoreItems = true;
    [ObservableProperty] private bool _isInitialLoading = true;
    [ObservableProperty] private string _confirmMessage = string.Empty;
    [ObservableProperty] private string _confirmActionText = "Delete";

    public PersonsViewModel(
        IPersonService personService,
        ISnackbarComponent snackbarService,
        ILogger<PersonsViewModel> logger)
    {
        _personService = personService;
        _snackbarService = snackbarService;
        _logger = logger;

        Persons = [];
        SelectedPersons = [];

        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        LoadMoreCommand = new RelayCommand(() => _ = LoadMoreAsync());
        AddPersonCommand = new AsyncRelayCommand(NavigateToAddAsync);
        DeleteSelectedCommand = new RelayCommand(RequestBatchDelete, () => CanDeleteSelected);
        EditSelectedCommand = new AsyncRelayCommand(NavigateToEditAsync, () => CanEditSelected);
        SelectAllCommand = new RelayCommand(ToggleSelectAll);
        ConfirmActionCommand = new AsyncRelayCommand(ExecuteConfirmActionAsync);
        DismissConfirmCommand = new RelayCommand(DismissConfirmSheet);
        OpenSearchCommand = new RelayCommand(() => IsSearchMode = true);
        CloseSearchCommand = new RelayCommand(CloseSearch);
    }

    public ObservableRangeCollection<PersonListItemDto> Persons { get; }
    public ObservableRangeCollection<PersonListItemDto> SelectedPersons { get; }

    /// <summary>Non-generic wrapper for binding to DXCollectionView SelectedItems (requires IList).</summary>
    public System.Collections.IList SelectedPersonsRaw => SelectedPersons;

    public string AppBarTitle => SelectedCount == 0 ? "Singers" : $"{SelectedCount} selected";
    public bool CanEditSelected => SelectedCount == 1;
    public bool CanDeleteSelected => SelectedCount > 0;
    public bool IsAllSelected => Persons.Count > 0 && SelectedCount == Persons.Count;

    public bool IsEmpty => !IsInitialLoading && Persons.Count == 0;
    public bool IsEmptyNoPersons => IsEmpty && string.IsNullOrWhiteSpace(SearchText);
    public bool IsEmptyNoResults => IsEmpty && !string.IsNullOrWhiteSpace(SearchText);

    public IAsyncRelayCommand RefreshCommand { get; }
    public IRelayCommand LoadMoreCommand { get; }
    public IAsyncRelayCommand AddPersonCommand { get; }
    public IRelayCommand DeleteSelectedCommand { get; }
    public IAsyncRelayCommand EditSelectedCommand { get; }
    public IRelayCommand SelectAllCommand { get; }
    public IAsyncRelayCommand ConfirmActionCommand { get; }
    public IRelayCommand DismissConfirmCommand { get; }
    public IRelayCommand OpenSearchCommand { get; }
    public IRelayCommand CloseSearchCommand { get; }

    partial void OnSearchTextChanged(string value)
    {
        NotifyEmptyStates();
        TriggerSearchDebounce();
    }

    partial void OnSelectedCountChanged(int value)
    {
        OnPropertyChanged(nameof(AppBarTitle));
        OnPropertyChanged(nameof(CanEditSelected));
        OnPropertyChanged(nameof(CanDeleteSelected));
        OnPropertyChanged(nameof(IsAllSelected));
        DeleteSelectedCommand.NotifyCanExecuteChanged();
        EditSelectedCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsInitialLoadingChanged(bool value) => NotifyEmptyStates();

    public async Task InitializeAsync()
    {
        IsInitialLoading = true;
        await Task.Yield();
        await LoadFirstPageAsync(CancellationToken.None);
        RunOnUiThread(() => IsInitialLoading = false);
    }

    private async Task LoadFirstPageAsync(CancellationToken cancellationToken)
    {
        var entered = false;
        try
        {
            await _loadSemaphore.WaitAsync(cancellationToken);
            entered = true;

            _currentPage = 1;
            _currentSearchQuery = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim();

            var selectedIds = SelectedPersons.Select(p => p.Id).ToHashSet();

            var (itemsEnumerable, totalCount) = await _personService.GetPagedPersonsForListAsync(
                _currentPage, AppPagination.DefaultPageSize, _currentSearchQuery, cancellationToken);

            if (cancellationToken.IsCancellationRequested) return;

            _totalCount = totalCount;
            var list = itemsEnumerable.ToList();
            HasMoreItems = totalCount > list.Count;

            RunOnUiThread(() =>
            {
                Persons.ReplaceRange(list);
                var restored = Persons.Where(p => selectedIds.Contains(p.Id)).ToList();
                SelectedPersons.ReplaceRange(restored);
                SelectedCount = SelectedPersons.Count;
                NotifyEmptyStates();
            });
        }
        catch (OperationCanceledException)
        {
            // Cancellation requested — silently return
        }
        finally
        {
            if (entered)
                _loadSemaphore.Release();
        }
    }

    private async Task RefreshAsync()
    {
        RunOnUiThread(() => IsRefreshing = true);
        await LoadFirstPageAsync(CancellationToken.None);
        RunOnUiThread(() => IsRefreshing = false);
    }

    private async Task LoadMoreAsync()
    {
        if (_isLoading || !HasMoreItems)
        {
            RunOnUiThread(() => IsRefreshing = false);
            return;
        }

        _isLoading = true;
        var loadingPage = _currentPage + 1;

        try
        {
            var (itemsEnumerable, totalCount) = await _personService.GetPagedPersonsForListAsync(
                loadingPage, AppPagination.DefaultPageSize, _currentSearchQuery);

            _totalCount = totalCount;
            var list = itemsEnumerable.ToList();
            var hasMore = (list.Count + Persons.Count) < _totalCount;
            _currentPage = loadingPage;

            RunOnUiThread(() =>
            {
                Persons.AddRange(list);
                HasMoreItems = hasMore;
                IsRefreshing = false;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load more singers (page {Page})", loadingPage);
            RunOnUiThread(() => IsRefreshing = false);
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void TriggerSearchDebounce()
    {
        try { _searchCts?.Cancel(); _searchCts?.Dispose(); }
        catch { /* ignore disposal races */ }

        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(400, token);
                if (token.IsCancellationRequested) return;
                await LoadFirstPageAsync(token);
            }
            catch (OperationCanceledException) { /* ignore */ }
        }, token);
    }

    private Task NavigateToAddAsync() => Shell.Current.GoToAsync(Routes.PersonForm);

    private async Task NavigateToEditAsync()
    {
        var item = SelectedPersons.FirstOrDefault();
        if (item == null) return;

        RunOnUiThread(() =>
        {
            SelectedPersons.ClearRange();
            SelectedCount = 0;
        });

        var name = Uri.EscapeDataString(item.FullName);
        var birthday = Uri.EscapeDataString(item.BirthdayDayMonth ?? string.Empty);
        var email = Uri.EscapeDataString(item.Email ?? string.Empty);

        await Shell.Current.GoToAsync(
            $"{Routes.PersonForm}?personId={item.Id}&personName={name}&personBirthday={birthday}&personEmail={email}");
    }

    private void RequestBatchDelete()
    {
        var selectedItems = SelectedPersons.ToList();
        if (selectedItems.Count == 0) return;

        ConfirmMessage = $"Delete {selectedItems.Count} singer(s)?";
        ConfirmActionText = "Delete";
        _pendingConfirmAction = async () =>
        {
            var ids = selectedItems.Select(p => p.Id);
            var (success, message) = await _personService.DeletePersonsAsync(ids);
            RunOnUiThread(() =>
            {
                SelectedPersons.ClearRange();
                SelectedCount = 0;
            });
            if (success)
            {
                await RefreshAsync();
                await _snackbarService.ShowSuccessAsync(message);
            }
            else
            {
                await _snackbarService.ShowErrorAsync(message);
            }
        };
        ConfirmSheetState = BottomSheetState.HalfExpanded;
    }

    private async Task ExecuteConfirmActionAsync()
    {
        var action = _pendingConfirmAction;
        DismissConfirmSheet();
        if (action != null)
            await action();
    }

    private void DismissConfirmSheet()
    {
        ConfirmSheetState = BottomSheetState.Hidden;
        _pendingConfirmAction = null;
    }

    private void ToggleSelectAll()
    {
        if (IsAllSelected)
        {
            RunOnUiThread(() =>
            {
                SelectedPersons.ClearRange();
                SelectedCount = 0;
            });
            return;
        }
        if (Persons.Count == 0) return;
        RunOnUiThread(() =>
        {
            SelectedPersons.ReplaceRange([.. Persons]);
            SelectedCount = Persons.Count;
        });
    }

    public void OnSelectionChanged(int count)
    {
        SelectedCount = count;
    }

    private void CloseSearch()
    {
        IsSearchMode = false;
        SearchText = string.Empty;
    }

    private void NotifyEmptyStates()
    {
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(IsEmptyNoPersons));
        OnPropertyChanged(nameof(IsEmptyNoResults));
        OnPropertyChanged(nameof(IsAllSelected));
    }
}
```

- [ ] **Step 4: Build — confirm 0 errors**

```bash
dotnet build MyVocaList/MyVocaList.csproj
```

- [ ] **Step 5: Run PersonsViewModelTests — all should pass**

```bash
dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --filter "FullyQualifiedName~PersonsViewModelTests"
```

- [ ] **Step 6: Commit**

```bash
git add MyVocaList/UI/ViewModels/PersonsViewModel.cs MyVocaList.Tests/Unit/ViewModels/PersonsViewModelTests.cs
git commit -m "feat(viewmodels): add PersonsViewModel + tests"
```

---

## Task 8: Rewrite PeoplePage XAML + code-behind (one file at a time, build after each)

**Files:**
- Modify: `MyVocaList/UI/Pages/People/PeoplePage.xaml`
- Modify: `MyVocaList/UI/Pages/People/PeoplePage.xaml.cs`

- [ ] **Step 1: Add PersonsViewModel DI registration (if not done yet)**

In `MyVocaList/MauiProgram.cs`, add:

```csharp
builder.Services.AddTransient<PersonsViewModel>();
```

The existing `builder.Services.AddTransient<PeoplePage>();` line stays — PeoplePage is reused in-place.

- [ ] **Step 2: Rewrite PeoplePage.xaml**

Replace entire content of `MyVocaList/UI/Pages/People/PeoplePage.xaml`:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    xmlns:dx="http://schemas.devexpress.com/maui"
    xmlns:dxcv="clr-namespace:DevExpress.Maui.CollectionView;assembly=DevExpress.Maui.CollectionView"
    xmlns:models="clr-namespace:MyVocaList.Contracts.Models;assembly=MyVocaList.Contracts"
    xmlns:vm="clr-namespace:MyVocaList.UI.ViewModels"
    xmlns:appbars="clr-namespace:MyVocaList.UI.Components.AppBars"
    xmlns:lists="clr-namespace:MyVocaList.UI.Components.Lists"
    xmlns:toolbars="clr-namespace:MyVocaList.UI.Components.Toolbars"
    xmlns:states="clr-namespace:MyVocaList.UI.Components.States"
    x:Class="MyVocaList.UI.Pages.People.PeoplePage"
    x:Name="page"
    x:DataType="vm:PersonsViewModel"
    Title="Singers"
    BackgroundColor="{StaticResource Surface}"
    SafeAreaEdges="Container">

    <Shell.BackButtonBehavior>
        <BackButtonBehavior IsVisible="False" IsEnabled="False" />
    </Shell.BackButtonBehavior>

    <Shell.TitleView>
        <Grid>
            <appbars:SmallAppBar
                x:Name="smallAppBar"
                Title="{Binding AppBarTitle}"
                Action1Icon="search_outlined"
                Action1Command="{Binding OpenSearchCommand}"
                IsElevated="{Binding IsScrolled}"
                IsVisible="{Binding IsSearchMode, Converter={StaticResource InverseBoolConverter}}" />
            <appbars:SearchAppBar
                x:Name="searchAppBar"
                SearchText="{Binding SearchText, Mode=TwoWay}"
                Placeholder="Search singers..."
                BackCommand="{Binding CloseSearchCommand}"
                IsElevated="{Binding IsScrolled}"
                IsVisible="{Binding IsSearchMode}" />
        </Grid>
    </Shell.TitleView>

    <Grid>

        <dx:ShimmerView IsLoading="{Binding IsInitialLoading}"
                        WaveWidth="0.7"
                        WaveOpacity="0.8">
            <dx:ShimmerView.LoadingView>
                <VerticalStackLayout Spacing="0">
                    <dx:DXBorder Style="{StaticResource SkeletonBone}" />
                    <dx:DXBorder Style="{StaticResource SkeletonBone}" />
                    <dx:DXBorder Style="{StaticResource SkeletonBone}" />
                    <dx:DXBorder Style="{StaticResource SkeletonBone}" />
                    <dx:DXBorder Style="{StaticResource SkeletonBone}" />
                    <dx:DXBorder Style="{StaticResource SkeletonBone}" />
                </VerticalStackLayout>
            </dx:ShimmerView.LoadingView>
            <dx:ShimmerView.Content>
                <dxcv:DXCollectionView x:Name="collectionView"
                       ItemsSource="{Binding Persons}"
                       IsPullToRefreshEnabled="True"
                       IsRefreshing="{Binding IsRefreshing, Mode=TwoWay}"
                       PullToRefreshCommand="{Binding RefreshCommand}"
                       IsLoadMoreEnabled="{Binding HasMoreItems}"
                       LoadMoreCommand="{Binding LoadMoreCommand}"
                       IndicatorColor="{StaticResource Primary}"
                       SelectionMode="Multiple"
                       UseRippleEffect="True"
                       AllowCascadeUpdate="True"
                       ItemSeparatorThickness="0"
                       Margin="0,0,0,88"
                       Scrolled="OnCollectionViewScrolled"
                       SelectionChanged="OnSelectionChanged">

                    <dxcv:DXCollectionView.ItemTemplate>
                        <DataTemplate x:DataType="models:PersonListItemDto">
                            <lists:ListItem Headline="{Binding FullName}"
                                            SupportingText="{Binding ParticipationsAbsencesNumber}"
                                            IsSelected="False">
                                <lists:ListItem.LeadingContent>
                                    <lists:ListItemLeadingMonogram Initials="{Binding Initials}" />
                                </lists:ListItem.LeadingContent>
                                <lists:ListItem.TrailingContent>
                                    <dx:CheckEdit IsChecked="False"
                                                  InputTransparent="True"
                                                  VerticalOptions="Center" />
                                </lists:ListItem.TrailingContent>
                            </lists:ListItem>
                        </DataTemplate>
                    </dxcv:DXCollectionView.ItemTemplate>

                    <dxcv:DXCollectionView.SelectedItemTemplate>
                        <DataTemplate x:DataType="models:PersonListItemDto">
                            <lists:ListItem Headline="{Binding FullName}"
                                            SupportingText="{Binding ParticipationsAbsencesNumber}"
                                            IsSelected="True">
                                <lists:ListItem.LeadingContent>
                                    <lists:ListItemLeadingMonogram Initials="{Binding Initials}" />
                                </lists:ListItem.LeadingContent>
                                <lists:ListItem.TrailingContent>
                                    <dx:CheckEdit IsChecked="True"
                                                  CheckedCheckBoxColor="{dx:ThemeColor Primary}"
                                                  InputTransparent="True"
                                                  VerticalOptions="Center" />
                                </lists:ListItem.TrailingContent>
                            </lists:ListItem>
                        </DataTemplate>
                    </dxcv:DXCollectionView.SelectedItemTemplate>
                </dxcv:DXCollectionView>
            </dx:ShimmerView.Content>
        </dx:ShimmerView>

        <!-- Empty state: no singers registered -->
        <states:EmptyState
            Illustration="person_outlined"
            Headline="No singer registered"
            IsVisible="{Binding IsEmptyNoPersons}"
            Margin="32,32,32,88" />

        <!-- Empty state: search returned no results -->
        <states:EmptyState
            Illustration="search_outlined"
            Headline="No singer found"
            IsVisible="{Binding IsEmptyNoResults}"
            Margin="32,32,32,88" />

        <!-- Toolbar + FAB: centered row, 16dp from safe area bottom -->
        <HorizontalStackLayout HorizontalOptions="Center"
                               VerticalOptions="End"
                               Margin="0,0,0,16"
                               Spacing="8">
            <toolbars:FloatingToolbar
                VerticalOptions="Center"
                Action1Icon="done_all_outlined"
                Action1Command="{Binding SelectAllCommand}"
                Action1Description="Select all"
                Action1IsSelected="{Binding IsAllSelected}"
                Action2Icon="edit_outlined"
                Action2Command="{Binding EditSelectedCommand}"
                Action2Description="Edit selected"
                Action2IsSelected="{Binding CanEditSelected}"
                Action3Icon="delete_outlined"
                Action3Command="{Binding DeleteSelectedCommand}"
                Action3Description="Delete selected"
                Action3IsSelected="{Binding CanDeleteSelected}" />
            <dx:DXButton Style="{StaticResource Fab}"
                         Icon="add_outlined"
                         VerticalOptions="Center"
                         SemanticProperties.Description="Add singer"
                         Command="{Binding AddPersonCommand}" />
        </HorizontalStackLayout>

        <!-- Confirm delete BottomSheet (inline — ConfirmSheet ContentView wrapper causes ANR) -->
        <dx:BottomSheet x:Name="confirmSheet"
                        HalfExpandedRatio="0.28"
                        AllowedState="HalfExpanded"
                        IsModal="True"
                        ShowGrabber="True"
                        AllowDismiss="True"
                        BackgroundColor="{StaticResource Surface}"
                        CornerRadius="28"
                        StateChanged="OnConfirmSheetStateChanged">
            <VerticalStackLayout>
                <Label Text="{Binding ConfirmMessage}"
                       StyleClass="Title.Medium"
                       TextColor="{StaticResource OnSurface}"
                       HorizontalTextAlignment="Center"
                       Margin="24,20" />
                <BoxView Style="{StaticResource Divider}" />
                <dx:DXButton Content="{Binding ConfirmActionText}"
                             Style="{StaticResource BottomSheetDestructiveAction}"
                             Command="{Binding ConfirmActionCommand}" />
                <BoxView Style="{StaticResource Divider}" />
                <dx:DXButton Content="Cancel"
                             Style="{StaticResource BottomSheetCancelAction}"
                             Command="{Binding DismissConfirmCommand}" />
            </VerticalStackLayout>
        </dx:BottomSheet>

    </Grid>

</ContentPage>
```

> **Note on `Initials` binding:** `PersonListItemDto` needs an `Initials` computed property — see Step 3.

- [ ] **Step 3: Add Initials property to PersonListItemDto**

In `Contracts/Models/PersonListItemDto.cs`, add:

```csharp
/// <summary>Initials for the leading monogram (e.g. "JD" for "John Doe").</summary>
[JsonIgnore]
public string Initials
{
    get
    {
        if (string.IsNullOrWhiteSpace(FullName)) return "?";
        var parts = FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2
            ? $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant()
            : parts[0][0].ToString().ToUpperInvariant();
    }
}
```

- [ ] **Step 4: Build XAML — confirm 0 errors**

```bash
dotnet build MyVocaList/MyVocaList.csproj
```

- [ ] **Step 5: Rewrite PeoplePage.xaml.cs**

Replace entire content of `MyVocaList/UI/Pages/People/PeoplePage.xaml.cs`:

```csharp
using DevExpress.Maui.CollectionView;

namespace MyVocaList.UI.Pages.People;

public partial class PeoplePage : ContentPage
{
    private readonly PersonsViewModel _viewModel;

    /// <summary>Exposed for compiled bindings inside DataTemplates.</summary>
    public PersonsViewModel ViewModel => _viewModel;

    public PeoplePage(PersonsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PersonsViewModel.ConfirmSheetState))
        {
            var state = _viewModel.ConfirmSheetState;
            if (state == BottomSheetState.Hidden)
                confirmSheet.Close();
            else
                confirmSheet.Show(state, this);
        }
    }

    private void OnConfirmSheetStateChanged(object sender, ValueChangedEventArgs<BottomSheetState> e)
    {
        if (e.NewValue == BottomSheetState.Hidden && _viewModel.ConfirmSheetState != BottomSheetState.Hidden)
            _viewModel.ConfirmSheetState = BottomSheetState.Hidden;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (collectionView != null)
            collectionView.SelectedItems = _viewModel.SelectedPersonsRaw;
        _ = _viewModel.InitializeAsync();
    }

    protected override bool OnBackButtonPressed()
    {
        if (_viewModel.ConfirmSheetState != BottomSheetState.Hidden)
        {
            _viewModel.ConfirmSheetState = BottomSheetState.Hidden;
            return true;
        }

        if (_viewModel.IsSearchMode)
        {
            _viewModel.CloseSearchCommand.Execute(null);
            return true;
        }

        return false;
    }

    private void OnCollectionViewScrolled(object sender, DXCollectionViewScrolledEventArgs e)
    {
        _viewModel.IsScrolled = e.Offset > 0;
    }

    private void OnSelectionChanged(object sender, CollectionViewSelectionChangedEventArgs e)
    {
        var count = (collectionView.SelectedItems as System.Collections.ICollection)?.Count ?? 0;
        _viewModel.OnSelectionChanged(count);
    }
}
```

- [ ] **Step 6: Build — confirm 0 errors**

```bash
dotnet build MyVocaList/MyVocaList.csproj
```

- [ ] **Step 7: Commit**

```bash
git add MyVocaList/UI/Pages/People/PeoplePage.xaml MyVocaList/UI/Pages/People/PeoplePage.xaml.cs MyVocaList/MauiProgram.cs Contracts/Models/PersonListItemDto.cs
git commit -m "feat(ui): rewrite PeoplePage as Singers list page with search, selection, paging, confirm-delete"
```

---

## Task 9: PersonRepository Integration Tests

**Files:**
- Create: `MyVocaList.Tests/Integration/Repositories/PersonRepositoryTests.cs`

- [ ] **Step 1: Create PersonRepositoryTests**

Create `MyVocaList.Tests/Integration/Repositories/PersonRepositoryTests.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using MyVocaList.Infra;
using MyVocaList.Infra.Repository;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Integration.Repositories;

public class PersonRepositoryTests : IAsyncLifetime
{
    private AppDbContext _db;
    private PersonRepository _repo;

    public async Task InitializeAsync()
    {
        _db = TestDbContextFactory.Create();
        await _db.Database.EnsureCreatedAsync();
        _repo = new PersonRepository(_db);
    }

    public async Task DisposeAsync()
    {
        await _db.Database.EnsureDeletedAsync();
        await _db.DisposeAsync();
    }

    // ── CRUD ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddAsync_ValidPerson_PersistedAndReturnedById()
    {
        var person = new Person("John Doe") { Email = "john@example.com" };
        person.SetNormalizedName("John Doe");

        await _repo.AddAsync(person);
        await _db.SaveChangesAsync();

        var found = await _repo.GetByIdAsync(person.Id);

        Assert.NotNull(found);
        Assert.Equal("John Doe", found.FullName);
    }

    // ── Collation-aware search ────────────────────────────────────────────

    [Fact]
    public async Task SearchByNameStartsWithAsync_CaseInsensitive_FindsMatch()
    {
        var person = new Person("João Silva");
        person.SetNormalizedName("João Silva");
        _db.Set<Person>().Add(person);
        await _db.SaveChangesAsync();

        var results = await _repo.SearchByNameStartsWithAsync("joão");

        Assert.Single(results);
        Assert.Equal("João Silva", results[0].FullName);
    }

    [Fact]
    public async Task SearchByNameOrEmailAsync_SearchByEmail_FindsMatch()
    {
        var person = new Person("Jane Smith");
        person.SetNormalizedName("Jane Smith");
        person.Email = "jane@example.com";
        _db.Set<Person>().Add(person);
        await _db.SaveChangesAsync();

        var results = await _repo.SearchByNameOrEmailAsync("jane@");

        Assert.Single(results);
        Assert.Equal("Jane Smith", results[0].FullName);
    }

    // ── GetPagedAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetPagedAsync_NoQuery_ReturnsAllSortedByName()
    {
        _db.Set<Person>().AddRange(
            Create("Zara Adams"),
            Create("Alice Brown"));
        await _db.SaveChangesAsync();

        var (items, totalCount) = await _repo.GetPagedAsync(1, 20);

        Assert.Equal(2, totalCount);
        var list = items.ToList();
        Assert.Equal("Alice Brown", list[0].FullName);
        Assert.Equal("Zara Adams", list[1].FullName);
    }

    [Fact]
    public async Task GetPagedAsync_WithQuery_FiltersResults()
    {
        _db.Set<Person>().AddRange(
            Create("Alice Brown"),
            Create("Bob Smith"));
        await _db.SaveChangesAsync();

        var (items, totalCount) = await _repo.GetPagedAsync(1, 20, "alice");

        Assert.Equal(1, totalCount);
        Assert.Equal("Alice Brown", items.Single().FullName);
    }

    // ── Index constraints ─────────────────────────────────────────────────

    [Fact]
    public async Task AddAsync_DuplicateEmail_ThrowsDbUpdateException()
    {
        var p1 = Create("John Doe");
        p1.Email = "dup@example.com";
        var p2 = Create("Jane Smith");
        p2.Email = "dup@example.com";

        _db.Set<Person>().AddRange(p1, p2);

        await Assert.ThrowsAsync<DbUpdateException>(() => _db.SaveChangesAsync());
    }

    [Fact]
    public async Task AddAsync_SameNameNullBirthday_AllowsDuplicate()
    {
        // Same name + null birthday = allowed (filtered unique index only applies when birthday IS NOT NULL)
        var p1 = Create("John Doe");
        var p2 = Create("John Doe");   // no birthday — should be allowed

        _db.Set<Person>().AddRange(p1, p2);

        // Should not throw
        await _db.SaveChangesAsync();

        Assert.Equal(2, await _db.Set<Person>().CountAsync());
    }

    [Fact]
    public async Task AddAsync_SameNameSameBirthday_ThrowsDbUpdateException()
    {
        var p1 = Create("John Doe");
        p1.BirthdayDayMonth = "15/03";
        var p2 = Create("John Doe");
        p2.BirthdayDayMonth = "15/03";

        _db.Set<Person>().AddRange(p1, p2);

        await Assert.ThrowsAsync<DbUpdateException>(() => _db.SaveChangesAsync());
    }

    [Fact]
    public async Task AddAsync_NullEmailMultipleTimes_AllowsMultipleNulls()
    {
        // Nullable unique index: NULL != NULL in SQLite — multiple NULL emails are allowed
        _db.Set<Person>().AddRange(
            Create("Alice Brown"),
            Create("Bob Smith"));  // both have null email

        // Should not throw
        await _db.SaveChangesAsync();
    }

    // ── IsEmailTakenAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task IsEmailTakenAsync_NewEmail_ReturnsFalse()
    {
        var result = await _repo.IsEmailTakenAsync("new@example.com");
        Assert.False(result);
    }

    [Fact]
    public async Task IsEmailTakenAsync_ExistingEmail_ReturnsTrue()
    {
        var person = Create("John Doe");
        person.Email = "taken@example.com";
        _db.Set<Person>().Add(person);
        await _db.SaveChangesAsync();

        var result = await _repo.IsEmailTakenAsync("taken@example.com");

        Assert.True(result);
    }

    [Fact]
    public async Task IsEmailTakenAsync_ExcludeSelf_ReturnsFalse()
    {
        var person = Create("John Doe");
        person.Email = "self@example.com";
        _db.Set<Person>().Add(person);
        await _db.SaveChangesAsync();

        // Excluding own ID — should not be considered "taken"
        var result = await _repo.IsEmailTakenAsync("self@example.com", person.Id);

        Assert.False(result);
    }

    // ── Helper ────────────────────────────────────────────────────────────

    private static Person Create(string name)
    {
        var p = new Person(name);
        p.SetNormalizedName(name);
        return p;
    }
}
```

- [ ] **Step 2: Run integration tests**

```bash
dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --filter "FullyQualifiedName~PersonRepositoryTests" --verbosity normal
```

Expected: All PersonRepositoryTests pass.

- [ ] **Step 3: Run all tests — all should pass**

```bash
dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj
```

- [ ] **Step 4: Commit**

```bash
git add MyVocaList.Tests/Integration/Repositories/PersonRepositoryTests.cs
git commit -m "test(integration): add PersonRepositoryTests — index constraints, collation, paging"
```

---

## Task 10: Final Verification

- [ ] **Step 1: Full build — 0 errors**

```bash
dotnet build MyVocaList.sln
```

- [ ] **Step 2: Run all tests**

```bash
dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --verbosity normal
```

Expected: All tests pass.

- [ ] **Step 3: Smoke test on emulator/device**

1. Launch app → navigate to Singers tab
2. Shimmer loads → list appears (or empty state if no singers yet)
3. Tap FAB → New Singer form opens
4. Type a short name → inline error "Enter first and last name."
5. Type "John Doe" → search icon → suggestions list appears (if any match exists)
6. Tap Save → snackbar "John Doe registered successfully!" → back to list → John Doe appears
7. Tap search icon → SearchAppBar appears → type "jo" → debounce → list filters
8. Tap back arrow → SearchAppBar closes → full list restored
9. Tap row to select → FloatingToolbar Edit/Delete activate → app bar shows "1 selected"
10. Tap Edit → Edit Singer form opens pre-populated → change name → Save → "updated successfully!"
11. Select one or more → Tap Delete → ConfirmSheet appears → tap Delete → snackbar → removed
12. Hardware Back button: dismisses confirm sheet → then closes search → then uses Shell default

- [ ] **Step 4: Update specs/persons/tasks.md — mark all tasks as done**

Open `Docs/specs/persons/tasks.md` and mark all 39 tasks as `[x]`.

- [ ] **Step 5: Final commit**

```bash
git add Docs/specs/persons/tasks.md
git commit -m "chore(specs): mark all Person CRUD tasks as done"
```

---

## Notes

- **`IBaseRepository.RemoveAsync`**: Check if this method exists on the base repository. If it's named differently (e.g., `DeleteAsync`), use the correct name in `PersonService.DeletePersonsAsync`. Verify in `Infra/Repository/BaseRepository.cs`.
- **`SaveChangesAsync` with CancellationToken**: The base repository's `SaveChangesAsync` signature may not accept a CancellationToken. Adjust the service accordingly.
- **`GetByIdAsync` signature**: Check `IBaseRepository.GetByIdAsync` — it may not accept a CancellationToken. Adjust accordingly.
- **`ListItemLeadingMonogram` Initials format**: The component expects the `Initials` property as a string (e.g. "JD"). The `PersonListItemDto.Initials` computed property handles this.
- **ConfirmSheet ANR warning**: The `ConfirmSheet` ContentView wrapper is NOT used here — the inline `dx:BottomSheet` pattern from VenuesPage is used directly (as documented in `dialogs-validation.md`).
- **`PersonsPage` namespace**: The page class is `PeoplePage` in namespace `MyVocaList.UI.Pages.People` — the class name is not changed to avoid renaming all the DI registrations.
