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
        _serviceMock.Setup(s => s.ValidateBirthday(null)).Returns((true, ""));
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
        _serviceMock.Setup(s => s.ValidateBirthday(null)).Returns((true, ""));
        _serviceMock.Setup(s => s.ValidateEmail(null)).Returns((true, ""));
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
        _serviceMock.Setup(s => s.ValidateBirthday(null)).Returns((true, ""));
        _serviceMock.Setup(s => s.ValidateEmail(null)).Returns((true, ""));
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
