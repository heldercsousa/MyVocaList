using CommunityToolkit.Mvvm.Messaging;
using MyVocaList.Navigation;
using MyVocaList.Tests.Infrastructure;
using MyVocaList.UI.Services;

namespace MyVocaList.Tests.Unit.ViewModels;

/// <summary>
/// Bug fix: the edit-mode load of the Song form ran its database calls on the UI thread. Microsoft.Data.Sqlite runs
/// its "async" calls synchronously, so the screen was blocked before the loading indicator was drawn (REQ-SLD-32,
/// "the screen is not blocked"). The service calls of the load now run on a background thread. Moq services,
/// <c>TestImageStore.PassThrough</c>; no database, no <c>Shell.Current</c>.
/// </summary>
public class SongFormViewModelLoadOffUiThreadTests
{
    private readonly Mock<INavigationService> _navigationMock = new();
    private readonly Mock<ISongSuggestionService> _suggestionsMock = new();
    private readonly Mock<ISnackbarComponent> _snackbarMock = new();
    private readonly Mock<IPhotoPicker> _pickerMock = new();
    private readonly Mock<ISongKaraokeUrlService> _urlServiceMock = new();

    public SongFormViewModelLoadOffUiThreadTests()
    {
        _urlServiceMock.Setup(s => s.GetUrlsForSongAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
    }

    private SongFormViewModel CreateSut(Mock<ISongService> songService)
    {
        var sut = new SongFormViewModel(
            new Mock<IArtistService>().Object,
            songService.Object,
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

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        for (var i = 0; i < 500 && !condition(); i++)
            await Task.Delay(10);
    }

    // [AC] REQ-SLD-32: the screen is not blocked while a stored song loads, so the song service is called on a background thread, not on the calling (UI) thread.
    [Fact]
    public async Task EditMode_LoadCallsTheSongService_OffTheCallingThread()
    {
        const int songId = 43;
        var serviceCalled = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var songService = new Mock<ISongService>();
        songService.Setup(s => s.GetSongByIdAsync(songId, It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                serviceCalled.TrySetResult(Environment.CurrentManagedThreadId);
                return Task.FromResult<Song?>(new Song { Id = songId, ArtistId = 8, Title = "Blitzkrieg Bop", OriginalArtist = new Artist { Id = 8, Name = "Ramones" } });
            });
        var sut = CreateSut(songService);

        // The calling thread is a dedicated thread, not a pool worker: a pool worker may run the queued work itself,
        // which would make the thread-id comparison meaningless.
        var callingThread = -1;
        var caller = new Thread(() =>
        {
            callingThread = Environment.CurrentManagedThreadId;
            sut.SongIdRaw = songId.ToString();
        });
        caller.Start();
        caller.Join();

        var serviceThread = await serviceCalled.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.NotEqual(callingThread, serviceThread);
    }
}
