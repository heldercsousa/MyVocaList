using CommunityToolkit.Mvvm.Messaging;
using MyVocaList.Contracts.DTOs;
using MyVocaList.Contracts.DTOs.Suggestions;
using MyVocaList.Domain.Resolution;
using MyVocaList.Navigation;
using MyVocaList.Tests.Infrastructure;
using MyVocaList.UI.Services;

namespace MyVocaList.Tests.Unit.ViewModels;

/// <summary>
/// The Title of a Deezer song is plain text in edit mode (REQ-SLD-36), the edit save sends the loaded title
/// (REQ-SLD-38), and a changed Title releases the Deezer pick for good in add mode (REQ-SLD-39, closes C25).
/// Moq services, <c>TestImageStore.PassThrough</c>; no database, no <c>Shell.Current</c>.
/// </summary>
public class SongFormViewModelFrozenTitleTests
{
    private const string Cover = "https://img.test/cover.jpg";
    private const string ArtistPic = "https://img.test/artist.jpg";
    private const string DeezerProvider = "Deezer";
    private const string DeezerId = "dz-1";

    private readonly Mock<INavigationService> _navigationMock = new();
    private readonly Mock<ISongSuggestionService> _suggestionsMock = new();
    private readonly Mock<ISnackbarComponent> _snackbarMock = new();
    private readonly Mock<IPhotoPicker> _pickerMock = new();
    private readonly Mock<ISongKaraokeUrlService> _urlServiceMock = new();
    private readonly Mock<ISongLyricsService> _lyricsMock = new();
    private readonly List<SongFormSaveRequest> _requests = [];

    public SongFormViewModelFrozenTitleTests()
    {
        _suggestionsMock
            .Setup(s => s.SearchTitleLocalAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _suggestionsMock
            .Setup(s => s.SearchTitleRemoteAsync(It.IsAny<string>(), It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SongRemoteSearchResult([], [], RemoteSearchStatus.Ok));
        _urlServiceMock.Setup(s => s.GetUrlsForSongAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _lyricsMock.Setup(s => s.LookupLyricsAsync(It.IsAny<LyricsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(LyricsLookupResult.NoAnswer);
    }

    // ── Factory and helpers ──────────────────────────────────────────────

    private SongFormViewModel CreateSut(Mock<ISongService>? songService = null)
    {
        var sut = new SongFormViewModel(
            new Mock<IArtistService>().Object,
            (songService ?? SaveService()).Object,
            _snackbarMock.Object,
            new Mock<ILogger<SongFormViewModel>>().Object,
            _urlServiceMock.Object,
            new Mock<ISharedLinkInbox>().Object,
            _navigationMock.Object,
            _suggestionsMock.Object,
            TestImageStore.PassThrough().Object,
            _pickerMock.Object,
            _lyricsMock.Object);
        sut.CompleteHydration();
        return sut;
    }

    /// <summary>An add-form VM past hydration (Shell has applied the query properties).</summary>
    private SongFormViewModel CreateAddFormSut() => CreateSut();

    /// <summary>An edit-form VM with the given stored song loaded.</summary>
    private async Task<SongFormViewModel> CreateEditSutAsync(Song song)
    {
        var songService = SaveService();
        songService.Setup(s => s.GetSongByIdAsync(song.Id, It.IsAny<CancellationToken>())).ReturnsAsync(song);
        var sut = CreateSut(songService);
        sut.SongIdRaw = song.Id.ToString();
        await WaitUntilAsync(() => !sut.IsSongLoading);
        return sut;
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        for (var i = 0; i < 500 && !condition(); i++)
            await Task.Delay(10);
    }

    private static Song DeezerSong(int artistId = 7, string artistName = "Queen") => new()
    {
        Id = 42,
        ArtistId = artistId,
        OriginalArtist = new Artist { Id = artistId, Name = artistName },
        Title = "Bohemian Rhapsody",
        ExternalProvider = DeezerProvider,
        ExternalId = "3135556"
    };

    private static Song LocalOnlySong() => new()
    {
        Id = 43,
        ArtistId = 8,
        OriginalArtist = new Artist { Id = 8, Name = "Ramones" },
        Title = "Blitzkrieg Bop",
    };

    private Mock<ISongService> SaveService()
    {
        var songService = new Mock<ISongService>();
        songService.Setup(s => s.ValidateTitleInput(It.IsAny<string>())).Returns((true, string.Empty));
        songService.Setup(s => s.SaveFromFormAsync(It.IsAny<SongFormSaveRequest>(), It.IsAny<CancellationToken>()))
            .Returns((SongFormSaveRequest r, CancellationToken _) =>
            {
                _requests.Add(r);
                return Task.FromResult(new SongFormSaveOutcome(SongFormSaveKind.Saved, "Song saved", SaveField.None, 5, [], false, []));
            });
        return songService;
    }

    private static SongSuggestionDto RemoteDto(
        string title = "Creep", string artist = "Radiohead",
        string? externalId = DeezerId, string? provider = DeezerProvider,
        int? localArtistId = null, string? localArtistName = null) =>
        new(null, title, artist, localArtistId, externalId, provider, true, localArtistName, Cover, ArtistPic, null);

    private static AutocompleteSuggestion Pick(SongSuggestionDto dto) => new(dto.Title, string.Empty, dto);

    private static Task SaveAsync(SongFormViewModel sut) => sut.SaveCommand.ExecuteAsync(null);

    // ── REQ-SLD-36: the Title of a stored Deezer song is plain text ──────

    // [AC] REQ-SLD-36: an edit-mode Deezer song shows its Title as plain text: frozen, not editable.
    [Fact]
    public async Task EditMode_DeezerSong_TitleIsFrozen()
    {
        var sut = await CreateEditSutAsync(DeezerSong());

        Assert.True(sut.IsTitleFrozen);
        Assert.False(sut.IsTitleEditable);
    }

    // [AC] REQ-SLD-36: the Title of a local-only song stays editable.
    [Fact]
    public async Task EditMode_LocalOnlySong_TitleIsEditable()
    {
        var sut = await CreateEditSutAsync(LocalOnlySong());

        Assert.False(sut.IsTitleFrozen);
        Assert.True(sut.IsTitleEditable);
    }

    // [AC] REQ-SLD-36: until the stored song has loaded the Title is plain text, so a Deezer song is never shown editable.
    [Fact]
    public async Task EditMode_BeforeTheSongHasLoaded_TitleIsFrozen()
    {
        var load = new TaskCompletionSource<Song?>();
        var songService = SaveService();
        songService.Setup(s => s.GetSongByIdAsync(DeezerSong().Id, It.IsAny<CancellationToken>())).Returns(load.Task);
        var sut = CreateSut(songService);

        sut.SongIdRaw = DeezerSong().Id.ToString();
        await Task.Yield();

        Assert.True(sut.IsTitleFrozen);
        Assert.False(sut.IsTitleEditable);
    }

    // [AC] REQ-SLD-36: in add mode the Title stays editable after a Deezer pick (the pick does not freeze it).
    [Fact]
    public async Task AddMode_AfterDeezerPick_TitleIsEditable()
    {
        var sut = CreateAddFormSut();

        await sut.SelectTitleCommand.ExecuteAsync(Pick(RemoteDto()));

        Assert.False(sut.IsTitleFrozen);
        Assert.True(sut.IsTitleEditable);
    }

    // ── REQ-SLD-38: a frozen Title is saved as loaded ────────────────────

    // [AC] REQ-SLD-38: saving a Deezer song in edit mode sends the title it loaded, whatever the Title text holds.
    [Fact]
    public async Task EditMode_DeezerSong_SaveSendsTheLoadedTitle()
    {
        var sut = await CreateEditSutAsync(DeezerSong());
        sut.SongTitle = "Changed in the text";

        await SaveAsync(sut);

        var request = Assert.Single(_requests);
        Assert.Equal("Bohemian Rhapsody", request.Title);
    }

    // ── REQ-SLD-39: a changed Title releases the Deezer pick for good ────

    // [AC] REQ-SLD-39: typing the picked title back after a change does not restore the pick: Save sends no external identity.
    [Fact]
    public async Task AddMode_TitleChangedThenTypedBack_SaveSendsNoExternalIdentity()
    {
        var sut = CreateAddFormSut();
        await sut.SelectTitleCommand.ExecuteAsync(Pick(RemoteDto()));

        sut.SongTitle = "Creep (Acoustic)";
        sut.SongTitle = "Creep";
        await SaveAsync(sut);

        var request = Assert.Single(_requests);
        Assert.Null(request.ExternalProvider);
        Assert.Null(request.ExternalId);
    }

    // [AC] REQ-SLD-39: typing the picked title back after a change leaves the Artist editable and keeps its text.
    [Fact]
    public async Task AddMode_TitleChangedThenTypedBack_ArtistIsEditableAndKeepsItsText()
    {
        var sut = CreateAddFormSut();
        await sut.SelectTitleCommand.ExecuteAsync(Pick(RemoteDto()));

        sut.SongTitle = "Creep (Acoustic)";
        sut.SongTitle = "Creep";

        Assert.False(sut.IsArtistFrozen);
        Assert.True(sut.IsArtistEditable);
        Assert.Equal("Radiohead", sut.ArtistSearchText);
    }

    // [AC] REQ-SLD-39: a pick whose Title is not changed keeps the external identity on Save (guard).
    [Fact]
    public async Task AddMode_PickWithUnchangedTitle_SaveKeepsTheIdentity()
    {
        var sut = CreateAddFormSut();
        await sut.SelectTitleCommand.ExecuteAsync(Pick(RemoteDto()));

        await SaveAsync(sut);

        var request = Assert.Single(_requests);
        Assert.Equal(DeezerProvider, request.ExternalProvider);
        Assert.Equal(DeezerId, request.ExternalId);
    }

    // [AC] REQ-SLD-39: a changed Title keeps the lyrics outcome of the pick lookup (REQ-SLD-26).
    [Fact]
    public async Task AddMode_TitleChangedAfterPick_SaveKeepsThePickLyricsOutcome()
    {
        _lyricsMock.Setup(s => s.LookupLyricsAsync(It.IsAny<LyricsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(LyricsLookupResult.Found("Lyrics of the match"));
        var sut = CreateAddFormSut();
        await sut.SelectTitleCommand.ExecuteAsync(Pick(RemoteDto()));
        await sut.LyricsLookupTask;

        sut.SongTitle = "Creep (Acoustic)";
        await SaveAsync(sut);

        var request = Assert.Single(_requests);
        Assert.Equal(LyricsLookupStatus.Found, request.PickLyricsLookup?.Status);
    }
}
