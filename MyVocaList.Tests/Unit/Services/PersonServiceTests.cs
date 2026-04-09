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
        _repoMock.Setup(r => r.GetByIdAsync(99))
                 .ReturnsAsync((Person)null);

        var (success, message) = await CreateSut().UpdatePersonAsync(99, "John Doe");

        Assert.False(success);
        Assert.NotEmpty(message);
    }

    [Fact]
    public async Task UpdatePersonAsync_InvalidName_ReturnsFalse()
    {
        var person = new Person("Old Name");
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(person);

        var (success, _) = await CreateSut().UpdatePersonAsync(1, "A");   // too short

        Assert.False(success);
    }

    [Fact]
    public async Task UpdatePersonAsync_EmailTakenByOther_ReturnsFalse()
    {
        var person = new Person("John Doe");
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(person);
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
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(person);
        _repoMock.Setup(r => r.IsEmailTakenAsync(It.IsAny<string>(), It.IsAny<int?>(), default))
                 .ReturnsAsync(false);
        _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

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
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(p1);
        _repoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(p2);
        _repoMock.Setup(r => r.DeleteAsync(It.IsAny<Person>())).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var (success, message) = await CreateSut().DeletePersonsAsync([1, 2]);

        Assert.True(success);
        Assert.Contains("2", message);
    }
}
