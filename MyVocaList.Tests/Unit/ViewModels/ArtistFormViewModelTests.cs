using CommunityToolkit.Mvvm.Messaging;
using MyVocaList.UI.ViewModels;

namespace MyVocaList.Tests.Unit.ViewModels;

/// <summary>
/// Level A tests for the Artist form validation standard (Task 05).
/// Requirement IDs (R1–R8) are derived from the Form Validation Standard in
/// `.claude/library/dialogs-validation.md` (sourced from the ui-form-validation-guide).
/// </summary>
public class ArtistFormViewModelTests
{
    private readonly Mock<IArtistService> _serviceMock = new();
    private readonly Mock<ISnackbarComponent> _snackMock = new();
    private readonly Mock<ILogger<ArtistFormViewModel>> _loggerMock = new();
    private readonly Mock<IMessenger> _messengerMock = new();

    private ArtistFormViewModel CreateSut() =>
        new(_serviceMock.Object, _snackMock.Object, _loggerMock.Object, _messengerMock.Object);

    // ── Blur validation (ValidateNameCommand) ────────────────────────────────

    [Fact]
    // [AC] R8: A pristine field the user only tabbed through must not show a blur error.
    public void ValidateNameCommand_PristineField_DoesNotSetError()
    {
        var sut = CreateSut();
        sut.CompleteHydration();   // ArtistName never edited → not dirty

        sut.ValidateNameCommand.Execute(null);

        Assert.False(sut.NameHasError);
        Assert.Empty(sut.NameErrorText);
        _serviceMock.Verify(s => s.ValidateNameInput(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    // [AC] R1: On blur, a dirty invalid field surfaces an inline error.
    public void ValidateNameCommand_DirtyInvalidField_SetsError()
    {
        var sut = CreateSut();
        sut.CompleteHydration();
        var longName = new string('x', 61);   // a real edit — dirties the field
        sut.ArtistName = longName;
        _serviceMock.Setup(s => s.ValidateNameInput(longName))
                    .Returns((false, "Name is too long. Maximum 60 characters."));

        sut.ValidateNameCommand.Execute(null);

        Assert.True(sut.NameHasError);
        Assert.Equal("Name is too long. Maximum 60 characters.", sut.NameErrorText);
    }

    [Fact]
    // [AC] R1: On blur, a dirty valid field shows no error.
    public void ValidateNameCommand_DirtyValidField_NoError()
    {
        var sut = CreateSut();
        sut.CompleteHydration();
        sut.ArtistName = "The Beatles";
        _serviceMock.Setup(s => s.ValidateNameInput("The Beatles")).Returns((true, ""));

        sut.ValidateNameCommand.Execute(null);

        Assert.False(sut.NameHasError);
        Assert.Empty(sut.NameErrorText);
    }

    // ── Keystroke behaviour ───────────────────────────────────────────────────

    [Fact]
    // [AC] R3: While NOT in error, keystrokes do not run validation (no "impatient teacher").
    public void OnArtistNameChanged_NotInError_DoesNotValidate()
    {
        var sut = CreateSut();
        sut.CompleteHydration();

        sut.ArtistName = new string('x', 61);   // invalid, but field is not yet in error

        Assert.False(sut.NameHasError);
        _serviceMock.Verify(s => s.ValidateNameInput(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    // [AC] R2: While in error, a keystroke that makes the field valid clears the error immediately.
    public void OnArtistNameChanged_WhileInError_ClearsErrorWhenValid()
    {
        var sut = CreateSut();
        sut.CompleteHydration();
        var longName = new string('x', 61);   // a real edit — dirties the field
        sut.ArtistName = longName;
        _serviceMock.Setup(s => s.ValidateNameInput(longName))
                    .Returns((false, "Name is too long. Maximum 60 characters."));
        sut.ValidateNameCommand.Execute(null);   // put the field into error
        Assert.True(sut.NameHasError);

        _serviceMock.Setup(s => s.ValidateNameInput("The Beatles")).Returns((true, ""));
        sut.ArtistName = "The Beatles";   // keystroke fixes the value

        Assert.False(sut.NameHasError);
        Assert.Empty(sut.NameErrorText);
    }

    [Fact]
    // [AC] R2: While in error and still invalid, a keystroke keeps the error and updates the message.
    public void OnArtistNameChanged_WhileInError_StillInvalid_KeepsError()
    {
        var sut = CreateSut();
        sut.CompleteHydration();
        var longName = new string('x', 61);
        _serviceMock.Setup(s => s.ValidateNameInput(longName))
                    .Returns((false, "Name is too long. Maximum 60 characters."));
        sut.ArtistName = longName;
        sut.ValidateNameCommand.Execute(null);
        Assert.True(sut.NameHasError);

        _serviceMock.Setup(s => s.ValidateNameInput(""))
                    .Returns((false, "Artist name is required"));
        sut.ArtistName = "";   // still invalid

        Assert.True(sut.NameHasError);
        Assert.Equal("Artist name is required", sut.NameErrorText);
    }

    // ── Edit-mode hydration guard ─────────────────────────────────────────────

    [Fact]
    // [AC] R8 (edit-mode dirty-guard): Shell QueryProperty pre-population must not mark the field
    // dirty, so a pre-filled value does not flash an error before the user interacts.
    public void OnArtistNameChanged_DuringHydration_DoesNotDirtyOrValidateOnBlur()
    {
        var sut = CreateSut();   // _isHydrating still true — CompleteHydration() not called

        sut.ArtistName = "Prefilled Artist";   // simulates Shell QueryProperty pre-population
        sut.ValidateNameCommand.Execute(null); // simulates a blur before OnAppearing runs

        Assert.False(sut.NameHasError);
        Assert.Empty(sut.NameErrorText);
        _serviceMock.Verify(s => s.ValidateNameInput(It.IsAny<string>()), Times.Never);
    }

    // ── Save safety net ───────────────────────────────────────────────────────

    [Fact]
    // [AC] R4: Save re-runs the validator as the final safety net; the invalid field gets an
    // inline error and the save aborts (service create never called).
    public async Task SaveCommand_UntouchedEmptyName_SetsErrorAndDoesNotCreate()
    {
        var sut = CreateSut();
        sut.CompleteHydration();   // field pristine — safety net must still catch it
        _serviceMock.Setup(s => s.ValidateNameInput(""))
                    .Returns((false, "Artist name is required"));

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.True(sut.NameHasError);
        Assert.Equal("Artist name is required", sut.NameErrorText);
        _serviceMock.Verify(
            s => s.CreateArtistAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    // [AC] R4: Save with a valid name calls the service create.
    public async Task SaveCommand_ValidName_CallsCreate()
    {
        var sut = CreateSut();
        sut.CompleteHydration();
        sut.ArtistName = "The Beatles";
        _serviceMock.Setup(s => s.ValidateNameInput("The Beatles")).Returns((true, ""));
        _serviceMock.Setup(s => s.CreateArtistAsync("The Beatles", It.IsAny<CancellationToken>()))
                    .ReturnsAsync((true, "Artist 'The Beatles' created successfully", new Artist { Name = "The Beatles" }));

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.False(sut.NameHasError);
        _serviceMock.Verify(
            s => s.CreateArtistAsync("The Beatles", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    // [AC] R4 + inline surfacing: an async create failure (duplicate name) is routed to the
    // Name field inline — documented single-field mapping, never a dialog.
    public async Task SaveCommand_DuplicateName_SetsNameHasErrorInline()
    {
        var sut = CreateSut();
        sut.CompleteHydration();
        sut.ArtistName = "The Beatles";
        _serviceMock.Setup(s => s.ValidateNameInput("The Beatles")).Returns((true, ""));
        _serviceMock.Setup(s => s.CreateArtistAsync("The Beatles", It.IsAny<CancellationToken>()))
                    .ReturnsAsync((false, "An artist with this name already exists", (Artist)null));

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.True(sut.NameHasError);
        Assert.Equal("An artist with this name already exists", sut.NameErrorText);
    }

    // ── Character counter — counts the same (trimmed) string the validator checks ─

    [Fact]
    // [AC] Counter threshold alignment: the counter must count the trimmed string, exactly what
    // ValidateNameInput checks — trailing whitespace must not inflate the count.
    public void OnArtistNameChanged_TrailingWhitespace_CounterUsesTrimmedLength()
    {
        var sut = CreateSut();
        sut.CompleteHydration();
        var name = new string('x', 49) + "      ";   // 49 trimmed, 55 untrimmed

        sut.ArtistName = name;

        _serviceMock.Verify(s => s.ShouldShowCharacterCounter(49), Times.Once);
        _serviceMock.Verify(s => s.ShouldShowCharacterCounter(55), Times.Never);
    }
}
