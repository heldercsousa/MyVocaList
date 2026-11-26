using CommunityToolkit.Mvvm.Messaging;
using MyVocaList.Contracts.DTOs.Suggestions;
using MyVocaList.Domain.Resolution;
using MyVocaList.Navigation;
using MyVocaList.Tests.Infrastructure;
using MyVocaList.UI.Services;

namespace MyVocaList.Tests.Unit.ViewModels;

/// <summary>
/// The edit-mode load of the Song form (REQ-SLD-32..34): the loading flag, Save while the song loads, and a
/// failed or missing song (message once, navigation back, nothing saved). Moq services, <c>TestImageStore.PassThrough</c>;
/// no database, no <c>Shell.Current</c>.
/// </summary>
public class SongFormViewModelLoadingTests
{
    private const string FailedLoadMessage = "This song could not be loaded.";

    private readonly Mock<INavigationService> _navigationMock = new();
    private readonly Mock<ISongSuggestionService> _suggestionsMock = new();
    private readonly Mock<ISnackbarComponent> _snackbarMock = new();
    private readonly Mock<IPhotoPicker> _pickerMock = new();
    private readonly Mock<ISongKaraokeUrlService> _urlServiceMock = new();

    public SongFormViewModelLoadingTests()
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

    private SongFormViewModel CreateSut(Mock<ISongService> songService)
    {
        var sut = new SongFormViewModel(
            new Mock<IArtistService>().Object,
            songService.Object,
            _snackbarMock.Object,
            new Mock<ILogger<SongFormViewModel>>().Object,
            _urlServiceMock.Object,
            new Mock<IMessenger>().Object,
            _navigationMock.Object,
            _suggestionsMock.Object,
            TestImageStore.PassThrough().Object,
            _pickerMock.Object,
            new Mock<ISongLyricsService>().Object);
        sut.CompleteHydration();
        return sut;
    }

    private Mock<ISongService> SongService()
    {
        var songService = new Mock<ISongService>();
        songService.Setup(s => s.ValidateTitleInput(It.IsAny<string>())).Returns((true, string.Empty));
        songService.Setup(s => s.SaveFromFormAsync(It.IsAny<SongFormSaveRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SongFormSaveOutcome(SongFormSaveKind.Saved, "Song saved", SaveField.None, 5, [], false, []));
        return songService;
    }

    private static Song LocalOnlySong() => new()
    {
        Id = 43,
        ArtistId = 8,
        OriginalArtist = new Artist { Id = 8, Name = "Ramones" },
        Title = "Blitzkrieg Bop",
    };

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        for (var i = 0; i < 500 && !condition(); i++)
            await Task.Delay(10);
    }

    // ── REQ-SLD-32: loading indicator ────────────────────────────────────

    // [AC] REQ-SLD-32: while a stored song loads in edit mode, the form reports that it is loading.
    [Fact]
    public async Task EditMode_WhileTheSongLoads_IsSongLoadingIsTrue()
    {
        var load = new TaskCompletionSource<Song?>();
        var songService = SongService();
        songService.Setup(s => s.GetSongByIdAsync(LocalOnlySong().Id, It.IsAny<CancellationToken>())).Returns(load.Task);
        var sut = CreateSut(songService);

        sut.SongIdRaw = LocalOnlySong().Id.ToString();
        await Task.Yield();

        Assert.True(sut.IsSongLoading);
    }

    // [AC] REQ-SLD-32: when the stored song has loaded, the loading flag is cleared and the change is raised.
    [Fact]
    public async Task EditMode_AfterTheLoad_IsSongLoadingIsFalseAndNotifies()
    {
        var load = new TaskCompletionSource<Song?>();
        var songService = SongService();
        songService.Setup(s => s.GetSongByIdAsync(LocalOnlySong().Id, It.IsAny<CancellationToken>())).Returns(load.Task);
        var sut = CreateSut(songService);
        sut.SongIdRaw = LocalOnlySong().Id.ToString();
        await Task.Yield();

        var raised = new List<string?>();
        sut.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        load.SetResult(LocalOnlySong());
        await WaitUntilAsync(() => raised.Contains(nameof(SongFormViewModel.IsSongLoading)));

        Assert.Contains(nameof(SongFormViewModel.IsSongLoading), raised);
        Assert.False(sut.IsSongLoading);
    }

    // [AC] REQ-SLD-32: add mode shows no loading placeholder.
    [Fact]
    public void AddMode_IsSongLoadingIsFalse()
    {
        var sut = CreateSut(SongService());

        Assert.False(sut.IsSongLoading);
    }

    // ── REQ-SLD-33: Save does nothing while the song loads ───────────────

    // [AC] REQ-SLD-33: Save while the stored song loads does not start a save.
    [Fact]
    public async Task Save_WhileTheSongLoads_DoesNothing()
    {
        var load = new TaskCompletionSource<Song?>();
        var songService = SongService();
        songService.Setup(s => s.GetSongByIdAsync(LocalOnlySong().Id, It.IsAny<CancellationToken>())).Returns(load.Task);
        var sut = CreateSut(songService);
        sut.SongIdRaw = LocalOnlySong().Id.ToString();
        await Task.Yield();

        await sut.SaveCommand.ExecuteAsync(null);

        songService.Verify(s => s.SaveFromFormAsync(It.IsAny<SongFormSaveRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── REQ-SLD-34: a failed load closes the form with a message ─────────

    // [AC] REQ-SLD-34: when loading the stored song throws, the form shows the message once, navigates back once,
    // clears the loading flag, and neither saves nor looks up lyrics.
    [Fact]
    public async Task EditMode_LoadThrows_ShowsTheMessageAndNavigatesBack()
    {
        var songService = SongService();
        songService.Setup(s => s.GetSongByIdAsync(LocalOnlySong().Id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("database unavailable"));
        var sut = CreateSut(songService);

        sut.SongIdRaw = LocalOnlySong().Id.ToString();
        await WaitUntilAsync(() => _navigationMock.Invocations.Count > 0);

        _snackbarMock.Verify(s => s.ShowErrorAsync(FailedLoadMessage), Times.Once);
        _navigationMock.Verify(n => n.GoToAsync(".."), Times.Once);
        Assert.False(sut.IsSongLoading);
        songService.Verify(s => s.SaveFromFormAsync(It.IsAny<SongFormSaveRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        songService.Verify(s => s.EnsureLyricsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // [AC] REQ-SLD-34: when the stored song no longer exists, the form shows the message once and navigates back once.
    [Fact]
    public async Task EditMode_SongNotFound_ShowsTheMessageAndNavigatesBack()
    {
        var songService = SongService();
        songService.Setup(s => s.GetSongByIdAsync(LocalOnlySong().Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Song?)null);
        var sut = CreateSut(songService);

        sut.SongIdRaw = LocalOnlySong().Id.ToString();
        await WaitUntilAsync(() => _navigationMock.Invocations.Count > 0);

        _snackbarMock.Verify(s => s.ShowErrorAsync(FailedLoadMessage), Times.Once);
        _navigationMock.Verify(n => n.GoToAsync(".."), Times.Once);
        Assert.False(sut.IsSongLoading);
    }
}
