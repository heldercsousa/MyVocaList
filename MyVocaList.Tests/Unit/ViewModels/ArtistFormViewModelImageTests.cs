using CommunityToolkit.Mvvm.Messaging;
using MyVocaList.Contracts.DTOs.Suggestions;
using MyVocaList.Domain.Rules;
using MyVocaList.UI.Services;

namespace MyVocaList.Tests.Unit.ViewModels;

/// <summary>
/// The artist form picture and the Name options' avatars (REQ-PIC-05, 10, 16, 34..37). The image store is
/// mocked: a display source is written as "<c>size:reference</c>", so a test can see which reference and
/// which size the ViewModel resolved.
/// </summary>
public class ArtistFormViewModelImageTests
{
    private const string DeezerPic = "https://img.test/artist.jpg";
    private const string Upload = "local:artists/0123456789abcdef0123456789abcdef";
    private const string StoredPic = "local:artists/fedcba9876543210fedcba9876543210";
    private const string ArtistPlaceholder = "account_circle_outlined";
    private const string PickFailed = "Could not use this picture.";

    private readonly Mock<IArtistService> _serviceMock = new();
    private readonly Mock<ISnackbarComponent> _snackMock = new();
    private readonly Mock<INavigationService> _navigationMock = new();
    private readonly Mock<IArtistSuggestionService> _suggestionsMock = new();
    private readonly Mock<IImageStore> _imageStoreMock = new();
    private readonly Mock<IPhotoPicker> _photoPickerMock = new();

    public ArtistFormViewModelImageTests()
    {
        _imageStoreMock
            .Setup(s => s.ResolveDisplaySource(It.IsAny<string?>(), It.IsAny<ImageSize>()))
            .Returns((string? reference, ImageSize size) => reference is null ? null : $"{size}:{reference}");
        _serviceMock.Setup(s => s.ValidateNameInput(It.IsAny<string>())).Returns((true, string.Empty));
        _serviceMock
            .Setup(s => s.SetArtistImageAsync(It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, "Picture saved"));
    }

    private ArtistFormViewModel CreateSut(int? artistId = null)
    {
        var sut = new ArtistFormViewModel(
            _serviceMock.Object, _snackMock.Object, new Mock<ILogger<ArtistFormViewModel>>().Object,
            new Mock<IMessenger>().Object, _navigationMock.Object, _suggestionsMock.Object,
            _imageStoreMock.Object, _photoPickerMock.Object);
        if (artistId.HasValue)
            sut.ArtistId = artistId;
        sut.CompleteHydration();
        return sut;
    }

    private static ArtistSuggestionDto LocalArtist(int id, string name, string? image) =>
        new(id, name, null, null, false, image);

    private static ArtistSuggestionDto RemoteArtist(string name, string? image) =>
        new(null, name, "dz-9", "Deezer", true, image);

    private static AutocompleteSuggestion Pick(ArtistSuggestionDto dto) => new(dto.Name, string.Empty, dto);

    private void PickerReturnsPhoto() =>
        _photoPickerMock.Setup(p => p.PickPhotoAsync(It.IsAny<CancellationToken>()))
                        .ReturnsAsync(() => new MemoryStream([1, 2, 3]));

    private void ImportReturns(bool success, string message, string? reference) =>
        _imageStoreMock
            .Setup(s => s.ImportAsync(It.IsAny<Stream>(), ImageOwnerKind.Artist, It.IsAny<CancellationToken>()))
            .ReturnsAsync((success, message, reference));

    private async Task UploadAsync(ArtistFormViewModel sut)
    {
        PickerReturnsPhoto();
        ImportReturns(true, string.Empty, Upload);
        await sut.PickImageCommand.ExecuteAsync(null);
    }

    private void CreateSucceeds(int newId = 42) =>
        _serviceMock
            .Setup(s => s.CreateArtistAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string name, string? _, string? _, CancellationToken _) =>
                (true, "Artist created", new Artist { Id = newId, Name = name }));

    private void UpdateSucceeds(int id) =>
        _serviceMock.Setup(s => s.UpdateArtistAsync(id, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((true, "Artist updated"));

    private void StoredArtist(int id, string? image) =>
        _serviceMock.Setup(s => s.GetArtistByIdAsync(id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new Artist { Id = id, Name = "Metallica", ImageUrl = image });

    private void SetupNameSearch(IReadOnlyList<ArtistSuggestionDto> local, params ArtistSuggestionDto[] remote)
    {
        _suggestionsMock.Setup(s => s.SearchNameLocalAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(local);
        _suggestionsMock.Setup(s => s.SearchNameRemoteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new ArtistRemoteSearchResult(remote, RemoteSearchStatus.Ok));
    }

    private void VerifyNoImageStored() =>
        _serviceMock.Verify(
            s => s.SetArtistImageAsync(It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);

    // ── Name options show the avatar (REQ-PIC-34) ──────────────────────────────

    [Fact]
    // [AC] REQ-PIC-34: a local Name option shows a leading avatar with the artist's picture (thumbnail size).
    public async Task NameOptionSource_LocalRow_IsAvatarWithResolvedThumbnail()
    {
        var sut = CreateSut();
        SetupNameSearch([LocalArtist(5, "Metallica", StoredPic)]);

        var options = await sut.NameOptionSource.SearchAsync("metallica", CancellationToken.None);

        var option = Assert.Single(options);
        Assert.Equal(AutocompleteLeadingKind.Avatar, option.LeadingKind);
        Assert.Equal($"{ImageSize.Thumbnail}:{StoredPic}", option.ImageUrl);
        Assert.Equal(ArtistPlaceholder, option.PlaceholderIcon);
    }

    [Fact]
    // [AC] REQ-PIC-34: a Deezer Name option shows a leading avatar with the Deezer picture (thumbnail size).
    public async Task NameOptionSource_DeezerRow_IsAvatarWithResolvedThumbnail()
    {
        var sut = CreateSut();
        SetupNameSearch([], RemoteArtist("Metallica Tribute", DeezerPic));

        var options = await sut.NameOptionSource.SearchAsync("metallica", CancellationToken.None);

        var option = Assert.Single(options);
        Assert.Equal(AutocompleteLeadingKind.Avatar, option.LeadingKind);
        Assert.Equal($"{ImageSize.Thumbnail}:{DeezerPic}", option.ImageUrl);
        Assert.Equal(ArtistPlaceholder, option.PlaceholderIcon);
    }

    [Fact]
    // [AC] REQ-PIC-34: a Name option without a picture shows the placeholder avatar.
    public async Task NameOptionSource_RowWithoutPicture_IsAvatarWithPlaceholderOnly()
    {
        var sut = CreateSut();
        SetupNameSearch([LocalArtist(5, "Metallica", null)]);

        var options = await sut.NameOptionSource.SearchAsync("metallica", CancellationToken.None);

        var option = Assert.Single(options);
        Assert.Equal(AutocompleteLeadingKind.Avatar, option.LeadingKind);
        Assert.Null(option.ImageUrl);
        Assert.Equal(ArtistPlaceholder, option.PlaceholderIcon);
    }

    // ── Size hint (REQ-PIC-16) ─────────────────────────────────────────────────

    [Fact]
    // [AC] REQ-PIC-16: the size hint text is exactly the approved wording.
    public void ImageHintText_IsTheApprovedSizeHint()
    {
        var sut = CreateSut();

        Assert.Equal("Square picture (1:1), 512 x 512 px or larger. Other shapes are cropped to the centre.",
            sut.ImageHintText);
    }

    // ── Upload (REQ-PIC-05, 10) ────────────────────────────────────────────────

    [Fact]
    // [AC] REQ-PIC-05: cancelling the pick changes nothing.
    public async Task PickImage_Cancelled_ChangesNothing()
    {
        var sut = CreateSut();
        _photoPickerMock.Setup(p => p.PickPhotoAsync(It.IsAny<CancellationToken>())).ReturnsAsync((Stream?)null);

        await sut.PickImageCommand.ExecuteAsync(null);

        Assert.False(sut.HasFormImage);
        _imageStoreMock.Verify(
            s => s.ImportAsync(It.IsAny<Stream>(), It.IsAny<ImageOwnerKind>(), It.IsAny<CancellationToken>()), Times.Never);
        _snackMock.Verify(s => s.ShowErrorAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    // [AC] REQ-PIC-10: an unusable photo shows the message and leaves the form picture unchanged.
    public async Task PickImage_ImportFails_ShowsMessageAndKeepsFormPicture()
    {
        var sut = CreateSut();
        await sut.SelectNameCommand.ExecuteAsync(Pick(RemoteArtist("Metallica Tribute", DeezerPic)));
        PickerReturnsPhoto();
        ImportReturns(false, PickFailed, null);

        await sut.PickImageCommand.ExecuteAsync(null);

        _snackMock.Verify(s => s.ShowErrorAsync(PickFailed), Times.Once);
        Assert.Equal($"{ImageSize.Large}:{DeezerPic}", sut.FormImageSource);
        Assert.False(sut.IsImageBusy);
    }

    // ── The artist's form picture (REQ-PIC-35) ─────────────────────────────────

    [Fact]
    // [AC] REQ-PIC-35: a new artist with no picture has no form picture (the placeholder shows).
    public void FormPicture_NewArtist_IsNone()
    {
        var sut = CreateSut();

        Assert.False(sut.HasFormImage);
        Assert.Null(sut.FormImageSource);
        Assert.Null(sut.FormImageThumbnailSource);
    }

    [Fact]
    // [AC] REQ-PIC-35: an uploaded picture is the form picture, in the large and thumbnail sizes.
    public async Task FormPicture_AfterUpload_IsTheUpload()
    {
        var sut = CreateSut();

        await UploadAsync(sut);

        Assert.True(sut.HasFormImage);
        Assert.Equal($"{ImageSize.Large}:{Upload}", sut.FormImageSource);
        Assert.Equal($"{ImageSize.Thumbnail}:{Upload}", sut.FormImageThumbnailSource);
    }

    [Fact]
    // [AC] REQ-PIC-35: add mode, a Deezer pick makes the picked artist's picture the form picture.
    public async Task FormPicture_AddModeDeezerPick_IsTheDeezerPicture()
    {
        var sut = CreateSut();

        await sut.SelectNameCommand.ExecuteAsync(Pick(RemoteArtist("Metallica Tribute", DeezerPic)));

        Assert.True(sut.HasFormImage);
        Assert.Equal($"{ImageSize.Large}:{DeezerPic}", sut.FormImageSource);
    }

    [Fact]
    // [AC] REQ-PIC-35: a Deezer pick made after an upload keeps the upload as the form picture (REQ-PIC-19).
    public async Task FormPicture_DeezerPickAfterUpload_KeepsTheUpload()
    {
        var sut = CreateSut();
        await UploadAsync(sut);

        await sut.SelectNameCommand.ExecuteAsync(Pick(RemoteArtist("Metallica Tribute", DeezerPic)));

        Assert.Equal($"{ImageSize.Large}:{Upload}", sut.FormImageSource);
    }

    [Fact]
    // [AC] REQ-PIC-35: X on Name drops the Deezer picture.
    public async Task FormPicture_ClearNameAfterDeezerPick_DropsTheDeezerPicture()
    {
        var sut = CreateSut();
        await sut.SelectNameCommand.ExecuteAsync(Pick(RemoteArtist("Metallica Tribute", DeezerPic)));

        sut.ClearNameCommand.Execute(null);

        Assert.False(sut.HasFormImage);
    }

    [Fact]
    // [AC] REQ-PIC-35: X on Name does not drop an uploaded picture.
    public async Task FormPicture_ClearNameAfterUpload_KeepsTheUpload()
    {
        var sut = CreateSut();
        await UploadAsync(sut);
        await sut.SelectNameCommand.ExecuteAsync(Pick(RemoteArtist("Metallica Tribute", DeezerPic)));

        sut.ClearNameCommand.Execute(null);

        Assert.Equal($"{ImageSize.Large}:{Upload}", sut.FormImageSource);
    }

    [Fact]
    // [AC] REQ-PIC-35: "Remove picture" leaves no form picture: the upload and the Deezer picture are dropped (REQ-PIC-20).
    public async Task FormPicture_Remove_LeavesNoPicture()
    {
        var sut = CreateSut();
        await sut.SelectNameCommand.ExecuteAsync(Pick(RemoteArtist("Metallica Tribute", DeezerPic)));
        await UploadAsync(sut);

        sut.RemoveImageCommand.Execute(null);

        Assert.False(sut.HasFormImage);
        Assert.Null(sut.FormImageSource);
    }

    [Fact]
    // [AC] REQ-PIC-35: after "Remove picture", a later Deezer pick sets a picture again (REQ-PIC-20).
    public async Task FormPicture_DeezerPickAfterRemove_SetsThePictureAgain()
    {
        var sut = CreateSut();
        await UploadAsync(sut);
        sut.RemoveImageCommand.Execute(null);

        await sut.SelectNameCommand.ExecuteAsync(Pick(RemoteArtist("Metallica Tribute", DeezerPic)));

        Assert.Equal($"{ImageSize.Large}:{DeezerPic}", sut.FormImageSource);
    }

    [Fact]
    // [AC] REQ-PIC-35: edit mode, the stored picture of the artist is the form picture.
    public void FormPicture_EditMode_IsTheStoredPicture()
    {
        StoredArtist(5, StoredPic);

        var sut = CreateSut(artistId: 5);

        Assert.True(sut.HasFormImage);
        Assert.Equal($"{ImageSize.Large}:{StoredPic}", sut.FormImageSource);
    }

    [Fact]
    // [AC] REQ-PIC-35: edit mode, an upload replaces the stored picture as the form picture.
    public async Task FormPicture_EditModeUpload_IsTheUpload()
    {
        StoredArtist(5, StoredPic);
        var sut = CreateSut(artistId: 5);

        await UploadAsync(sut);

        Assert.Equal($"{ImageSize.Large}:{Upload}", sut.FormImageSource);
    }

    [Fact]
    // [AC] REQ-PIC-35: edit mode, "Remove picture" hides the stored picture.
    public void FormPicture_EditModeRemove_IsNone()
    {
        StoredArtist(5, StoredPic);
        var sut = CreateSut(artistId: 5);

        sut.RemoveImageCommand.Execute(null);

        Assert.False(sut.HasFormImage);
    }

    // ── Save stores the artist picture (REQ-PIC-36) ────────────────────────────

    [Fact]
    // [AC] REQ-PIC-36: add mode, the new artist's picture is the form picture (an upload).
    public async Task Save_AddModeWithUpload_StoresTheUploadOnTheNewArtist()
    {
        var sut = CreateSut();
        sut.ArtistName = "Metallica";
        await UploadAsync(sut);
        CreateSucceeds(newId: 42);

        await sut.SaveCommand.ExecuteAsync(null);

        _serviceMock.Verify(s => s.SetArtistImageAsync(42, Upload, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    // [AC] REQ-PIC-36: add mode, a Deezer pick stores the picked artist's picture on the new artist.
    public async Task Save_AddModeDeezerPick_StoresTheDeezerPicture()
    {
        var sut = CreateSut();
        await sut.SelectNameCommand.ExecuteAsync(Pick(RemoteArtist("Metallica Tribute", DeezerPic)));
        CreateSucceeds(newId: 42);

        await sut.SaveCommand.ExecuteAsync(null);

        _serviceMock.Verify(s => s.SetArtistImageAsync(42, DeezerPic, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    // [AC] REQ-PIC-36: add mode with no form picture stores no picture.
    public async Task Save_AddModeWithoutPicture_StoresNoPicture()
    {
        var sut = CreateSut();
        sut.ArtistName = "Metallica";
        CreateSucceeds();

        await sut.SaveCommand.ExecuteAsync(null);

        VerifyNoImageStored();
    }

    [Fact]
    // [AC] REQ-PIC-36: edit mode, an untouched picture is not stored again.
    public async Task Save_EditModeUntouchedPicture_StoresNoPicture()
    {
        StoredArtist(5, StoredPic);
        var sut = CreateSut(artistId: 5);
        sut.ArtistName = "Metallica Live";
        UpdateSucceeds(5);

        await sut.SaveCommand.ExecuteAsync(null);

        _serviceMock.Verify(s => s.UpdateArtistAsync(5, "Metallica Live", It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoImageStored();
    }

    [Fact]
    // [AC] REQ-PIC-36: edit mode, an uploaded picture is stored on the artist.
    public async Task Save_EditModeUpload_StoresTheUpload()
    {
        StoredArtist(5, StoredPic);
        var sut = CreateSut(artistId: 5);
        sut.ArtistName = "Metallica";
        await UploadAsync(sut);
        UpdateSucceeds(5);

        await sut.SaveCommand.ExecuteAsync(null);

        _serviceMock.Verify(s => s.SetArtistImageAsync(5, Upload, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    // [AC] REQ-PIC-36: edit mode, a removed picture is stored as no picture.
    public async Task Save_EditModeRemove_StoresNull()
    {
        StoredArtist(5, StoredPic);
        var sut = CreateSut(artistId: 5);
        sut.ArtistName = "Metallica";
        sut.RemoveImageCommand.Execute(null);
        UpdateSucceeds(5);

        await sut.SaveCommand.ExecuteAsync(null);

        _serviceMock.Verify(s => s.SetArtistImageAsync(5, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    // [AC] REQ-PIC-36: a failed Save stores nothing.
    public async Task Save_CreateFails_StoresNoPicture()
    {
        var sut = CreateSut();
        sut.ArtistName = "Metallica";
        await UploadAsync(sut);
        _serviceMock
            .Setup(s => s.CreateArtistAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "An artist with this name already exists", (Artist?)null));

        await sut.SaveCommand.ExecuteAsync(null);

        VerifyNoImageStored();
        Assert.Equal($"{ImageSize.Large}:{Upload}", sut.FormImageSource);
    }

    [Fact]
    // [AC] REQ-PIC-36: a failed Save stores nothing (edit mode: the update fails).
    public async Task Save_UpdateFails_StoresNoPicture()
    {
        StoredArtist(5, StoredPic);
        var sut = CreateSut(artistId: 5);
        sut.ArtistName = "Metallica";
        await UploadAsync(sut);
        _serviceMock.Setup(s => s.UpdateArtistAsync(5, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((false, "An artist with this name already exists"));

        await sut.SaveCommand.ExecuteAsync(null);

        VerifyNoImageStored();
    }

    [Fact]
    // [AC] REQ-PIC-36: when storing the picture fails, its message shows and the form stays open with the
    // created artist in edit mode, keeping the picture for the next Save.
    public async Task Save_AddModeStoringPictureFails_ShowsMessageAndSwitchesToEditMode()
    {
        var sut = CreateSut();
        sut.ArtistName = "Metallica";
        await UploadAsync(sut);
        CreateSucceeds(newId: 42);
        _serviceMock.Setup(s => s.SetArtistImageAsync(42, Upload, It.IsAny<CancellationToken>()))
                    .ReturnsAsync((false, "Artist not found"));

        await sut.SaveCommand.ExecuteAsync(null);

        _snackMock.Verify(s => s.ShowErrorAsync("Artist not found"), Times.Once);
        _snackMock.Verify(s => s.ShowSuccessAsync(It.IsAny<string>()), Times.Never);
        Assert.True(sut.IsEditMode);
        Assert.Equal(42, sut.ArtistId);
        Assert.Equal($"{ImageSize.Large}:{Upload}", sut.FormImageSource);
    }

    [Fact]
    // [AC] REQ-PIC-36: after a failed picture store, the next Save updates the created artist and stores the picture.
    public async Task Save_AfterStoringPictureFailed_NextSaveUpdatesAndStoresThePicture()
    {
        var sut = CreateSut();
        sut.ArtistName = "Metallica";
        await UploadAsync(sut);
        CreateSucceeds(newId: 42);
        _serviceMock.SetupSequence(s => s.SetArtistImageAsync(42, Upload, It.IsAny<CancellationToken>()))
                    .ReturnsAsync((false, "Artist not found"))
                    .ReturnsAsync((true, "Picture saved"));
        await sut.SaveCommand.ExecuteAsync(null);
        UpdateSucceeds(42);

        await sut.SaveCommand.ExecuteAsync(null);

        _serviceMock.Verify(s => s.UpdateArtistAsync(42, "Metallica", It.IsAny<CancellationToken>()), Times.Once);
        _serviceMock.Verify(s => s.SetArtistImageAsync(42, Upload, It.IsAny<CancellationToken>()), Times.Exactly(2));
        _serviceMock.Verify(
            s => s.CreateArtistAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── Name as tap-to-edit text (REQ-PIC-37) ──────────────────────────────────

    [Fact]
    // [AC] REQ-PIC-37: releasing a picked Deezer Name by a tap keeps the text and the picked state ends.
    public async Task ReleaseByTap_AfterDeezerPick_KeepsTheText()
    {
        var sut = CreateSut();
        await sut.SelectNameCommand.ExecuteAsync(Pick(RemoteArtist("Metallica Tribute", DeezerPic)));

        sut.IsNameLocked = false;   // what TapToEditField does on a tap (TwoWay)
        sut.IsNameEditing = true;

        Assert.Equal("Metallica Tribute", sut.ArtistName);
        Assert.True(sut.HasFormImage);
    }

    [Fact]
    // [AC] REQ-PIC-37: a released Deezer Name saved unchanged keeps the Deezer picture.
    public async Task Save_ReleasedDeezerNameUnchanged_StoresTheDeezerPicture()
    {
        var sut = CreateSut();
        await sut.SelectNameCommand.ExecuteAsync(Pick(RemoteArtist("Metallica Tribute", DeezerPic)));
        sut.IsNameLocked = false;
        CreateSucceeds(newId: 42);

        await sut.SaveCommand.ExecuteAsync(null);

        _serviceMock.Verify(s => s.SetArtistImageAsync(42, DeezerPic, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    // [AC] REQ-PIC-37: a released Deezer Name changed before Save drops the Deezer picture.
    public async Task Save_ReleasedDeezerNameChanged_StoresNoPicture()
    {
        var sut = CreateSut();
        await sut.SelectNameCommand.ExecuteAsync(Pick(RemoteArtist("Metallica Tribute", DeezerPic)));
        sut.IsNameLocked = false;
        sut.ArtistName = "Metallica Tribute Band";
        CreateSucceeds(newId: 42);

        await sut.SaveCommand.ExecuteAsync(null);

        VerifyNoImageStored();
        Assert.False(sut.HasFormImage);
    }

    [Fact]
    // [AC] REQ-PIC-37: a changed released Name still keeps an uploaded picture.
    public async Task Save_ReleasedDeezerNameChangedWithUpload_StoresTheUpload()
    {
        var sut = CreateSut();
        await sut.SelectNameCommand.ExecuteAsync(Pick(RemoteArtist("Metallica Tribute", DeezerPic)));
        await UploadAsync(sut);
        sut.IsNameLocked = false;
        sut.ArtistName = "Metallica Tribute Band";
        CreateSucceeds(newId: 42);

        await sut.SaveCommand.ExecuteAsync(null);

        _serviceMock.Verify(s => s.SetArtistImageAsync(42, Upload, It.IsAny<CancellationToken>()), Times.Once);
    }
}
