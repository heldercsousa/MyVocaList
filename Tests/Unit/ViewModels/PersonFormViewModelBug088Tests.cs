using MyVocaList.Domain.Entity;
using MyVocaList.Domain.ReadModels;
using MyVocaList.UI.Models;
using MyVocaList.UI.Services;

namespace MyVocaList.Tests.Unit.ViewModels;

/// <summary>BUG-088: same-name soft warning sheet, its title and cap, name-only singers and its commands (REQ-DUP-08..13a).</summary>
public class PersonFormViewModelBug088Tests
{
    private const string DuplicateMessage = "This singer is already registered.";

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
        _serviceMock.Setup(s => s.GetSameNamePersonsAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SameNameResult([], 0));
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
            .ReturnsAsync(new SameNameResult(matches.ToList(), matches.Length));

    // Total may exceed the listed rows (the service caps the list at 3).
    private void SetupSameName(int total, params Person[] listed) =>
        _serviceMock.Setup(s => s.GetSameNamePersonsAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SameNameResult(listed.ToList(), total));

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

    // ── REQ-DUP-09: sheet title and cap ────────────────────────────────────

    [Fact]
    // [AC] REQ-DUP-09: one same-name singer gives the singular title and no more-line
    public async Task SaveCommand_OneSameNameSinger_SingularTitleAndNoMoreLine()
    {
        var sut = CreateSut();
        sut.PersonName = "John Doe";
        sut.PersonEmail = "john@example.com";
        SetupSameName(1, new Person("John Doe") { Id = 7, BirthDay = 3, BirthMonth = 4 });

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.Equal("A singer with this name already exists", sut.SameNameSheetTitle);
        Assert.False(sut.HasMoreSameName);
        Assert.Equal(string.Empty, sut.SameNameMoreText);
    }

    [Fact]
    // [AC] REQ-DUP-09: two same-name singers give the plural title with the count
    public async Task SaveCommand_TwoSameNameSingers_PluralTitleWithCount()
    {
        var sut = CreateSut();
        sut.PersonName = "John Doe";
        sut.PersonEmail = "john@example.com";
        SetupSameName(2, new Person("John Doe") { Id = 7 }, new Person("John Doe") { Id = 8 });

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.Equal("2 singers with this name already exist", sut.SameNameSheetTitle);
        Assert.Equal(2, sut.SameNameMatches.Count);
    }

    [Fact]
    // [AC] REQ-DUP-09: more than 3 same-name singers list 3 rows and show the more-line with the number not listed
    public async Task SaveCommand_FourSameNameSingers_ListsThreeAndShowsMoreLine()
    {
        var sut = CreateSut();
        sut.PersonName = "John Doe";
        sut.PersonEmail = "john@example.com";
        SetupSameName(4, new Person("John Doe") { Id = 7 }, new Person("John Doe") { Id = 8 }, new Person("John Doe") { Id = 9 });

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.Equal("4 singers with this name already exist", sut.SameNameSheetTitle);
        Assert.Equal(3, sut.SameNameMatches.Count);
        Assert.True(sut.HasMoreSameName);
        Assert.Equal("…and 1 more. Find them in the Singers list.", sut.SameNameMoreText);
    }

    [Fact]
    // [AC] REQ-DUP-09: exactly 3 same-name singers are all listed, so there is no more-line
    public async Task SaveCommand_ThreeSameNameSingers_NoMoreLine()
    {
        var sut = CreateSut();
        sut.PersonName = "John Doe";
        sut.PersonEmail = "john@example.com";
        SetupSameName(3, new Person("John Doe") { Id = 7 }, new Person("John Doe") { Id = 8 }, new Person("John Doe") { Id = 9 });

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.Equal(3, sut.SameNameMatches.Count);
        Assert.False(sut.HasMoreSameName);
        Assert.Equal(string.Empty, sut.SameNameMoreText);
    }

    // ── REQ-DUP-12 / REQ-DUP-13a: name-only singers ────────────────────────

    [Fact]
    // [AC] REQ-DUP-12: a name-only singer with no namesake saves directly and never opens the sheet
    public async Task SaveCommand_NameOnlyNoNamesake_SavesWithoutSheet()
    {
        var sut = CreateSut();
        sut.PersonName = "John Doe";

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.False(sut.IsSameNameSheetVisible);
        _serviceMock.Verify(s => s.CreatePersonAsync("John Doe", null, null, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    // [AC] REQ-DUP-13a: a name-only form with a namesake opens the sheet without Save as new (create mode)
    public async Task SaveCommand_NameOnlyWithNamesake_SheetHidesSaveAsNew()
    {
        var sut = CreateSut();
        sut.PersonName = "John Doe";
        SetupSameName(1, new Person("John Doe") { Id = 7, Email = "other@example.com" });

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.True(sut.IsSameNameSheetVisible);
        Assert.False(sut.CanSaveAsNewFromSheet);
        _serviceMock.Verify(s => s.CreatePersonAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    // [AC] REQ-DUP-13a: a name-only edit that changes the name to a namesake's name also hides Save anyway
    public async Task SaveCommand_EditModeNameOnlyWithNamesake_SheetHidesSaveAnyway()
    {
        var sut = CreateSut();
        sut.PersonIdRaw = "5";
        sut.PersonName = "John Doe";
        sut.CompleteHydration();
        sut.PersonName = "Johnny Doe";
        SetupSameName(1, new Person("Johnny Doe") { Id = 9, Email = "other@example.com" });

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.True(sut.IsSameNameSheetVisible);
        Assert.False(sut.CanSaveAsNewFromSheet);
    }

    [Fact]
    // [AC] REQ-DUP-13a: with an email or a birthday, the sheet keeps Save as new
    public async Task SaveCommand_WithEmailAndNamesake_SheetKeepsSaveAsNew()
    {
        var sut = CreateSut();
        sut.PersonName = "John Doe";
        sut.PersonEmail = "john@example.com";
        SetupSameName(1, new Person("John Doe") { Id = 7, Email = "other@example.com" });

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.True(sut.CanSaveAsNewFromSheet);
    }

    [Fact]
    // [AC] REQ-DUP-13a: with only a birthday, the sheet keeps Save as new
    public async Task SaveCommand_WithBirthdayOnlyAndNamesake_SheetKeepsSaveAsNew()
    {
        var sut = CreateSut();
        sut.PersonName = "John Doe";
        sut.SelectedBirthDay = 15;
        sut.SelectedBirthMonth = sut.Months[2];
        SetupSameName(1, new Person("John Doe") { Id = 7, BirthDay = 1, BirthMonth = 1 });

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.True(sut.CanSaveAsNewFromSheet);
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
    // [AC] REQ-DUP-16: opening a listed singer clears the name suggestions, the same as a suggestion pick
    public async Task OpenSameNameCommand_ClearsNameSuggestions()
    {
        var sut = CreateSut();
        var person = new Person("John Doe") { Id = 7, BirthDay = 3, BirthMonth = 4 };
        var match = new AutocompleteSuggestion("John Doe", person.GetDisplayIdentifier(), person);
        sut.Suggestions = [match];

        await sut.OpenSameNameCommand.ExecuteAsync(match);

        Assert.Empty(sut.Suggestions);
    }

    [Fact]
    // [AC] REQ-DUP-10: a singer loaded into the same form after hydration (pick or Open) is the baseline, so saving it unchanged opens no sheet
    public async Task SaveCommand_SingerLoadedIntoSameForm_NameUnchanged_DoesNotQuerySameName()
    {
        var sut = CreateSut();
        sut.CompleteHydration();   // Add-mode form: hydration ends with an empty name
        sut.PersonIdRaw = "5";     // a pick or an Open re-applies the query to the same form
        sut.PersonName = "John Doe";
        sut.PersonEmail = "john@example.com";

        await sut.SaveCommand.ExecuteAsync(null);

        _serviceMock.Verify(s => s.GetSameNamePersonsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Never);
        _serviceMock.Verify(s => s.UpdatePersonAsync(5, "John Doe", null, null, "john@example.com", It.IsAny<CancellationToken>()), Times.Once);
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

    // ── REQ-DUP-16: the name drop-down closes when the same-name sheet takes over ──

    [Fact]
    // [AC] REQ-DUP-16: Save with the name drop-down still open closes it when the same-name sheet opens
    public async Task SaveCommand_SameNameSheetOpens_ClosesNameDropDown()
    {
        var sut = CreateSut();
        sut.PersonName = "John Doe";
        sut.PersonEmail = "john@example.com";
        SetupSameNameMatches("John Doe", new Person("John Doe") { Id = 7, BirthDay = 3, BirthMonth = 4 });
        sut.IsNameDropDownOpen = true;   // the drop-down is open at Save, without a blur

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.True(sut.IsSameNameSheetVisible);
        Assert.False(sut.IsNameDropDownOpen);
    }

    [Fact]
    // [AC] REQ-DUP-16: Open on a same-name row closes the name drop-down, so the loaded singer's name does not reopen it
    public async Task OpenSameNameCommand_ClosesNameDropDown()
    {
        var sut = CreateSut();
        sut.PersonName = "John Doe";
        sut.PersonEmail = "john@example.com";
        SetupSameNameMatches("John Doe", new Person("John Doe") { Id = 7, BirthDay = 3, BirthMonth = 4 });
        await sut.SaveCommand.ExecuteAsync(null);
        var match = sut.SameNameMatches[0];
        sut.IsNameDropDownOpen = true;   // the field reports the drop-down open again before Open

        await sut.OpenSameNameCommand.ExecuteAsync(match);

        Assert.False(sut.IsNameDropDownOpen);
    }
}
