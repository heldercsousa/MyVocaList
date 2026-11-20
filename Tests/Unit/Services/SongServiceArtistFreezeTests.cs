using MyVocaList.Domain.Resolution;
using MyVocaList.Infra.Similarity;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Unit.Services;

/// <summary>
/// The artist of a song from the music database cannot change (REQ-SLD-13, 15 to 18, 30). Same fixture as
/// <c>SongServiceSaveFromFormTests</c>: Moq repositories and services, <c>PassthroughUnitOfWork.Over</c>, no
/// database. Artists are compared by id only, never by name text.
/// </summary>
public class SongServiceArtistFreezeTests
{
    private readonly Mock<ISongRepository> _songRepoMock = new();
    private readonly Mock<ICatalogRepository> _catalogRepoMock = new();
    private readonly Mock<IArtistRepository> _artistRepoMock = new();
    private readonly Mock<ISongKaraokeUrlRepository> _urlRepoMock = new();
    private readonly Mock<ISongKaraokeUrlService> _urlServiceMock = new();
    private readonly Mock<IArtistService> _artistServiceMock = new();
    private readonly Mock<IArtistResolutionService> _artistResolutionMock = new();
    private readonly Mock<ISongResolutionService> _songResolutionMock = new();
    private readonly Mock<ILogger<SongService>> _loggerMock = new();

    private const int StoredArtistId = 3;
    private const int OtherArtistId = 4;
    private const int SavedSongIdValue = 99;
    private const string DeezerProvider = "Deezer";
    private const string DeezerTrackId = "3135556";
    private const string FrozenMessage = "The artist of a song from the music database cannot be changed.";

    public SongServiceArtistFreezeTests()
    {
        _artistServiceMock
            .Setup(a => a.ValidateNameInput(It.IsAny<string>()))
            .Returns((true, string.Empty));
        _artistServiceMock
            .Setup(a => a.CreateArtistAsync(
                It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string name, string? _, string? __, CancellationToken ___) =>
                (true, string.Empty, (Artist?)new Artist { Id = 12, Name = name }));

        _artistResolutionMock
            .Setup(r => r.ResolveAsync(It.IsAny<ArtistCandidate>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ArtistResolution(ResolutionKind.NoMatch, null, []));
        _songResolutionMock
            .Setup(r => r.ResolveAsync(It.IsAny<SongCandidate>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SongResolution(ResolutionKind.NoMatch, null, []));

        _artistRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) => new Artist { Id = id, Name = $"Stored artist {id}" });

        _songRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Song>(), It.IsAny<CancellationToken>()))
            .Callback<Song, CancellationToken>((song, _) =>
            {
                song.Id = SavedSongIdValue;
                AddedSong = song;
            })
            .Returns(Task.CompletedTask);
    }

    private Song? AddedSong { get; set; }

    private SongService CreateSut() => new(
        _songRepoMock.Object,
        _artistRepoMock.Object,
        _urlRepoMock.Object,
        _urlServiceMock.Object,
        PassthroughUnitOfWork.Over(
            _songRepoMock, _artistRepoMock, _urlRepoMock, _urlServiceMock, _catalogRepoMock.Object, _artistServiceMock,
            _artistResolutionMock.Object, _songResolutionMock),
        _loggerMock.Object);

    private static SongFormSaveRequest NewRequest(
        int? songId = null,
        string title = "Don't Cry",
        int? artistId = null,
        string? artistText = "Guns N' Roses",
        string? provider = null,
        string? externalId = null,
        ArtistDecision? artistDecision = null,
        SongDecision songDecision = SongDecision.None)
        => new(
            songId, title, Lyrics: null,
            artistId, artistText, provider, externalId,
            PendingUrls: [],
            artistDecision ?? new ArtistDecision.None(),
            songDecision);

    // The Deezer song the edit form opens: stored with StoredArtistId and a Deezer identity.
    private Song SetupStoredDeezerSong()
    {
        var stored = new Song
        {
            Id = 50, ArtistId = StoredArtistId, Title = "Old title",
            ExternalProvider = DeezerProvider, ExternalId = DeezerTrackId,
            OriginalArtist = new Artist { Id = StoredArtistId, Name = "Stored artist 3" }
        };
        _songRepoMock
            .Setup(r => r.GetByIdAsync(50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stored);
        return stored;
    }

    private void VerifyArtistWorkNeverRun()
    {
        _artistResolutionMock.Verify(
            r => r.ResolveAsync(It.IsAny<ArtistCandidate>(), It.IsAny<CancellationToken>()), Times.Never);
        _artistServiceMock.Verify(a => a.CreateArtistAsync(
            It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        _artistRepoMock.Verify(r => r.AddAsync(It.IsAny<Artist>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private void VerifySongNeverUpdated()
        => _songRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Song>(), It.IsAny<CancellationToken>()), Times.Never);

    private void AssertFrozenOnArtist(SongFormSaveOutcome outcome)
    {
        Assert.Equal(SongFormSaveKind.Failed, outcome.Kind);
        Assert.Equal(FrozenMessage, outcome.Message);
        Assert.Equal(SaveField.Artist, outcome.FailedField);
        Assert.Null(outcome.SavedSongId);
    }

    // [AC] REQ-SLD-15: a Deezer song saved with another artist id is refused on the artist, nothing stored.
    [Fact]
    public async Task SaveFromForm_DeezerSongOtherArtistId_RefusedOnArtist()
    {
        SetupStoredDeezerSong();

        var outcome = await CreateSut().SaveFromFormAsync(NewRequest(
            songId: 50, title: "Old title", artistId: OtherArtistId, artistText: "Stored artist 3"));

        AssertFrozenOnArtist(outcome);
        VerifySongNeverUpdated();
    }

    // [AC] REQ-SLD-15: a typed artist name on a Deezer song is refused before any artist resolution or creation.
    [Fact]
    public async Task SaveFromForm_DeezerSongTypedArtistName_RefusedBeforeAnyResolution()
    {
        SetupStoredDeezerSong();

        var outcome = await CreateSut().SaveFromFormAsync(NewRequest(
            songId: 50, title: "Old title", artistId: null, artistText: "Some Other Band"));

        AssertFrozenOnArtist(outcome);
        VerifyArtistWorkNeverRun();
        VerifySongNeverUpdated();
    }

    // [AC] REQ-SLD-15: a create-typed decision on a Deezer song is refused, whatever name it carries.
    [Fact]
    public async Task SaveFromForm_DeezerSongCreateTypedDecision_Refused()
    {
        SetupStoredDeezerSong();

        var outcome = await CreateSut().SaveFromFormAsync(NewRequest(
            songId: 50, title: "Old title", artistId: null, artistText: "Some Other Band",
            artistDecision: new ArtistDecision.CreateTyped()));

        AssertFrozenOnArtist(outcome);
        VerifyArtistWorkNeverRun();
        VerifySongNeverUpdated();
    }

    // [AC] REQ-SLD-15: the artist is compared by id only; a typed name equal to the stored artist's name is still refused.
    [Fact]
    public async Task SaveFromForm_DeezerSongTypedNameOfTheStoredArtist_StillRefused()
    {
        SetupStoredDeezerSong();

        var outcome = await CreateSut().SaveFromFormAsync(NewRequest(
            songId: 50, title: "Old title", artistId: null, artistText: "Stored artist 3"));

        AssertFrozenOnArtist(outcome);
        VerifyArtistWorkNeverRun();
        VerifySongNeverUpdated();
    }

    // [AC] REQ-SLD-15: UpdateSongAsync refuses an artist id that differs, or a new artist name, on a Deezer song.
    [Theory]
    [InlineData(OtherArtistId, null)]
    [InlineData(null, "New Artist")]
    public async Task UpdateSongAsync_DeezerSongOtherArtistIdOrNewName_Refused(int? artistId, string? newArtistName)
    {
        var stored = SetupStoredDeezerSong();

        var (success, message) = await CreateSut().UpdateSongAsync(
            50, "Changed title", null,
            artistId: artistId, newArtistName: newArtistName);

        Assert.False(success);
        Assert.Equal(FrozenMessage, message);
        Assert.Equal("Old title", stored.Title);
        Assert.Equal(StoredArtistId, stored.ArtistId);
        VerifySongNeverUpdated();
    }

    // [AC] REQ-SLD-30: a Deezer song saved with its stored artist id keeps that artist, with no artist resolution.
    [Fact]
    public async Task SaveFromForm_DeezerSongStoredArtistId_SavesWithoutArtistResolution()
    {
        SetupStoredDeezerSong();

        var outcome = await CreateSut().SaveFromFormAsync(NewRequest(
            songId: 50, title: "Old title", artistId: StoredArtistId, artistText: "Anything typed"));

        Assert.Equal(SongFormSaveKind.Saved, outcome.Kind);
        VerifyArtistWorkNeverRun();
        _artistServiceMock.Verify(a => a.ValidateNameInput(It.IsAny<string>()), Times.Never);
        _songRepoMock.Verify(r => r.UpdateAsync(
            It.Is<Song>(s => s.Id == 50 && s.ArtistId == StoredArtistId && s.Title == "Old title"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // [AC] REQ-SLD-16: adding a Deezer song with a chosen existing artist stores the Deezer id with that artist.
    [Fact]
    public async Task SaveFromForm_AddModeUseExistingArtist_StoresDeezerSongWithThatArtist()
    {
        var outcome = await CreateSut().SaveFromFormAsync(NewRequest(
            songId: null, title: "Creep", artistId: null, artistText: "Radiohead",
            provider: DeezerProvider, externalId: DeezerTrackId,
            artistDecision: new ArtistDecision.UseExisting(OtherArtistId)));

        Assert.Equal(SongFormSaveKind.Saved, outcome.Kind);
        Assert.NotNull(AddedSong);
        Assert.Equal(DeezerTrackId, AddedSong!.ExternalId);
        Assert.Equal(OtherArtistId, AddedSong.ArtistId);
    }

    // [AC] REQ-SLD-18: a save as a new song from a Deezer pick stores no external identity.
    [Theory]
    [InlineData(SongDecision.SaveAsNewSong)]
    public async Task SaveFromForm_SaveAsNewSong_StoresNoIdentity(SongDecision decision)
    {
        var outcome = await CreateSut().SaveFromFormAsync(NewRequest(
            songId: null, title: "Creep", artistId: StoredArtistId, artistText: "Radiohead",
            provider: DeezerProvider, externalId: DeezerTrackId, songDecision: decision));

        Assert.Equal(SongFormSaveKind.Saved, outcome.Kind);
        Assert.NotNull(AddedSong);
        Assert.Null(AddedSong!.ExternalId);
        Assert.Null(AddedSong.ExternalProvider);
    }

    // [AC] REQ-SLD-13: the artist of a local-only song stays editable.
    [Fact]
    public async Task SaveFromForm_LocalOnlySongOtherArtist_Saved()
    {
        _songRepoMock
            .Setup(r => r.GetByIdAsync(60, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Song { Id = 60, ArtistId = StoredArtistId, Title = "Local" });

        var outcome = await CreateSut().SaveFromFormAsync(NewRequest(
            songId: 60, title: "Local", artistId: OtherArtistId, artistText: "Stored artist 4"));

        Assert.Equal(SongFormSaveKind.Saved, outcome.Kind);
        _songRepoMock.Verify(r => r.UpdateAsync(
            It.Is<Song>(s => s.Id == 60 && s.ArtistId == OtherArtistId),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
