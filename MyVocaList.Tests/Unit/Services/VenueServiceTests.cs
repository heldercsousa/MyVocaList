using MyVocaList.Infra.Repository;

namespace MyVocaList.Tests.Unit.Services;

public class VenueServiceTests
{
    private readonly Mock<IVenueRepository> _repoMock = new();
    private readonly Mock<IEventRepository> _eventRepoMock = new();
    private readonly Mock<ILogger<VenueService>> _loggerMock = new();

    private VenueService CreateSut() => new(_repoMock.Object, _eventRepoMock.Object, _loggerMock.Object);

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
        var name = new string('x', 31); // exceeds 30-char limit

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
    // [AC] Counter threshold alignment: 31 chars is rejected by the validator → counter error.
    public void GetCharacterCounterInfo_OverMaxLength_IsError()
    {
        var sut = CreateSut();

        var (_, _, isError) = sut.GetCharacterCounterInfo(31);

        Assert.True(isError);
    }

    // ── CreateVenueAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task CreateVenueAsync_NameTooLong_ReturnsFalse()
    {
        var sut = CreateSut();
        var name = new string('x', 31);

        var (success, message) = await sut.CreateVenueAsync(name);

        Assert.False(success);
        Assert.NotEmpty(message);
        _repoMock.Verify(r => r.AddAsync(It.IsAny<Venue>()), Times.Never);
    }

    [Fact]
    public async Task CreateVenueAsync_DuplicateName_ReturnsFalse()
    {
        _repoMock.Setup(r => r.GetByNameAsync(It.IsAny<string>()))
                 .ReturnsAsync(new Venue { Id = 1, Name = "Existing Venue" });
        var sut = CreateSut();

        var (success, message) = await sut.CreateVenueAsync("Existing Venue");

        Assert.False(success);
        Assert.NotEmpty(message);
        _repoMock.Verify(r => r.AddAsync(It.IsAny<Venue>()), Times.Never);
    }

    [Fact]
    public async Task CreateVenueAsync_ValidName_ReturnsSuccess()
    {
        _repoMock.Setup(r => r.GetByNameAsync(It.IsAny<string>()))
                 .ReturnsAsync((Venue?)null);
        _repoMock.Setup(r => r.AddAsync(It.IsAny<Venue>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync())
                 .Returns(Task.CompletedTask);
        var sut = CreateSut();

        var (success, message) = await sut.CreateVenueAsync("New Venue");

        Assert.True(success);
        Assert.NotEmpty(message);
        _repoMock.Verify(r => r.AddAsync(It.IsAny<Venue>()), Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
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
    public async Task UpdateVenueAsync_DuplicateName_ReturnsFalse()
    {
        _repoMock.Setup(r => r.GetByIdAsync(1))
                 .ReturnsAsync(new Venue { Id = 1, Name = "Original" });
        _repoMock.Setup(r => r.GetByNameAsync("Duplicate"))
                 .ReturnsAsync(new Venue { Id = 2, Name = "Duplicate" }); // different id → duplicate
        var sut = CreateSut();

        var (success, message) = await sut.UpdateVenueAsync(1, "Duplicate");

        Assert.False(success);
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Venue>()), Times.Never);
    }

    [Fact]
    public async Task UpdateVenueAsync_ValidUpdate_ReturnsSuccess()
    {
        var venue = new Venue { Id = 1, Name = "Original" };
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(venue);
        _repoMock.Setup(r => r.GetByNameAsync("Updated")).ReturnsAsync((Venue?)null);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Venue>())).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
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
        _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        var sut = CreateSut();

        var (success, message) = await sut.DeleteVenuesAsync([1]);

        Assert.True(success);
        _repoMock.Verify(r => r.DeleteRangeAsync(It.IsAny<IEnumerable<Venue>>()), Times.Once);
    }
}
