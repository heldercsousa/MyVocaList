using MyVocaList.UI.ViewModels;

namespace MyVocaList.Tests.Unit.ViewModels;

/// <summary>
/// Level A tests for the Venue form validation standard (reference implementation).
/// Requirement IDs (R1–R9) are derived from the Form Validation Standard in
/// `.claude/library/dialogs-validation.md` (sourced from the ui-form-validation-guide).
/// </summary>
public class VenueFormViewModelTests
{
    private readonly Mock<IVenueService> _serviceMock = new();
    private readonly Mock<ISnackbarComponent> _snackMock = new();
    private readonly Mock<ILogger<VenueFormViewModel>> _loggerMock = new();

    private VenueFormViewModel CreateSut() =>
        new(_serviceMock.Object, _snackMock.Object, _loggerMock.Object);

    // ── Blur validation (ValidateNameCommand) ────────────────────────────────

    [Fact]
    // [AC] R8: A pristine field the user only tabbed through must not show a blur error.
    public void ValidateNameCommand_PristineField_DoesNotSetError()
    {
        var sut = CreateSut();   // VenueName never edited → not dirty

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
        sut.VenueName = "A";   // edit marks field dirty
        _serviceMock.Setup(s => s.ValidateNameInput("A"))
                    .Returns((false, "Name is too short. Minimum is 2 characters."));

        sut.ValidateNameCommand.Execute(null);

        Assert.True(sut.NameHasError);
        Assert.Equal("Name is too short. Minimum is 2 characters.", sut.NameErrorText);
    }

    [Fact]
    // [AC] R1: On blur, a dirty valid field shows no error.
    public void ValidateNameCommand_DirtyValidField_NoError()
    {
        var sut = CreateSut();
        sut.VenueName = "Jazz Club";
        _serviceMock.Setup(s => s.ValidateNameInput("Jazz Club")).Returns((true, ""));

        sut.ValidateNameCommand.Execute(null);

        Assert.False(sut.NameHasError);
        Assert.Empty(sut.NameErrorText);
    }

    // ── Keystroke behaviour ───────────────────────────────────────────────────

    [Fact]
    // [AC] R3: While NOT in error, keystrokes do not run validation (no "impatient teacher").
    public void OnVenueNameChanged_NotInError_DoesNotValidate()
    {
        var sut = CreateSut();

        sut.VenueName = "A";   // invalid, but field is not yet in error

        Assert.False(sut.NameHasError);
        _serviceMock.Verify(s => s.ValidateNameInput(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    // [AC] R2: While in error, a keystroke that makes the field valid clears the error immediately.
    public void OnVenueNameChanged_WhileInError_ClearsErrorWhenValid()
    {
        var sut = CreateSut();
        sut.VenueName = "A";
        _serviceMock.Setup(s => s.ValidateNameInput("A"))
                    .Returns((false, "Name is too short. Minimum is 2 characters."));
        sut.ValidateNameCommand.Execute(null);   // put the field into error
        Assert.True(sut.NameHasError);

        _serviceMock.Setup(s => s.ValidateNameInput("Jazz Club")).Returns((true, ""));
        sut.VenueName = "Jazz Club";   // keystroke fixes the value

        Assert.False(sut.NameHasError);
        Assert.Empty(sut.NameErrorText);
    }

    [Fact]
    // [AC] R2: While in error and still invalid, a keystroke keeps the error and updates the message.
    public void OnVenueNameChanged_WhileInError_StillInvalid_KeepsError()
    {
        var sut = CreateSut();
        sut.VenueName = "A";
        _serviceMock.Setup(s => s.ValidateNameInput("A"))
                    .Returns((false, "Name is too short. Minimum is 2 characters."));
        sut.ValidateNameCommand.Execute(null);
        Assert.True(sut.NameHasError);

        _serviceMock.Setup(s => s.ValidateNameInput(""))
                    .Returns((false, "Venue name is required"));
        sut.VenueName = "";   // still invalid

        Assert.True(sut.NameHasError);
        Assert.Equal("Venue name is required", sut.NameErrorText);
    }

    // ── Save safety net ───────────────────────────────────────────────────────

    [Fact]
    // [AC] R4: Save re-runs the validator as the final safety net and surfaces the error inline.
    public async Task SaveCommand_InvalidName_SetsNameHasError()
    {
        var sut = CreateSut();
        sut.VenueName = "A";
        _serviceMock.Setup(s => s.ValidateNameInput("A"))
                    .Returns((false, "Name is too short. Minimum is 2 characters."));

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.True(sut.NameHasError);
        Assert.Equal("Name is too short. Minimum is 2 characters.", sut.NameErrorText);
        _serviceMock.Verify(s => s.CreateVenueAsync(It.IsAny<string>()), Times.Never);
    }
}
