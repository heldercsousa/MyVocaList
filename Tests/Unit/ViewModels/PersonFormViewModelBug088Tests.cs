using MyVocaList.Domain.Entity;
using MyVocaList.UI.Models;
using MyVocaList.UI.Services;

namespace MyVocaList.Tests.Unit.ViewModels;

/// <summary>BUG-088 T3: either/or inline error, same-name soft warning sheet and its commands (REQ-DUP-07..10).</summary>
public class PersonFormViewModelBug088Tests
{
    private const string DuplicateMessage = "This singer is already registered.";
    private const string EitherOrMessage = "Enter an email or a birthday.";

    private readonly Mock<IPersonService> _serviceMock = new();
    private readonly Mock<ISnackbarComponent> _snackMock = new();
    private readonly Mock<INavigationService> _navigationMock = new();
    private readonly Mock<ILogger<PersonFormViewModel>> _loggerMock = new();

    public PersonFormViewModelBug088Tests()
    {
        // Valid defaults so each test only changes the rule it covers.
        _serviceMock.Setup(s => s.ValidateNameInput(It.IsAny<string>())).Returns((true, ""));
        _serviceMock.Setup(s => s.ValidateBirthday(It.IsAny<int?>(), It.IsAny<int?>())).Returns((true, ""));
        _serviceMock.Setup(s => s.ValidateEmail(It.IsAny<string>())).Returns((true, ""));
        _serviceMock.Setup(s => s.ValidateContact(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>())).Returns((true, ""));
        _serviceMock.Setup(s => s.GetSameNamePersonsAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Person>());
        _serviceMock.Setup(s => s.CreatePersonAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, "Saved", new Person("John Doe")));
        _serviceMock.Setup(s => s.UpdatePersonAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, "Saved"));
        _snackMock.Setup(s => s.ShowSuccessAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        _snackMock.Setup(s => s.ShowErrorAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
    }

    private PersonFormViewModel CreateSut() =>
        new(_serviceMock.Object, _snackMock.Object, _navigationMock.Object, _loggerMock.Object);

    private void SetupSameNameMatches(string name, params Person[] matches) =>
        _serviceMock.Setup(s => s.GetSameNamePersonsAsync(
                name, It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(matches.ToList());

    // ── REQ-DUP-07: either/or inline error on Email ─────────────────────────

    [Fact]
    // [AC] REQ-DUP-07: Save with email and birthday both empty shows the either/or error on Email and does not save
    public async Task SaveCommand_EmailAndBirthdayEmpty_ShowsEitherOrErrorOnEmailAndDoesNotCallService()
    {
        var sut = CreateSut();
        sut.PersonName = "John Doe";
        _serviceMock.Setup(s => s.ValidateContact(It.IsAny<string>(), null, null)).Returns((false, EitherOrMessage));

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.True(sut.EmailHasError);
        Assert.Equal(EitherOrMessage, sut.EmailErrorText);
        _serviceMock.Verify(s => s.CreatePersonAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _serviceMock.Verify(s => s.GetSameNamePersonsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    // [AC] REQ-DUP-07: the either/or error clears when the form is valid on the next Save
    public async Task SaveCommand_EitherOrResolvedOnNextSave_ClearsEmailError()
    {
        var sut = CreateSut();
        sut.PersonName = "John Doe";
        _serviceMock.SetupSequence(s => s.ValidateContact(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>()))
            .Returns((false, EitherOrMessage))
            .Returns((true, ""));

        await sut.SaveCommand.ExecuteAsync(null);
        Assert.True(sut.EmailHasError);

        sut.PersonEmail = "john@example.com";
        await sut.SaveCommand.ExecuteAsync(null);

        Assert.False(sut.EmailHasError);
        Assert.Equal(string.Empty, sut.EmailErrorText);
    }

    [Fact]
    // [AC] REQ-DUP-07a: the either/or error and the birthday error are applied independently and can show together
    public async Task SaveCommand_EitherOrAndBirthdayErrorBothFail_ShowsBothErrors()
    {
        var sut = CreateSut();
        sut.PersonName = "John Doe";
        sut.SelectedBirthDay = 15;
        _serviceMock.Setup(s => s.ValidateBirthday(15, null)).Returns((false, "Select both day and month"));
        _serviceMock.Setup(s => s.ValidateContact(It.IsAny<string>(), 15, null)).Returns((false, EitherOrMessage));

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.True(sut.BirthdayHasError);
        Assert.True(sut.BirthdayErrorOnMonth);
        Assert.True(sut.EmailHasError);
        Assert.Equal(EitherOrMessage, sut.EmailErrorText);
    }

    [Fact]
    // [AC] REQ-DUP-07a: an email format error and the either/or check do not overwrite each other on Email
    public async Task SaveCommand_InvalidEmailFormat_ShowsFormatErrorNotEitherOr()
    {
        var sut = CreateSut();
        sut.PersonName = "John Doe";
        sut.PersonEmail = "notanemail";
        _serviceMock.Setup(s => s.ValidateEmail("notanemail")).Returns((false, "Invalid email"));

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.True(sut.EmailHasError);
        Assert.Equal("Invalid email", sut.EmailErrorText);
    }

    // ── REQ-DUP-09: create mode soft warning ───────────────────────────────

    [Fact]
    // [AC] REQ-DUP-09: create with a same-name singer of another identity opens the sheet and does not save
    public async Task SaveCommand_CreateMode_SameNameMatchExists_OpensSheetWithoutSaving()
    {
        var sut = CreateSut();
        sut.PersonName = "John Doe";
        sut.PersonEmail = "john@example.com";
        SetupSameNameMatches("John Doe", new Person("John Doe") { Id = 7, BirthDay = 3, BirthMonth = 4 });

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.True(sut.IsSameNameSheetVisible);
        Assert.Single(sut.SameNameMatches);
        Assert.Equal("John Doe", sut.SameNameMatches[0].Headline);
        Assert.Equal(7, ((Person)sut.SameNameMatches[0].Data).Id);
        _serviceMock.Verify(s => s.CreatePersonAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    // [AC] REQ-DUP-09: create with no same-name singer saves directly and never opens the sheet
    public async Task SaveCommand_CreateMode_NoSameNameMatch_SavesWithoutSheet()
    {
        var sut = CreateSut();
        sut.PersonName = "John Doe";
        sut.PersonEmail = "john@example.com";

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.False(sut.IsSameNameSheetVisible);
        _serviceMock.Verify(s => s.CreatePersonAsync("John Doe", null, null, "john@example.com", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    // [AC] REQ-DUP-09: the same-name query receives the typed name, email, birthday and no excluded id in create mode
    public async Task SaveCommand_CreateMode_QueriesSameNameWithFormValues()
    {
        var sut = CreateSut();
        sut.PersonName = "John Doe";
        sut.PersonEmail = "john@example.com";
        sut.SelectedBirthDay = 15;
        sut.SelectedBirthMonth = sut.Months[2];

        await sut.SaveCommand.ExecuteAsync(null);

        _serviceMock.Verify(s => s.GetSameNamePersonsAsync("John Doe", "john@example.com", 15, 3, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── REQ-DUP-10: edit mode only when the name changed ───────────────────

    [Fact]
    // [AC] REQ-DUP-10: edit with the loaded name unchanged never queries same-name and saves directly
    public async Task SaveCommand_EditMode_NameUnchanged_DoesNotQuerySameName()
    {
        var sut = CreateSut();
        sut.PersonIdRaw = "5";
        sut.PersonName = "John Doe";
        sut.PersonEmail = "john@example.com";
        sut.CompleteHydration();

        await sut.SaveCommand.ExecuteAsync(null);

        _serviceMock.Verify(s => s.GetSameNamePersonsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Never);
        _serviceMock.Verify(s => s.UpdatePersonAsync(5, "John Doe", null, null, "john@example.com", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    // [AC] REQ-DUP-10: edit with a changed name queries same-name, excluding the person being edited
    public async Task SaveCommand_EditMode_NameChanged_QueriesSameNameExcludingSelf()
    {
        var sut = CreateSut();
        sut.PersonIdRaw = "5";
        sut.PersonName = "John Doe";
        sut.PersonEmail = "john@example.com";
        sut.CompleteHydration();

        sut.PersonName = "Johnny Doe";
        await sut.SaveCommand.ExecuteAsync(null);

        _serviceMock.Verify(s => s.GetSameNamePersonsAsync("Johnny Doe", "john@example.com", null, null, 5, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    // [AC] REQ-DUP-10: edit with a changed name and a same-name singer opens the sheet and does not update yet
    public async Task SaveCommand_EditMode_NameChangedMatchExists_OpensSheetWithoutUpdating()
    {
        var sut = CreateSut();
        sut.PersonIdRaw = "5";
        sut.PersonName = "John Doe";
        sut.PersonEmail = "john@example.com";
        sut.CompleteHydration();
        sut.PersonName = "Johnny Doe";
        SetupSameNameMatches("Johnny Doe", new Person("Johnny Doe") { Id = 9, Email = "other@example.com" });

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.True(sut.IsSameNameSheetVisible);
        _serviceMock.Verify(s => s.UpdatePersonAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── REQ-DUP-09: sheet buttons ──────────────────────────────────────────

    [Fact]
    // [AC] REQ-DUP-09: the sheet's save button reads "Save as new" in create mode
    public void SameNameSaveButtonText_CreateMode_IsSaveAsNew()
    {
        var sut = CreateSut();

        Assert.Equal("Save as new", sut.SameNameSaveButtonText);
    }

    [Fact]
    // [AC] REQ-DUP-09: the sheet's save button reads "Save anyway" in edit mode
    public void SameNameSaveButtonText_EditMode_IsSaveAnyway()
    {
        var sut = CreateSut();
        sut.PersonIdRaw = "5";

        Assert.Equal("Save anyway", sut.SameNameSaveButtonText);
    }

    [Fact]
    // [AC] REQ-DUP-09: Save as new closes the sheet and inserts the form as a new singer
    public async Task SaveAsNewCommand_CreateMode_ClosesSheetAndInserts()
    {
        var sut = CreateSut();
        sut.PersonName = "John Doe";
        sut.PersonEmail = "john@example.com";
        SetupSameNameMatches("John Doe", new Person("John Doe") { Id = 7, BirthDay = 3, BirthMonth = 4 });
        await sut.SaveCommand.ExecuteAsync(null);

        await sut.SaveAsNewCommand.ExecuteAsync(null);

        Assert.False(sut.IsSameNameSheetVisible);
        _serviceMock.Verify(s => s.CreatePersonAsync("John Doe", null, null, "john@example.com", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    // [AC] REQ-DUP-09: Save anyway in edit mode closes the sheet and saves the edit, not an insert
    public async Task SaveAsNewCommand_EditMode_ClosesSheetAndUpdates()
    {
        var sut = CreateSut();
        sut.PersonIdRaw = "5";
        sut.PersonName = "John Doe";
        sut.PersonEmail = "john@example.com";
        sut.CompleteHydration();
        sut.PersonName = "Johnny Doe";
        SetupSameNameMatches("Johnny Doe", new Person("Johnny Doe") { Id = 9, Email = "other@example.com" });
        await sut.SaveCommand.ExecuteAsync(null);

        await sut.SaveAsNewCommand.ExecuteAsync(null);

        Assert.False(sut.IsSameNameSheetVisible);
        _serviceMock.Verify(s => s.UpdatePersonAsync(5, "Johnny Doe", null, null, "john@example.com", It.IsAny<CancellationToken>()), Times.Once);
        _serviceMock.Verify(s => s.CreatePersonAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    // [AC] REQ-DUP-09: tapping a listed singer closes the sheet and opens that singer in edit mode
    public async Task OpenSameNameCommand_ListedSinger_ClosesSheetAndNavigatesToEdit()
    {
        var sut = CreateSut();
        sut.PersonName = "John Doe";
        sut.PersonEmail = "john@example.com";
        SetupSameNameMatches("John Doe", new Person("John Doe") { Id = 7, BirthDay = 3, BirthMonth = 4 });
        await sut.SaveCommand.ExecuteAsync(null);
        var match = sut.SameNameMatches[0];

        await sut.OpenSameNameCommand.ExecuteAsync(match);

        Assert.False(sut.IsSameNameSheetVisible);
        _navigationMock.Verify(n => n.GoToAsync(It.Is<string>(url =>
            url.StartsWith("../") && url.Contains("personId=7") && url.Contains("personBirthDay=3") && url.Contains("personBirthMonth=4"))), Times.Once);
        _serviceMock.Verify(s => s.CreatePersonAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    // [AC] REQ-DUP-09: Cancel or a user dismissal closes the sheet and saves nothing
    public async Task DismissSameNameSheetCommand_ClosesSheetWithoutSaving()
    {
        var sut = CreateSut();
        sut.PersonName = "John Doe";
        sut.PersonEmail = "john@example.com";
        SetupSameNameMatches("John Doe", new Person("John Doe") { Id = 7, BirthDay = 3, BirthMonth = 4 });
        await sut.SaveCommand.ExecuteAsync(null);

        sut.DismissSameNameSheetCommand.Execute(null);

        Assert.False(sut.IsSameNameSheetVisible);
        _serviceMock.Verify(s => s.CreatePersonAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _navigationMock.Verify(n => n.GoToAsync(It.IsAny<string>()), Times.Never);
    }

    // ── REQ-DUP-08: duplicate from the service ─────────────────────────────

    [Fact]
    // [AC] REQ-DUP-08: the service duplicate message shows as an error snackbar and the page stays
    public async Task SaveCommand_ServiceReturnsDuplicateMessage_ShowsErrorSnackbarAndStaysOnPage()
    {
        var sut = CreateSut();
        sut.PersonName = "John Doe";
        sut.PersonEmail = "john@example.com";
        _serviceMock.Setup(s => s.CreatePersonAsync("John Doe", null, null, "john@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, DuplicateMessage, (Person?)null));

        await sut.SaveCommand.ExecuteAsync(null);

        _snackMock.Verify(s => s.ShowErrorAsync(DuplicateMessage), Times.Once);
        _snackMock.Verify(s => s.ShowSuccessAsync(It.IsAny<string>()), Times.Never);
        Assert.False(sut.EmailHasError);
        Assert.False(sut.IsBusy);
        _navigationMock.Verify(n => n.GoToAsync(It.IsAny<string>()), Times.Never);
    }
}
