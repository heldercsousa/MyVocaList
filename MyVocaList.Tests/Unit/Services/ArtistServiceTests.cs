namespace MyVocaList.Tests.Unit.Services;

public class ArtistServiceTests
{
    private readonly Mock<IArtistRepository> _artistRepoMock = new();
    private readonly Mock<ISongRepository> _songRepoMock = new();
    private readonly Mock<ICatalogRepository> _catalogRepoMock = new();
    private readonly Mock<ILogger<ArtistService>> _loggerMock = new();

    private ArtistService CreateSut() => new(
        _artistRepoMock.Object, _songRepoMock.Object, _catalogRepoMock.Object, _loggerMock.Object);

    // ── ValidateNameInput ─────────────────────────────────────────────────

    [Fact]
    public void ValidateNameInput_EmptyName_ReturnsFalse()
    {
        var sut = CreateSut();
        var (isValid, message) = sut.ValidateNameInput(string.Empty);
        Assert.False(isValid);
        Assert.NotEmpty(message);
    }

    [Fact]
    public void ValidateNameInput_WhitespaceName_ReturnsFalse()
    {
        var sut = CreateSut();
        var (isValid, message) = sut.ValidateNameInput("   ");
        Assert.False(isValid);
        Assert.NotEmpty(message);
    }

    [Fact]
    public void ValidateNameInput_NameTooLong_ReturnsFalse()
    {
        var sut = CreateSut();
        var name = new string('x', 61); // exceeds 60-char limit
        var (isValid, message) = sut.ValidateNameInput(name);
        Assert.False(isValid);
        Assert.NotEmpty(message);
    }

    [Fact]
    public void ValidateNameInput_ValidName_ReturnsTrue()
    {
        var sut = CreateSut();
        var (isValid, message) = sut.ValidateNameInput("The Beatles");
        Assert.True(isValid);
    }

    [Fact]
    public void ValidateNameInput_MaxLength60_ReturnsTrue()
    {
        var sut = CreateSut();
        var name = new string('x', 60);
        var (isValid, _) = sut.ValidateNameInput(name);
        Assert.True(isValid);
    }

    // ── GetCharacterCounterInfo ───────────────────────────────────────────

    [Fact]
    // [AC] Counter threshold alignment (Form Validation Standard): the counter reports an error
    // only when ValidateNameInput would reject the same length. 60 chars is valid → no error.
    public void GetCharacterCounterInfo_AtMaxLength60_IsNotError()
    {
        var sut = CreateSut();

        var (_, _, isError) = sut.GetCharacterCounterInfo(60);

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

    // ── CreateArtistAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateArtistAsync_ValidName_ReturnsSuccessAndEntity()
    {
        _artistRepoMock.Setup(r => r.ExistsByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(false);
        _artistRepoMock.Setup(r => r.AddAsync(It.IsAny<Artist>(), It.IsAny<CancellationToken>()))
                       .Returns(Task.CompletedTask);
        var sut = CreateSut();

        var (success, message, artist) = await sut.CreateArtistAsync("Queen");

        Assert.True(success);
        Assert.NotNull(artist);
        Assert.Equal("Queen", artist.Name);
    }

    [Fact]
    public async Task CreateArtistAsync_NameTooLong_ReturnsFalse()
    {
        var sut = CreateSut();
        var name = new string('x', 61);

        var (success, message, artist) = await sut.CreateArtistAsync(name);

        Assert.False(success);
        Assert.NotEmpty(message);
        Assert.Null(artist);
        _artistRepoMock.Verify(r => r.AddAsync(It.IsAny<Artist>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateArtistAsync_DuplicateName_ReturnsFalse()
    {
        _artistRepoMock.Setup(r => r.ExistsByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(true);
        var sut = CreateSut();

        var (success, message, artist) = await sut.CreateArtistAsync("Nirvana");

        Assert.False(success);
        Assert.NotEmpty(message);
        Assert.Null(artist);
        _artistRepoMock.Verify(r => r.AddAsync(It.IsAny<Artist>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── UpdateArtistAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task UpdateArtistAsync_ValidName_ReturnsSuccess()
    {
        var existing = new Artist { Id = 1, Name = "Old Name" };
        _artistRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(existing);
        _artistRepoMock.Setup(r => r.ExistsByNameAsync(It.IsAny<string>(), 1, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(false);
        _artistRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Artist>(), It.IsAny<CancellationToken>()))
                       .Returns(Task.CompletedTask);
        var sut = CreateSut();

        var (success, message) = await sut.UpdateArtistAsync(1, "New Name");

        Assert.True(success);
    }

    [Fact]
    public async Task UpdateArtistAsync_ArtistNotFound_ReturnsFalse()
    {
        _artistRepoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                       .ReturnsAsync((Artist)null);
        var sut = CreateSut();

        var (success, message) = await sut.UpdateArtistAsync(99, "New Name");

        Assert.False(success);
        Assert.NotEmpty(message);
    }

    [Fact]
    public async Task UpdateArtistAsync_DuplicateNameExcludingSelf_ReturnsFalse()
    {
        var existing = new Artist { Id = 1, Name = "Artist" };
        _artistRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(existing);
        _artistRepoMock.Setup(r => r.ExistsByNameAsync(It.IsAny<string>(), 1, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(true);
        var sut = CreateSut();

        var (success, message) = await sut.UpdateArtistAsync(1, "Existing Other Artist");

        Assert.False(success);
        Assert.NotEmpty(message);
    }

    // ── DeleteArtistsAsync ────────────────────────────────────────────────

    [Fact]
    public async Task DeleteArtistsAsync_ArtistWithSongs_ReturnsFalse()
    {
        var artist = new Artist { Id = 1, Name = "Artist With Songs" };
        _artistRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(artist);
        _catalogRepoMock.Setup(r => r.CountByArtistAsync(1, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(3);
        var sut = CreateSut();

        var (success, message) = await sut.DeleteArtistsAsync([1]);

        Assert.False(success);
        Assert.NotEmpty(message);
        _artistRepoMock.Verify(r => r.DeleteAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteArtistsAsync_ArtistWithNoSongs_ReturnsSuccess()
    {
        var artist = new Artist { Id = 1, Name = "Solo Artist" };
        _artistRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(artist);
        _catalogRepoMock.Setup(r => r.CountByArtistAsync(1, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(0);
        _artistRepoMock.Setup(r => r.DeleteAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
                       .Returns(Task.CompletedTask);
        var sut = CreateSut();

        var (success, message) = await sut.DeleteArtistsAsync([1]);

        Assert.True(success);
    }
}
