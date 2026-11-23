using MyVocaList.Contracts.DTOs;
using MyVocaList.Contracts.Enums;
using MyVocaList.Domain.Resolution;
using MyVocaList.Infra.Similarity;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Unit.Services;

/// <summary>
/// Where stored lyrics came from (REQ-SLD-18, 26, 27): the origin is decided by the service on Save, in add mode
/// from the held pick's lookup and in edit mode from the lyrics stored at that moment. Same fixture as
/// <c>SongServiceSaveFromFormTests</c>: Moq repositories, <c>PassthroughUnitOfWork.Over</c>, no database.
/// </summary>
public class SongServiceLyricsOriginTests
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

    private const int StoredArtistId = 7;
    private const int StoredSongId = 50;
    private const int SavedSongIdValue = 99;
    private const string DeezerProvider = "Deezer";
    private const string DeezerTrackId = "3135556";

    public SongServiceLyricsOriginTests()
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

    private static SongFormSaveRequest Request(
        int? songId = null,
        int? artistId = StoredArtistId,
        string? lyrics = null,
        string? provider = null,
        string? externalId = null,
        SongDecision songDecision = SongDecision.None,
        LyricsLookupResult? pickLookup = null)
        => new(
            songId, "Don't Cry", Lyrics: lyrics,
            artistId, "Guns N' Roses", provider, externalId,
            PendingUrls: [],
            new ArtistDecision.None(),
            songDecision,
            PickLyricsLookup: pickLookup);

    // The song the edit form opens: stored with its lyrics and origin; not a Deezer song unless asked.
    private Song SetupStoredSong(string? lyrics, LyricsOrigin? origin = null, string? provider = null, string? externalId = null)
    {
        var stored = new Song
        {
            Id = StoredSongId,
            ArtistId = StoredArtistId,
            Title = "Old title",
            Lyrics = lyrics,
            LyricsOrigin = origin,
            ExternalProvider = provider,
            ExternalId = externalId,
            OriginalArtist = new Artist { Id = StoredArtistId, Name = "Stored artist 7" }
        };
        _songRepoMock
            .Setup(r => r.GetByIdAsync(StoredSongId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stored);
        return stored;
    }

    [Fact]
    // [AC] REQ-SLD-26: add mode, saved lyrics equal to the Found text of the held pick's lookup give origin lyrics source.
    public async Task SaveFromForm_AddLyricsEqualThePickLookup_OriginLyricsSource()
    {
        var outcome = await CreateSut().SaveFromFormAsync(
            Request(lyrics: "La la la", pickLookup: LyricsLookupResult.Found("La la la")));

        Assert.Equal(SongFormSaveKind.Saved, outcome.Kind);
        Assert.Equal(LyricsOrigin.LyricsSource, AddedSong!.LyricsOrigin);
    }

    [Fact]
    // [AC] REQ-SLD-26: add mode, non-blank lyrics that do not come from the pick lookup give origin user.
    public async Task SaveFromForm_AddTypedLyrics_OriginUser()
    {
        var outcome = await CreateSut().SaveFromFormAsync(Request(lyrics: "Typed words"));

        Assert.Equal(SongFormSaveKind.Saved, outcome.Kind);
        Assert.Equal(LyricsOrigin.User, AddedSong!.LyricsOrigin);
    }

    [Fact]
    // [AC] REQ-SLD-26: add mode, blank lyrics and a pick lookup with a definite no match give origin none found.
    public async Task SaveFromForm_AddBlankLyricsAndNoMatch_OriginNoneFound()
    {
        var outcome = await CreateSut().SaveFromFormAsync(
            Request(lyrics: null, pickLookup: LyricsLookupResult.NoMatch));

        Assert.Equal(SongFormSaveKind.Saved, outcome.Kind);
        Assert.Equal(LyricsOrigin.NoneFound, AddedSong!.LyricsOrigin);
    }

    [Fact]
    // [AC] REQ-SLD-26: add mode, blank lyrics and no lookup (no answer or no pick) leave the origin not recorded.
    public async Task SaveFromForm_AddBlankLyricsAndNoLookup_OriginNotRecorded()
    {
        var outcome = await CreateSut().SaveFromFormAsync(
            Request(lyrics: null, pickLookup: LyricsLookupResult.NoAnswer));

        Assert.Equal(SongFormSaveKind.Saved, outcome.Kind);
        Assert.Null(AddedSong!.LyricsOrigin);
    }

    [Fact]
    // [AC] REQ-SLD-18: "Save as new song" drops the Deezer identity; the origin is still derived from the lyrics.
    public async Task SaveFromForm_SaveAsNewSongWithPickLookup_OriginStillDerived()
    {
        var outcome = await CreateSut().SaveFromFormAsync(Request(
            provider: DeezerProvider, externalId: DeezerTrackId, songDecision: SongDecision.SaveAsNewSong,
            lyrics: "La la la", pickLookup: LyricsLookupResult.Found("La la la")));

        Assert.Equal(SongFormSaveKind.Saved, outcome.Kind);
        Assert.Null(AddedSong!.ExternalId);
        Assert.Equal(LyricsOrigin.LyricsSource, AddedSong.LyricsOrigin);
    }

    [Fact]
    // [AC] REQ-SLD-27: edit mode, saved lyrics differ from the stored ones give origin user.
    public async Task SaveFromForm_EditLyricsChanged_OriginUser()
    {
        var stored = SetupStoredSong("Old lyrics", LyricsOrigin.LyricsSource);

        var outcome = await CreateSut().SaveFromFormAsync(Request(songId: StoredSongId, lyrics: "New lyrics"));

        Assert.Equal(SongFormSaveKind.Saved, outcome.Kind);
        Assert.Equal(LyricsOrigin.User, stored.LyricsOrigin);
    }

    [Fact]
    // [AC] REQ-SLD-27: edit mode, emptying the stored lyrics gives origin user (an emptied field is the user's choice).
    public async Task SaveFromForm_EditLyricsEmptied_OriginUser()
    {
        var stored = SetupStoredSong("Old lyrics", LyricsOrigin.LyricsSource);

        var outcome = await CreateSut().SaveFromFormAsync(Request(songId: StoredSongId, lyrics: null));

        Assert.Equal(SongFormSaveKind.Saved, outcome.Kind);
        Assert.Equal(LyricsOrigin.User, stored.LyricsOrigin);
    }

    [Theory]
    // [AC] REQ-SLD-27: edit mode, unchanged lyrics keep the stored origin whatever it is.
    [InlineData(null)]
    [InlineData(LyricsOrigin.LyricsSource)]
    [InlineData(LyricsOrigin.NoneFound)]
    public async Task SaveFromForm_EditLyricsUnchanged_OriginKept(LyricsOrigin? storedOrigin)
    {
        var stored = SetupStoredSong("Same lyrics", storedOrigin);

        var outcome = await CreateSut().SaveFromFormAsync(Request(songId: StoredSongId, lyrics: "Same lyrics"));

        Assert.Equal(SongFormSaveKind.Saved, outcome.Kind);
        Assert.Equal(storedOrigin, stored.LyricsOrigin);
    }

    [Fact]
    // [AC] REQ-SLD-27: UpdateSongAsync with changed lyrics gives origin user.
    public async Task UpdateSongAsync_LyricsChanged_OriginUser()
    {
        var stored = SetupStoredSong("Old lyrics", LyricsOrigin.LyricsSource);

        var (success, _) = await CreateSut().UpdateSongAsync(
            StoredSongId, "Old title", "New lyrics");

        Assert.True(success);
        Assert.Equal(LyricsOrigin.User, stored.LyricsOrigin);
    }
}
