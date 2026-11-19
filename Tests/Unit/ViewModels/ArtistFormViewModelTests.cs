using CommunityToolkit.Mvvm.Messaging;
using MyVocaList.Contracts.DTOs.Suggestions;
using MyVocaList.Contracts.Models;
using MyVocaList.Navigation;
using MyVocaList.Services;
using MyVocaList.UI.Services;

namespace MyVocaList.Tests.Unit.ViewModels;

/// <summary>
/// Level A tests for the Artist form validation standard (Task 05).
/// Requirement IDs (R1–R8) are derived from the Form Validation Standard in
/// `.claude/library/dialogs-validation.md` (sourced from the ui-form-validation-guide).
/// Name search (Task 6.2, REQ-SFT-47…50, REQ-SFT-06/09) is covered from the "Name search" section onward.
/// </summary>
public class ArtistFormViewModelTests
{
    private readonly Mock<IArtistService> _serviceMock = new();
    private readonly Mock<ISnackbarComponent> _snackMock = new();
    private readonly Mock<ILogger<ArtistFormViewModel>> _loggerMock = new();
    private readonly Mock<IMessenger> _messengerMock = new();
    private readonly Mock<INavigationService> _navigationMock = new();
    private readonly Mock<IArtistSuggestionService> _suggestionsMock = new();

    private ArtistFormViewModel CreateSut() =>
        new(_serviceMock.Object, _snackMock.Object, _loggerMock.Object, _messengerMock.Object,
            _navigationMock.Object, _suggestionsMock.Object,
            new Mock<IImageStore>().Object, new Mock<IPhotoPicker>().Object);

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
            s => s.CreateArtistAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
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
        _serviceMock.Setup(s => s.CreateArtistAsync("The Beatles", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((true, "Artist 'The Beatles' created successfully", new Artist { Name = "The Beatles" }));

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.False(sut.NameHasError);
        _serviceMock.Verify(
            s => s.CreateArtistAsync("The Beatles", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
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
        _serviceMock.Setup(s => s.CreateArtistAsync("The Beatles", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((false, "An artist with this name already exists", (Artist)null));

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.True(sut.NameHasError);
        Assert.Equal("An artist with this name already exists", sut.NameErrorText);
    }

    // ── Re-entrancy guard (BUG-049) ────────────────────────────────────────────

    [Fact]
    // [AC] BUG-049: a fast double-tap on Save while a save is in flight must not fire the
    // create/update service call twice (which caused a duplicate "GoToAsync("..")" that
    // overshot the nav stack root).
    public async Task SaveCommand_DoubleInvokedWhileSaving_CallsCreateOnlyOnce()
    {
        var sut = CreateSut();
        sut.CompleteHydration();
        sut.ArtistName = "The Beatles";
        _serviceMock.Setup(s => s.ValidateNameInput("The Beatles")).Returns((true, ""));

        var gate = new TaskCompletionSource<(bool, string, Artist?)>();
        _serviceMock.Setup(s => s.CreateArtistAsync("The Beatles", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .Returns(gate.Task);

        var firstCall = sut.SaveCommand.ExecuteAsync(null);
        var secondCall = sut.SaveCommand.ExecuteAsync(null);   // simulates the second tap of a double-tap

        gate.SetResult((true, "Artist 'The Beatles' created successfully", new Artist { Name = "The Beatles" }));
        await Task.WhenAll(firstCall, secondCall);

        _serviceMock.Verify(
            s => s.CreateArtistAsync("The Beatles", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
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

    // ── Name search (Task 6.2: REQ-SFT-47…50, REQ-SFT-06, REQ-SFT-09) ──────────
    // Notes are the Artist-form wording approved under Q18 (REQ-SFT-47) and the Song-form unavailable
    // text, verbatim (REQ-SFT-47 note). Thresholds and dedup are owned by ArtistSuggestionService (6.1)
    // and are not retested here.

    private const string ArtistOfflineNote = "You're offline — showing artists from your library only.";
    private const string UnavailableNote = "Online search isn't available right now — showing your library only.";

    private static ArtistSuggestionDto LocalArtist(int id, string name) =>
        new(id, name, null, null, false);

    private static ArtistSuggestionDto RemoteArtist(string name, string externalId = "dz-9") =>
        new(null, name, externalId, "Deezer", true);

    private static AutocompleteSuggestion Pick(ArtistSuggestionDto dto) =>
        new(dto.Name, string.Empty, dto);

    private static IReadOnlyList<ArtistSuggestionDto> NoRows() => Array.Empty<ArtistSuggestionDto>();

    private static ArtistRemoteSearchResult RemoteResult(RemoteSearchStatus status, params ArtistSuggestionDto[] rows) =>
        new(rows, status);

    /// <summary>An add-form VM past hydration (Shell has applied the query properties).</summary>
    private ArtistFormViewModel CreateAddFormSut()
    {
        var sut = CreateSut();
        sut.CompleteHydration();
        return sut;
    }

    private void SetupNameSearch(string query, IReadOnlyList<ArtistSuggestionDto> local, ArtistRemoteSearchResult remote)
    {
        _suggestionsMock
            .Setup(s => s.SearchNameLocalAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(local);
        _suggestionsMock
            .Setup(s => s.SearchNameRemoteAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(remote);
    }

    // ── Option source: local first, then Deezer ────────────────────────────────

    [Fact]
    // [AC] REQ-SFT-47: local artist options come first ("In your library"), then Deezer options ("Deezer").
    public async Task NameOptionSource_LocalThenRemote_LocalRowsFirstWithSupportingText()
    {
        var sut = CreateAddFormSut();
        SetupNameSearch("metallica",
            new[] { LocalArtist(5, "Metallica") },
            RemoteResult(RemoteSearchStatus.Ok, RemoteArtist("Metallica Tribute")));

        var options = await sut.NameOptionSource.SearchAsync("metallica", CancellationToken.None);

        Assert.Collection(options,
            o => { Assert.Equal("Metallica", o.Headline); Assert.Equal("In your library", o.SupportingText); },
            o => { Assert.Equal("Metallica Tribute", o.Headline); Assert.Equal("Deezer", o.SupportingText); });
    }

    // ── Local pick opens the artist ────────────────────────────────────────────

    [Fact]
    // [AC] REQ-SFT-48: picking a local artist navigates to that artist's edit form, replacing the add form.
    public async Task SelectName_LocalPick_NavigatesToArtistEditFormReplacingAddForm()
    {
        var sut = CreateAddFormSut();

        await sut.SelectNameCommand.ExecuteAsync(Pick(LocalArtist(5, "Metallica")));

        _navigationMock.Verify(n => n.GoToAsync($"../{Routes.ArtistForm}?artistId=5&artistName=Metallica"), Times.Once);
    }

    [Fact]
    // [AC] REQ-SFT-48: the local artist name in the route is URI-escaped.
    public async Task SelectName_LocalPickWithSpecialCharacters_EscapesArtistNameInRoute()
    {
        var sut = CreateAddFormSut();
        const string name = "Guns N' Roses & Co";

        await sut.SelectNameCommand.ExecuteAsync(Pick(LocalArtist(7, name)));

        _navigationMock.Verify(
            n => n.GoToAsync($"../{Routes.ArtistForm}?artistId=7&artistName={Uri.EscapeDataString(name)}"),
            Times.Once);
    }

    [Fact]
    // [AC] REQ-SFT-48: a local pick only navigates; it does not fill Name or create anything.
    public async Task SelectName_LocalPick_DoesNotFillNameOrCreate()
    {
        var sut = CreateAddFormSut();

        await sut.SelectNameCommand.ExecuteAsync(Pick(LocalArtist(5, "Metallica")));

        Assert.Equal(string.Empty, sut.ArtistName);
        Assert.False(sut.IsNameLocked);
        _serviceMock.Verify(
            s => s.CreateArtistAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── Deezer pick fills the name only ────────────────────────────────────────

    [Fact]
    // [AC] REQ-SFT-49: picking a Deezer artist fills Name with the provider's name and locks it; nothing navigates.
    public async Task SelectName_DeezerPick_FillsNameAndLocksWithoutNavigating()
    {
        var sut = CreateAddFormSut();

        await sut.SelectNameCommand.ExecuteAsync(Pick(RemoteArtist("Metallica Tribute")));

        Assert.Equal("Metallica Tribute", sut.ArtistName);
        Assert.True(sut.IsNameLocked);
        _navigationMock.Verify(n => n.GoToAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    // [AC] REQ-SFT-49: the Deezer artist id and provider are not stored; Save creates the artist with the name only.
    public async Task SelectName_DeezerPickThenSave_CreatesWithNameOnlyNoExternalIdentity()
    {
        var sut = CreateAddFormSut();
        await sut.SelectNameCommand.ExecuteAsync(Pick(RemoteArtist("Metallica Tribute", externalId: "dz-9")));
        _serviceMock.Setup(s => s.ValidateNameInput("Metallica Tribute")).Returns((true, ""));
        _serviceMock
            .Setup(s => s.CreateArtistAsync("Metallica Tribute", null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, "Artist 'Metallica Tribute' created successfully", new Artist { Name = "Metallica Tribute" }));

        await sut.SaveCommand.ExecuteAsync(null);

        _serviceMock.Verify(
            s => s.CreateArtistAsync("Metallica Tribute", null, null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    // [AC] REQ-SFT-49: X on Name empties the picked name and unlocks the field.
    public async Task ClearName_AfterDeezerPick_EmptiesNameAndUnlocks()
    {
        var sut = CreateAddFormSut();
        await sut.SelectNameCommand.ExecuteAsync(Pick(RemoteArtist("Metallica Tribute")));

        sut.ClearNameCommand.Execute(null);

        Assert.Equal(string.Empty, sut.ArtistName);
        Assert.False(sut.IsNameLocked);
    }

    // ── Edit mode: no search, rename unchanged ─────────────────────────────────

    [Fact]
    // [AC] REQ-SFT-47: in edit mode Name is not a search field (IsNameSearchEnabled is false).
    public void IsNameSearchEnabled_EditMode_IsFalse()
    {
        var sut = CreateSut();
        sut.ArtistId = 5;

        Assert.False(sut.IsNameSearchEnabled);
    }

    [Fact]
    // [AC] REQ-SFT-47: in add mode Name is the search field (IsNameSearchEnabled is true).
    public void IsNameSearchEnabled_AddMode_IsTrue()
    {
        var sut = CreateSut();

        Assert.True(sut.IsNameSearchEnabled);
    }

    [Fact]
    // [AC] REQ-SFT-47: in edit mode the Name option source issues no search and returns no options.
    public async Task NameOptionSource_EditMode_IssuesNoSearch()
    {
        var sut = CreateSut();
        sut.ArtistId = 5;

        var options = await sut.NameOptionSource.SearchAsync("metallica", CancellationToken.None);

        Assert.Empty(options);
        _suggestionsMock.Verify(s => s.SearchNameLocalAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _suggestionsMock.Verify(s => s.SearchNameRemoteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    // [AC] REQ-SFT-47: in edit mode Name stays editable (rename unchanged): Save updates the artist.
    public async Task SaveCommand_EditModeRename_CallsUpdate()
    {
        var sut = CreateAddFormSut();
        sut.ArtistId = 5;
        sut.ArtistName = "Metallica Live";
        _serviceMock.Setup(s => s.ValidateNameInput("Metallica Live")).Returns((true, ""));
        _serviceMock
            .Setup(s => s.UpdateArtistAsync(5, "Metallica Live", It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, "Artist updated"));

        await sut.SaveCommand.ExecuteAsync(null);

        _serviceMock.Verify(s => s.UpdateArtistAsync(5, "Metallica Live", It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── NameSupportingText from the remote status ──────────────────────────────

    [Fact]
    // [AC] REQ-SFT-47: Offline status sets NameSupportingText to the artist offline note (Q18 wording).
    public async Task NameOptionSource_StatusOffline_SetsArtistOfflineNote()
    {
        var sut = CreateAddFormSut();
        SetupNameSearch("metallica", NoRows(), RemoteResult(RemoteSearchStatus.Offline));

        await sut.NameOptionSource.SearchAsync("metallica", CancellationToken.None);

        Assert.Equal(ArtistOfflineNote, sut.NameSupportingText);
    }

    [Fact]
    // [AC] REQ-SFT-47: Unavailable status sets NameSupportingText to the unavailable note (Song-form text, verbatim).
    public async Task NameOptionSource_StatusUnavailable_SetsUnavailableNote()
    {
        var sut = CreateAddFormSut();
        SetupNameSearch("metallica", NoRows(), RemoteResult(RemoteSearchStatus.Unavailable));

        await sut.NameOptionSource.SearchAsync("metallica", CancellationToken.None);

        Assert.Equal(UnavailableNote, sut.NameSupportingText);
    }

    [Fact]
    // [AC] REQ-SFT-06: an Ok status (including zero results) clears the note.
    public async Task NameOptionSource_OfflineThenOk_ClearsNote()
    {
        var sut = CreateAddFormSut();
        SetupNameSearch("metallica", NoRows(), RemoteResult(RemoteSearchStatus.Offline));
        await sut.NameOptionSource.SearchAsync("metallica", CancellationToken.None);
        Assert.Equal(ArtistOfflineNote, sut.NameSupportingText);

        SetupNameSearch("metallica", NoRows(), RemoteResult(RemoteSearchStatus.Ok));
        await sut.NameOptionSource.SearchAsync("metallica", CancellationToken.None);

        Assert.Equal(string.Empty, sut.NameSupportingText);
    }

    [Fact]
    // [AC] REQ-SFT-06: a query shorter than the remote threshold leaves the current note as it is.
    public async Task NameOptionSource_ShortQueryAfterOffline_KeepsNote()
    {
        var sut = CreateAddFormSut();
        SetupNameSearch("metallica", NoRows(), RemoteResult(RemoteSearchStatus.Offline));
        await sut.NameOptionSource.SearchAsync("metallica", CancellationToken.None);

        SetupNameSearch("me", NoRows(), RemoteResult(RemoteSearchStatus.Ok));
        await sut.NameOptionSource.SearchAsync("me", CancellationToken.None);

        Assert.Equal(ArtistOfflineNote, sut.NameSupportingText);
    }

    [Fact]
    // [AC] REQ-SFT-06: emptying Name (X on Name) clears the note.
    public async Task ClearName_ClearsNameSupportingText()
    {
        var sut = CreateAddFormSut();
        SetupNameSearch("metallica", NoRows(), RemoteResult(RemoteSearchStatus.Unavailable));
        await sut.NameOptionSource.SearchAsync("metallica", CancellationToken.None);
        Assert.Equal(UnavailableNote, sut.NameSupportingText);

        sut.ClearNameCommand.Execute(null);

        Assert.Equal(string.Empty, sut.NameSupportingText);
    }

    // ── Cancellation (latest query wins) ───────────────────────────────────────

    [Fact]
    // [AC] REQ-SFT-09: the caller's CancellationToken is forwarded to the remote call.
    public async Task NameOptionSource_ForwardsCancellationTokenToRemote()
    {
        var sut = CreateAddFormSut();
        using var cts = new CancellationTokenSource();
        SetupNameSearch("metallica", NoRows(), RemoteResult(RemoteSearchStatus.Ok));

        await sut.NameOptionSource.SearchAsync("metallica", cts.Token);

        _suggestionsMock.Verify(s => s.SearchNameRemoteAsync("metallica", cts.Token), Times.Once);
    }

    [Fact]
    // [AC] REQ-SFT-09: a cancelled (stale) query throws and never touches NameSupportingText.
    public async Task NameOptionSource_CancelledQuery_ThrowsAndLeavesNoteUntouched()
    {
        var sut = CreateAddFormSut();
        using var cts = new CancellationTokenSource();
        SetupNameSearch("metallica", NoRows(), RemoteResult(RemoteSearchStatus.Unavailable));
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => sut.NameOptionSource.SearchAsync("metallica", cts.Token));

        Assert.Equal(string.Empty, sut.NameSupportingText);
    }
}
