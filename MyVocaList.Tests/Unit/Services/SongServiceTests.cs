using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Unit.Services;

public class SongServiceTests
{
    private readonly Mock<ISongRepository> _songRepoMock = new();
    private readonly Mock<IArtistRepository> _artistRepoMock = new();
    private readonly Mock<ISongKaraokeUrlRepository> _urlRepoMock = new();
    private readonly Mock<ISongKaraokeUrlService> _urlServiceMock = new();
    private readonly Mock<ILogger<SongService>> _loggerMock = new();

    // The service now takes IUnitOfWork; PassthroughUnitOfWork runs each wrapped body inline
    // against these same mocks, so every existing assertion keeps its original meaning
    // (`plan.md` Task 2.2 Step 1).
    private SongService CreateSut() => new(
        _songRepoMock.Object,
        _artistRepoMock.Object,
        _urlRepoMock.Object,
        _urlServiceMock.Object,
        PassthroughUnitOfWork.Over(
            _songRepoMock, _artistRepoMock, _urlRepoMock, _urlServiceMock),
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
        var title = new string('x', 251); // exceeds 250-char limit (REQ-STL-01)
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
    // [AC] AC-STL-01: a 250-char title is valid
    public void ValidateTitleInput_MaxLength250_ReturnsTrue()
    {
        var sut = CreateSut();
        var title = new string('x', 250);
        var (isValid, _) = sut.ValidateTitleInput(title);
        Assert.True(isValid);
    }

    [Fact]
    // [AC] AC-STL-01: 251 chars is rejected with the 250 message
    public void ValidateTitleInput_251Chars_ReturnsFalseWith250Message()
    {
        var sut = CreateSut();
        var (isValid, message) = sut.ValidateTitleInput(new string('x', 251));
        Assert.False(isValid);
        Assert.Equal("Title is too long. Maximum 250 characters.", message);
    }

    // ── GetCharacterCounterInfo ───────────────────────────────────────────

    [Fact]
    // [AC] AC-STL-02 / Counter threshold alignment (Form Validation Standard): the counter reports an error
    // only when ValidateTitleInput would reject the same length. 250 chars is valid → no error.
    public void GetCharacterCounterInfo_AtMaxLength250_IsNotError()
    {
        var sut = CreateSut();

        var (text, _, isError) = sut.GetCharacterCounterInfo(250);

        Assert.Equal("250/250", text);
        Assert.False(isError);
    }

    [Fact]
    // [AC] Counter threshold alignment: 251 chars is rejected by the validator → counter error.
    public void GetCharacterCounterInfo_OverMaxLength_IsError()
    {
        var sut = CreateSut();

        var (_, _, isError) = sut.GetCharacterCounterInfo(251);

        Assert.True(isError);
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
        var title = new string('x', 251);

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

        var (success, message) = await sut.UpdateSongAsync(1, "New Title", null, null);

        Assert.True(success);
    }

    [Fact]
    public async Task UpdateSongAsync_SongNotFound_ReturnsFalse()
    {
        _songRepoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                     .ReturnsAsync((Song)null);
        var sut = CreateSut();

        var (success, message) = await sut.UpdateSongAsync(99, "New Title", null, null);

        Assert.False(success);
        Assert.NotEmpty(message);
    }

    [Fact]
    public async Task UpdateSongAsync_DuplicateTitleExcludingSelf_ReturnsFalse()
    {
        var song = new Song { Id = 1, ArtistId = 1, Title = "Title" };
        _songRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(song);
        _songRepoMock.Setup(r => r.FindIdByTitleVersionForArtistAsync(1, It.IsAny<string>(), It.IsAny<string>(), 1, It.IsAny<CancellationToken>()))
                     .ReturnsAsync((int?)2);
        var sut = CreateSut();

        var (success, message) = await sut.UpdateSongAsync(1, "Existing Other Title", null, null);

        Assert.False(success);
        Assert.NotEmpty(message);
    }

    // ── UpdateSongAsync — version-aware uniqueness, external identity (SFT 3.2a) ──

    private const string DuplicateMessage = "A song with this title and version already exists for this artist";

    private void SetupStoredSong(Song song)
    {
        _songRepoMock.Setup(r => r.GetByIdAsync(song.Id, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(song);
        _songRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Song>(), It.IsAny<CancellationToken>()))
                     .Returns(Task.CompletedTask);
    }

    [Fact]
    // [AC] REQ-SFT-29: Radiohead has "Creep" (Version "") and "Creep" (Version "Live"); editing the
    // "Live" song's Version to "Acoustic" succeeds. Regression: the update check used to ignore Version.
    public async Task UpdateSongAsync_VersionChangedToNewVersionOfExistingTitle_Succeeds()
    {
        SetupStoredSong(new Song { Id = 2, ArtistId = 1, Title = "Creep", Version = "Live" });
        // The old, title-only check would see the canonical "Creep" and reject.
        _songRepoMock.Setup(r => r.ExistsByTitleForArtistAsync(1, It.IsAny<string>(), 2, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(true);
        _songRepoMock.Setup(r => r.FindIdByTitleVersionForArtistAsync(1, "Creep", "Acoustic", 2, It.IsAny<CancellationToken>()))
                     .ReturnsAsync((int?)null);
        var sut = CreateSut();

        var (success, message) = await sut.UpdateSongAsync(2, "Creep", null, null, version: "Acoustic");

        Assert.True(success, message);
    }

    [Fact]
    // [AC] REQ-SFT-29: changing that Version to "" collides with the canonical "Creep" and fails
    // with the duplicate message; nothing is written.
    public async Task UpdateSongAsync_VersionChangedToExistingVersion_FailsDuplicate()
    {
        SetupStoredSong(new Song { Id = 2, ArtistId = 1, Title = "Creep", Version = "Live" });
        _songRepoMock.Setup(r => r.FindIdByTitleVersionForArtistAsync(1, "Creep", "", 2, It.IsAny<CancellationToken>()))
                     .ReturnsAsync((int?)1);
        var sut = CreateSut();

        var (success, message) = await sut.UpdateSongAsync(2, "Creep", null, null, version: "");

        Assert.False(success);
        Assert.Equal(DuplicateMessage, message);
        _songRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Song>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    // [AC] REQ-SFT-27: editing the title of an API-sourced song changes only the local title; the
    // stored ("Deezer","3135556") identity is untouched when the call passes null/null.
    public async Task UpdateSongAsync_TitleEditedOnApiSourcedSong_KeepsExternalIdentity()
    {
        var song = new Song
        {
            Id = 1, ArtistId = 1, Title = "Creep", Version = "",
            ExternalProvider = "Deezer", ExternalId = "3135556"
        };
        SetupStoredSong(song);
        var sut = CreateSut();

        var (success, message) = await sut.UpdateSongAsync(
            1, "Creep (Remastered)", null, null, externalId: null, externalProvider: null);

        Assert.True(success, message);
        Assert.Equal("Creep (Remastered)", song.Title);
        Assert.Equal("Deezer", song.ExternalProvider);
        Assert.Equal("3135556", song.ExternalId);
    }

    [Fact]
    // [AC] REQ-SFT-27: an edited title that collides with another song of the same artist and
    // version is rejected with the duplicate message.
    public async Task UpdateSongAsync_EditedTitleCollidesWithArtistTitleVersion_Rejected()
    {
        var song = new Song { Id = 1, ArtistId = 1, Title = "Karma Police", Version = "" };
        SetupStoredSong(song);
        _songRepoMock.Setup(r => r.FindIdByTitleVersionForArtistAsync(1, "creep", "", 1, It.IsAny<CancellationToken>()))
                     .ReturnsAsync((int?)5);
        var sut = CreateSut();

        var (success, message) = await sut.UpdateSongAsync(1, "creep", null, null);

        Assert.False(success);
        Assert.Equal(DuplicateMessage, message);
        Assert.Equal("Karma Police", song.Title);
        _songRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Song>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    // [AC] REQ-SFT-26: an external identity already held by ANOTHER song (self excluded) is
    // rejected and nothing is written.
    public async Task UpdateSongAsync_ExternalIdentityHeldByAnotherSong_Fails()
    {
        var song = new Song { Id = 1, ArtistId = 1, Title = "Creep", Version = "" };
        SetupStoredSong(song);
        _songRepoMock.Setup(r => r.ExistsByExternalIdentityAsync("Deezer", "999", 1, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(true);
        var sut = CreateSut();

        var (success, message) = await sut.UpdateSongAsync(
            1, "Creep", null, null, externalId: "999", externalProvider: "Deezer");

        Assert.False(success);
        Assert.Equal("This music-database song is already in your library", message);
        _songRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Song>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private SongService CreateSutWithArtistService(Mock<IArtistService> artistServiceMock) => new(
        _songRepoMock.Object,
        _artistRepoMock.Object,
        _urlRepoMock.Object,
        _urlServiceMock.Object,
        PassthroughUnitOfWork.Over(
            _songRepoMock, _artistRepoMock, _urlRepoMock, _urlServiceMock, artistServiceMock),
        _loggerMock.Object);

    [Fact]
    // [AC] REQ-SFT-28: a typed new artist on edit is created through the sp-resolved IArtistService
    // inside the song's unit of work, and the song is re-linked to the new artist's id.
    public async Task UpdateSongAsync_NewArtistName_CreatesArtistInSameUnitOfWork()
    {
        var song = new Song { Id = 1, ArtistId = 1, Title = "Creep", Version = "" };
        SetupStoredSong(song);
        var artistService = new Mock<IArtistService>();
        artistService.Setup(s => s.CreateArtistAsync("New Band", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync((true, "created", new Artist { Id = 9, Name = "New Band" }));
        var sut = CreateSutWithArtistService(artistService);

        var (success, message) = await sut.UpdateSongAsync(
            1, "Creep", null, null, newArtistName: "New Band");

        Assert.True(success, message);
        Assert.Equal(9, song.ArtistId);
        artistService.Verify(s => s.CreateArtistAsync("New Band", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _songRepoMock.Verify(r => r.UpdateAsync(song, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    // [AC] REQ-SFT-28: a failed artist creation (e.g. the REQ-SFT-52 collated-duplicate refusal)
    // returns its message and the song is not written.
    public async Task UpdateSongAsync_NewArtistCreationFails_ReturnsMessageAndDoesNotWrite()
    {
        var song = new Song { Id = 1, ArtistId = 1, Title = "Creep", Version = "" };
        SetupStoredSong(song);
        var artistService = new Mock<IArtistService>();
        artistService.Setup(s => s.CreateArtistAsync("Dup", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync((false, "Artist already exists", (Artist?)null));
        var sut = CreateSutWithArtistService(artistService);

        var (success, message) = await sut.UpdateSongAsync(
            1, "Creep", null, null, newArtistName: "Dup");

        Assert.False(success);
        Assert.Equal("Artist already exists", message);
        Assert.Equal(1, song.ArtistId);
        _songRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Song>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    // [AC] REQ-SFT-28: at most one of artistId / newArtistName (REQ-SFT-21 XOR); supplying both is
    // rejected before anything is read or written.
    public async Task UpdateSongAsync_BothArtistIdAndNewArtistName_Rejected()
    {
        var song = new Song { Id = 1, ArtistId = 1, Title = "Creep", Version = "" };
        SetupStoredSong(song);
        var sut = CreateSut();

        var (success, message) = await sut.UpdateSongAsync(
            1, "Creep", null, null, artistId: 2, newArtistName: "New Band");

        Assert.False(success);
        Assert.Equal("Artist is required", message);
        _songRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Song>(), It.IsAny<CancellationToken>()), Times.Never);
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

        var (success, _) = await sut.UpdateSongAsync(1, "New Title", null, "Updated lyrics");

        Assert.True(success);
    }

    [Fact]
    // [AC] REQ-TRIM-06: the comparison term used by ExistsByTitleForArtistAsync must be
    // normalized the same way persisted titles will be (Task 6's ValueConverter), so dedup
    // checks agree with the stored, collapsed-whitespace value.
    public async Task ExistsByTitleForArtistAsync_TitleWithExtraWhitespace_ComparesNormalizedTerm()
    {
        _songRepoMock.Setup(r => r.ExistsByTitleForArtistAsync(1, "my song", It.IsAny<CancellationToken>()))
                     .ReturnsAsync(true);
        var sut = CreateSut();

        var exists = await sut.ExistsByTitleForArtistAsync(" my  song ", 1);

        Assert.True(exists);
        _songRepoMock.Verify(r => r.ExistsByTitleForArtistAsync(1, "my song", It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── GetPagedSongsForListAsync ─────────────────────────────────────────

    [Fact]
    public async Task GetPagedSongsForListAsync_ReturnsFromRepository()
    {
        var items = new List<SongListItemDto>
        {
            new(1, "Bohemian Rhapsody", 1, "Queen", null, null)
        };
        _songRepoMock.Setup(r => r.GetPagedAsync(1, 20, null, It.IsAny<CancellationToken>()))
                     .ReturnsAsync((items.AsEnumerable(), 1));
        var sut = CreateSut();

        var (result, totalCount) = await sut.GetPagedSongsForListAsync(1, 20);

        Assert.Single(result);
        Assert.Equal(1, totalCount);
    }

    [Fact]
    // [AC] REQ-TRIM-01/03: query whitespace (leading/trailing/internal runs) is normalized
    // before being forwarded to the repository.
    public async Task GetPagedSongsForListAsync_QueryWithExtraWhitespace_NormalizesBeforeRepositoryCall()
    {
        var items = new List<SongListItemDto>
        {
            new(1, "Bohemian Rhapsody", 1, "Queen", null, null)
        };
        _songRepoMock.Setup(r => r.GetPagedAsync(1, 20, "my song", It.IsAny<CancellationToken>()))
                     .ReturnsAsync((items.AsEnumerable(), 1));
        var sut = CreateSut();

        var (result, totalCount) = await sut.GetPagedSongsForListAsync(1, 20, " my  song ");

        Assert.Single(result);
        Assert.Equal(1, totalCount);
        _songRepoMock.Verify(r => r.GetPagedAsync(1, 20, "my song", It.IsAny<CancellationToken>()), Times.Once);
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
            1, "New Title", null, null,
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
            1, "New Title", null, null,
            externalId: null, externalProvider: null);

        Assert.True(success);
        // null params must not overwrite the existing identity
        Assert.Equal("keep-me", song.ExternalId);
        Assert.Equal("spotify", song.ExternalProvider);
    }

    // ── UpdateSongAsync — artist re-link (BUG-067) ────────────────────────

    [Fact]
    // [AC] REQ-ACREATE-16: BUG-067 — changing the artist of an already-saved song must be
    // persisted. Before the fix UpdateSongAsync had no artistId parameter at all and never
    // assigned song.ArtistId, so the user's edit was silently discarded.
    public async Task UpdateSongAsync_WithChangedArtistId_PersistsNewArtist()
    {
        var song = new Song { Id = 1, ArtistId = 1, Title = "Title" };
        _songRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(song);
        _artistRepoMock.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(new Artist { Id = 2, Name = "New Artist" });
        _songRepoMock.Setup(r => r.ExistsByTitleForArtistAsync(2, It.IsAny<string>(), 1, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(false);
        _songRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Song>(), It.IsAny<CancellationToken>()))
                     .Returns(Task.CompletedTask);
        var sut = CreateSut();

        var (success, _) = await sut.UpdateSongAsync(1, "Title", null, null, artistId: 2);

        Assert.True(success);
        Assert.Equal(2, song.ArtistId);
    }

    [Fact]
    // [AC] REQ-ACREATE-16: null artistId keeps the existing link (same null-keeps-existing
    // semantics as version/externalId) — an edit that does not touch the artist must not clear it.
    public async Task UpdateSongAsync_NullArtistId_KeepsExistingArtist()
    {
        var song = new Song { Id = 1, ArtistId = 7, Title = "Title" };
        _songRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(song);
        _songRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Song>(), It.IsAny<CancellationToken>()))
                     .Returns(Task.CompletedTask);
        var sut = CreateSut();

        var (success, _) = await sut.UpdateSongAsync(1, "Title", null, null, artistId: null);

        Assert.True(success);
        Assert.Equal(7, song.ArtistId);
    }

    [Fact]
    // [AC] REQ-ACREATE-16: an unknown artist id is rejected with a message and no write.
    public async Task UpdateSongAsync_UnknownArtistId_ReturnsFalseAndDoesNotWrite()
    {
        var song = new Song { Id = 1, ArtistId = 7, Title = "Title" };
        _songRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(song);
        _artistRepoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                       .ReturnsAsync((Artist?)null);
        var sut = CreateSut();

        var (success, message) = await sut.UpdateSongAsync(1, "Title", null, null, artistId: 99);

        Assert.False(success);
        Assert.NotEmpty(message);
        Assert.Equal(7, song.ArtistId);
        _songRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Song>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    // [AC] REQ-ACREATE-16: the title-uniqueness check on update is evaluated against the NEW
    // artist — moving a song to an artist that already has that title must be rejected.
    public async Task UpdateSongAsync_ChangedArtistWithDuplicateTitle_ReturnsFalse()
    {
        var song = new Song { Id = 1, ArtistId = 1, Title = "Title" };
        _songRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(song);
        _artistRepoMock.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(new Artist { Id = 2, Name = "New Artist" });
        _songRepoMock.Setup(r => r.FindIdByTitleVersionForArtistAsync(2, It.IsAny<string>(), It.IsAny<string>(), 1, It.IsAny<CancellationToken>()))
                     .ReturnsAsync((int?)3);
        var sut = CreateSut();

        var (success, message) = await sut.UpdateSongAsync(1, "Title", null, null, artistId: 2);

        Assert.False(success);
        Assert.NotEmpty(message);
        Assert.Equal(1, song.ArtistId);
        _songRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Song>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── UpdateSongAsync — version (BUG-024) ───────────────────────────────

    [Fact]
    // [AC] BUG-024: edits to the Version field were discarded — UpdateSongAsync must persist
    // the provided version label.
    public async Task UpdateSongAsync_WithVersion_PersistsVersion()
    {
        var song = new Song { Id = 1, ArtistId = 1, Title = "Title", Version = "Live" };
        _songRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(song);
        _songRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Song>(), It.IsAny<CancellationToken>()))
                     .Returns(Task.CompletedTask);
        var sut = CreateSut();

        var (success, _) = await sut.UpdateSongAsync(
            1, "Title", null, null, version: "Acoustic");

        Assert.True(success);
        Assert.Equal("Acoustic", song.Version);
    }

    [Fact]
    // [AC] BUG-024: a null version must not overwrite the stored version (null = keep existing,
    // same semantics as externalId/externalProvider).
    public async Task UpdateSongAsync_NullVersion_KeepsExistingVersion()
    {
        var song = new Song { Id = 1, ArtistId = 1, Title = "Title", Version = "Live" };
        _songRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(song);
        _songRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Song>(), It.IsAny<CancellationToken>()))
                     .Returns(Task.CompletedTask);
        var sut = CreateSut();

        var (success, _) = await sut.UpdateSongAsync(1, "Title", null, null, version: null);

        Assert.True(success);
        Assert.Equal("Live", song.Version);
    }

    [Fact]
    // [AC] BUG-024: version exceeding the 60-char limit is rejected before any write.
    public async Task UpdateSongAsync_VersionTooLong_ReturnsFalse()
    {
        var sut = CreateSut();
        var version = new string('x', 61);

        var (success, message) = await sut.UpdateSongAsync(1, "Title", null, null, version: version);

        Assert.False(success);
        Assert.NotEmpty(message);
        _songRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Song>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── GetSongByIdAsync (BUG-024) ────────────────────────────────────────

    [Fact]
    // [AC] BUG-024: edit-mode hydration needs the full entity — GetSongByIdAsync must return
    // the song with FeaturedArtists, Lyrics and Version intact.
    public async Task GetSongByIdAsync_ExistingId_ReturnsSongWithAllFields()
    {
        var song = new Song
        {
            Id = 1,
            ArtistId = 1,
            Title = "Title",
            Version = "Live",
            FeaturedArtists = "Feat A",
            Lyrics = "Some lyrics"
        };
        _songRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(song);
        var sut = CreateSut();

        var result = await sut.GetSongByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal("Live", result.Version);
        Assert.Equal("Feat A", result.FeaturedArtists);
        Assert.Equal("Some lyrics", result.Lyrics);
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
            1, null, "Bohemian Rhapsody", "", null, null, null, null,
            ["https://youtube.com/watch?v=abc12345678", "https://youtube.com/watch?v=def12345678"]);

        Assert.True(success);
        Assert.NotNull(song);
        // Both URLs staged via url repo
        _urlRepoMock.Verify(r => r.AddAsync(It.IsAny<SongKaraokeUrl>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        // [AC] REQ-UOW-10: ISongKaraokeUrlRepository.SaveChangesAsync is retired outright (Phase 4.3)
        // - the single atomic commit moved to IUnitOfWork, and is asserted against a real context by
        // Bug068RegressionTests.CreateSongWithUrls_UrlAddFaults_PersistsNoSongRow.
    }

    [Fact]
    // [AC] AC-6.2: CreateSongWithUrlsAsync rolls back on duplicate (artistId, title, version)
    public async Task CreateSongWithUrlsAsync_DuplicateTitleVersion_ReturnsFalseAndPersistsNothing()
    {
        SetupArtistAndDuplicateCheck(1, duplicate: true);
        var sut = CreateSut();

        var (success, message, song) = await sut.CreateSongWithUrlsAsync(
            1, null, "Existing Song", "", null, null, null, null,
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
            1, null, "Bohemian Rhapsody", "", null, null, null, null,
            []);

        Assert.True(success);
        Assert.NotNull(song);
        _urlRepoMock.Verify(r => r.AddAsync(It.IsAny<SongKaraokeUrl>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    // [AC] REQ-SFT-21: neither artistId nor newArtistName supplied -> "Artist is required", nothing written
    public async Task CreateSongWithUrlsAsync_NeitherArtistIdNorNewName_FailsArtistRequired()
    {
        SetupArtistAndDuplicateCheck(1);
        var sut = CreateSut();

        var (success, message, song) = await sut.CreateSongWithUrlsAsync(
            null, null, "Bohemian Rhapsody", "", null, null, null, null, []);

        Assert.False(success);
        Assert.Equal("Artist is required", message);
        Assert.Null(song);
        _songRepoMock.Verify(r => r.AddAsync(It.IsAny<Song>(), It.IsAny<CancellationToken>()), Times.Never);
        _artistRepoMock.Verify(r => r.AddAsync(It.IsAny<Artist>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    // [AC] REQ-SFT-21: both artistId and newArtistName supplied -> "Artist is required", nothing written
    public async Task CreateSongWithUrlsAsync_BothArtistIdAndNewName_FailsArtistRequired()
    {
        SetupArtistAndDuplicateCheck(1);
        var sut = CreateSut();

        var (success, message, song) = await sut.CreateSongWithUrlsAsync(
            1, "New Artist", "Bohemian Rhapsody", "", null, null, null, null, []);

        Assert.False(success);
        Assert.Equal("Artist is required", message);
        Assert.Null(song);
        _songRepoMock.Verify(r => r.AddAsync(It.IsAny<Song>(), It.IsAny<CancellationToken>()), Times.Never);
        _artistRepoMock.Verify(r => r.AddAsync(It.IsAny<Artist>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    // [AC] REQ-SFT-26: external identity already held by another song -> rejected, song never staged
    public async Task CreateSongWithUrlsAsync_ExternalIdentityHeldByAnotherSong_FailsWithMessage()
    {
        SetupArtistAndDuplicateCheck(1);
        _songRepoMock.Setup(r => r.ExistsByExternalIdentityAsync(
                         "deezer", "123", It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(true);
        var sut = CreateSut();

        var (success, message, song) = await sut.CreateSongWithUrlsAsync(
            1, null, "Bohemian Rhapsody", "", null, null, "123", "deezer", []);

        Assert.False(success);
        Assert.Equal("This music-database song is already in your library", message);
        Assert.Null(song);
        _songRepoMock.Verify(r => r.AddAsync(It.IsAny<Song>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
