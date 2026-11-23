using MyVocaList.Domain.Resolution;
using MyVocaList.Infra.Similarity;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Unit.Services;

/// <summary>
/// The title of a song from the music database cannot change (REQ-SLD-37). Same fixture as
/// <c>SongServiceArtistFreezeTests</c>: Moq repositories and services, <c>PassthroughUnitOfWork.Over</c>, no
/// database. Titles are compared after the trimming every title gets, character by character (ordinal).
/// </summary>
public class SongServiceTitleFreezeTests
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
    private const int SavedSongIdValue = 99;
    private const string DeezerProvider = "Deezer";
    private const string DeezerTrackId = "3135556";
    private const string StoredTitle = "Old title";
    private const string FrozenTitleMessage = "The title of a song from the music database cannot be changed.";

    public SongServiceTitleFreezeTests()
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
        string title = StoredTitle,
        int? artistId = null,
        string? artistText = "Stored artist 3",
        string? provider = null,
        string? externalId = null,
        SongDecision songDecision = SongDecision.None)
        => new(
            songId, title, Lyrics: null,
            artistId, artistText, provider, externalId,
            PendingUrls: [],
            new ArtistDecision.None(),
            songDecision);

    // The Deezer song the edit form opens: stored with StoredArtistId, StoredTitle and a Deezer identity.
    private Song SetupStoredDeezerSong()
    {
        var stored = new Song
        {
            Id = 50, ArtistId = StoredArtistId, Title = StoredTitle,
            ExternalProvider = DeezerProvider, ExternalId = DeezerTrackId,
            OriginalArtist = new Artist { Id = StoredArtistId, Name = "Stored artist 3" }
        };
        _songRepoMock
            .Setup(r => r.GetByIdAsync(50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stored);
        return stored;
    }

    private void VerifySongNeverUpdated()
        => _songRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Song>(), It.IsAny<CancellationToken>()), Times.Never);

    private void AssertFrozenOnTitle(SongFormSaveOutcome outcome)
    {
        Assert.Equal(SongFormSaveKind.Failed, outcome.Kind);
        Assert.Equal(FrozenTitleMessage, outcome.Message);
        Assert.Equal(SaveField.Title, outcome.FailedField);
        Assert.Null(outcome.SavedSongId);
    }

    // [AC] REQ-SLD-37: a Deezer song saved with another title is refused on the title field, nothing stored.
    [Fact]
    public async Task SaveFromForm_DeezerSongTitleChanged_RefusedOnTitle()
    {
        var stored = SetupStoredDeezerSong();

        var outcome = await CreateSut().SaveFromFormAsync(NewRequest(
            songId: 50, title: "Renamed title", artistId: StoredArtistId));

        AssertFrozenOnTitle(outcome);
        VerifySongNeverUpdated();
        Assert.Equal(StoredTitle, stored.Title);
    }

    // [AC] REQ-SLD-37: the stored title sent back is accepted.
    [Fact]
    public async Task SaveFromForm_DeezerSongSameTitle_Saved()
    {
        SetupStoredDeezerSong();

        var outcome = await CreateSut().SaveFromFormAsync(NewRequest(
            songId: 50, title: StoredTitle, artistId: StoredArtistId));

        Assert.Equal(SongFormSaveKind.Saved, outcome.Kind);
        _songRepoMock.Verify(r => r.UpdateAsync(
            It.Is<Song>(s => s.Id == 50 && s.Title == StoredTitle && s.ExternalId == DeezerTrackId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // [AC] REQ-SLD-37: a title that differs from the stored one only by surrounding whitespace is the same title after trimming.
    [Fact]
    public async Task SaveFromForm_DeezerSongTitleDifferingOnlyBySurroundingWhitespace_Saved()
    {
        SetupStoredDeezerSong();

        var outcome = await CreateSut().SaveFromFormAsync(NewRequest(
            songId: 50, title: "  Old title  ", artistId: StoredArtistId));

        Assert.Equal(SongFormSaveKind.Saved, outcome.Kind);
        _songRepoMock.Verify(r => r.UpdateAsync(
            It.Is<Song>(s => s.Id == 50 && s.Title.Trim() == StoredTitle),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // [AC] REQ-SLD-37: a title that differs by letter case is a different title and is refused.
    [Fact]
    public async Task SaveFromForm_DeezerSongTitleDifferingByCase_RefusedOnTitle()
    {
        var stored = SetupStoredDeezerSong();

        var outcome = await CreateSut().SaveFromFormAsync(NewRequest(
            songId: 50, title: "old title", artistId: StoredArtistId));

        AssertFrozenOnTitle(outcome);
        VerifySongNeverUpdated();
        Assert.Equal(StoredTitle, stored.Title);
    }

    // [AC] REQ-SLD-37: UpdateSongAsync refuses a changed title on a Deezer song and changes nothing.
    [Fact]
    public async Task UpdateSongAsync_DeezerSongTitleChanged_Refused()
    {
        var stored = SetupStoredDeezerSong();

        var (success, message) = await CreateSut().UpdateSongAsync(
            50, "Renamed title", null, artistId: StoredArtistId, newArtistName: null);

        Assert.False(success);
        Assert.Equal(FrozenTitleMessage, message);
        Assert.Equal(StoredTitle, stored.Title);
        VerifySongNeverUpdated();
    }

    // [AC] REQ-SLD-37: a local-only song is renamed as today; the title freeze does not apply to it.
    [Fact]
    public async Task SaveFromForm_LocalOnlySongTitleChanged_Saved()
    {
        _songRepoMock
            .Setup(r => r.GetByIdAsync(60, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Song { Id = 60, ArtistId = StoredArtistId, Title = "Local" });

        var outcome = await CreateSut().SaveFromFormAsync(NewRequest(
            songId: 60, title: "Local renamed", artistId: StoredArtistId));

        Assert.Equal(SongFormSaveKind.Saved, outcome.Kind);
        _songRepoMock.Verify(r => r.UpdateAsync(
            It.Is<Song>(s => s.Id == 60 && s.Title == "Local renamed"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // [AC] REQ-SLD-37: "Save as new song" from a Deezer pick creates a song with the typed title; it is not an update.
    [Fact]
    public async Task SaveFromForm_SaveAsNewSongWithOtherTitle_CreatesSongWithTypedTitle()
    {
        var outcome = await CreateSut().SaveFromFormAsync(NewRequest(
            songId: null, title: "Creep", artistId: StoredArtistId, artistText: "Radiohead",
            provider: DeezerProvider, externalId: DeezerTrackId, songDecision: SongDecision.SaveAsNewSong));

        Assert.Equal(SongFormSaveKind.Saved, outcome.Kind);
        Assert.NotNull(AddedSong);
        Assert.Equal("Creep", AddedSong!.Title);
        Assert.Null(AddedSong.ExternalProvider);
        VerifySongNeverUpdated();
    }
}
