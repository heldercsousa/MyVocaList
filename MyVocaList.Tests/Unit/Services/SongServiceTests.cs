namespace MyVocaList.Tests.Unit.Services;

public class SongServiceTests
{
    private readonly Mock<ISongRepository> _songRepoMock = new();
    private readonly Mock<IArtistRepository> _artistRepoMock = new();
    private readonly Mock<ILogger<SongService>> _loggerMock = new();

    private SongService CreateSut() => new(_songRepoMock.Object, _artistRepoMock.Object, _loggerMock.Object);

    // ── ValidateTitleInput ────────────────────────────────────────────────

    [Fact]
    public void ValidateTitleInput_EmptyTitle_ReturnsFalse()
    {
        var sut = CreateSut();
        var (isValid, message) = sut.ValidateTitleInput(string.Empty);
        Assert.False(isValid);
        Assert.NotEmpty(message);
    }

    [Fact]
    public void ValidateTitleInput_WhitespaceTitle_ReturnsFalse()
    {
        var sut = CreateSut();
        var (isValid, message) = sut.ValidateTitleInput("   ");
        Assert.False(isValid);
        Assert.NotEmpty(message);
    }

    [Fact]
    public void ValidateTitleInput_TitleTooLong_ReturnsFalse()
    {
        var sut = CreateSut();
        var title = new string('x', 101); // exceeds 100-char limit
        var (isValid, message) = sut.ValidateTitleInput(title);
        Assert.False(isValid);
        Assert.NotEmpty(message);
    }

    [Fact]
    public void ValidateTitleInput_ValidTitle_ReturnsTrue()
    {
        var sut = CreateSut();
        var (isValid, _) = sut.ValidateTitleInput("Bohemian Rhapsody");
        Assert.True(isValid);
    }

    [Fact]
    public void ValidateTitleInput_MaxLength100_ReturnsTrue()
    {
        var sut = CreateSut();
        var title = new string('x', 100);
        var (isValid, _) = sut.ValidateTitleInput(title);
        Assert.True(isValid);
    }

    // ── CreateSongAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task CreateSongAsync_ValidInput_ReturnsSuccessAndEntity()
    {
        var artist = new Artist { Id = 1, Name = "Queen", NameNormalized = "queen" };
        _artistRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(artist);
        _songRepoMock.Setup(r => r.ExistsByTitleForArtistAsync(1, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(false);
        _songRepoMock.Setup(r => r.AddAsync(It.IsAny<Song>(), It.IsAny<CancellationToken>()))
                     .Returns(Task.CompletedTask);
        var sut = CreateSut();

        var (success, message, song) = await sut.CreateSongAsync(1, "Bohemian Rhapsody");

        Assert.True(success);
        Assert.NotNull(song);
        Assert.Equal("Bohemian Rhapsody", song.Title);
        Assert.Equal(1, song.ArtistId);
    }

    [Fact]
    public async Task CreateSongAsync_TitleTooLong_ReturnsFalse()
    {
        var sut = CreateSut();
        var title = new string('x', 101);

        var (success, message, song) = await sut.CreateSongAsync(1, title);

        Assert.False(success);
        Assert.NotEmpty(message);
        Assert.Null(song);
        _songRepoMock.Verify(r => r.AddAsync(It.IsAny<Song>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateSongAsync_ArtistNotFound_ReturnsFalse()
    {
        _artistRepoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                       .ReturnsAsync((Artist)null);
        var sut = CreateSut();

        var (success, message, song) = await sut.CreateSongAsync(99, "Valid Title");

        Assert.False(success);
        Assert.NotEmpty(message);
        Assert.Null(song);
    }

    [Fact]
    public async Task CreateSongAsync_DuplicateTitleForArtist_ReturnsFalse()
    {
        var artist = new Artist { Id = 1, Name = "Queen", NameNormalized = "queen" };
        _artistRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(artist);
        _songRepoMock.Setup(r => r.ExistsByTitleForArtistAsync(1, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(true);
        var sut = CreateSut();

        var (success, message, song) = await sut.CreateSongAsync(1, "Bohemian Rhapsody");

        Assert.False(success);
        Assert.NotEmpty(message);
        Assert.Null(song);
    }

    // ── UpdateSongAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task UpdateSongAsync_ValidTitle_ReturnsSuccess()
    {
        var song = new Song { Id = 1, ArtistId = 1, Title = "Old Title", TitleNormalized = "old title" };
        _songRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(song);
        _songRepoMock.Setup(r => r.ExistsByTitleForArtistAsync(1, It.IsAny<string>(), 1, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(false);
        _songRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Song>(), It.IsAny<CancellationToken>()))
                     .Returns(Task.CompletedTask);
        var sut = CreateSut();

        var (success, message) = await sut.UpdateSongAsync(1, "New Title");

        Assert.True(success);
    }

    [Fact]
    public async Task UpdateSongAsync_SongNotFound_ReturnsFalse()
    {
        _songRepoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                     .ReturnsAsync((Song)null);
        var sut = CreateSut();

        var (success, message) = await sut.UpdateSongAsync(99, "New Title");

        Assert.False(success);
        Assert.NotEmpty(message);
    }

    [Fact]
    public async Task UpdateSongAsync_DuplicateTitleExcludingSelf_ReturnsFalse()
    {
        var song = new Song { Id = 1, ArtistId = 1, Title = "Title", TitleNormalized = "title" };
        _songRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(song);
        _songRepoMock.Setup(r => r.ExistsByTitleForArtistAsync(1, It.IsAny<string>(), 1, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(true);
        var sut = CreateSut();

        var (success, message) = await sut.UpdateSongAsync(1, "Existing Other Title");

        Assert.False(success);
        Assert.NotEmpty(message);
    }

    // ── DeleteSongsAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task DeleteSongsAsync_ValidIds_ReturnsSuccess()
    {
        _songRepoMock.Setup(r => r.DeleteAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
                     .Returns(Task.CompletedTask);
        var sut = CreateSut();

        var (success, message) = await sut.DeleteSongsAsync([1, 2]);

        Assert.True(success);
    }
}
