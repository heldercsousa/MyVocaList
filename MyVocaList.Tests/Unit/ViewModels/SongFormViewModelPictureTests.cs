using CommunityToolkit.Mvvm.Messaging;
using MyVocaList.Contracts.DTOs.Suggestions;
using MyVocaList.Domain.ReadModels;
using MyVocaList.Domain.Resolution;
using MyVocaList.Domain.Rules;
using MyVocaList.Navigation;
using MyVocaList.Tests.Infrastructure;
using MyVocaList.UI.Services;

namespace MyVocaList.Tests.Unit.ViewModels;

/// <summary>
/// The Song form picture (upload, remove, which picture is the form picture, what Save sends) and the
/// release of a picked Title or Artist by a tap (REQ-PIC-10, 13, 16..22, 27, 28, 31, 32, 39).
/// </summary>
public class SongFormViewModelPictureTests
{
    private const string Cover = "https://img.test/cover.jpg";
    private const string ArtistPic = "https://img.test/artist.jpg";
    private const string Uploaded = "local:songs/0123456789abcdef0123456789abcdef";
    private const string Uploaded2 = "local:songs/fedcba9876543210fedcba9876543210";
    private const string Stored = "local:songs/aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string ImportFailure = "Could not use this picture.";

    private readonly Mock<INavigationService> _navigationMock = new();
    private readonly Mock<ISongSuggestionService> _suggestionsMock = new();
    private readonly Mock<ISnackbarComponent> _snackbarMock = new();
    private readonly Mock<IPhotoPicker> _pickerMock = new();
    private readonly Mock<IImageStore> _imageStoreMock = TestImageStore.PassThrough();
    private readonly Mock<ISongKaraokeUrlService> _urlServiceMock = new();
    private readonly Queue<string> _importReferences = new();
    private readonly List<SongFormSaveRequest> _requests = [];

    public SongFormViewModelPictureTests()
    {
        _pickerMock.Setup(p => p.PickPhotoAsync(It.IsAny<CancellationToken>()))
                   .ReturnsAsync(() => new MemoryStream([1, 2, 3]));
        _imageStoreMock
            .Setup(s => s.ImportAsync(It.IsAny<Stream>(), ImageOwnerKind.Song, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => (true, string.Empty, _importReferences.Count > 0 ? _importReferences.Dequeue() : Uploaded));
        _urlServiceMock.Setup(s => s.GetUrlsForSongAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
    }

    // ── Factory and helpers ──────────────────────────────────────────────

    private SongFormViewModel CreateSut(Mock<ISongService>? songService = null, Mock<IArtistService>? artistService = null)
    {
        var sut = new SongFormViewModel(
            (artistService ?? new Mock<IArtistService>()).Object,
            (songService ?? SaveService()).Object,
            _snackbarMock.Object,
            new Mock<ILogger<SongFormViewModel>>().Object,
            _urlServiceMock.Object,
            new Mock<ISecureStorageWrapper>().Object,
            new Mock<IMessenger>().Object,
            _navigationMock.Object,
            _suggestionsMock.Object,
            _imageStoreMock.Object,
            _pickerMock.Object);
        sut.CompleteHydration();
        return sut;
    }

    /// <summary>An edit-form VM with the stored song loaded (artist 1 "Radiohead", picture <paramref name="storedImage"/>).</summary>
    private async Task<SongFormViewModel> CreateEditSutAsync(
        string? storedImage = Stored, Func<SongFormSaveRequest, SongFormSaveOutcome>? decide = null)
    {
        var song = new Song
        {
            Id = 42,
            ArtistId = 1,
            OriginalArtist = new Artist { Id = 1, Name = "Radiohead" },
            Title = "Creep",
            Version = string.Empty,
            ImageUrl = storedImage
        };
        var songService = SaveService(decide);
        songService.Setup(s => s.GetSongByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync(song);

        var sut = CreateSut(songService);
        sut.SongIdRaw = "42";
        await Task.Yield();
        return sut;
    }

    private Mock<ISongService> SaveService(Func<SongFormSaveRequest, SongFormSaveOutcome>? decide = null)
    {
        var songService = new Mock<ISongService>();
        songService.Setup(s => s.ValidateTitleInput(It.IsAny<string>())).Returns((true, string.Empty));
        songService.Setup(s => s.ValidateVersionInput(It.IsAny<string>(), It.IsAny<bool>())).Returns((true, string.Empty));
        songService.Setup(s => s.SaveFromFormAsync(It.IsAny<SongFormSaveRequest>(), It.IsAny<CancellationToken>()))
            .Returns((SongFormSaveRequest r, CancellationToken _) =>
            {
                _requests.Add(r);
                return Task.FromResult((decide ?? (_ => Saved()))(r));
            });
        return songService;
    }

    private static SongFormSaveOutcome Saved() =>
        new(SongFormSaveKind.Saved, "Song saved", SaveField.None, 5, [], false, []);

    private static SongFormSaveOutcome Failed(SaveField field, string message) =>
        new(SongFormSaveKind.Failed, message, field, null, [], false, []);

    private static SongSuggestionDto RemoteDto(
        string title = "Creep", string artist = "Radiohead", string? image = Cover, string? artistImage = ArtistPic,
        int? localArtistId = null, string? localArtistName = null) =>
        new(null, title, artist, localArtistId, "dz-1", "Deezer", true, string.Empty, localArtistName, image, artistImage, null);

    private static AutocompleteSuggestion Pick(SongSuggestionDto dto) => new(dto.Title, string.Empty, dto);

    private static AutocompleteSuggestion ArtistPick(int id, string name) =>
        new(name, string.Empty, new ArtistListItem(id, name, string.Empty, 1));

    private static Task SaveAsync(SongFormViewModel sut) => sut.SaveCommand.ExecuteAsync(null);

    private static Task UploadAsync(SongFormViewModel sut) => sut.PickImageCommand.ExecuteAsync(null);

    // ── REQ-PIC-17: nothing is stored before Save ────────────────────────

    [Fact]
    public async Task PickImage_Upload_ChangesTheFormPictureOnlyAndCallsNoSave()
    {
        // [AC] REQ-PIC-17: picking a picture changes the form picture only; the record changes on Save.
        var songService = SaveService();
        var sut = CreateSut(songService);

        await UploadAsync(sut);

        Assert.True(sut.HasFormImage);
        songService.Verify(s => s.SaveFromFormAsync(It.IsAny<SongFormSaveRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RemoveImage_EditMode_ChangesTheFormPictureOnlyAndCallsNoSave()
    {
        // [AC] REQ-PIC-17: removing a picture changes the form picture only; the record changes on Save.
        var sut = await CreateEditSutAsync();

        sut.RemoveImageCommand.Execute(null);

        Assert.False(sut.HasFormImage);
        Assert.Empty(_requests);
    }

    // ── REQ-PIC-18: order ────────────────────────────────────────────────

    [Fact]
    public async Task FormPicture_AddModeDeezerPick_IsThePendingCover()
    {
        // [AC] REQ-PIC-18: add mode with no upload, the form picture is the pending song image of a Deezer pick.
        var sut = CreateSut();

        await sut.SelectTitleCommand.ExecuteAsync(Pick(RemoteDto()));

        Assert.True(sut.HasFormImage);
        Assert.Equal(Cover, sut.FormImageSource);
    }

    [Fact]
    public async Task FormPicture_AddModeUploadThenDeezerPick_IsTheUpload()
    {
        // [AC] REQ-PIC-18: the picture uploaded in this form comes first in add mode.
        var sut = CreateSut();
        await UploadAsync(sut);

        await sut.SelectTitleCommand.ExecuteAsync(Pick(RemoteDto()));

        Assert.Equal(Uploaded, sut.FormImageSource);
    }

    [Fact]
    public async Task FormPicture_EditMode_IsTheStoredPicture()
    {
        // [AC] REQ-PIC-18: edit mode with no upload, the form picture is the stored picture of the song.
        var sut = await CreateEditSutAsync();

        Assert.True(sut.HasFormImage);
        Assert.Equal(Stored, sut.FormImageSource);
    }

    [Fact]
    public async Task FormPicture_EditModeUpload_IsTheUpload()
    {
        // [AC] REQ-PIC-18: the picture uploaded in this form comes before the stored picture in edit mode.
        var sut = await CreateEditSutAsync();

        await UploadAsync(sut);

        Assert.Equal(Uploaded, sut.FormImageSource);
    }

    [Fact]
    public async Task FormPicture_DeezerPickWithArtistImageOnly_HasNoPicture()
    {
        // [AC] REQ-PIC-18: the artist picture is never the form picture of a song.
        var sut = CreateSut();

        await sut.SelectTitleCommand.ExecuteAsync(Pick(RemoteDto(image: null, artistImage: ArtistPic)));

        Assert.False(sut.HasFormImage);
        Assert.Null(sut.FormImageSource);
        Assert.Null(sut.FormImageThumbnailSource);
    }

    // ── REQ-PIC-13 / 16: size and hint ───────────────────────────────────

    [Fact]
    public async Task FormPicture_Sources_LargeAndThumbnailSizesOfTheReference()
    {
        // [AC] REQ-PIC-13: the form picture uses the large size (the thumbnail is drawn under it).
        _imageStoreMock.Setup(s => s.ResolveDisplaySource(It.IsAny<string?>(), It.IsAny<ImageSize>()))
                       .Returns((string? r, ImageSize size) => r is null ? null : $"{size}:{r}");
        var sut = CreateSut();

        await UploadAsync(sut);

        Assert.Equal($"Large:{Uploaded}", sut.FormImageSource);
        Assert.Equal($"Thumbnail:{Uploaded}", sut.FormImageThumbnailSource);
    }

    [Fact]
    public void ImageHintText_IsTheSizeHint()
    {
        // [AC] REQ-PIC-16: the hint reads "Square picture (1:1), 512 x 512 px or larger. Other shapes are cropped to the centre."
        var sut = CreateSut();

        Assert.Equal("Square picture (1:1), 512 x 512 px or larger. Other shapes are cropped to the centre.", sut.ImageHintText);
    }

    // ── REQ-PIC-19: an upload outlives picks ─────────────────────────────

    [Fact]
    public async Task Upload_ThenXOnTitle_KeepsTheUpload()
    {
        // [AC] REQ-PIC-19: X on Title made after an upload keeps the uploaded picture as the form picture.
        var sut = CreateSut();
        await sut.SelectTitleCommand.ExecuteAsync(Pick(RemoteDto()));
        await UploadAsync(sut);

        sut.ClearTitleCommand.Execute(null);

        Assert.True(sut.HasFormImage);
        Assert.Equal(Uploaded, sut.FormImageSource);
    }

    [Fact]
    public async Task Upload_ThenDeezerPickAndSave_SendsTheUpload()
    {
        // [AC] REQ-PIC-19: a Deezer pick made after an upload keeps the uploaded picture.
        var sut = CreateSut();
        await UploadAsync(sut);
        await sut.SelectTitleCommand.ExecuteAsync(Pick(RemoteDto()));

        await SaveAsync(sut);

        Assert.Equal(Uploaded, Assert.Single(_requests).ImageUrl);
    }

    // ── REQ-PIC-20: remove ───────────────────────────────────────────────

    [Fact]
    public async Task RemoveImage_AddModeAfterDeezerPickAndUpload_LeavesNoPicture()
    {
        // [AC] REQ-PIC-20: Remove drops the upload and the pending Deezer song image.
        var sut = CreateSut();
        await sut.SelectTitleCommand.ExecuteAsync(Pick(RemoteDto()));
        await UploadAsync(sut);

        sut.RemoveImageCommand.Execute(null);
        await SaveAsync(sut);

        Assert.False(sut.HasFormImage);
        Assert.Null(Assert.Single(_requests).ImageUrl);
    }

    [Fact]
    public async Task RemoveImage_EditMode_LeavesNoPicture()
    {
        // [AC] REQ-PIC-20: in edit mode Remove marks the stored picture for removal.
        var sut = await CreateEditSutAsync();

        sut.RemoveImageCommand.Execute(null);

        Assert.False(sut.HasFormImage);
        Assert.Null(sut.FormImageSource);
    }

    [Fact]
    public async Task RemoveImage_ThenDeezerPick_SetsAPictureAgain()
    {
        // [AC] REQ-PIC-20: a later Deezer pick sets a picture again.
        var sut = CreateSut();
        await sut.SelectTitleCommand.ExecuteAsync(Pick(RemoteDto(image: "https://img.test/old.jpg")));
        sut.RemoveImageCommand.Execute(null);

        await sut.SelectTitleCommand.ExecuteAsync(Pick(RemoteDto()));

        Assert.True(sut.HasFormImage);
        Assert.Equal(Cover, sut.FormImageSource);
    }

    [Fact]
    public async Task RemoveImage_EditModeThenUpload_SetsAPictureAgain()
    {
        // [AC] REQ-PIC-20: a later upload sets a picture again.
        var sut = await CreateEditSutAsync();
        sut.RemoveImageCommand.Execute(null);

        await UploadAsync(sut);

        Assert.True(sut.HasFormImage);
        Assert.Equal(Uploaded, sut.FormImageSource);
    }

    // ── REQ-PIC-21: Save stores the form picture ─────────────────────────

    [Fact]
    public async Task Save_AddModeUpload_RequestCarriesTheUploadedReference()
    {
        // [AC] REQ-PIC-21: add mode, the stored song's reference is the form picture's reference.
        var sut = CreateSut();
        sut.SongTitle = "Creep";
        sut.ArtistSearchText = "Radiohead";
        await UploadAsync(sut);

        await SaveAsync(sut);

        Assert.Equal(Uploaded, Assert.Single(_requests).ImageUrl);
    }

    [Fact]
    public async Task Save_EditModeUntouched_SendsStoredReferenceWithImageChangedFalse()
    {
        // [AC] REQ-PIC-21: edit mode with no upload or remove, the stored reference is unchanged.
        var sut = await CreateEditSutAsync();

        await SaveAsync(sut);

        var request = Assert.Single(_requests);
        Assert.False(request.ImageChanged);
        Assert.Equal(Stored, request.ImageUrl);
    }

    [Fact]
    public async Task Save_EditModeUpload_SendsUploadWithImageChangedTrue()
    {
        // [AC] REQ-PIC-21: edit mode after an upload, the stored reference becomes the form picture's reference.
        var sut = await CreateEditSutAsync();
        await UploadAsync(sut);

        await SaveAsync(sut);

        var request = Assert.Single(_requests);
        Assert.True(request.ImageChanged);
        Assert.Equal(Uploaded, request.ImageUrl);
    }

    [Fact]
    public async Task Save_EditModeRemove_SendsNullWithImageChangedTrue()
    {
        // [AC] REQ-PIC-21: edit mode after Remove, the stored reference becomes null.
        var sut = await CreateEditSutAsync();
        sut.RemoveImageCommand.Execute(null);

        await SaveAsync(sut);

        var request = Assert.Single(_requests);
        Assert.True(request.ImageChanged);
        Assert.Null(request.ImageUrl);
    }

    // ── REQ-PIC-22: a failed Save keeps the form picture ────────────────

    [Fact]
    public async Task Save_AfterFailedOutcome_KeepsTheUploadForTheNextSave()
    {
        // [AC] REQ-PIC-22: a failed Save keeps the form picture for the next Save.
        var attempt = 0;
        var sut = await CreateEditSutAsync(decide: _ => ++attempt == 1 ? Failed(SaveField.None, "No") : Saved());
        await UploadAsync(sut);

        await SaveAsync(sut);
        await SaveAsync(sut);

        Assert.True(sut.HasFormImage);
        Assert.Equal(2, _requests.Count);
        Assert.Equal(Uploaded, _requests[1].ImageUrl);
        Assert.True(_requests[1].ImageChanged);
    }

    // ── REQ-PIC-10: failure and cancel ───────────────────────────────────

    [Fact]
    public async Task PickImage_ImportFails_ShowsTheMessageAndKeepsThePicture()
    {
        // [AC] REQ-PIC-10: an unusable photo leaves the form picture unchanged and shows one message.
        var sut = CreateSut();
        await UploadAsync(sut);
        _imageStoreMock
            .Setup(s => s.ImportAsync(It.IsAny<Stream>(), ImageOwnerKind.Song, It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, ImportFailure, (string?)null));

        await UploadAsync(sut);

        Assert.Equal(Uploaded, sut.FormImageSource);
        _snackbarMock.Verify(s => s.ShowErrorAsync(ImportFailure), Times.Once);
        Assert.False(sut.IsImageBusy);
    }

    [Fact]
    public async Task PickImage_Cancelled_ChangesNothingAndImportsNothing()
    {
        // [AC] REQ-PIC-05: cancelling the pick changes nothing.
        _pickerMock.Setup(p => p.PickPhotoAsync(It.IsAny<CancellationToken>())).ReturnsAsync((Stream?)null);
        var sut = CreateSut();

        await UploadAsync(sut);

        Assert.False(sut.HasFormImage);
        _imageStoreMock.Verify(s => s.ImportAsync(It.IsAny<Stream>(), It.IsAny<ImageOwnerKind>(), It.IsAny<CancellationToken>()), Times.Never);
        _snackbarMock.Verify(s => s.ShowErrorAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task PickImage_Replace_SecondUploadReplacesTheFirst()
    {
        // [AC] REQ-PIC-15: "Replace picture" picks again and the new upload becomes the form picture.
        _importReferences.Enqueue(Uploaded);
        _importReferences.Enqueue(Uploaded2);
        var sut = CreateSut();

        await UploadAsync(sut);
        await UploadAsync(sut);

        Assert.Equal(Uploaded2, sut.FormImageSource);
    }

    // ── REQ-PIC-27: releasing a picked Title ─────────────────────────────

    [Fact]
    public async Task TitleReleased_TextUnchanged_SaveKeepsTheDeezerIdentityAndImage()
    {
        // [AC] REQ-PIC-27: a Title opened by a tap and saved unchanged keeps the Deezer identity.
        var sut = CreateSut();
        await sut.SelectTitleCommand.ExecuteAsync(Pick(RemoteDto()));

        sut.IsTitleLocked = false;   // the tap-to-edit wrapper releases the pick, the text stays
        await SaveAsync(sut);

        var request = Assert.Single(_requests);
        Assert.Equal("Creep", sut.SongTitle);
        Assert.Equal("Deezer", request.ExternalProvider);
        Assert.Equal("dz-1", request.ExternalId);
        Assert.Equal(Cover, request.ImageUrl);
    }

    [Fact]
    public async Task TitleReleased_TextChanged_SaveSendsNoIdentityButKeepsTheImage()
    {
        // [AC] REQ-PIC-27: a Title opened by a tap and changed is saved with no external identity; the pending image is kept.
        var sut = CreateSut();
        await sut.SelectTitleCommand.ExecuteAsync(Pick(RemoteDto()));

        sut.IsTitleLocked = false;
        sut.SongTitle = "Creep (Acoustic)";
        await SaveAsync(sut);

        var request = Assert.Single(_requests);
        Assert.Null(request.ExternalProvider);
        Assert.Null(request.ExternalId);
        Assert.Equal(Cover, request.ImageUrl);
        Assert.Equal(ArtistPic, request.ArtistImageUrl);
    }

    // ── REQ-PIC-28: releasing a picked Artist ────────────────────────────

    [Fact]
    public async Task ArtistReleased_TextUnchanged_SendsTextNotIdAndTheDeezerArtistImage()
    {
        // [AC] REQ-PIC-28: a released Artist drops the local id and resolves by its text; unchanged text keeps the Deezer artist image.
        var sut = CreateSut();
        await sut.SelectTitleCommand.ExecuteAsync(Pick(RemoteDto(localArtistId: 8, localArtistName: "Radiohead")));

        sut.IsArtistLocked = false;   // the tap-to-edit wrapper releases the pick, the text stays
        await SaveAsync(sut);

        var request = Assert.Single(_requests);
        Assert.Equal("Radiohead", sut.ArtistSearchText);
        Assert.Null(request.ArtistId);
        Assert.Equal("Radiohead", request.ArtistText);
        Assert.Equal(ArtistPic, request.ArtistImageUrl);
    }

    [Fact]
    public async Task ArtistReleased_TextChanged_SendsTheNewTextAndNoDeezerArtistImage()
    {
        // [AC] REQ-PIC-28: the Deezer artist image is sent only when the Artist text still equals the picked name exactly.
        var sut = CreateSut();
        await sut.SelectTitleCommand.ExecuteAsync(Pick(RemoteDto()));

        sut.IsArtistLocked = false;
        sut.ArtistSearchText = "Radiohead Tribute";
        await SaveAsync(sut);

        var request = Assert.Single(_requests);
        Assert.Null(request.ArtistId);
        Assert.Equal("Radiohead Tribute", request.ArtistText);
        Assert.Null(request.ArtistImageUrl);
        Assert.Equal(Cover, request.ImageUrl);
    }

    [Fact]
    public async Task ArtistReleased_ThenBlurWithoutPick_KeepsTextAndShowsNoError()
    {
        // [AC] REQ-PIC-28: after the release a blur keeps the typed artist text for resolution on Save (REQ-SFT-17).
        var sut = CreateSut();
        sut.SelectArtistCommand.Execute(ArtistPick(8, "Radiohead"));

        sut.IsArtistLocked = false;
        sut.ArtistSearchText = "Radiohed";
        sut.ArtistBlurredWithoutSelectionCommand.Execute(null);

        Assert.Equal("Radiohed", sut.ArtistSearchText);
        Assert.False(sut.ArtistHasError);
    }

    // ── REQ-PIC-31: the Artist of a stored song can be changed ──────────

    [Fact]
    public async Task EditMode_ArtistReleasedThenOtherArtistPicked_SendsTheNewArtistId()
    {
        // [AC] REQ-PIC-31: in edit mode a released Artist accepts a pick; Save links the picked artist.
        var sut = await CreateEditSutAsync();

        sut.IsArtistLocked = false;
        sut.SelectArtistCommand.Execute(ArtistPick(9, "Portishead"));
        await SaveAsync(sut);

        var request = Assert.Single(_requests);
        Assert.True(sut.IsArtistLocked);
        Assert.Equal(9, request.ArtistId);
        Assert.Null(request.ArtistText);
    }

    [Fact]
    public async Task EditMode_ArtistReleasedThenTyped_SendsTheTypedText()
    {
        // [AC] REQ-PIC-31: in edit mode a released Artist accepts typed text; Save resolves the artist from it.
        var sut = await CreateEditSutAsync();

        sut.IsArtistLocked = false;
        sut.ArtistSearchText = "Portishead";
        await SaveAsync(sut);

        var request = Assert.Single(_requests);
        Assert.Equal(42, request.SongId);
        Assert.Null(request.ArtistId);
        Assert.Equal("Portishead", request.ArtistText);
    }

    [Fact]
    public async Task EditMode_LoadedArtistNotReleased_SendsTheStoredArtistId()
    {
        // [AC] REQ-PIC-31: loading the stored song is not a release; an untouched Artist keeps the stored link.
        var sut = await CreateEditSutAsync();

        await SaveAsync(sut);

        Assert.True(sut.IsArtistLocked);
        Assert.Equal(1, Assert.Single(_requests).ArtistId);
    }

    // ── REQ-PIC-32: a refused artist change shows on the Artist field ───

    [Fact]
    public async Task EditMode_SaveFailedOnArtist_ShowsTheMessageOnTheArtistField()
    {
        // [AC] REQ-PIC-32: a change that would duplicate (artist, title, version) is refused with a message on the form.
        const string message = "This artist already has a song with this title and version.";
        var sut = await CreateEditSutAsync(decide: _ => Failed(SaveField.Artist, message));
        sut.IsArtistLocked = false;
        sut.ArtistSearchText = "Portishead";

        await SaveAsync(sut);

        Assert.True(sut.ArtistHasError);
        Assert.Equal(message, sut.ArtistErrorText);
        _navigationMock.Verify(n => n.GoToAsync(It.IsAny<string>()), Times.Never);
    }

    // ── REQ-PIC-39: options show uploaded pictures through the thumbnail ─

    [Fact]
    public async Task TitleOption_LocalReference_CarriesTheResolvedThumbnail()
    {
        // [AC] REQ-PIC-39: Title options show an uploaded picture through its thumbnail file.
        _imageStoreMock.Setup(s => s.ResolveDisplaySource(Uploaded, ImageSize.Thumbnail)).Returns("/images/songs/x_t.jpg");
        var local = new SongSuggestionDto(3, "Creep", "Radiohead", 1, null, null, false, string.Empty, "Radiohead", Uploaded, null, null);
        _suggestionsMock.Setup(s => s.SearchTitleLocalAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync([local]);
        _suggestionsMock
            .Setup(s => s.SearchTitleRemoteAsync(It.IsAny<string>(), It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SongRemoteSearchResult([], [], RemoteSearchStatus.Ok));
        var sut = CreateSut();

        var option = Assert.Single(await sut.TitleOptionSource.SearchAsync("creep", CancellationToken.None));

        Assert.Equal("/images/songs/x_t.jpg", option.ImageUrl);
    }

    [Fact]
    public async Task ArtistOption_LocalReference_CarriesTheResolvedThumbnail()
    {
        // [AC] REQ-PIC-39: Artist options show an uploaded picture through its thumbnail file.
        const string artistRef = "local:artists/0123456789abcdef0123456789abcdef";
        _imageStoreMock.Setup(s => s.ResolveDisplaySource(artistRef, ImageSize.Thumbnail)).Returns("/images/artists/a_t.jpg");
        var artistService = new Mock<IArtistService>();
        artistService
            .Setup(s => s.SearchArtistsByNameAsync("radio", It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ArtistListItem(2, "Radiohead", string.Empty, 3, artistRef)]);
        var sut = CreateSut(artistService: artistService);

        var option = Assert.Single(await sut.ArtistOptionSource.SearchAsync("radio", CancellationToken.None));

        Assert.Equal("/images/artists/a_t.jpg", option.ImageUrl);
    }
}
