using MyVocaList.Domain.Exceptions;
using MyVocaList.Extensions.Strings;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Unit.Services;

public class PersonServiceTests
{
    private readonly Mock<IPersonRepository> _repoMock = new();
    private readonly Mock<ILogger<PersonService>> _loggerMock = new();

    // The service now takes IUnitOfWork; PassthroughUnitOfWork runs each wrapped body inline
    // against this same mock, so every existing assertion keeps its original meaning
    // (`plan.md` Task 4.2).
    private PersonService CreateSut() => new(
        _repoMock.Object,
        PassthroughUnitOfWork.Over(_repoMock),
        _loggerMock.Object);

    // ── GetCharacterCounterInfo ────────────────────────────────────────────────

    [Fact]
    // [AC] Counter threshold alignment (Form Validation Standard): the counter reports an error
    // only when ValidateNameInput would reject the same length. 200 chars is valid → no error.
    public void GetCharacterCounterInfo_AtMaxLength200_IsNotError()
    {
        var sut = CreateSut();

        var (_, _, isError) = sut.GetCharacterCounterInfo(200);

        Assert.False(isError);
    }

    [Fact]
    // [AC] Counter threshold alignment: 201 chars is rejected by the validator → counter error.
    public void GetCharacterCounterInfo_OverMaxLength_IsError()
    {
        var sut = CreateSut();

        var (_, _, isError) = sut.GetCharacterCounterInfo(201);

        Assert.True(isError);
    }

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

    // [AC] REQ-BDAY-05: both null is valid (birthday is optional)
    [Fact]
    public void ValidateBirthday_BothNull_ReturnsValid()
    {
        var (isValid, message) = CreateSut().ValidateBirthday(null, null);
        Assert.True(isValid);
        Assert.Equal("", message);
    }

    // [AC] REQ-BDAY-05: exactly one of day or month is rejected
    [Fact]
    public void ValidateBirthday_OnlyDay_ReturnsSelectBothMessage()
    {
        var (isValid, message) = CreateSut().ValidateBirthday(15, null);
        Assert.False(isValid);
        Assert.Equal("Select both day and month", message);
    }

    // [AC] REQ-BDAY-05: exactly one of day or month is rejected
    [Fact]
    public void ValidateBirthday_OnlyMonth_ReturnsSelectBothMessage()
    {
        var (isValid, message) = CreateSut().ValidateBirthday(null, 3);
        Assert.False(isValid);
        Assert.Equal("Select both day and month", message);
    }

    // [AC] REQ-BDAY-05: month outside 1 to 12 is rejected
    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void ValidateBirthday_MonthOutOfRange_ReturnsMonthMessage(int month)
    {
        var (isValid, message) = CreateSut().ValidateBirthday(15, month);
        Assert.False(isValid);
        Assert.Equal("Month must be between 1 and 12", message);
    }

    // [AC] REQ-BDAY-05: day outside 1 to 31 is rejected
    [Theory]
    [InlineData(0)]
    [InlineData(32)]
    public void ValidateBirthday_DayOutOfRange_ReturnsInvalidDateMessage(int day)
    {
        var (isValid, message) = CreateSut().ValidateBirthday(day, 1);
        Assert.False(isValid);
        Assert.Equal("Invalid date for this month", message);
    }

    // [AC] REQ-BDAY-05: 31 is not valid for April
    [Fact]
    public void ValidateBirthday_31Apr_ReturnsInvalidDateMessage()
    {
        var (isValid, message) = CreateSut().ValidateBirthday(31, 4);
        Assert.False(isValid);
        Assert.Equal("Invalid date for this month", message);
    }

    // [AC] REQ-BDAY-05: 29 February is valid (no year is stored)
    [Fact]
    public void ValidateBirthday_29Feb_ReturnsValid()
    {
        var (isValid, _) = CreateSut().ValidateBirthday(29, 2);
        Assert.True(isValid);
    }

    // [AC] REQ-BDAY-05: 30 February is rejected
    [Fact]
    public void ValidateBirthday_30Feb_ReturnsInvalidDateMessage()
    {
        var (isValid, message) = CreateSut().ValidateBirthday(30, 2);
        Assert.False(isValid);
        Assert.Equal("Invalid date for this month", message);
    }

    // [AC] REQ-BDAY-05: a valid day and month pair is accepted
    [Fact]
    public void ValidateBirthday_15Mar_ReturnsValid()
    {
        var (isValid, _) = CreateSut().ValidateBirthday(15, 3);
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

    // ── SearchPersonsAsync (BUG-046) ────────────────────────────────────────────

    // [AC] REQ-TRIM-01: whitespace-polluted query produces the same repo call as the clean query
    [Theory]
    [InlineData("  jo")]
    [InlineData("jo ")]
    [InlineData(" jo  hn ")]
    public async Task SearchPersonsAsync_ExtraWhitespace_ForwardsNormalizedTermToRepository(string dirty)
    {
        var expected = dirty.NormalizeSearchQuery();
        _repoMock
            .Setup(r => r.SearchByNameStartsWithAsync(expected, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Person>())
            .Verifiable();

        await CreateSut().SearchPersonsAsync(dirty);

        _repoMock.Verify(); // fails before fix: repo receives the raw dirty term
    }

    // [AC] REQ-TRIM-04: query that normalizes below min length returns empty WITHOUT hitting the repo
    [Theory]
    [InlineData(" a ")]
    [InlineData("  ")]
    [InlineData(null)]
    public async Task SearchPersonsAsync_NormalizesBelowMinLength_ReturnsEmptyWithoutRepositoryCall(string dirty)
    {
        var result = await CreateSut().SearchPersonsAsync(dirty);

        Assert.Empty(result);
        _repoMock.Verify(
            r => r.SearchByNameStartsWithAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── SearchPersonsStartsWithAsync (BUG-046) ──────────────────────────────────

    // [AC] REQ-TRIM-01: whitespace-polluted query produces the same repo call as the clean query
    [Theory]
    [InlineData("  jo")]
    [InlineData("jo ")]
    [InlineData(" jo  hn ")]
    public async Task SearchPersonsStartsWithAsync_ExtraWhitespace_ForwardsNormalizedTermToRepository(string dirty)
    {
        var expected = dirty.NormalizeSearchQuery();
        _repoMock
            .Setup(r => r.SearchByNameStartsWithAsync(expected, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Person>())
            .Verifiable();

        await CreateSut().SearchPersonsStartsWithAsync(dirty);

        _repoMock.Verify(); // fails before fix: repo receives the raw dirty term
    }

    // [AC] REQ-TRIM-04: query that normalizes below min length returns empty WITHOUT hitting the repo
    [Theory]
    [InlineData(" a ")]
    [InlineData("  ")]
    [InlineData(null)]
    public async Task SearchPersonsStartsWithAsync_NormalizesBelowMinLength_ReturnsEmptyWithoutRepositoryCall(string dirty)
    {
        var result = await CreateSut().SearchPersonsStartsWithAsync(dirty);

        Assert.Empty(result);
        _repoMock.Verify(
            r => r.SearchByNameStartsWithAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
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
            new("John Doe") { BirthDay = 15, BirthMonth = 3, Email = "john@example.com", Participations = 3, Absences = 1 }
        };
        _repoMock.Setup(r => r.GetPagedAsync(1, 20, null, default))
                 .ReturnsAsync((persons.AsEnumerable(), 1));

        var (items, totalCount) = await CreateSut().GetPagedPersonsForListAsync(1, 20);

        Assert.Equal(1, totalCount);
        var dto = Assert.Single(items);
        Assert.Equal("John Doe", dto.FullName);
        Assert.Equal(15, dto.BirthDay);
        Assert.Equal(3, dto.BirthMonth);
        Assert.Equal("john@example.com", dto.Email);
        Assert.Equal(3, dto.Participations);
    }

    // [AC] REQ-TRIM-03: list search term with extra whitespace is normalized before hitting the repository
    [Fact]
    public async Task GetPagedPersonsForListAsync_ExtraWhitespaceQuery_ForwardsNormalizedTermToRepository()
    {
        _repoMock.Setup(r => r.GetPagedAsync(1, 20, "jo hn", default))
                 .ReturnsAsync((Enumerable.Empty<Person>(), 0))
                 .Verifiable();

        await CreateSut().GetPagedPersonsForListAsync(1, 20, " jo  hn ");

        _repoMock.Verify();
    }

    // ── UpdatePersonAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task UpdatePersonAsync_PersonNotFound_ReturnsFalse()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99))
                 .ReturnsAsync((Person)null);

        var (success, message) = await CreateSut().UpdatePersonAsync(99, "John Doe", null, null, null);

        Assert.False(success);
        Assert.NotEmpty(message);
    }

    [Fact]
    public async Task UpdatePersonAsync_InvalidName_ReturnsFalse()
    {
        var person = new Person("Old Name");
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(person);

        var (success, _) = await CreateSut().UpdatePersonAsync(1, "A", null, null, null);   // too short

        Assert.False(success);
    }

    [Fact]
    public async Task UpdatePersonAsync_EmailTakenByOther_ReturnsFalse()
    {
        var person = new Person("John Doe");
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(person);
        _repoMock.Setup(r => r.IsEmailTakenAsync("taken@example.com", 1, default))
                 .ReturnsAsync(true);

        var (success, message) = await CreateSut().UpdatePersonAsync(1, "John Doe", null, null, "taken@example.com");

        Assert.False(success);
        Assert.Contains("Email", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdatePersonAsync_Valid_SavesAndReturnsTrue()
    {
        var person = new Person("Old Name");
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(person);
        _repoMock.Setup(r => r.IsEmailTakenAsync(It.IsAny<string>(), It.IsAny<int?>(), default))
                 .ReturnsAsync(false);

        var (success, message) = await CreateSut().UpdatePersonAsync(1, "John Doe", null, null, "john@example.com");

        Assert.True(success);
        Assert.Contains("updated", message, StringComparison.OrdinalIgnoreCase);
    }

    // [AC] REQ-BDAY-05: a one-sided birthday is rejected on update and nothing is saved
    [Fact]
    public async Task UpdatePersonAsync_DayWithoutMonth_ReturnsSelectBothAndDoesNotSave()
    {
        var (success, message) = await CreateSut().UpdatePersonAsync(1, "John Doe", 15, null, null);

        Assert.False(success);
        Assert.Equal("Select both day and month", message);
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Person>()), Times.Never);
    }

    // [AC] REQ-BDAY-07: the service assigns the day and month to the entity on update
    [Fact]
    public async Task UpdatePersonAsync_ValidBirthday_AssignsDayAndMonth()
    {
        var person = new Person("Old Name");
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(person);

        var (success, _) = await CreateSut().UpdatePersonAsync(1, "John Doe", 15, 3, null);

        Assert.True(success);
        Assert.Equal(15, person.BirthDay);
        Assert.Equal(3, person.BirthMonth);
    }

    // [AC] REQ-BDAY-07: the service assigns the day and month to the new entity on create
    [Fact]
    public async Task CreatePersonAsync_ValidBirthday_StoresDayAndMonth()
    {
        var (success, _, person) = await CreateSut().CreatePersonAsync("John Doe", 15, 3, null);

        Assert.True(success);
        Assert.NotNull(person);
        Assert.Equal(15, person!.BirthDay);
        Assert.Equal(3, person.BirthMonth);
    }

    // [AC] REQ-BDAY-05: a one-sided birthday is rejected on create and nothing is saved
    [Fact]
    public async Task CreatePersonAsync_MonthWithoutDay_ReturnsSelectBothAndDoesNotSave()
    {
        var (success, message, person) = await CreateSut().CreatePersonAsync("John Doe", null, 3, null);

        Assert.False(success);
        Assert.Equal("Select both day and month", message);
        Assert.Null(person);
        _repoMock.Verify(r => r.AddAsync(It.IsAny<Person>()), Times.Never);
    }

    // [AC] REQ-DUP-01: an identity that already exists is rejected on create, the tuple carries the duplicate message, nothing is saved
    [Fact]
    public async Task CreatePersonAsync_IdentityExists_ReturnsDuplicateMessage()
    {
        _repoMock.Setup(r => r.IdentityExistsAsync(
                It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<int?>(),
                It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var (success, message, person) = await CreateSut().CreatePersonAsync("John Doe", 15, 3, "john@example.com");

        Assert.False(success);
        Assert.Equal("This singer is already registered.", message);
        Assert.Null(person);
        _repoMock.Verify(r => r.AddAsync(It.IsAny<Person>()), Times.Never);
    }

    // [AC] REQ-DUP-03: neither email nor birthday on create is rejected before any save
    [Fact]
    public async Task CreatePersonAsync_NoEmailNoBirthday_ReturnsEitherOrMessageAndDoesNotSave()
    {
        var (success, message, person) = await CreateSut().CreatePersonAsync("John Doe", null, null, null);

        Assert.False(success);
        Assert.Equal("Enter an email or a birthday.", message);
        Assert.Null(person);
        _repoMock.Verify(r => r.AddAsync(It.IsAny<Person>()), Times.Never);
    }

    // [AC] REQ-DUP-03: neither email nor birthday on update is rejected before any save
    [Fact]
    public async Task UpdatePersonAsync_NoEmailNoBirthday_ReturnsEitherOrMessageAndDoesNotSave()
    {
        var (success, message) = await CreateSut().UpdatePersonAsync(1, "John Doe", null, null, null);

        Assert.False(success);
        Assert.Equal("Enter an email or a birthday.", message);
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Person>()), Times.Never);
    }

    // [AC] REQ-DUP-03: ValidateContact accepts an email alone or a birthday alone, and rejects neither
    [Fact]
    public void ValidateContact_EmailOnly_ReturnsValid()
    {
        var (isValid, _) = CreateSut().ValidateContact("john@example.com", null, null);
        Assert.True(isValid);
    }

    [Fact]
    public void ValidateContact_BirthdayOnly_ReturnsValid()
    {
        var (isValid, _) = CreateSut().ValidateContact(null, 15, 3);
        Assert.True(isValid);
    }

    [Fact]
    public void ValidateContact_NeitherEmailNorBirthday_ReturnsEitherOrMessage()
    {
        var (isValid, message) = CreateSut().ValidateContact("  ", null, null);
        Assert.False(isValid);
        Assert.Equal("Enter an email or a birthday.", message);
    }

    // [AC] REQ-DUP-02: another person (not the one being edited) already has the resulting identity; nothing is saved
    [Fact]
    public async Task UpdatePersonAsync_OtherPersonHasIdentity_ReturnsDuplicateMessage()
    {
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Person("Old Name"));
        _repoMock.Setup(r => r.IdentityExistsAsync(
                It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<int?>(),
                1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var (success, message) = await CreateSut().UpdatePersonAsync(1, "John Doe", 15, 3, "john@example.com");

        Assert.False(success);
        Assert.Equal("This singer is already registered.", message);
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Person>()), Times.Never);
    }

    // [AC] REQ-DUP-02: the person's own identity is not a duplicate, because the check excludes the person being edited
    [Fact]
    public async Task UpdatePersonAsync_OwnIdentity_Passes()
    {
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Person("Old Name"));
        _repoMock.Setup(r => r.IdentityExistsAsync(
                It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<int?>(),
                It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .Returns((string n, string? e, int? d, int? m, int? excludeId, CancellationToken ct)
                => Task.FromResult(excludeId is null));

        var (success, _) = await CreateSut().UpdatePersonAsync(1, "John Doe", 15, 3, "john@example.com");

        Assert.True(success);
    }

    // [AC] REQ-DUP-06: a constraint violation at save time maps to the duplicate tuple instead of throwing
    [Fact]
    public async Task CreatePersonAsync_ConstraintViolationOnSave_ReturnsDuplicateMessage()
    {
        _repoMock.Setup(r => r.AddAsync(It.IsAny<Person>()))
            .ThrowsAsync(new ConstraintViolationException("constraint", new Exception("UNIQUE")));

        var (success, message, person) = await CreateSut().CreatePersonAsync("John Doe", 15, 3, "john@example.com");

        Assert.False(success);
        Assert.Equal("This singer is already registered.", message);
        Assert.Null(person);
    }

    // [AC] REQ-DUP-06: the same mapping on update
    [Fact]
    public async Task UpdatePersonAsync_ConstraintViolationOnSave_ReturnsDuplicateMessage()
    {
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Person("Old Name"));
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Person>()))
            .ThrowsAsync(new ConstraintViolationException("constraint", new Exception("UNIQUE")));

        var (success, message) = await CreateSut().UpdatePersonAsync(1, "John Doe", 15, 3, "john@example.com");

        Assert.False(success);
        Assert.Equal("This singer is already registered.", message);
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
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(p1);
        _repoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(p2);
        _repoMock.Setup(r => r.DeleteAsync(It.IsAny<Person>())).Returns(Task.CompletedTask);

        var (success, message) = await CreateSut().DeletePersonsAsync([1, 2]);

        Assert.True(success);
        Assert.Contains("2", message);
    }
}
