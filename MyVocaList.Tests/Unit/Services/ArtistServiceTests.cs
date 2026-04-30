namespace MyVocaList.Tests.Unit.Services;

public class ArtistServiceTests
{
    private readonly Mock<IArtistRepository> _artistRepoMock = new();
    private readonly Mock<ISongRepository> _songRepoMock = new();
    private readonly Mock<ILogger<ArtistService>> _loggerMock = new();

    private ArtistService CreateSut() => new(_artistRepoMock.Object, _songRepoMock.Object, _loggerMock.Object);

    // ── ValidateNameInput ─────────────────────────────────────────────────

    [Fact]
    public void ValidateNameInput_EmptyName_ReturnsInvalid()
    {
        var sut = CreateSut();
        var (isValid, message) = sut.ValidateNameInput(string.Empty);
        Assert.False(isValid);
        Assert.NotEmpty(message);
    }

    [Fact]
    public void ValidateNameInput_WhitespaceName_ReturnsInvalid()
    {
        var sut = CreateSut();
        var (isValid, message) = sut.ValidateNameInput("   ");
        Assert.False(isValid);
        Assert.NotEmpty(message);
    }

    [Fact]
    public void ValidateNameInput_NameTooLong_ReturnsInvalid()
    {
        var sut = CreateSut();
        var (isValid, message) = sut.ValidateNameInput(new string('x', 101));
        Assert.False(isValid);
        Assert.NotEmpty(message);
    }

    [Fact]
    public void ValidateNameInput_ValidName_ReturnsValid()
    {
        var sut = CreateSut();
        var (isValid, _) = sut.ValidateNameInput("The Beatles");
        Assert.True(isValid);
    }

    // ── CreateArtistAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateArtistAsync_DuplicateName_ReturnsFalse()
    {
        _artistRepoMock.Setup(r => r.ExistsByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(true);
        var sut = CreateSut();

        var (success, message, artist) = await sut.CreateArtistAsync("Queen");

        Assert.False(success);
        Assert.Null(artist);
        _artistRepoMock.Verify(r => r.AddAsync(It.IsAny<Artist>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateArtistAsync_ValidName_ReturnsSuccessAndEntity()
    {
        _artistRepoMock.Setup(r => r.ExistsByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(false);
        var sut = CreateSut();

        var (success, message, artist) = await sut.CreateArtistAsync("Queen");

        Assert.True(success);
        Assert.NotNull(artist);
        Assert.Equal("Queen", artist.Name);
    }

    [Fact]
    public async Task CreateArtistAsync_ValidName_AddsToRepository()
    {
        _artistRepoMock.Setup(r => r.ExistsByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(false);
        var sut = CreateSut();

        await sut.CreateArtistAsync("Queen");

        _artistRepoMock.Verify(r => r.AddAsync(It.IsAny<Artist>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateArtistAsync_TooLongName_ReturnsFalseWithoutCallingRepo()
    {
        var sut = CreateSut();

        var (success, _, artist) = await sut.CreateArtistAsync(new string('x', 101));

        Assert.False(success);
        Assert.Null(artist);
        _artistRepoMock.Verify(r => r.ExistsByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── GetDeleteConfirmationAsync ────────────────────────────────────────

    [Fact]
    public async Task GetDeleteConfirmationAsync_WithSongs_MessageIncludesSongCount()
    {
        _songRepoMock.Setup(r => r.CountByArtistsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(5);
        var sut = CreateSut();

        var message = await sut.GetDeleteConfirmationAsync([1, 2]);

        Assert.Contains("5", message);
    }

    [Fact]
    public async Task GetDeleteConfirmationAsync_NoSongs_MessageDoesNotMentionSongs()
    {
        _songRepoMock.Setup(r => r.CountByArtistsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(0);
        var sut = CreateSut();

        var message = await sut.GetDeleteConfirmationAsync([1]);

        Assert.DoesNotContain("song", message, StringComparison.OrdinalIgnoreCase);
    }
}
