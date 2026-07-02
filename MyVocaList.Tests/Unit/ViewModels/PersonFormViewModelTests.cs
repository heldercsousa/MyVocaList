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

    [Fact]
    // [AC] Counter threshold alignment: the counter must count the trimmed string, exactly what
    // ValidateNameInput checks — trailing whitespace must not inflate the count.
    public void OnPersonNameChanged_TrailingWhitespace_CounterUsesTrimmedLength()
    {
        var sut = CreateSut();
        sut.CompleteHydration();
        var name = "John " + new string('x', 174) + "      ";   // 179 trimmed, 185 untrimmed

        sut.PersonName = name;

        _serviceMock.Verify(s => s.ShouldShowCharacterCounter(179), Times.Once);
        _serviceMock.Verify(s => s.ShouldShowCharacterCounter(185), Times.Never);
    }

    // ── Name field: blur-first validation (Form Validation Standard) ──────

    [Fact]
    // [AC] R8: A pristine field the user only tabbed through must not show a blur error.
    public void ValidateNameCommand_PristineField_DoesNotSetError()
    {
        var sut = CreateSut();
        sut.CompleteHydration();

        sut.ValidateNameCommand.Execute(null);

        Assert.False(sut.NameHasError);
        Assert.Empty(sut.NameErrorText);
        _serviceMock.Verify(s => s.ValidateNameInput(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    // [AC] R1: On blur, a dirty invalid name field surfaces an inline error.
    public void ValidateNameCommand_DirtyInvalidField_SetsError()
    {
        var sut = CreateSut();
        sut.CompleteHydration();
        sut.PersonName = "A";
        _serviceMock.Setup(s => s.ValidateNameInput("A")).Returns((false, "Name too short. Minimum 2 characters."));

        sut.ValidateNameCommand.Execute(null);

        Assert.True(sut.NameHasError);
        Assert.Equal("Name too short. Minimum 2 characters.", sut.NameErrorText);
    }

    [Fact]
    // [AC] R1: On blur, a dirty valid name field shows no error.
    public void ValidateNameCommand_DirtyValidField_NoError()
    {
        var sut = CreateSut();
        sut.CompleteHydration();
        sut.PersonName = "John Doe";
        _serviceMock.Setup(s => s.ValidateNameInput("John Doe")).Returns((true, ""));

        sut.ValidateNameCommand.Execute(null);

        Assert.False(sut.NameHasError);
        Assert.Empty(sut.NameErrorText);
    }

    [Fact]
    // [AC] R3: While NOT in error, keystrokes do not run validation (no "impatient teacher").
    public void OnPersonNameChanged_NotInError_DoesNotValidate()
    {
        var sut = CreateSut();
        sut.CompleteHydration();

        sut.PersonName = "A";   // invalid, but field is not yet in error

        Assert.False(sut.NameHasError);
        _serviceMock.Verify(s => s.ValidateNameInput(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    // [AC] R2: While in error, a keystroke that makes the field valid clears the error immediately.
    public void OnPersonNameChanged_WhileInError_ClearsErrorWhenValid()
    {
        var sut = CreateSut();
        sut.CompleteHydration();
        sut.PersonName = "A";
        _serviceMock.Setup(s => s.ValidateNameInput("A")).Returns((false, "Name too short. Minimum 2 characters."));
        sut.ValidateNameCommand.Execute(null);
        Assert.True(sut.NameHasError);

        _serviceMock.Setup(s => s.ValidateNameInput("John Doe")).Returns((true, ""));
        sut.PersonName = "John Doe";

        Assert.False(sut.NameHasError);
        Assert.Empty(sut.NameErrorText);
    }

    [Fact]
    // [AC] R2: While in error and still invalid, a keystroke keeps the error and updates the message.
    public void OnPersonNameChanged_WhileInError_StillInvalid_KeepsError()
    {
        var sut = CreateSut();
        sut.CompleteHydration();
        sut.PersonName = "A";
        _serviceMock.Setup(s => s.ValidateNameInput("A")).Returns((false, "Name too short. Minimum 2 characters."));
        sut.ValidateNameCommand.Execute(null);
        Assert.True(sut.NameHasError);

        _serviceMock.Setup(s => s.ValidateNameInput("")).Returns((false, "Name is required"));
        sut.PersonName = "";

        Assert.True(sut.NameHasError);
        Assert.Equal("Name is required", sut.NameErrorText);
    }

    [Fact]
    // [AC] Edit-mode dirty-guard: hydration (query-property pre-population) must not mark the name field
    // dirty, so a pre-filled invalid value does not flash an error before the user interacts.
    public void OnPersonNameChanged_DuringHydration_DoesNotDirtyOrValidateOnBlur()
    {
        var sut = CreateSut();   // _isHydrating still true — CompleteHydration() not called

        sut.PersonName = "A";                 // simulates Shell QueryProperty pre-population
        sut.ValidateNameCommand.Execute(null); // simulates a blur that happens before OnAppearing runs

        Assert.False(sut.NameHasError);
        Assert.Empty(sut.NameErrorText);
        _serviceMock.Verify(s => s.ValidateNameInput(It.IsAny<string>()), Times.Never);
    }

    // ── Birthday field: blur-first validation ──────────────────────────────

    [Fact]
    // [AC] R8: A pristine birthday field must not show a blur error.
    public void ValidateBirthdayCommand_PristineField_DoesNotSetError()
    {
        var sut = CreateSut();
        sut.CompleteHydration();

        sut.ValidateBirthdayCommand.Execute(null);

        Assert.False(sut.BirthdayHasError);
        Assert.Empty(sut.BirthdayErrorText);
        _serviceMock.Verify(s => s.ValidateBirthday(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    // [AC] R1: On blur, a dirty invalid birthday field surfaces an inline error.
    public void ValidateBirthdayCommand_DirtyInvalidField_SetsError()
    {
        var sut = CreateSut();
        sut.CompleteHydration();
        sut.PersonBirthday = "99/99";
        _serviceMock.Setup(s => s.ValidateBirthday("99/99")).Returns((false, "Use DD/MM format (e.g.: 15/03)"));

        sut.ValidateBirthdayCommand.Execute(null);

        Assert.True(sut.BirthdayHasError);
        Assert.Equal("Use DD/MM format (e.g.: 15/03)", sut.BirthdayErrorText);
    }

    [Fact]
    // [AC] R3: While NOT in error, birthday keystrokes do not run validation.
    public void OnPersonBirthdayChanged_NotInError_DoesNotValidate()
    {
        var sut = CreateSut();
        sut.CompleteHydration();

        sut.PersonBirthday = "99/99";

        Assert.False(sut.BirthdayHasError);
        _serviceMock.Verify(s => s.ValidateBirthday(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    // [AC] R2: While in error, a keystroke that makes the birthday valid clears the error immediately.
    public void OnPersonBirthdayChanged_WhileInError_ClearsErrorWhenValid()
    {
        var sut = CreateSut();
        sut.CompleteHydration();
        sut.PersonBirthday = "99/99";
        _serviceMock.Setup(s => s.ValidateBirthday("99/99")).Returns((false, "Use DD/MM format (e.g.: 15/03)"));
        sut.ValidateBirthdayCommand.Execute(null);
        Assert.True(sut.BirthdayHasError);

        _serviceMock.Setup(s => s.ValidateBirthday("15/03")).Returns((true, ""));
        sut.PersonBirthday = "15/03";

        Assert.False(sut.BirthdayHasError);
        Assert.Empty(sut.BirthdayErrorText);
    }

    [Fact]
    // [AC] Edit-mode dirty-guard: hydration must not mark the birthday field dirty.
    public void OnPersonBirthdayChanged_DuringHydration_DoesNotDirtyOrValidateOnBlur()
    {
        var sut = CreateSut();   // hydrating — CompleteHydration() not called

        sut.PersonBirthday = "99/99";               // simulates pre-population with a stale/invalid value
        sut.ValidateBirthdayCommand.Execute(null);

        Assert.False(sut.BirthdayHasError);
        Assert.Empty(sut.BirthdayErrorText);
        _serviceMock.Verify(s => s.ValidateBirthday(It.IsAny<string>()), Times.Never);
    }

    // ── Email field: blur-first validation ─────────────────────────────────

    [Fact]
    // [AC] R8: A pristine email field must not show a blur error.
    public void ValidateEmailCommand_PristineField_DoesNotSetError()
    {
        var sut = CreateSut();
        sut.CompleteHydration();

        sut.ValidateEmailCommand.Execute(null);

        Assert.False(sut.EmailHasError);
        Assert.Empty(sut.EmailErrorText);
        _serviceMock.Verify(s => s.ValidateEmail(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    // [AC] R1: On blur, a dirty invalid email field surfaces an inline error.
    public void ValidateEmailCommand_DirtyInvalidField_SetsError()
    {
        var sut = CreateSut();
        sut.CompleteHydration();
        sut.PersonEmail = "notanemail";
        _serviceMock.Setup(s => s.ValidateEmail("notanemail")).Returns((false, "Invalid email"));

        sut.ValidateEmailCommand.Execute(null);

        Assert.True(sut.EmailHasError);
        Assert.Equal("Invalid email", sut.EmailErrorText);
    }

    [Fact]
    // [AC] R3: While NOT in error, email keystrokes do not run validation.
    public void OnPersonEmailChanged_NotInError_DoesNotValidate()
    {
        var sut = CreateSut();
        sut.CompleteHydration();

        sut.PersonEmail = "notanemail";

        Assert.False(sut.EmailHasError);
        _serviceMock.Verify(s => s.ValidateEmail(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    // [AC] R2: While in error, a keystroke that makes the email valid clears the error immediately.
    public void OnPersonEmailChanged_WhileInError_ClearsErrorWhenValid()
    {
        var sut = CreateSut();
        sut.CompleteHydration();
        sut.PersonEmail = "notanemail";
        _serviceMock.Setup(s => s.ValidateEmail("notanemail")).Returns((false, "Invalid email"));
        sut.ValidateEmailCommand.Execute(null);
        Assert.True(sut.EmailHasError);

        _serviceMock.Setup(s => s.ValidateEmail("singer@example.com")).Returns((true, ""));
        sut.PersonEmail = "singer@example.com";

        Assert.False(sut.EmailHasError);
        Assert.Empty(sut.EmailErrorText);
    }

    [Fact]
    // [AC] Edit-mode dirty-guard: hydration must not mark the email field dirty.
    public void OnPersonEmailChanged_DuringHydration_DoesNotDirtyOrValidateOnBlur()
    {
        var sut = CreateSut();   // hydrating — CompleteHydration() not called

        sut.PersonEmail = "notanemail";            // simulates pre-population with a stale/invalid value
        sut.ValidateEmailCommand.Execute(null);

        Assert.False(sut.EmailHasError);
        Assert.Empty(sut.EmailErrorText);
        _serviceMock.Verify(s => s.ValidateEmail(It.IsAny<string>()), Times.Never);
    }

    // ── Save safety net ─────────────────────────────────────────────────────

    [Fact]
    // [AC] R4: Save re-runs ALL field validators as the final safety net, not just the first invalid one.
    public async Task SaveCommand_MultipleInvalidFields_SetsHasErrorOnAllOfThem()
    {
        var sut = CreateSut();
        sut.CompleteHydration();
        sut.PersonName = "A";
        sut.PersonBirthday = "99/99";
        sut.PersonEmail = "notanemail";

        _serviceMock.Setup(s => s.ValidateNameInput("A")).Returns((false, "Name too short. Minimum 2 characters."));
        _serviceMock.Setup(s => s.ValidateBirthday("99/99")).Returns((false, "Use DD/MM format (e.g.: 15/03)"));
        _serviceMock.Setup(s => s.ValidateEmail("notanemail")).Returns((false, "Invalid email"));

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.True(sut.NameHasError);
        Assert.True(sut.BirthdayHasError);
        Assert.True(sut.EmailHasError);
        _serviceMock.Verify(s => s.CreatePersonAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), default), Times.Never);
    }
}
