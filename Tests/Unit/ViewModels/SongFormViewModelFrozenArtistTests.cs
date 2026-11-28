using CommunityToolkit.Mvvm.Messaging;
using MyVocaList.Contracts.DTOs.Suggestions;
using MyVocaList.Domain.Resolution;
using MyVocaList.Navigation;
using MyVocaList.Tests.Infrastructure;
using MyVocaList.UI.Services;

namespace MyVocaList.Tests.Unit.ViewModels;

/// <summary>
/// The Artist of a Deezer song is plain text and fixed (REQ-SLD-12..14, 17, 30): the frozen state of the form,
/// its release by Title or X, and what Save sends for the artist and the identity. Moq services, <c>TestImageStore.PassThrough</c>; no database, no <c>Shell.Current</c>.
/// </summary>
public class SongFormViewModelFrozenArtistTests
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
    private readonly List<SongFormSaveRequest> _requests = [];

    public SongFormViewModelFrozenArtistTests()
    {
        _suggestionsMock
            .Setup(s => s.SearchTitleLocalAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _suggestionsMock
            .Setup(s => s.SearchTitleRemoteAsync(It.IsAny<string>(), It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SongRemoteSearchResult([], [], RemoteSearchStatus.Ok));
        _urlServiceMock.Setup(s => s.GetUrlsForSongAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
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
            new Mock<ISharedVideoService>().Object,
            _navigationMock.Object,
            _suggestionsMock.Object,
            TestImageStore.PassThrough().Object,
            _pickerMock.Object,
            new Mock<ISongLyricsService>().Object);
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

    // ── REQ-SLD-12: the artist of a Deezer song is plain text ───────────

    // [AC] REQ-SLD-12: after a Deezer pick the Artist is plain text: frozen, not editable.
    [Fact]
    public async Task DeezerPick_ArtistIsFrozen()
    {
        var sut = CreateAddFormSut();

        await sut.SelectTitleCommand.ExecuteAsync(Pick(RemoteDto()));

        Assert.True(sut.IsArtistFrozen);
        Assert.False(sut.IsArtistEditable);
    }

    // [AC] REQ-SLD-12: an edit-mode Deezer song shows its Artist as plain text.
    [Fact]
    public async Task EditMode_DeezerSong_ArtistIsFrozen()
    {
        var sut = await CreateEditSutAsync(DeezerSong());

        Assert.True(sut.IsArtistFrozen);
        Assert.False(sut.IsArtistEditable);
    }

    // [AC] REQ-SLD-13: an add-mode form without a pick has an editable Artist.
    [Fact]
    public void AddMode_NoPick_ArtistIsEditable()
    {
        var sut = CreateAddFormSut();

        Assert.False(sut.IsArtistFrozen);
        Assert.True(sut.IsArtistEditable);
    }

    // [AC] REQ-SLD-13: an edit-mode local-only song has an editable Artist.
    [Fact]
    public async Task EditMode_LocalOnlySong_ArtistIsEditable()
    {
        var sut = await CreateEditSutAsync(LocalOnlySong());

        Assert.False(sut.IsArtistFrozen);
        Assert.True(sut.IsArtistEditable);
    }

    // ── REQ-SLD-14: releasing the Deezer pick releases the artist ───────

    // [AC] REQ-SLD-14: X on Title releases the artist: it becomes editable and keeps its text.
    [Fact]
    public async Task ClearTitle_AfterDeezerPick_ReleasesTheArtistAndKeepsItsText()
    {
        var sut = CreateAddFormSut();
        await sut.SelectTitleCommand.ExecuteAsync(Pick(RemoteDto()));

        sut.ClearTitleCommand.Execute(null);

        Assert.False(sut.IsArtistFrozen);
        Assert.True(sut.IsArtistEditable);
        Assert.Equal("Radiohead", sut.ArtistSearchText);
    }

    // [AC] REQ-SLD-14: changing the Title text away from the picked title releases the artist and keeps its text.
    [Fact]
    public async Task TitleTextChanged_AfterDeezerPick_ReleasesTheArtistAndKeepsItsText()
    {
        var sut = CreateAddFormSut();
        await sut.SelectTitleCommand.ExecuteAsync(Pick(RemoteDto()));

        sut.SongTitle = "Creep (Acoustic)";

        Assert.False(sut.IsArtistFrozen);
        Assert.True(sut.IsArtistEditable);
        Assert.Equal("Radiohead", sut.ArtistSearchText);
    }

    // [AC] REQ-SLD-14: a new Deezer pick after a release freezes the artist again.
    [Fact]
    public async Task NewDeezerPick_AfterRelease_FreezesAgain()
    {
        var sut = CreateAddFormSut();
        await sut.SelectTitleCommand.ExecuteAsync(Pick(RemoteDto()));
        sut.ClearTitleCommand.Execute(null);

        await sut.SelectTitleCommand.ExecuteAsync(Pick(RemoteDto("Creep", "Radiohead", externalId: "dz-2")));

        Assert.True(sut.IsArtistFrozen);
        Assert.False(sut.IsArtistEditable);
    }

    // [AC] REQ-SLD-14: the page is told when the pick freezes the artist and when a release unfreezes it.
    [Fact]
    public async Task IsArtistFrozen_RaisesPropertyChangedOnPickAndRelease()
    {
        var sut = CreateAddFormSut();
        var raised = new List<string?>();
        sut.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        await sut.SelectTitleCommand.ExecuteAsync(Pick(RemoteDto()));
        Assert.Contains(nameof(SongFormViewModel.IsArtistFrozen), raised);
        Assert.Contains(nameof(SongFormViewModel.IsArtistEditable), raised);

        raised.Clear();
        sut.ClearTitleCommand.Execute(null);
        Assert.Contains(nameof(SongFormViewModel.IsArtistFrozen), raised);
        Assert.Contains(nameof(SongFormViewModel.IsArtistEditable), raised);
    }

    // [AC] REQ-SLD-14: the form shows the artist as plain text exactly while a Save sends the Deezer identity.
    [Theory]
    [InlineData("pick")]
    [InlineData("title changed")]
    [InlineData("title cleared")]
    public async Task FrozenState_MatchesWhetherSaveSendsTheIdentity(string scenario)
    {
        var sut = CreateAddFormSut();
        await sut.SelectTitleCommand.ExecuteAsync(Pick(RemoteDto()));
        switch (scenario)
        {
            case "title changed": sut.SongTitle = "Creep (Acoustic)"; break;
            case "title cleared": sut.ClearTitleCommand.Execute(null); break;
        }

        await SaveAsync(sut);

        var request = Assert.Single(_requests);
        Assert.Equal(sut.IsArtistFrozen, request.ExternalId is not null);
    }

    // [AC] REQ-SLD-14: a pick that holds only one part of the Deezer identity is no identity: the Artist stays
    // editable and Save sends neither part.
    [Theory]
    [InlineData("Deezer", "")]
    [InlineData(null, "dz-1")]
    public async Task Save_PickWithOnlyOneIdentityPart_SendsNoIdentityAndArtistIsEditable(string? provider, string externalId)
    {
        var sut = CreateAddFormSut();
        await sut.SelectTitleCommand.ExecuteAsync(Pick(RemoteDto(externalId: externalId, provider: provider)));

        Assert.False(sut.IsArtistFrozen);
        Assert.True(sut.IsArtistEditable);

        await SaveAsync(sut);

        var request = Assert.Single(_requests);
        Assert.Null(request.ExternalProvider);
        Assert.Null(request.ExternalId);
    }

    // [AC] REQ-SLD-30: saving a Deezer song in edit mode sends the stored artist id, no artist text and no decision.
    [Fact]
    public async Task EditMode_DeezerSong_SaveSendsTheStoredArtistIdAndNoText()
    {
        var sut = await CreateEditSutAsync(DeezerSong(artistId: 7));

        await SaveAsync(sut);

        var request = Assert.Single(_requests);
        Assert.Equal(7, request.ArtistId);
        Assert.Null(request.ArtistText);
        Assert.IsType<ArtistDecision.None>(request.ArtistDecision);
    }

    // [AC] REQ-SLD-30: a catalog query artist does not replace the stored artist of a Deezer song after the load.
    [Fact]
    public async Task EditMode_DeezerSong_QueryArtistDoesNotReplaceTheStoredArtist()
    {
        var sut = await CreateEditSutAsync(DeezerSong(artistId: 7, artistName: "Queen"));
        sut.ArtistIdRaw = "99";
        sut.ArtistName = "Other";

        sut.InitializeArtistField();
        await SaveAsync(sut);

        var request = Assert.Single(_requests);
        Assert.Equal(7, request.ArtistId);
        Assert.Equal("Queen", sut.ArtistSearchText);
    }

    // [AC] REQ-SLD-30: a Title text change on a Deezer song keeps its Artist frozen and its stored id on Save.
    // [AC] REQ-SLD-38: the same save sends the Title as loaded, not the changed text.
    [Fact]
    public async Task EditMode_DeezerSong_TitleTextChangeKeepsTheArtistFrozenAndSavesTheLoadedTitle()
    {
        var sut = await CreateEditSutAsync(DeezerSong(artistId: 7));

        sut.SongTitle = "Bohemian Rhapsody (Remastered)";
        Assert.True(sut.IsArtistFrozen);
        await SaveAsync(sut);

        var request = Assert.Single(_requests);
        Assert.Equal(7, request.ArtistId);
        Assert.Equal("Bohemian Rhapsody", request.Title);
    }

    // [AC] REQ-SLD-12: in edit mode the Artist stays plain text until the stored song has loaded, so a Deezer song is never shown editable.
    [Fact]
    public async Task EditMode_BeforeTheSongHasLoaded_ArtistIsFrozen()
    {
        var load = new TaskCompletionSource<Song?>();
        var songService = SaveService();
        songService.Setup(s => s.GetSongByIdAsync(DeezerSong().Id, It.IsAny<CancellationToken>())).Returns(load.Task);
        var sut = CreateSut(songService);

        sut.SongIdRaw = DeezerSong().Id.ToString();
        await Task.Yield();

        Assert.True(sut.IsArtistFrozen);
        Assert.False(sut.IsArtistEditable);
    }

    // [AC] REQ-SLD-13: once a local-only song has loaded in edit mode its Artist is editable, and the change is raised.
    [Fact]
    public async Task EditMode_LocalOnlySongAfterLoad_ArtistBecomesEditableAndNotifies()
    {
        var load = new TaskCompletionSource<Song?>();
        var songService = SaveService();
        songService.Setup(s => s.GetSongByIdAsync(LocalOnlySong().Id, It.IsAny<CancellationToken>())).Returns(load.Task);
        var sut = CreateSut(songService);
        sut.SongIdRaw = LocalOnlySong().Id.ToString();
        await Task.Yield();

        var raised = new List<string?>();
        sut.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        load.SetResult(LocalOnlySong());
        for (var i = 0; i < 500 && !raised.Contains(nameof(SongFormViewModel.IsArtistEditable)); i++)
            await Task.Delay(10);

        Assert.Contains(nameof(SongFormViewModel.IsArtistEditable), raised);
        Assert.True(sut.IsArtistEditable);
        Assert.False(sut.IsArtistFrozen);
    }
}
