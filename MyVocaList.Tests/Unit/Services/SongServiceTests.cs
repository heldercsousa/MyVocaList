namespace MyVocaList.Tests.Unit.Services;

public class SongServiceTests
{
    private readonly Mock<ISongRepository> _songRepoMock = new();
    private readonly Mock<ILogger<SongService>> _loggerMock = new();

    private SongService CreateSut() => new(_songRepoMock.Object, _loggerMock.Object);

    // ── ValidateTitleInput ────────────────────────────────────────────────

    [Fact]
    public void ValidateTitleInput_EmptyTitle_ReturnsInvalid()
    {
        var sut = CreateSut();
        var (isValid, message) = sut.ValidateTitleInput(string.Empty);
        Assert.False(isValid);
        Assert.NotEmpty(message);
    }

    [Fact]
    public void ValidateTitleInput_WhitespaceTitle_ReturnsInvalid()
    {
        var sut = CreateSut();
        var (isValid, message) = sut.ValidateTitleInput("   ");
        Assert.False(isValid);
        Assert.NotEmpty(message);
    }

    [Fact]
    public void ValidateTitleInput_TitleTooLong_ReturnsInvalid()
    {
        var sut = CreateSut();
        var (isValid, message) = sut.ValidateTitleInput(new string('x', 201));
        Assert.False(isValid);
        Assert.NotEmpty(message);
    }

    [Fact]
    public void ValidateTitleInput_ValidTitle_ReturnsValid()
    {
        var sut = CreateSut();
        var (isValid, _) = sut.ValidateTitleInput("Bohemian Rhapsody");
        Assert.True(isValid);
    }

    // ── CreateSongAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task CreateSongAsync_DuplicateTitleForArtist_ReturnsFalse()
    {
        _songRepoMock.Setup(r => r.ExistsByTitleForArtistAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(true);
        var sut = CreateSut();

        var (success, message, song) = await sut.CreateSongAsync(1, "Bohemian Rhapsody");

        Assert.False(success);
        Assert.Null(song);
        _songRepoMock.Verify(r => r.AddAsync(It.IsAny<Song>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateSongAsync_ValidTitle_ReturnsSuccessAndEntity()
    {
        _songRepoMock.Setup(r => r.ExistsByTitleForArtistAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(false);
        var sut = CreateSut();

        var (success, message, song) = await sut.CreateSongAsync(1, "Bohemian Rhapsody");

        Assert.True(success);
        Assert.NotNull(song);
        Assert.Equal("Bohemian Rhapsody", song.Title);
        Assert.Equal(1, song.ArtistId);
    }

    [Fact]
    public async Task CreateSongAsync_ValidTitle_AddsToRepository()
    {
        _songRepoMock.Setup(r => r.ExistsByTitleForArtistAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(false);
        var sut = CreateSut();

        await sut.CreateSongAsync(1, "Bohemian Rhapsody");

        _songRepoMock.Verify(r => r.AddAsync(It.IsAny<Song>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateSongAsync_TooLongTitle_ReturnsFalseWithoutCallingRepo()
    {
        var sut = CreateSut();

        var (success, _, song) = await sut.CreateSongAsync(1, new string('x', 201));

        Assert.False(success);
        Assert.Null(song);
        _songRepoMock.Verify(r => r.ExistsByTitleForArtistAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── DeleteSongsAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task DeleteSongsAsync_SingleId_ReturnsSuccess()
    {
        var sut = CreateSut();

        var (success, message) = await sut.DeleteSongsAsync([42]);

        Assert.True(success);
        _songRepoMock.Verify(r => r.DeleteAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
