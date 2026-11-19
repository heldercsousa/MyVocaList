using MyVocaList.Contracts.DTOs.Venues;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Unit.Services;

public class VenueServiceTests
{
    private readonly Mock<IVenueRepository> _repoMock = new();
    private readonly Mock<ILogger<VenueService>> _loggerMock = new();

    // The service now takes IUnitOfWork; PassthroughUnitOfWork runs each wrapped body inline
    // against this same mock, so every existing assertion keeps its original meaning
    // (`plan.md` Task 4.4).
    private VenueService CreateSut() => new(
        _repoMock.Object,
        PassthroughUnitOfWork.Over(_repoMock),
        _loggerMock.Object);

    // ── ValidateNameInput ─────────────────────────────────────────────────────

    [Fact]
    public void ValidateNameInput_EmptyName_ReturnsInvalid()
    {
        var sut = CreateSut();

        var (isValid, message) = sut.ValidateNameInput("  ");

        Assert.False(isValid);
        Assert.NotEmpty(message);
    }

    [Fact]
    public void ValidateNameInput_NameTooShort_ReturnsInvalid()
    {
        var sut = CreateSut();

        var (isValid, message) = sut.ValidateNameInput("A");

        Assert.False(isValid);
        Assert.NotEmpty(message);
    }

    [Fact]
    public void ValidateNameInput_NameTooLong_ReturnsInvalid()
    {
        var sut = CreateSut();
        var name = new string('x', 61); // exceeds 60-char limit

        var (isValid, message) = sut.ValidateNameInput(name);

        Assert.False(isValid);
        Assert.NotEmpty(message);
    }

    [Fact]
    public void ValidateNameInput_ValidName_ReturnsValid()
    {
        var sut = CreateSut();

        var (isValid, message) = sut.ValidateNameInput("Jazz Club");

        Assert.True(isValid);
        Assert.Empty(message);
    }

    // ── GetCharacterCounterInfo ───────────────────────────────────────────────

    [Fact]
    // [AC] Counter threshold alignment (Form Validation Standard): the counter reports an error
    // only when ValidateNameInput would reject the same length. 30 chars is valid → no error.
    public void GetCharacterCounterInfo_AtMaxLength30_IsNotError()
    {
        var sut = CreateSut();

        var (_, _, isError) = sut.GetCharacterCounterInfo(30);

        Assert.False(isError);
    }

    [Fact]
    // [AC] Counter threshold alignment: 61 chars is rejected by the validator → counter error.
    public void GetCharacterCounterInfo_OverMaxLength_IsError()
    {
        var sut = CreateSut();

        var (_, _, isError) = sut.GetCharacterCounterInfo(61);

        Assert.True(isError);
    }

    // ── CreateVenueAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task CreateVenueAsync_NameTooLong_ReturnsFalse()
    {
        var sut = CreateSut();
        var name = new string('x', 61);

        var (success, message) = await sut.CreateVenueAsync(name);

        Assert.False(success);
        Assert.NotEmpty(message);
        _repoMock.Verify(r => r.AddAsync(It.IsAny<Venue>()), Times.Never);
    }

    [Fact]
    // [AC] REQ-VEN-44: a create with an empty address is refused when another venue has the same name and no address.
    public async Task CreateVenueAsync_DuplicateName_ReturnsFalse()
    {
        _repoMock.Setup(r => r.NameAndAddressExistsAsync("Existing Venue", VenueAddress.Empty, null, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);
        var sut = CreateSut();

        var (success, message) = await sut.CreateVenueAsync("Existing Venue");

        Assert.False(success);
        Assert.Equal("There is another venue registered with this name", message);
        _repoMock.Verify(r => r.AddAsync(It.IsAny<Venue>()), Times.Never);
    }

    [Fact]
    // [AC] REQ-VEN-45: a create by name checks the pair with the empty address the venue will have.
    public async Task CreateVenueAsync_ValidName_ReturnsSuccess()
    {
        _repoMock.Setup(r => r.NameAndAddressExistsAsync(It.IsAny<string>(), VenueAddress.Empty, null, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);
        _repoMock.Setup(r => r.AddAsync(It.IsAny<Venue>()))
                 .Returns(Task.CompletedTask);
        var sut = CreateSut();

        var (success, message) = await sut.CreateVenueAsync("New Venue");

        Assert.True(success);
        Assert.NotEmpty(message);
        _repoMock.Verify(r => r.AddAsync(It.IsAny<Venue>()), Times.Once);
    }

    // ── UpdateVenueAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateVenueAsync_VenueNotFound_ReturnsFalse()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
                 .ReturnsAsync((Venue?)null);
        var sut = CreateSut();

        var (success, message) = await sut.UpdateVenueAsync(1, "New Name");

        Assert.False(success);
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Venue>()), Times.Never);
    }

    [Fact]
    // [AC] REQ-VEN-45: a rename to a name another venue already has at the stored address (empty here) is refused on Name.
    public async Task UpdateVenueAsync_DuplicateName_ReturnsFalse()
    {
        _repoMock.Setup(r => r.GetByIdAsync(1))
                 .ReturnsAsync(new Venue { Id = 1, Name = "Original" });
        _repoMock.Setup(r => r.NameAndAddressExistsAsync("Duplicate", VenueAddress.Empty, 1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);
        var sut = CreateSut();

        var (success, message) = await sut.UpdateVenueAsync(1, "Duplicate");

        Assert.False(success);
        Assert.Equal("There is another venue registered with this name", message);
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Venue>()), Times.Never);
    }

    [Fact]
    // [AC] REQ-VEN-45: a rename is checked against the stored address, so a pair match at that address is refused on Name.
    public async Task UpdateVenueAsync_NameAndStoredAddressPairExists_RefusedWithAddressMessage()
    {
        var stored = new Venue
        {
            Id = 1, Name = "Original", AddressLine1 = "Rua Augusta, 100", City = "São Paulo", Region = "SP",
            PostalCode = "01304-001", Country = "Brasil",
        };
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(stored);
        _repoMock.Setup(r => r.NameAndAddressExistsAsync(
                "Duplicate",
                It.Is<VenueAddress>(a => a.Line1 == "Rua Augusta, 100" && a.Line2 == null && a.City == "São Paulo"
                                         && a.Region == "SP" && a.PostalCode == "01304-001" && a.Country == "Brasil"),
                1,
                It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);
        var sut = CreateSut();

        var (success, message) = await sut.UpdateVenueAsync(1, "Duplicate");

        Assert.False(success);
        Assert.Equal("There is another venue registered with this name at this address", message);
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Venue>()), Times.Never);
    }

    [Fact]
    // [AC] REQ-VEN-42: a rename to a name that is free at the stored address is saved.
    public async Task UpdateVenueAsync_ValidUpdate_ReturnsSuccess()
    {
        var venue = new Venue { Id = 1, Name = "Original" };
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(venue);
        _repoMock.Setup(r => r.NameAndAddressExistsAsync("Updated", VenueAddress.Empty, 1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Venue>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        var (success, message) = await sut.UpdateVenueAsync(1, "Updated");

        Assert.True(success);
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Venue>()), Times.Once);
    }

    // ── DeleteVenuesAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteVenuesAsync_EmptyList_ReturnsFalse()
    {
        var sut = CreateSut();

        var (success, message) = await sut.DeleteVenuesAsync([]);

        Assert.False(success);
    }

    [Fact]
    public async Task DeleteVenuesAsync_VenueWithEvents_ReturnsFalseAndDoesNotDelete()
    {
        var venue = new Venue { Id = 1, Name = "Jazz Club" };
        _repoMock.Setup(r => r.GetByIdsWithHasEventsAsync(It.IsAny<IEnumerable<int>>()))
                 .ReturnsAsync([(venue, 2)]); // has 2 events
        var sut = CreateSut();

        var (success, message) = await sut.DeleteVenuesAsync([1]);

        Assert.False(success);
        _repoMock.Verify(r => r.DeleteRangeAsync(It.IsAny<IEnumerable<Venue>>()), Times.Never);
    }

    [Fact]
    public async Task DeleteVenuesAsync_VenueWithNoEvents_ReturnsSuccess()
    {
        var venue = new Venue { Id = 1, Name = "Jazz Club" };
        _repoMock.Setup(r => r.GetByIdsWithHasEventsAsync(It.IsAny<IEnumerable<int>>()))
                 .ReturnsAsync([(venue, 0)]);
        _repoMock.Setup(r => r.DeleteRangeAsync(It.IsAny<IEnumerable<Venue>>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        var (success, message) = await sut.DeleteVenuesAsync([1]);

        Assert.True(success);
        _repoMock.Verify(r => r.DeleteRangeAsync(It.IsAny<IEnumerable<Venue>>()), Times.Once);
    }

    // ── GetPagedVenuesForListAsync ──────────────────────────────────────────────

    [Fact]
    // [AC] REQ-TRIM-03: search query with extra whitespace reaches the repository normalized
    public async Task GetPagedVenuesForListAsync_QueryWithExtraWhitespace_NormalizesBeforeRepositoryCall()
    {
        string capturedQuery = null;
        _repoMock
            .Setup(r => r.GetPagedWithEventInfoAsync(1, 10, It.IsAny<string>()))
            .Callback<int, int, string>((_, _, query) => capturedQuery = query)
            .ReturnsAsync((new List<(Venue venue, int eventCount)>(), 0));
        var sut = CreateSut();

        await sut.GetPagedVenuesForListAsync(1, 10, " bar  x ");

        Assert.Equal("bar x", capturedQuery);
    }
}
