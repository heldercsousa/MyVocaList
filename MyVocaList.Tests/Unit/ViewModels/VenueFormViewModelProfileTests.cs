using System.IO;
using CommunityToolkit.Mvvm.Messaging;
using MyVocaList.Contracts.DTOs.Venues;
using MyVocaList.Domain.Rules;
using MyVocaList.Domain.ServicesInterfaces;
using MyVocaList.UI.Messages;
using MyVocaList.UI.Services;

namespace MyVocaList.Tests.Unit.ViewModels;

/// <summary>
/// Level A tests for the venue form's picture, address fields, edit load and map pick (design.md § 2.7, § 7).
/// Tests stub every save as a failure where the success path would reach Shell.Current.
/// </summary>
public class VenueFormViewModelProfileTests
{
    private readonly Mock<IVenueService> _serviceMock = new();
    private readonly Mock<ISnackbarComponent> _snackMock = new();
    private readonly Mock<ILogger<VenueFormViewModel>> _loggerMock = new();
    private readonly Mock<IImageStore> _imageStoreMock = new();
    private readonly Mock<IPhotoPicker> _photoPickerMock = new();
    private readonly WeakReferenceMessenger _messenger = new();
    private readonly Mock<IVenuePictureLookupService> _pictureLookupMock = new();
    private readonly Mock<IRemoteImageFetcher> _fetcherMock = new();

    private const string StubbedFailure = "stubbed failure — avoids Shell.Current in test context";

    private VenueFormViewModel CreateSut() =>
        new(_serviceMock.Object, _snackMock.Object, _loggerMock.Object,
            _imageStoreMock.Object, _photoPickerMock.Object, _messenger,
            _pictureLookupMock.Object, _fetcherMock.Object);

    private void StubPickedPhoto(string reference)
    {
        _photoPickerMock.Setup(p => p.PickPhotoAsync(It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new MemoryStream());
        _imageStoreMock.Setup(i => i.ImportAsync(It.IsAny<Stream>(), ImageOwnerKind.Venue, It.IsAny<CancellationToken>()))
                       .ReturnsAsync((true, string.Empty, reference));
    }

    private void StubSaveFailure(VenueField field = VenueField.None, string message = StubbedFailure)
    {
        _serviceMock.Setup(s => s.SaveVenueAsync(It.IsAny<VenueSaveRequest>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((false, message, field));
    }

    private void StubValidForm()
    {
        _serviceMock.Setup(s => s.ValidateNameInput(It.IsAny<string>())).Returns((true, string.Empty));
        _serviceMock.Setup(s => s.ValidateAddressInput(It.IsAny<VenueAddress>()))
                    .Returns((true, string.Empty, VenueField.None));
    }

    private void StubEditVenue(VenueEditDto dto) =>
        _serviceMock.Setup(s => s.GetVenueForEditAsync(dto.Id, It.IsAny<CancellationToken>())).ReturnsAsync(dto);

    // ── Picture (REQ-VEN-08..11) ──────────────────────────────────────────────

    [Fact]
    // [AC] REQ-VEN-08: WHEN the form opens in add mode, the picture field shows the default venue image (no form picture).
    public void NewForm_InAddMode_HasNoFormPicture()
    {
        var sut = CreateSut();

        Assert.False(sut.HasFormImage);
        Assert.Null(sut.FormImageReference);
    }

    [Fact]
    // [AC] REQ-VEN-09: With a form picture the field offers "Replace picture" and "Remove picture" (HasFormImage drives it).
    public async Task PickImage_Success_SetsFormPicture()
    {
        var sut = CreateSut();
        StubPickedPhoto("local:venues/0123456789abcdef0123456789abcdef");

        await sut.PickImageCommand.ExecuteAsync(null);

        Assert.True(sut.HasFormImage);
        Assert.Equal("local:venues/0123456789abcdef0123456789abcdef", sut.FormImageReference);
    }

    [Fact]
    // [AC] REQ-VEN-10: Picking a picture changes the form only; nothing is stored before Save.
    public async Task PickImage_DoesNotCallService()
    {
        var sut = CreateSut();
        StubPickedPhoto("local:venues/0123456789abcdef0123456789abcdef");

        await sut.PickImageCommand.ExecuteAsync(null);

        _serviceMock.VerifyNoOtherCalls();
    }

    [Fact]
    // [AC] REQ-VEN-10: On Save in add mode the form picture's reference goes in the request, marked as changed.
    public async Task SaveCommand_AddModeWithPicture_SendsPictureReference()
    {
        var sut = CreateSut();
        StubValidForm();
        StubSaveFailure();
        StubPickedPhoto("local:venues/0123456789abcdef0123456789abcdef");
        sut.VenueName = "Jazz Club";
        await sut.PickImageCommand.ExecuteAsync(null);

        await sut.SaveCommand.ExecuteAsync(null);

        _serviceMock.Verify(s => s.SaveVenueAsync(
            It.Is<VenueSaveRequest>(r => r.Id == null
                && r.ImageReference == "local:venues/0123456789abcdef0123456789abcdef"
                && r.ImageChanged),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    // [AC] REQ-VEN-10: Edit mode with a removed picture sends no reference and marks the picture as changed.
    public async Task SaveCommand_EditModeRemovedPicture_SendsNullReferenceAsChanged()
    {
        var sut = CreateSut();
        StubValidForm();
        StubSaveFailure();
        StubEditVenue(new VenueEditDto(7, "Jazz Club", "local:venues/old", VenueAddress.Empty, null, null));
        sut.VenueIdRaw = "7";
        sut.RemoveImageCommand.Execute(null);

        await sut.SaveCommand.ExecuteAsync(null);

        _serviceMock.Verify(s => s.SaveVenueAsync(
            It.Is<VenueSaveRequest>(r => r.Id == 7 && r.ImageReference == null && r.ImageChanged),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    // [AC] REQ-VEN-10: Edit mode without a pick or removal sends the stored reference, marked as unchanged.
    public async Task SaveCommand_EditModeUntouchedPicture_SendsStoredReferenceAsUnchanged()
    {
        var sut = CreateSut();
        StubValidForm();
        StubSaveFailure();
        StubEditVenue(new VenueEditDto(7, "Jazz Club", "local:venues/old", VenueAddress.Empty, null, null));
        sut.VenueIdRaw = "7";

        await sut.SaveCommand.ExecuteAsync(null);

        _serviceMock.Verify(s => s.SaveVenueAsync(
            It.Is<VenueSaveRequest>(r => r.ImageReference == "local:venues/old" && !r.ImageChanged),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    // [AC] REQ-VEN-11: A failed Save keeps the form picture.
    public async Task SaveCommand_Failure_KeepsFormPicture()
    {
        var sut = CreateSut();
        StubValidForm();
        StubSaveFailure(VenueField.Name, "There is another venue registered with this name");
        StubPickedPhoto("local:venues/0123456789abcdef0123456789abcdef");
        sut.VenueName = "Jazz Club";
        await sut.PickImageCommand.ExecuteAsync(null);

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.True(sut.HasFormImage);
        Assert.Equal("local:venues/0123456789abcdef0123456789abcdef", sut.FormImageReference);
    }

    // ── Edit load (REQ-VEN-17) ────────────────────────────────────────────────

    [Fact]
    // [AC] REQ-VEN-17: WHEN the form opens in edit mode, it loads the venue by id and shows its name and six address fields.
    public void EditMode_LoadsNameAndAddressFromService()
    {
        var sut = CreateSut();
        var address = new VenueAddress("Rua Augusta, 100", null, "São Paulo", "SP", "01304-000", "Brasil");
        StubEditVenue(new VenueEditDto(7, "Jazz Club", null, address, -23.55, -46.65));

        sut.VenueIdRaw = "7";

        Assert.Equal("Jazz Club", sut.VenueName);
        Assert.Equal("Rua Augusta, 100", sut.AddressLine1);
        Assert.Null(sut.AddressLine2);
        Assert.Equal("São Paulo", sut.City);
        Assert.Equal("SP", sut.Region);
        Assert.Equal("01304-000", sut.PostalCode);
        Assert.Equal("Brasil", sut.Country);
        Assert.Equal(-23.55, sut.Latitude);
        Assert.Equal(-46.65, sut.Longitude);
    }

    [Fact]
    // [AC] REQ-VEN-17: IF the venue no longer exists, THEN the form shows the snackbar "Venue not found".
    public void EditMode_VenueMissing_ShowsVenueNotFound()
    {
        var sut = CreateSut();
        _serviceMock.Setup(s => s.GetVenueForEditAsync(9, It.IsAny<CancellationToken>()))
                    .ReturnsAsync((VenueEditDto?)null);

        sut.VenueIdRaw = "9";

        _snackMock.Verify(s => s.ShowErrorAsync("Venue not found"), Times.Once);
    }

    // ── Field error routing (REQ-VEN-14, 19, 20) ──────────────────────────────

    [Fact]
    // [AC] REQ-VEN-14: IF an address field is not blank and the street is blank, Save shows "Street address is required." on that field.
    public async Task SaveCommand_CountryOnly_ShowsStreetAddressError()
    {
        var sut = CreateSut();
        sut.VenueName = "Jazz Club";
        _serviceMock.Setup(s => s.ValidateNameInput("Jazz Club")).Returns((true, string.Empty));
        sut.Country = "Brasil";
        _serviceMock.Setup(s => s.ValidateAddressInput(It.Is<VenueAddress>(a => a.Country == "Brasil")))
                    .Returns((false, "Street address is required.", VenueField.AddressLine1));

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.True(sut.AddressLine1HasError);
        Assert.Equal("Street address is required.", sut.AddressLine1ErrorText);
        _serviceMock.Verify(s => s.SaveVenueAsync(It.IsAny<VenueSaveRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    // [AC] REQ-VEN-19: IF another venue has an equal name, THEN Save is refused on the Name field.
    public async Task SaveCommand_DuplicateName_ShowsNameError()
    {
        var sut = CreateSut();
        StubValidForm();
        StubSaveFailure(VenueField.Name, "There is another venue registered with this name");
        sut.VenueName = "Jazz Club";

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.True(sut.NameHasError);
        Assert.Equal("There is another venue registered with this name", sut.NameErrorText);
    }

    [Fact]
    // [AC] REQ-VEN-20: IF another venue has the same six address fields, THEN Save is refused on the Street address field.
    public async Task SaveCommand_DuplicateAddress_ShowsStreetAddressError()
    {
        var sut = CreateSut();
        StubValidForm();
        StubSaveFailure(VenueField.AddressLine1, "There is another venue registered at this address");
        sut.VenueName = "Jazz Club";
        sut.AddressLine1 = "Rua Augusta, 100";

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.True(sut.AddressLine1HasError);
        Assert.Equal("There is another venue registered at this address", sut.AddressLine1ErrorText);
    }

    // ── Map button text (REQ-VEN-23) ──────────────────────────────────────────

    [Fact]
    // [AC] REQ-VEN-23: Without coordinates the map button reads "Pick on map".
    public void MapButtonText_WithoutCoordinates_IsPickOnMap()
    {
        var sut = CreateSut();

        Assert.Equal("Pick on map", sut.MapButtonText);
    }

    [Fact]
    // [AC] REQ-VEN-23: WHEN the venue has coordinates, the map button reads "Change on map".
    public void MapButtonText_WithCoordinates_IsChangeOnMap()
    {
        var sut = CreateSut();

        sut.Latitude = -23.55;
        sut.Longitude = -46.65;

        Assert.Equal("Change on map", sut.MapButtonText);
    }

    // ── Picked place (REQ-VEN-30, 31, 33) ─────────────────────────────────────

    [Fact]
    // [AC] REQ-VEN-30: Confirm fills the form: coordinates replace the form's, and the six address fields take the place's values.
    public void PlacePicked_FillsSixFieldsAndCoordinates()
    {
        var sut = CreateSut();
        var place = new VenuePlace(-23.5613, -46.6565,
            new VenueAddress("Rua Augusta, 100", null, "São Paulo", "SP", "01304-000", "Brasil"),
            null, "Rua Augusta, 100, São Paulo");

        _messenger.Send(new VenuePlacePickedMessage(place));

        Assert.Equal("Rua Augusta, 100", sut.AddressLine1);
        Assert.Null(sut.AddressLine2);
        Assert.Equal("São Paulo", sut.City);
        Assert.Equal("SP", sut.Region);
        Assert.Equal("01304-000", sut.PostalCode);
        Assert.Equal("Brasil", sut.Country);
        Assert.Equal(-23.5613, sut.Latitude);
        Assert.Equal(-46.6565, sut.Longitude);
    }

    [Fact]
    // [AC] REQ-VEN-30: A place without any address field leaves the form's address fields unchanged.
    public void PlacePicked_NoAddressFields_KeepsFormAddress()
    {
        var sut = CreateSut();
        sut.AddressLine1 = "Rua Augusta, 100";
        sut.City = "São Paulo";
        var place = new VenuePlace(-23.5, -46.6, VenueAddress.Empty, null, "Somewhere");

        _messenger.Send(new VenuePlacePickedMessage(place));

        Assert.Equal("Rua Augusta, 100", sut.AddressLine1);
        Assert.Equal("São Paulo", sut.City);
    }

    [Fact]
    // [AC] REQ-VEN-30: A non-blank Name is never changed by a picked place.
    public void PlacePicked_NonBlankName_IsKept()
    {
        var sut = CreateSut();
        sut.VenueName = "Jazz Club";
        var place = new VenuePlace(-23.5, -46.6, VenueAddress.Empty, "Bar do Zé", "Somewhere");

        _messenger.Send(new VenuePlacePickedMessage(place));

        Assert.Equal("Jazz Club", sut.VenueName);
    }

    [Fact]
    // [AC] REQ-VEN-31: Leaving the picker without confirming sends no message, so the form stays as it was.
    public void NoPlacePicked_FormUnchanged()
    {
        var sut = CreateSut();
        sut.VenueName = "Jazz Club";
        sut.AddressLine1 = "Rua Augusta, 100";
        sut.Latitude = -23.5;
        sut.Longitude = -46.6;

        Assert.Equal("Jazz Club", sut.VenueName);
        Assert.Equal("Rua Augusta, 100", sut.AddressLine1);
        Assert.Equal(-23.5, sut.Latitude);
        Assert.Equal(-46.6, sut.Longitude);
        _serviceMock.Verify(s => s.SaveVenueAsync(It.IsAny<VenueSaveRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    // [AC] REQ-VEN-33: A hand edit of an address field after a map pick keeps the stored coordinates.
    public void HandEditOfAddressField_KeepsCoordinates()
    {
        var sut = CreateSut();
        sut.Latitude = -23.5;
        sut.Longitude = -46.6;

        sut.AddressLine1 = "Rua Augusta, 200";

        Assert.Equal(-23.5, sut.Latitude);
        Assert.Equal(-46.6, sut.Longitude);
    }

    // ── Picture from a map pick (REQ-VEN-38, 39) ──────────────────────────────

    private const string FetchedReference = "local:venues/0123456789abcdef0123456789abcdef";
    private static readonly Uri PictureUri = new("https://commons.wikimedia.org/wiki/Special:FilePath/Bar.png?width=512");
    private static readonly VenuePictureSource WikidataSource = new("Q918151", null, null);
    private static readonly VenuePlace PlaceWithPicture =
        new(-23.5613, -46.6565, VenueAddress.Empty, null, "Rua Augusta, 100", WikidataSource);

    private void StubPictureWork(bool importSucceeds = true)
    {
        _pictureLookupMock.Setup(p => p.ResolveAsync(It.IsAny<VenuePictureSource>(), It.IsAny<CancellationToken>()))
                          .ReturnsAsync(PictureUri);
        _fetcherMock.Setup(f => f.FetchAsync(PictureUri, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new MemoryStream());
        _imageStoreMock.Setup(i => i.ImportAsync(It.IsAny<Stream>(), ImageOwnerKind.Venue, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(importSucceeds
                           ? (true, string.Empty, FetchedReference)
                           : (false, "Could not use this picture.", (string?)null));
    }

    /// <summary>The picture work runs without being awaited by the field fill; wait until it has finished.</summary>
    private static async Task SettleAsync(VenueFormViewModel sut)
    {
        for (var i = 0; i < 400 && sut.IsImageBusy; i++)
            await Task.Delay(5);
    }

    [Fact]
    // [AC] REQ-VEN-38: a confirmed place with a picture source fills the picture when the form has none, as a changed picture.
    public async Task PlacePicked_FormHasNoPicture_FillsPictureFromPlace()
    {
        var sut = CreateSut();
        StubPictureWork();

        _messenger.Send(new VenuePlacePickedMessage(PlaceWithPicture));
        await SettleAsync(sut);

        Assert.True(sut.HasFormImage);
        Assert.Equal(FetchedReference, sut.FormImageReference);
    }

    [Fact]
    // [AC] REQ-VEN-38: a form that already shows a picture keeps it, and no picture request is sent.
    public async Task PlacePicked_FormAlreadyHasPicture_KeepsItAndSendsNoRequest()
    {
        var sut = CreateSut();
        StubPickedPhoto("local:venues/picked");
        await sut.PickImageCommand.ExecuteAsync(null);
        StubPictureWork();

        _messenger.Send(new VenuePlacePickedMessage(PlaceWithPicture));
        await SettleAsync(sut);

        Assert.Equal("local:venues/picked", sut.FormImageReference);
        _pictureLookupMock.Verify(p => p.ResolveAsync(It.IsAny<VenuePictureSource>(), It.IsAny<CancellationToken>()), Times.Never);
        _fetcherMock.Verify(f => f.FetchAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    // [AC] REQ-VEN-38: a place with an empty picture source sends no picture request.
    public async Task PlacePicked_EmptyPictureSource_SendsNoRequest()
    {
        var sut = CreateSut();
        StubPictureWork();
        var place = new VenuePlace(-23.5, -46.6, VenueAddress.Empty, null, "Somewhere", new VenuePictureSource(null, null, null));

        _messenger.Send(new VenuePlacePickedMessage(place));
        await SettleAsync(sut);

        Assert.False(sut.HasFormImage);
        _pictureLookupMock.Verify(p => p.ResolveAsync(It.IsAny<VenuePictureSource>(), It.IsAny<CancellationToken>()), Times.Never);
        _imageStoreMock.Verify(i => i.ImportAsync(It.IsAny<Stream>(), It.IsAny<ImageOwnerKind>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    // [AC] REQ-VEN-39: when no picture address resolves, the form keeps no picture and shows no error.
    public async Task PlacePicked_ResolverReturnsNull_KeepsNoPictureAndShowsNoError()
    {
        var sut = CreateSut();
        _pictureLookupMock.Setup(p => p.ResolveAsync(It.IsAny<VenuePictureSource>(), It.IsAny<CancellationToken>()))
                          .ReturnsAsync((Uri?)null);

        _messenger.Send(new VenuePlacePickedMessage(PlaceWithPicture));
        await SettleAsync(sut);

        Assert.False(sut.HasFormImage);
        _snackMock.Verify(s => s.ShowErrorAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    // [AC] REQ-VEN-39: a picture that cannot be downloaded is skipped silently: no picture, no error.
    public async Task PlacePicked_DownloadReturnsNothing_KeepsNoPictureAndShowsNoError()
    {
        var sut = CreateSut();
        StubPictureWork();
        _fetcherMock.Setup(f => f.FetchAsync(PictureUri, It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Stream?)null);

        _messenger.Send(new VenuePlacePickedMessage(PlaceWithPicture));
        await SettleAsync(sut);

        Assert.False(sut.HasFormImage);
        _snackMock.Verify(s => s.ShowErrorAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    // [AC] REQ-VEN-39: a store refusal of the fetched picture is skipped silently: no picture, no error.
    public async Task PlacePicked_ImportRefused_KeepsNoPictureAndShowsNoError()
    {
        var sut = CreateSut();
        StubPictureWork(importSucceeds: false);

        _messenger.Send(new VenuePlacePickedMessage(PlaceWithPicture));
        await SettleAsync(sut);

        Assert.False(sut.HasFormImage);
        _snackMock.Verify(s => s.ShowErrorAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    // [AC] REQ-VEN-39: a failing picture step leaves the address and name fill in place; the fields never wait for the picture.
    public async Task PlacePicked_PictureWorkThrows_FieldsStayFilled()
    {
        var sut = CreateSut();
        _pictureLookupMock.Setup(p => p.ResolveAsync(It.IsAny<VenuePictureSource>(), It.IsAny<CancellationToken>()))
                          .ThrowsAsync(new InvalidOperationException("stubbed picture failure"));
        var place = new VenuePlace(-23.5613, -46.6565,
            new VenueAddress("Rua Augusta, 100", null, "São Paulo", "SP", "01304-000", "Brasil"),
            null, "Rua Augusta, 100, São Paulo", WikidataSource);

        _messenger.Send(new VenuePlacePickedMessage(place));
        await SettleAsync(sut);

        Assert.Equal("Rua Augusta, 100", sut.AddressLine1);
        Assert.Equal("São Paulo", sut.City);
        Assert.Equal(-23.5613, sut.Latitude);
        Assert.False(sut.HasFormImage);
        _snackMock.Verify(s => s.ShowErrorAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    // [AC] REQ-VEN-38: a removal the user makes while the picture is fetched is not overwritten by the late picture.
    public async Task PlacePicked_UserRemovesWhileFetching_LatePictureDoesNotOverwrite()
    {
        var sut = CreateSut();
        var importResult = new TaskCompletionSource<(bool success, string message, string? reference)>();
        StubPictureWork();
        _imageStoreMock.Setup(i => i.ImportAsync(It.IsAny<Stream>(), ImageOwnerKind.Venue, It.IsAny<CancellationToken>()))
                       .Returns(importResult.Task);

        _messenger.Send(new VenuePlacePickedMessage(PlaceWithPicture));
        sut.RemoveImageCommand.Execute(null);
        importResult.SetResult((true, string.Empty, FetchedReference));
        await SettleAsync(sut);

        Assert.False(sut.HasFormImage);
        Assert.Null(sut.FormImageReference);
    }
}
