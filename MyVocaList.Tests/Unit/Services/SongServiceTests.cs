namespace MyVocaList.Tests.Unit.Services;

public class SongServiceTests
{
    private readonly Mock<ISongRepository> _songRepoMock = new();
    private readonly Mock<IArtistRepository> _artistRepoMock = new();
    private readonly Mock<ISongKaraokeUrlRepository> _urlRepoMock = new();
    private readonly Mock<ISongKaraokeUrlService> _urlServiceMock = new();
    private readonly Mock<ILogger<SongService>> _loggerMock = new();

    private SongService CreateSut() => new(
        _songRepoMock.Object,
        _artistRepoMock.Object,
        _urlRepoMock.Object,
        _urlServiceMock.Object,
        _loggerMock.Object);

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

    // ── ValidateVersionInput (Form Validation Standard) ──────────────────

    [Fact]
    // [AC] Version is optional in the main form: empty + not required → valid.
    public void ValidateVersionInput_EmptyNotRequired_ReturnsTrue()
    {
        var sut = CreateSut();
        var (isValid, _) = sut.ValidateVersionInput(string.Empty, isRequired: false);
        Assert.True(isValid);
    }

    [Fact]
    // [AC] AC-1.2: "Save as new version" flow requires a non-empty version label.
    public void ValidateVersionInput_EmptyRequired_ReturnsFalse()
    {
        var sut = CreateSut();
        var (isValid, message) = sut.ValidateVersionInput(string.Empty, isRequired: true);
        Assert.False(isValid);
        Assert.NotEmpty(message);
    }

    [Fact]
    public void ValidateVersionInput_TooLong_ReturnsFalse()
    {
        var sut = CreateSut();
        var version = new string('x', 61); // exceeds 60-char limit
        var (isValid, message) = sut.ValidateVersionInput(version);
        Assert.False(isValid);
        Assert.NotEmpty(message);
    }

    [Fact]
    public void ValidateVersionInput_MaxLength60_ReturnsTrue()
    {
        var sut = CreateSut();
        var version = new string('x', 60);
        var (isValid, _) = sut.ValidateVersionInput(version);
        Assert.True(isValid);
    }

    [Fact]
    public void ValidateVersionInput_ValidValue_ReturnsTrue()
    {
        var sut = CreateSut();
        var (isValid, _) = sut.ValidateVersionInput("Live");
        Assert.True(isValid);
    }

    // ── CreateSongAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task CreateSongAsync_ValidInput_ReturnsSuccessAndEntity()
    {
        var artist = new Artist { Id = 1, Name = "Queen" };
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
        var artist = new Artist { Id = 1, Name = "Queen" };
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
        var song = new Song { Id = 1, ArtistId = 1, Title = "Old Title" };
        _songRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(song);
        _songRepoMock.Setup(r => r.ExistsByTitleForArtistAsync(1, It.IsAny<string>(), 1, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(false);
        _songRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Song>(), It.IsAny<CancellationToken>()))
                     .Returns(Task.CompletedTask);
        var sut = CreateSut();

        var (success, message) = await sut.UpdateSongAsync(1, "New Title", null, null, true);

        Assert.True(success);
    }

    [Fact]
    public async Task UpdateSongAsync_SongNotFound_ReturnsFalse()
    {
        _songRepoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                     .ReturnsAsync((Song)null);
        var sut = CreateSut();

        var (success, message) = await sut.UpdateSongAsync(99, "New Title", null, null, true);

        Assert.False(success);
        Assert.NotEmpty(message);
    }

    [Fact]
    public async Task UpdateSongAsync_DuplicateTitleExcludingSelf_ReturnsFalse()
    {
        var song = new Song { Id = 1, ArtistId = 1, Title = "Title" };
        _songRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(song);
        _songRepoMock.Setup(r => r.ExistsByTitleForArtistAsync(1, It.IsAny<string>(), 1, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(true);
        var sut = CreateSut();

        var (success, message) = await sut.UpdateSongAsync(1, "Existing Other Title", null, null, true);

        Assert.False(success);
        Assert.NotEmpty(message);
    }

    // ── CreateSongAsync — lyrics ──────────────────────────────────────────

    [Fact]
    public async Task CreateSongAsync_WithLyrics_PersistsLyrics()
    {
        var artist = new Artist { Id = 1, Name = "Queen" };
        _artistRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(artist);
        _songRepoMock.Setup(r => r.ExistsByTitleForArtistAsync(1, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(false);
        _songRepoMock.Setup(r => r.AddAsync(It.IsAny<Song>(), It.IsAny<CancellationToken>()))
                     .Returns(Task.CompletedTask);
        var sut = CreateSut();

        var (success, _, song) = await sut.CreateSongAsync(1, "Bohemian Rhapsody", lyrics: "Some lyrics");

        Assert.True(success);
        Assert.NotNull(song);
        Assert.Equal("Some lyrics", song.Lyrics);
    }

    // ── UpdateSongAsync — lyrics ──────────────────────────────────────────

    [Fact]
    public async Task UpdateSongAsync_WithLyrics_ReturnsSuccess()
    {
        var song = new Song { Id = 1, ArtistId = 1, Title = "Old Title" };
        _songRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(song);
        _songRepoMock.Setup(r => r.ExistsByTitleForArtistAsync(1, It.IsAny<string>(), 1, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(false);
        _songRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Song>(), It.IsAny<CancellationToken>()))
                     .Returns(Task.CompletedTask);
        var sut = CreateSut();

        var (success, _) = await sut.UpdateSongAsync(1, "New Title", null, "Updated lyrics", true);

        Assert.True(success);
    }

    // ── GetPagedSongsForListAsync ─────────────────────────────────────────

    [Fact]
    public async Task GetPagedSongsForListAsync_ReturnsFromRepository()
    {
        var items = new List<SongListItemDto>
        {
            new(1, "Bohemian Rhapsody", 1, "Queen", null, null, false)
        };
        _songRepoMock.Setup(r => r.GetPagedAsync(1, 20, null, It.IsAny<CancellationToken>()))
                     .ReturnsAsync((items.AsEnumerable(), 1));
        var sut = CreateSut();

        var (result, totalCount) = await sut.GetPagedSongsForListAsync(1, 20);

        Assert.Single(result);
        Assert.Equal(1, totalCount);
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

    // ── UpdateSongAsync — external identity (M2) ──────────────────────────

    [Fact]
    // [AC] AC-2.4: UpdateSongAsync persists externalProvider and externalId when provided
    public async Task UpdateSongAsync_WithExternalIdentity_PersistsProviderAndId()
    {
        var song = new Song { Id = 1, ArtistId = 1, Title = "Old Title" };
        _songRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(song);
        _songRepoMock.Setup(r => r.ExistsByTitleForArtistAsync(1, It.IsAny<string>(), 1, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(false);
        _songRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Song>(), It.IsAny<CancellationToken>()))
                     .Returns(Task.CompletedTask);
        var sut = CreateSut();

        var (success, _) = await sut.UpdateSongAsync(
            1, "New Title", null, null, false,
            externalId: "ext-123", externalProvider: "spotify");

        Assert.True(success);
        Assert.Equal("ext-123", song.ExternalId);
        Assert.Equal("spotify", song.ExternalProvider);
    }

    [Fact]
    public async Task UpdateSongAsync_WithNullExternalIdentity_DoesNotOverwriteExistingIdentity()
    {
        var song = new Song { Id = 1, ArtistId = 1, Title = "Old Title", ExternalId = "keep-me", ExternalProvider = "spotify" };
        _songRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(song);
        _songRepoMock.Setup(r => r.ExistsByTitleForArtistAsync(1, It.IsAny<string>(), 1, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(false);
        _songRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Song>(), It.IsAny<CancellationToken>()))
                     .Returns(Task.CompletedTask);
        var sut = CreateSut();

        var (success, _) = await sut.UpdateSongAsync(
            1, "New Title", null, null, false,
            externalId: null, externalProvider: null);

        Assert.True(success);
        // null params must not overwrite the existing identity
        Assert.Equal("keep-me", song.ExternalId);
        Assert.Equal("spotify", song.ExternalProvider);
    }

    // ── CreateSongWithUrlsAsync (N3 atomic save, AC-6.1/6.2) ─────────────

    private void SetupArtistAndDuplicateCheck(int artistId, bool duplicate = false)
    {
        _artistRepoMock.Setup(r => r.GetByIdAsync(artistId, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(new Artist { Id = artistId, Name = "Artist" });
        _songRepoMock.Setup(r => r.ExistsByTitleVersionForArtistAsync(
                         artistId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(duplicate);
        _songRepoMock.Setup(r => r.AddAsync(It.IsAny<Song>(), It.IsAny<CancellationToken>()))
                     .Returns(Task.CompletedTask);
        _urlRepoMock.Setup(r => r.AddAsync(It.IsAny<SongKaraokeUrl>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
        _urlServiceMock.Setup(s => s.ExtractVideoId(It.IsAny<string>()))
                       .Returns((string url) => url.Contains("youtube") ? "vid-" + url.GetHashCode().ToString("x") : null);
    }

    [Fact]
    // [AC] AC-6.1: CreateSongWithUrlsAsync creates song and persists all provided URLs
    public async Task CreateSongWithUrlsAsync_ValidSongAndUrls_PersistsBoth()
    {
        SetupArtistAndDuplicateCheck(1);
        var sut = CreateSut();

        var (success, _, song) = await sut.CreateSongWithUrlsAsync(
            1, "Bohemian Rhapsody", "", null, null, null, null,
            ["https://youtube.com/watch?v=abc12345678", "https://youtube.com/watch?v=def12345678"]);

        Assert.True(success);
        Assert.NotNull(song);
        // Both URLs staged via url repo
        _urlRepoMock.Verify(r => r.AddAsync(It.IsAny<SongKaraokeUrl>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        // Single SaveChangesAsync on song repo (atomic commit)
        _songRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        // URL repo must NOT call its own SaveChangesAsync
        _urlRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    // [AC] AC-6.2: CreateSongWithUrlsAsync rolls back on duplicate (artistId, title, version)
    public async Task CreateSongWithUrlsAsync_DuplicateTitleVersion_ReturnsFalseAndPersistsNothing()
    {
        SetupArtistAndDuplicateCheck(1, duplicate: true);
        var sut = CreateSut();

        var (success, message, song) = await sut.CreateSongWithUrlsAsync(
            1, "Existing Song", "", null, null, null, null,
            ["https://youtube.com/watch?v=abc12345678"]);

        Assert.False(success);
        Assert.NotEmpty(message);
        Assert.Null(song);
        _songRepoMock.Verify(r => r.AddAsync(It.IsAny<Song>(), It.IsAny<CancellationToken>()), Times.Never);
        _urlRepoMock.Verify(r => r.AddAsync(It.IsAny<SongKaraokeUrl>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    // [AC] AC-6.1: CreateSongWithUrlsAsync with no URLs creates song only (no URL repo calls)
    public async Task CreateSongWithUrlsAsync_EmptyUrlList_CreatesSongOnly()
    {
        SetupArtistAndDuplicateCheck(1);
        var sut = CreateSut();

        var (success, _, song) = await sut.CreateSongWithUrlsAsync(
            1, "Bohemian Rhapsody", "", null, null, null, null,
            []);

        Assert.True(success);
        Assert.NotNull(song);
        _urlRepoMock.Verify(r => r.AddAsync(It.IsAny<SongKaraokeUrl>(), It.IsAny<CancellationToken>()), Times.Never);
        _songRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
