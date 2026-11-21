using MyVocaList.Domain.Constants;
using MyVocaList.Domain.Entity;
using MyVocaList.Domain.ReadModels;
using MyVocaList.UI.Models;
using MyVocaList.UI.Services;

namespace MyVocaList.Tests.Unit.ViewModels;

public class PersonFormViewModelTests
{
    private readonly Mock<IPersonService> _serviceMock = new();
    private readonly Mock<ISnackbarComponent> _snackMock = new();
    private readonly Mock<INavigationService> _navigationMock = new();
    private readonly Mock<ILogger<PersonFormViewModel>> _loggerMock = new();

    // BUG-088: the same-name query is empty by default, so each save test only sets up the path it covers
    // (the sheet rules themselves are tested in PersonFormViewModelBug088Tests).
    public PersonFormViewModelTests()
    {
        _serviceMock.Setup(s => s.GetSameNamePersonsAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SameNameResult([], 0));
    }

    private PersonFormViewModel CreateSut() =>
        new(_serviceMock.Object, _snackMock.Object, _navigationMock.Object, _loggerMock.Object);

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
        sut.SelectedBirthDay = 15;

        _serviceMock.Setup(s => s.ValidateNameInput("John Doe")).Returns((true, ""));
        _serviceMock.Setup(s => s.ValidateBirthday(15, null)).Returns((false, "Select both day and month"));

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.True(sut.BirthdayHasError);
        Assert.NotEmpty(sut.BirthdayErrorText);
    }

    [Fact]
    public async Task SaveCommand_InvalidEmail_SetsEmailHasError()
    {
        var sut = CreateSut();
        sut.PersonName = "John Doe";
        sut.PersonEmail = "notanemail";

        _serviceMock.Setup(s => s.ValidateNameInput("John Doe")).Returns((true, ""));
        _serviceMock.Setup(s => s.ValidateBirthday(null, null)).Returns((true, ""));
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
        sut.PersonEmail = "";

        _serviceMock.Setup(s => s.ValidateNameInput("John Doe")).Returns((true, ""));
        _serviceMock.Setup(s => s.ValidateBirthday(null, null)).Returns((true, ""));
        _serviceMock.Setup(s => s.ValidateEmail(null)).Returns((true, ""));
        _serviceMock.Setup(s => s.CreatePersonAsync("John Doe", null, null, null, default))
                    .ReturnsAsync((true, "John Doe registered successfully!", new Person("John Doe")));
        _snackMock.Setup(s => s.ShowSuccessAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

        await sut.SaveCommand.ExecuteAsync(null);

        _serviceMock.Verify(s => s.CreatePersonAsync("John Doe", null, null, null, default), Times.Once);
    }

    [Fact]
    public async Task SaveCommand_EditMode_ValidInputs_CallsUpdateAsync()
    {
        var sut = CreateSut();
        sut.PersonId = 5;
        sut.PersonName = "John Doe";
        sut.PersonEmail = "";

        _serviceMock.Setup(s => s.ValidateNameInput("John Doe")).Returns((true, ""));
        _serviceMock.Setup(s => s.ValidateBirthday(null, null)).Returns((true, ""));
        _serviceMock.Setup(s => s.ValidateEmail(null)).Returns((true, ""));
        _serviceMock.Setup(s => s.UpdatePersonAsync(5, "John Doe", null, null, null, default))
                    .ReturnsAsync((true, "John Doe updated successfully!"));
        _snackMock.Setup(s => s.ShowSuccessAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

        await sut.SaveCommand.ExecuteAsync(null);

        _serviceMock.Verify(s => s.UpdatePersonAsync(5, "John Doe", null, null, null, default), Times.Once);
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

    // ── Birthday dropdowns: Day list, month clamp, validation, hydration ─────

    [Theory]
    // [AC] REQ-BDAY-02: The Birth day list holds only the days valid for the selected month.
    [InlineData(1, 31)]
    [InlineData(2, 29)]
    [InlineData(4, 30)]
    [InlineData(9, 30)]
    [InlineData(12, 31)]
    public void SelectedBirthMonth_Change_RebuildsDaysForThatMonth(int month, int expectedDays)
    {
        var sut = CreateSut();

        sut.SelectedBirthMonth = sut.Months.Single(m => m.Number == month);

        Assert.Equal(expectedDays, sut.Days.Count);
        Assert.Equal(1, sut.Days.First());
        Assert.Equal(expectedDays, sut.Days.Last());
    }

    [Fact]
    // [AC] REQ-BDAY-02: With no month selected, the Birth day list holds 1 to 31.
    public void Days_NoMonthSelected_Holds1To31()
    {
        var sut = CreateSut();

        Assert.Null(sut.SelectedBirthMonth);
        Assert.Equal(31, sut.Days.Count);
        Assert.Equal(31, sut.Days.Last());
    }

    [Fact]
    // [AC] REQ-BDAY-02: The Months list holds the twelve English month names in order.
    public void Months_HoldsEnglishNamesJanuaryToDecember()
    {
        var sut = CreateSut();

        Assert.Equal(12, sut.Months.Count);
        Assert.Equal("January", sut.Months[0].Name);
        Assert.Equal(1, sut.Months[0].Number);
        Assert.Equal("December", sut.Months[11].Name);
        Assert.Equal(12, sut.Months[11].Number);
    }

    [Fact]
    // [AC] REQ-BDAY-03: Choosing a month that cannot hold the selected day clears the day (31, then April).
    public void SelectedBirthMonth_DayInvalidForNewMonth_ClearsDay()
    {
        var sut = CreateSut();
        sut.SelectedBirthMonth = sut.Months.Single(m => m.Number == 3);
        sut.SelectedBirthDay = 31;

        sut.SelectedBirthMonth = sut.Months.Single(m => m.Number == 4);

        Assert.Null(sut.SelectedBirthDay);
        Assert.Equal(30, sut.Days.Count);
    }

    [Fact]
    // [AC] REQ-BDAY-03: Choosing a month that still allows the selected day keeps the day.
    public void SelectedBirthMonth_DayValidForNewMonth_KeepsDay()
    {
        var sut = CreateSut();
        sut.SelectedBirthMonth = sut.Months.Single(m => m.Number == 3);
        sut.SelectedBirthDay = 15;

        sut.SelectedBirthMonth = sut.Months.Single(m => m.Number == 4);

        Assert.Equal(15, sut.SelectedBirthDay);
    }

    [Fact]
    // [AC] REQ-BDAY-06: Save with a day but no month shows the error on the empty Birth month dropdown and does not save.
    public async Task SaveCommand_DayWithoutMonth_ShowsErrorOnMonthAndDoesNotSave()
    {
        var sut = CreateSut();
        sut.PersonName = "John Doe";
        sut.SelectedBirthDay = 15;
        _serviceMock.Setup(s => s.ValidateNameInput("John Doe")).Returns((true, ""));
        _serviceMock.Setup(s => s.ValidateBirthday(15, null)).Returns((false, "Select both day and month"));

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.True(sut.BirthdayHasError);
        Assert.True(sut.BirthdayErrorOnMonth);
        Assert.False(sut.BirthdayErrorOnDay);
        Assert.Equal("Select both day and month", sut.BirthdayErrorText);
        _serviceMock.Verify(s => s.CreatePersonAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>(), default), Times.Never);
    }

    [Fact]
    // [AC] REQ-BDAY-06: Save with a month but no day shows the error on the empty Birth day dropdown.
    public async Task SaveCommand_MonthWithoutDay_ShowsErrorOnDayAndDoesNotSave()
    {
        var sut = CreateSut();
        sut.PersonName = "John Doe";
        sut.SelectedBirthMonth = sut.Months.Single(m => m.Number == 3);
        _serviceMock.Setup(s => s.ValidateNameInput("John Doe")).Returns((true, ""));
        _serviceMock.Setup(s => s.ValidateBirthday(null, 3)).Returns((false, "Select both day and month"));

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.True(sut.BirthdayErrorOnDay);
        Assert.False(sut.BirthdayErrorOnMonth);
        _serviceMock.Verify(s => s.CreatePersonAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>(), default), Times.Never);
    }

    [Fact]
    // [AC] REQ-BDAY-06: While the birthday is in error, a selection that makes it valid clears the error at once.
    public async Task BirthdayDropdown_WhileInError_ClearsErrorWhenValid()
    {
        var sut = CreateSut();
        sut.PersonName = "John Doe";
        sut.SelectedBirthDay = 15;
        _serviceMock.Setup(s => s.ValidateNameInput("John Doe")).Returns((true, ""));
        _serviceMock.Setup(s => s.ValidateBirthday(15, null)).Returns((false, "Select both day and month"));
        await sut.SaveCommand.ExecuteAsync(null);
        Assert.True(sut.BirthdayHasError);

        _serviceMock.Setup(s => s.ValidateBirthday(15, 3)).Returns((true, ""));
        sut.SelectedBirthMonth = sut.Months.Single(m => m.Number == 3);

        Assert.False(sut.BirthdayHasError);
        Assert.Empty(sut.BirthdayErrorText);
    }

    [Fact]
    // [AC] REQ-BDAY-06: While the birthday is not in error, a selection does not run validation.
    public void BirthdayDropdown_NotInError_DoesNotValidate()
    {
        var sut = CreateSut();

        sut.SelectedBirthDay = 15;

        Assert.False(sut.BirthdayHasError);
        _serviceMock.Verify(s => s.ValidateBirthday(It.IsAny<int?>(), It.IsAny<int?>()), Times.Never);
    }

    [Fact]
    // [AC] REQ-BDAY-09: Query parameters pre-select the stored day and month in edit mode.
    public void QueryParams_Hydration_PreSelectsDayAndMonth()
    {
        var sut = CreateSut();

        sut.PersonBirthMonthRaw = "2";
        sut.PersonBirthDayRaw = "29";

        Assert.Equal(29, sut.SelectedBirthDay);
        Assert.NotNull(sut.SelectedBirthMonth);
        Assert.Equal("February", sut.SelectedBirthMonth!.Name);
        Assert.Equal(29, sut.Days.Count);
    }

    [Fact]
    // [AC] REQ-BDAY-09: Hydration with empty query parameters leaves both dropdowns empty.
    public void QueryParams_EmptyValues_LeaveDropdownsEmpty()
    {
        var sut = CreateSut();

        sut.PersonBirthDayRaw = "";
        sut.PersonBirthMonthRaw = "";

        Assert.Null(sut.SelectedBirthDay);
        Assert.Null(sut.SelectedBirthMonth);
    }

    [Fact]
    // [AC] REQ-BDAY-09: Hydration does not run birthday validation.
    public void QueryParams_Hydration_DoesNotValidate()
    {
        var sut = CreateSut();

        sut.PersonBirthDayRaw = "31";
        sut.PersonBirthMonthRaw = "4";

        Assert.False(sut.BirthdayHasError);
        _serviceMock.Verify(s => s.ValidateBirthday(It.IsAny<int?>(), It.IsAny<int?>()), Times.Never);
    }

    [Fact]
    // [AC] REQ-BDAY-09: Save sends the selected day and month (as ints) to the service.
    public async Task SaveCommand_CreateMode_PassesDayAndMonthToService()
    {
        var sut = CreateSut();
        sut.PersonName = "John Doe";
        sut.SelectedBirthMonth = sut.Months.Single(m => m.Number == 3);
        sut.SelectedBirthDay = 15;
        _serviceMock.Setup(s => s.ValidateNameInput("John Doe")).Returns((true, ""));
        _serviceMock.Setup(s => s.ValidateBirthday(15, 3)).Returns((true, ""));
        _serviceMock.Setup(s => s.ValidateEmail(null)).Returns((true, ""));
        _serviceMock.Setup(s => s.CreatePersonAsync("John Doe", 15, 3, null, default))
                    .ReturnsAsync((true, "John Doe registered successfully!", new Person("John Doe")));
        _snackMock.Setup(s => s.ShowSuccessAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

        await sut.SaveCommand.ExecuteAsync(null);

        _serviceMock.Verify(s => s.CreatePersonAsync("John Doe", 15, 3, null, default), Times.Once);
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
        sut.SelectedBirthDay = 15;
        sut.PersonEmail = "notanemail";

        _serviceMock.Setup(s => s.ValidateNameInput("A")).Returns((false, "Name too short. Minimum 2 characters."));
        _serviceMock.Setup(s => s.ValidateBirthday(15, null)).Returns((false, "Select both day and month"));
        _serviceMock.Setup(s => s.ValidateEmail("notanemail")).Returns((false, "Invalid email"));

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.True(sut.NameHasError);
        Assert.True(sut.BirthdayHasError);
        Assert.True(sut.EmailHasError);
        _serviceMock.Verify(s => s.CreatePersonAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>(), default), Times.Never);
    }

    // ── Re-entrancy guard (BUG-049) ────────────────────────────────────────

    [Fact]
    // [AC] BUG-049: a fast double-tap on Save while a save is in flight must not fire the
    // create service call twice (which caused a duplicate "GoToAsync("..")" that overshot the
    // nav stack root).
    public async Task SaveCommand_DoubleInvokedWhileSaving_CallsCreateOnlyOnce()
    {
        var sut = CreateSut();
        sut.CompleteHydration();
        sut.PersonName = "Jane Doe";
        _serviceMock.Setup(s => s.ValidateNameInput("Jane Doe")).Returns((true, ""));
        _serviceMock.Setup(s => s.ValidateBirthday(null, null)).Returns((true, ""));
        _serviceMock.Setup(s => s.ValidateEmail(null)).Returns((true, ""));

        var gate = new TaskCompletionSource<(bool success, string message, Person? person)>();
        _serviceMock.Setup(s => s.CreatePersonAsync("Jane Doe", null, null, null, default)).Returns(gate.Task);

        var firstCall = sut.SaveCommand.ExecuteAsync(null);
        var secondCall = sut.SaveCommand.ExecuteAsync(null);   // simulates the second tap of a double-tap

        gate.SetResult((true, "Singer created", new Person { FullName = "Jane Doe" }));
        await Task.WhenAll(firstCall, secondCall);

        _serviceMock.Verify(s => s.CreatePersonAsync("Jane Doe", null, null, null, default), Times.Once);
    }

    // ── NameOptionSource ──────────────────────────────────────────────────

    [Fact]
    // [AC] REQ-ACF-16: the Person option source returns the search's own result list and passes the request token through; it never reads Suggestions.
    public async Task NameOptionSource_SearchAsync_ReturnsMappedServiceResults_AndPassesToken()
    {
        using var cts = new CancellationTokenSource();
        var person = new Person("John Doe");
        _serviceMock.Setup(s => s.SearchPersonsStartsWithAsync("jo", SearchConstants.MaxSearchResults, cts.Token))
                    .ReturnsAsync(new List<Person> { person });
        var sut = CreateSut();

        var result = await sut.NameOptionSource.SearchAsync("jo", cts.Token);

        var option = Assert.Single(result);
        Assert.Equal("John Doe", option.Headline);
        Assert.Same(person, option.Data);
        Assert.Empty(sut.Suggestions);   // BUG-056: the observable is neither the source nor written
        _serviceMock.Verify(s => s.SearchPersonsStartsWithAsync("jo", SearchConstants.MaxSearchResults, cts.Token), Times.Once);
    }

    [Theory]
    [InlineData("a")]
    [InlineData(" a")]
    [InlineData("a ")]
    // [AC] REQ-ACF-13: below the trimmed minimum query length the option source returns nothing and never calls the service.
    public async Task NameOptionSource_SearchAsync_BelowTrimmedThreshold_ReturnsEmptyAndSkipsService(string term)
    {
        var sut = CreateSut();

        var result = await sut.NameOptionSource.SearchAsync(term, CancellationToken.None);

        Assert.Empty(result);
        _serviceMock.Verify(s => s.SearchPersonsStartsWithAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
