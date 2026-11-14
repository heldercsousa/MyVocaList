using MyVocaList.Contracts.DTOs.Venues;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Unit.Services;

public class VenueServiceSaveTests
{
    private const string ValidLocalReference = "local:venues/0123456789abcdef0123456789abcdef";
    private const string OtherLocalReference = "local:venues/fedcba9876543210fedcba9876543210";

    private readonly Mock<IVenueRepository> _repoMock = new();
    private readonly Mock<ILogger<VenueService>> _loggerMock = new();
    private Venue? _added;
    private Venue? _updated;

    public VenueServiceSaveTests()
    {
        _repoMock.Setup(r => r.GetByNameAsync(It.IsAny<string>())).ReturnsAsync((Venue?)null);
        _repoMock.Setup(r => r.AddressExistsAsync(It.IsAny<VenueAddress>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);
        _repoMock.Setup(r => r.AddAsync(It.IsAny<Venue>()))
                 .Callback<Venue>(v => _added = v)
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Venue>()))
                 .Callback<Venue>(v => _updated = v)
                 .Returns(Task.CompletedTask);
    }

    // The service takes IUnitOfWork; PassthroughUnitOfWork runs each wrapped body inline against this mock.
    private VenueService CreateSut() => new(
        _repoMock.Object,
        PassthroughUnitOfWork.Over(_repoMock),
        _loggerMock.Object);

    private static VenueAddress Addr(
        string? line1 = null, string? line2 = null, string? city = null,
        string? region = null, string? postalCode = null, string? country = null)
        => new(line1, line2, city, region, postalCode, country);

    private static VenueSaveRequest AddRequest(
        string name = "New Venue", VenueAddress? address = null,
        double? latitude = null, double? longitude = null, string? image = null)
        => new(null, name, address ?? VenueAddress.Empty, latitude, longitude, image, false);

    private static VenueSaveRequest EditRequest(
        string name = "Jazz Club", VenueAddress? address = null, double? latitude = null,
        double? longitude = null, string? image = null, bool imageChanged = false, int id = 1)
        => new(id, name, address ?? VenueAddress.Empty, latitude, longitude, image, imageChanged);

    private static Venue StoredVenue(int id = 1, string name = "Jazz Club") => new()
    {
        Id = id,
        Name = name,
        ImageUrl = OtherLocalReference,
        AddressLine1 = "Rua Augusta, 100",
        AddressLine2 = "Loja 2",
        City = "São Paulo",
        Region = "SP",
        PostalCode = "01304-001",
        Country = "Brasil",
        Latitude = -23.55,
        Longitude = -46.65,
    };

    // ── Name limit and counter (REQ-VEN-22) ───────────────────────────────────

    [Fact]
    // [AC] REQ-VEN-22: a name of 60 characters is valid.
    public void ValidateNameInput_Name60Characters_IsValid()
    {
        var sut = CreateSut();

        var (isValid, message) = sut.ValidateNameInput(new string('x', 60));

        Assert.True(isValid);
        Assert.Empty(message);
    }

    [Fact]
    // [AC] REQ-VEN-22: a name of 61 characters is refused with the new maximum in the message.
    public void ValidateNameInput_Name61Characters_IsRefusedWithMaximum60()
    {
        var sut = CreateSut();

        var result = sut.ValidateNameInput(new string('x', 61));

        Assert.Equal((false, "Name is too long. Maximum 60 characters."), result);
    }

    [Fact]
    // [AC] REQ-VEN-22: the character counter is hidden at 50 characters.
    public void ShouldShowCharacterCounter_At50_IsHidden()
    {
        var sut = CreateSut();

        Assert.False(sut.ShouldShowCharacterCounter(50));
    }

    [Fact]
    // [AC] REQ-VEN-22: the character counter appears above 50 characters.
    public void ShouldShowCharacterCounter_At51_IsShown()
    {
        var sut = CreateSut();

        Assert.True(sut.ShouldShowCharacterCounter(51));
    }

    [Fact]
    // [AC] REQ-VEN-22: no warning colour at 55 characters.
    public void GetCharacterCounterInfo_At55_IsNotWarning()
    {
        var sut = CreateSut();

        var (_, isWarning, _) = sut.GetCharacterCounterInfo(55);

        Assert.False(isWarning);
    }

    [Fact]
    // [AC] REQ-VEN-22: the warning colour starts at 56 characters.
    public void GetCharacterCounterInfo_At56_IsWarning()
    {
        var sut = CreateSut();

        var (_, isWarning, _) = sut.GetCharacterCounterInfo(56);

        Assert.True(isWarning);
    }

    [Fact]
    // [AC] REQ-VEN-22: a 60-character name shows the warning colour and no error colour.
    public void GetCharacterCounterInfo_At60_IsWarningButNotError()
    {
        var sut = CreateSut();

        var (_, isWarning, isError) = sut.GetCharacterCounterInfo(60);

        Assert.True(isWarning);
        Assert.False(isError);
    }

    // ── SaveVenueAsync: step order and field routing ─────────────────────────

    [Fact]
    // [AC] REQ-VEN-22: a 61-character name is refused on Name before the repository is queried.
    public async Task SaveVenueAsync_Name61Characters_RefusedOnNameWithoutQueryingRepository()
    {
        var sut = CreateSut();

        var result = await sut.SaveVenueAsync(AddRequest(name: new string('x', 61)));

        Assert.Equal((false, "Name is too long. Maximum 60 characters.", VenueField.Name), result);
        _repoMock.Verify(r => r.GetByNameAsync(It.IsAny<string>()), Times.Never);
        _repoMock.Verify(r => r.AddAsync(It.IsAny<Venue>()), Times.Never);
    }

    [Fact]
    // [AC] REQ-VEN-22: a 60-character name is accepted and saved.
    public async Task SaveVenueAsync_Name60Characters_IsSaved()
    {
        var sut = CreateSut();

        var result = await sut.SaveVenueAsync(AddRequest(name: new string('x', 60)));

        Assert.True(result.success);
        _repoMock.Verify(r => r.AddAsync(It.IsAny<Venue>()), Times.Once);
    }

    [Fact]
    // [AC] REQ-VEN-14: an address with only a country is refused on Street address.
    public async Task SaveVenueAsync_CountryOnly_RefusedOnStreetAddress()
    {
        var sut = CreateSut();

        var result = await sut.SaveVenueAsync(AddRequest(address: Addr(country: "Brasil")));

        Assert.Equal((false, "Street address is required.", VenueField.AddressLine1), result);
        _repoMock.Verify(r => r.AddAsync(It.IsAny<Venue>()), Times.Never);
    }

    [Fact]
    // [AC] REQ-VEN-14: a street without a city is refused on City.
    public async Task SaveVenueAsync_StreetWithoutCity_RefusedOnCity()
    {
        var sut = CreateSut();

        var result = await sut.SaveVenueAsync(AddRequest(address: Addr(line1: "Rua Augusta, 100")));

        Assert.Equal((false, "City is required.", VenueField.City), result);
        _repoMock.Verify(r => r.AddAsync(It.IsAny<Venue>()), Times.Never);
    }

    [Fact]
    // [AC] REQ-VEN-15: a postal code of 21 characters is refused on Postal code.
    public async Task SaveVenueAsync_PostalCode21Characters_RefusedOnPostalCode()
    {
        var sut = CreateSut();
        var address = Addr(line1: "Rua Augusta, 100", city: "São Paulo", postalCode: new string('1', 21));

        var result = await sut.SaveVenueAsync(AddRequest(address: address));

        Assert.Equal((false, "Postal code is too long. Maximum 20 characters.", VenueField.PostalCode), result);
        _repoMock.Verify(r => r.AddAsync(It.IsAny<Venue>()), Times.Never);
    }

    [Fact]
    // [AC] REQ-VEN-14: an invalid address is reported before a duplicate name.
    public async Task SaveVenueAsync_InvalidAddressAndDuplicateName_ReportsAddressFirst()
    {
        _repoMock.Setup(r => r.GetByNameAsync(It.IsAny<string>()))
                 .ReturnsAsync(new Venue { Id = 2, Name = "Jazz Club" });
        var sut = CreateSut();

        var result = await sut.SaveVenueAsync(AddRequest(name: "Jazz Club", address: Addr(country: "Brasil")));

        Assert.Equal((false, "Street address is required.", VenueField.AddressLine1), result);
    }

    [Fact]
    // [AC] REQ-VEN-19: a duplicate name is reported before a duplicate address.
    public async Task SaveVenueAsync_DuplicateNameAndDuplicateAddress_ReportsNameFirst()
    {
        _repoMock.Setup(r => r.GetByNameAsync(It.IsAny<string>()))
                 .ReturnsAsync(new Venue { Id = 2, Name = "Jazz Club" });
        _repoMock.Setup(r => r.AddressExistsAsync(It.IsAny<VenueAddress>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);
        var sut = CreateSut();
        var address = Addr(line1: "Rua Augusta, 100", city: "São Paulo");

        var result = await sut.SaveVenueAsync(AddRequest(name: "Jazz Club", address: address));

        Assert.Equal((false, "There is another venue registered with this name", VenueField.Name), result);
        _repoMock.Verify(r => r.AddressExistsAsync(It.IsAny<VenueAddress>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── SaveVenueAsync: add mode ─────────────────────────────────────────────

    [Fact]
    // [AC] REQ-VEN-13: a venue saves with an empty address.
    // [AC] REQ-VEN-21: an empty address never runs the duplicate-address check.
    public async Task SaveVenueAsync_EmptyAddress_SavesWithoutAddressDuplicateCheck()
    {
        var sut = CreateSut();

        var result = await sut.SaveVenueAsync(AddRequest(address: VenueAddress.Empty));

        Assert.True(result.success);
        Assert.Equal(VenueField.None, result.field);
        Assert.NotNull(_added);
        _repoMock.Verify(r => r.AddressExistsAsync(It.IsAny<VenueAddress>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    // [AC] REQ-VEN-16: address fields are stored trimmed and blank fields as null.
    public async Task SaveVenueAsync_AddMode_AddressStoredTrimmedWithBlanksAsNull()
    {
        var sut = CreateSut();
        var address = Addr(line1: "  Rua Augusta, 100 ", line2: "   ", city: "São Paulo",
                           region: " SP ", postalCode: "", country: "Brasil ");

        await sut.SaveVenueAsync(AddRequest(address: address));

        Assert.NotNull(_added);
        Assert.Equal("Rua Augusta, 100", _added!.AddressLine1);
        Assert.Null(_added.AddressLine2);
        Assert.Equal("São Paulo", _added.City);
        Assert.Equal("SP", _added.Region);
        Assert.Null(_added.PostalCode);
        Assert.Equal("Brasil", _added.Country);
    }

    [Fact]
    // [AC] REQ-VEN-10: a valid local picture reference is kept on add.
    public async Task SaveVenueAsync_AddMode_ValidImageReference_IsKept()
    {
        var sut = CreateSut();

        await sut.SaveVenueAsync(AddRequest(image: ValidLocalReference));

        Assert.NotNull(_added);
        Assert.Equal(ValidLocalReference, _added!.ImageUrl);
    }

    [Fact]
    // [AC] REQ-VEN-03: a malformed local reference is stored as null.
    public async Task SaveVenueAsync_AddMode_MalformedImageReference_IsStoredAsNull()
    {
        var sut = CreateSut();

        await sut.SaveVenueAsync(AddRequest(image: "local:venues/../../x"));

        Assert.NotNull(_added);
        Assert.Null(_added!.ImageUrl);
    }

    [Fact]
    // [AC] REQ-VEN-33: a latitude without a longitude is stored as both null.
    public async Task SaveVenueAsync_AddMode_OneCoordinateMissing_BothStoredAsNull()
    {
        var sut = CreateSut();

        await sut.SaveVenueAsync(AddRequest(latitude: 45.0, longitude: null));

        Assert.NotNull(_added);
        Assert.Null(_added!.Latitude);
        Assert.Null(_added.Longitude);
    }

    [Fact]
    // [AC] REQ-VEN-33: a latitude outside -90..90 is stored as both null.
    public async Task SaveVenueAsync_AddMode_LatitudeOutOfRange_BothStoredAsNull()
    {
        var sut = CreateSut();

        await sut.SaveVenueAsync(AddRequest(latitude: 91, longitude: 10));

        Assert.NotNull(_added);
        Assert.Null(_added!.Latitude);
        Assert.Null(_added.Longitude);
    }

    [Fact]
    // [AC] REQ-VEN-33: valid coordinates are stored on add.
    public async Task SaveVenueAsync_AddMode_ValidCoordinates_AreStored()
    {
        var sut = CreateSut();

        await sut.SaveVenueAsync(AddRequest(latitude: -23.55, longitude: -46.65));

        Assert.NotNull(_added);
        Assert.Equal(-23.55, _added!.Latitude);
        Assert.Equal(-46.65, _added.Longitude);
    }

    [Fact]
    // [AC] REQ-VEN-19: a duplicate name on add is refused on Name and nothing is stored.
    public async Task SaveVenueAsync_AddMode_DuplicateName_RefusedOnNameAndNothingStored()
    {
        _repoMock.Setup(r => r.GetByNameAsync(It.IsAny<string>()))
                 .ReturnsAsync(new Venue { Id = 5, Name = "Jazz Club" });
        var sut = CreateSut();

        var result = await sut.SaveVenueAsync(AddRequest(name: "Jazz Club"));

        Assert.Equal((false, "There is another venue registered with this name", VenueField.Name), result);
        _repoMock.Verify(r => r.AddAsync(It.IsAny<Venue>()), Times.Never);
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Venue>()), Times.Never);
    }

    [Fact]
    // [AC] REQ-VEN-20: a duplicate address on add is refused on Street address, checked without excluding any venue.
    public async Task SaveVenueAsync_AddMode_DuplicateAddress_RefusedOnStreetAddressAndNothingStored()
    {
        _repoMock.Setup(r => r.AddressExistsAsync(It.IsAny<VenueAddress>(), null, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);
        var sut = CreateSut();
        var address = Addr(line1: "Rua Augusta, 100", city: "São Paulo", region: "SP", postalCode: "01304-001", country: "Brasil");

        var result = await sut.SaveVenueAsync(AddRequest(address: address));

        Assert.Equal((false, "There is another venue registered at this address", VenueField.AddressLine1), result);
        _repoMock.Verify(r => r.AddAsync(It.IsAny<Venue>()), Times.Never);
    }

    [Fact]
    // [AC] REQ-VEN-20: the duplicate-address check receives the normalised address.
    public async Task SaveVenueAsync_AddMode_AddressCheckReceivesNormalisedAddress()
    {
        var sut = CreateSut();
        var address = Addr(line1: "  Rua Augusta, 100  ", city: " São Paulo ");

        await sut.SaveVenueAsync(AddRequest(address: address));

        _repoMock.Verify(r => r.AddressExistsAsync(
            It.Is<VenueAddress>(a => a.Line1 == "Rua Augusta, 100" && a.Line2 == null && a.City == "São Paulo"
                                     && a.Region == null && a.PostalCode == null && a.Country == null),
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    // [AC] REQ-VEN-36: a successful add returns the existing created message.
    public async Task SaveVenueAsync_AddMode_Success_ReturnsCreatedMessage()
    {
        var sut = CreateSut();

        var result = await sut.SaveVenueAsync(AddRequest(name: "New Venue"));

        Assert.Equal((true, "Venue 'New Venue' successfully created!", VenueField.None), result);
    }

    // ── SaveVenueAsync: edit mode ────────────────────────────────────────────

    [Fact]
    // [AC] REQ-VEN-17: a venue missing at save time is refused with "Venue not found".
    public async Task SaveVenueAsync_EditMode_VenueMissing_ReturnsNotFound()
    {
        _repoMock.Setup(r => r.GetByIdAsync(9)).ReturnsAsync((Venue?)null);
        var sut = CreateSut();

        var result = await sut.SaveVenueAsync(EditRequest(id: 9));

        Assert.Equal((false, "Venue not found", VenueField.None), result);
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Venue>()), Times.Never);
    }

    [Fact]
    // [AC] REQ-VEN-19: the venue's own stored name is accepted on edit.
    public async Task SaveVenueAsync_EditMode_OwnName_IsAccepted()
    {
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(StoredVenue(1, "Jazz Club"));
        _repoMock.Setup(r => r.GetByNameAsync("Jazz Club")).ReturnsAsync(StoredVenue(1, "Jazz Club"));
        var sut = CreateSut();

        var result = await sut.SaveVenueAsync(EditRequest(name: "Jazz Club", id: 1));

        Assert.True(result.success);
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Venue>()), Times.Once);
    }

    [Fact]
    // [AC] REQ-VEN-19: another venue's name is refused on edit.
    public async Task SaveVenueAsync_EditMode_OtherVenuesName_RefusedOnName()
    {
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(StoredVenue(1, "Jazz Club"));
        _repoMock.Setup(r => r.GetByNameAsync("Blues Bar")).ReturnsAsync(new Venue { Id = 2, Name = "Blues Bar" });
        var sut = CreateSut();

        var result = await sut.SaveVenueAsync(EditRequest(name: "Blues Bar", id: 1));

        Assert.Equal((false, "There is another venue registered with this name", VenueField.Name), result);
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Venue>()), Times.Never);
    }

    [Fact]
    // [AC] REQ-VEN-20: the duplicate-address check excludes the venue being edited.
    public async Task SaveVenueAsync_EditMode_AddressCheckExcludesOwnId()
    {
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(StoredVenue(1));
        var sut = CreateSut();
        var address = Addr(line1: "Rua Augusta, 100", city: "São Paulo");

        await sut.SaveVenueAsync(EditRequest(address: address, id: 1));

        _repoMock.Verify(r => r.AddressExistsAsync(It.IsAny<VenueAddress>(), 1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    // [AC] REQ-VEN-20: another venue's address is refused on edit, and nothing is updated.
    public async Task SaveVenueAsync_EditMode_DuplicateAddress_RefusedOnStreetAddress()
    {
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(StoredVenue(1));
        _repoMock.Setup(r => r.AddressExistsAsync(It.IsAny<VenueAddress>(), 1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);
        var sut = CreateSut();
        var address = Addr(line1: "Rua Augusta, 100", city: "São Paulo");

        var result = await sut.SaveVenueAsync(EditRequest(address: address, id: 1));

        Assert.Equal((false, "There is another venue registered at this address", VenueField.AddressLine1), result);
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Venue>()), Times.Never);
        _repoMock.Verify(r => r.AddAsync(It.IsAny<Venue>()), Times.Never);
    }

    [Fact]
    // [AC] REQ-VEN-10: a picture reference is kept when the picture was not changed on edit.
    public async Task SaveVenueAsync_EditMode_ImageNotChanged_KeepsStoredReference()
    {
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(StoredVenue(1));
        var sut = CreateSut();

        await sut.SaveVenueAsync(EditRequest(image: ValidLocalReference, imageChanged: false, id: 1));

        Assert.NotNull(_updated);
        Assert.Equal(OtherLocalReference, _updated!.ImageUrl);
    }

    [Fact]
    // [AC] REQ-VEN-10: a picked picture replaces the stored reference on edit.
    public async Task SaveVenueAsync_EditMode_ImageChangedWithReference_ReplacesStoredReference()
    {
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(StoredVenue(1));
        var sut = CreateSut();

        await sut.SaveVenueAsync(EditRequest(image: ValidLocalReference, imageChanged: true, id: 1));

        Assert.NotNull(_updated);
        Assert.Equal(ValidLocalReference, _updated!.ImageUrl);
    }

    [Fact]
    // [AC] REQ-VEN-10: a removed picture clears the stored reference on edit.
    public async Task SaveVenueAsync_EditMode_ImageChangedWithNull_ClearsStoredReference()
    {
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(StoredVenue(1));
        var sut = CreateSut();

        await sut.SaveVenueAsync(EditRequest(image: null, imageChanged: true, id: 1));

        Assert.NotNull(_updated);
        Assert.Null(_updated!.ImageUrl);
    }

    [Fact]
    // [AC] REQ-VEN-33: coordinates are saved in edit mode.
    public async Task SaveVenueAsync_EditMode_CoordinatesAreSaved()
    {
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(StoredVenue(1));
        var sut = CreateSut();

        await sut.SaveVenueAsync(EditRequest(latitude: 41.9, longitude: 12.5, id: 1));

        Assert.NotNull(_updated);
        Assert.Equal(41.9, _updated!.Latitude);
        Assert.Equal(12.5, _updated.Longitude);
    }

    [Fact]
    // [AC] REQ-VEN-16: the edited address replaces the stored six fields, trimmed with blanks as null.
    public async Task SaveVenueAsync_EditMode_AddressReplacedTrimmedWithBlanksAsNull()
    {
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(StoredVenue(1));
        var sut = CreateSut();
        var address = Addr(line1: " Rua Nova, 5 ", line2: " ", city: "Rio", region: null, postalCode: " 20000 ", country: "Brasil");

        await sut.SaveVenueAsync(EditRequest(address: address, id: 1));

        Assert.NotNull(_updated);
        Assert.Equal("Rua Nova, 5", _updated!.AddressLine1);
        Assert.Null(_updated.AddressLine2);
        Assert.Equal("Rio", _updated.City);
        Assert.Null(_updated.Region);
        Assert.Equal("20000", _updated.PostalCode);
        Assert.Equal("Brasil", _updated.Country);
    }

    [Fact]
    // [AC] REQ-VEN-11: a failed save calls neither AddAsync nor UpdateAsync.
    public async Task SaveVenueAsync_EditMode_FailedSave_WritesNothing()
    {
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(StoredVenue(1));
        _repoMock.Setup(r => r.GetByNameAsync("Blues Bar")).ReturnsAsync(new Venue { Id = 2, Name = "Blues Bar" });
        var sut = CreateSut();

        var result = await sut.SaveVenueAsync(EditRequest(name: "Blues Bar", imageChanged: true, image: ValidLocalReference, id: 1));

        Assert.False(result.success);
        _repoMock.Verify(r => r.AddAsync(It.IsAny<Venue>()), Times.Never);
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Venue>()), Times.Never);
    }

    [Fact]
    // [AC] REQ-VEN-36: a successful edit returns the existing updated message, whatever fields changed.
    public async Task SaveVenueAsync_EditMode_Success_ReturnsUpdatedMessage()
    {
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(StoredVenue(1));
        var sut = CreateSut();

        var result = await sut.SaveVenueAsync(EditRequest(name: "Renamed", latitude: 1.5, longitude: 2.5, id: 1));

        Assert.Equal((true, "Venue name successfully updated to 'Renamed'!", VenueField.None), result);
    }

    // ── GetVenueForEditAsync (REQ-VEN-17) ─────────────────────────────────────

    [Fact]
    // [AC] REQ-VEN-17: an existing venue is loaded for the edit form with every field mapped.
    public async Task GetVenueForEditAsync_ExistingVenue_MapsAllFields()
    {
        _repoMock.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(StoredVenue(3, "Jazz Club"));
        var sut = CreateSut();

        var dto = await sut.GetVenueForEditAsync(3);

        Assert.NotNull(dto);
        Assert.Equal(3, dto!.Id);
        Assert.Equal("Jazz Club", dto.Name);
        Assert.Equal(OtherLocalReference, dto.ImageUrl);
        Assert.Equal(Addr("Rua Augusta, 100", "Loja 2", "São Paulo", "SP", "01304-001", "Brasil"), dto.Address);
        Assert.Equal(-23.55, dto.Latitude);
        Assert.Equal(-46.65, dto.Longitude);
    }

    [Fact]
    // [AC] REQ-VEN-17: a missing venue gives null, so the form can show "Venue not found".
    public async Task GetVenueForEditAsync_MissingVenue_ReturnsNull()
    {
        _repoMock.Setup(r => r.GetByIdAsync(404)).ReturnsAsync((Venue?)null);
        var sut = CreateSut();

        var dto = await sut.GetVenueForEditAsync(404);

        Assert.Null(dto);
    }

    // ── ValidateAddressInput (REQ-VEN-13, 14) ────────────────────────────────

    [Fact]
    // [AC] REQ-VEN-13: white-space-only fields count as empty, so the address is valid.
    public void ValidateAddressInput_WhiteSpaceOnlyFields_AreValid()
    {
        var sut = CreateSut();

        var result = sut.ValidateAddressInput(Addr(line1: "  ", line2: " ", city: "", region: null, postalCode: " ", country: "\t"));

        Assert.Equal((true, "", VenueField.None), result);
    }

    [Fact]
    // [AC] REQ-VEN-14: the address is normalised before validation, so a blank street with a city is refused on Street address.
    public void ValidateAddressInput_BlankStreetWithCity_IsRefusedOnStreetAddress()
    {
        var sut = CreateSut();

        var result = sut.ValidateAddressInput(Addr(line1: "   ", city: "São Paulo"));

        Assert.Equal((false, "Street address is required.", VenueField.AddressLine1), result);
    }

    // ── UpdateVenueAsync keeps the other columns (REQ-VEN-10) ────────────────

    [Fact]
    // [AC] REQ-VEN-10: an update by name changes only the name and leaves address, coordinates and picture as stored.
    public async Task UpdateVenueAsync_ByName_LeavesOtherColumnsAsStored()
    {
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(StoredVenue(1, "Jazz Club"));
        var sut = CreateSut();

        var (success, _) = await sut.UpdateVenueAsync(1, "Renamed Club");

        Assert.True(success);
        Assert.NotNull(_updated);
        Assert.Equal("Renamed Club", _updated!.Name);
        Assert.Equal(OtherLocalReference, _updated.ImageUrl);
        Assert.Equal("Rua Augusta, 100", _updated.AddressLine1);
        Assert.Equal("Loja 2", _updated.AddressLine2);
        Assert.Equal("São Paulo", _updated.City);
        Assert.Equal("SP", _updated.Region);
        Assert.Equal("01304-001", _updated.PostalCode);
        Assert.Equal("Brasil", _updated.Country);
        Assert.Equal(-23.55, _updated.Latitude);
        Assert.Equal(-46.65, _updated.Longitude);
    }
}
