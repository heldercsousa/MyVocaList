using MyVocaList.UI.Services;

namespace MyVocaList.Tests.Unit.ViewModels;

public class SongFormViewModelTests
{
    private static SongKaraokeUrlDto MakeDto(string videoId = "abc123", int songId = 42) =>
        new(videoId, songId, 0, null, null, DateTime.UtcNow, null, false);

    private SongFormViewModel CreateSut(
        Mock<ISongKaraokeUrlService>? urlService = null,
        Mock<ISnackbarComponent>? snackbar = null,
        Mock<ISecureStorageWrapper>? secureStorage = null)
    {
        return new SongFormViewModel(
            new Mock<IArtistService>().Object,
            new Mock<ISongService>().Object,
            (snackbar ?? new Mock<ISnackbarComponent>()).Object,
            new Mock<ILogger<SongFormViewModel>>().Object,
            new Mock<IMusicMetadataService>().Object,
            (urlService ?? new Mock<ISongKaraokeUrlService>()).Object,
            new Mock<IYouTubeSearchService>().Object,
            (secureStorage ?? new Mock<ISecureStorageWrapper>()).Object);
    }

    // ── RemoveUrlAsync ────────────────────────────────────────────────────────

    // [AC] AC-1.5 — URL removal commits to DB immediately (before snackbar is shown)
    [Fact]
    public async Task RemoveUrlAsync_ValidUrl_CommitsDeleteBeforeSnackbar()
    {
        // Arrange
        var callOrder = new List<string>();

        var urlService = new Mock<ISongKaraokeUrlService>();
        urlService.Setup(s => s.RemoveUrlAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .Callback<int, string, CancellationToken>((_, _, _) => callOrder.Add("remove"))
                  .ReturnsAsync((true, string.Empty));
        urlService.Setup(s => s.GetUrlsForSongAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync([]);
        var snackbar = new Mock<ISnackbarComponent>();
        // Timer expires — do NOT invoke the undo callback
        snackbar.Setup(s => s.ShowWithUndoAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Func<Task>>()))
                .Callback<string, string, Func<Task>>((_, _, _) => callOrder.Add("snackbar"))
                .Returns(Task.CompletedTask);
        var secureStorage = new Mock<ISecureStorageWrapper>();
        secureStorage.Setup(s => s.GetAsync(It.IsAny<string>())).ReturnsAsync((string?)null);

        var sut = CreateSut(urlService: urlService, snackbar: snackbar, secureStorage: secureStorage);
        sut.SongIdRaw = "42";
        await Task.Yield();

        var dto = MakeDto();
        sut.KaraokeUrls.Add(dto);

        // Act
        await sut.RemoveUrlCommand.ExecuteAsync(dto);

        // Assert — DB delete must be the first call (before snackbar is shown)
        Assert.Equal(["remove", "snackbar"], callOrder);
    }

    // [AC] AC-1.5 — UNDO re-inserts the URL
    [Fact]
    public async Task RemoveUrlAsync_UndoTapped_ReAddsUrlToList()
    {
        // Arrange
        var urlService = new Mock<ISongKaraokeUrlService>();
        urlService.Setup(s => s.RemoveUrlAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync((true, string.Empty));
        urlService.Setup(s => s.GetUrlsForSongAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync([]);
        var reAdded = MakeDto();
        urlService.Setup(s => s.AddUrlAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync((true, string.Empty, reAdded));
        var snackbar = new Mock<ISnackbarComponent>();
        // Simulate UNDO tap — invoke the callback immediately
        snackbar.Setup(s => s.ShowWithUndoAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Func<Task>>()))
                .Callback<string, string, Func<Task>>((_, _, action) => action().GetAwaiter().GetResult())
                .Returns(Task.CompletedTask);
        var secureStorage = new Mock<ISecureStorageWrapper>();
        secureStorage.Setup(s => s.GetAsync(It.IsAny<string>())).ReturnsAsync((string?)null);

        var sut = CreateSut(urlService: urlService, snackbar: snackbar, secureStorage: secureStorage);
        sut.SongIdRaw = "42";
        await Task.Yield();

        var dto = MakeDto();
        sut.KaraokeUrls.Add(dto);

        // Act
        await sut.RemoveUrlCommand.ExecuteAsync(dto);

        // Assert — URL must be back in the list after undo
        Assert.Contains(sut.KaraokeUrls, u => u.VideoId == "abc123");
    }

    // [AC] AC-1.5 — Remove fails → show error, list unchanged
    [Fact]
    public async Task RemoveUrlAsync_RemoveFails_ShowsErrorAndKeepsUrl()
    {
        // Arrange
        var urlService = new Mock<ISongKaraokeUrlService>();
        urlService.Setup(s => s.RemoveUrlAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync((false, "Not found"));
        urlService.Setup(s => s.GetUrlsForSongAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync([]);
        var snackbar = new Mock<ISnackbarComponent>();
        var secureStorage = new Mock<ISecureStorageWrapper>();
        secureStorage.Setup(s => s.GetAsync(It.IsAny<string>())).ReturnsAsync((string?)null);

        var sut = CreateSut(urlService: urlService, snackbar: snackbar, secureStorage: secureStorage);
        sut.SongIdRaw = "42";
        await Task.Yield();

        var dto = MakeDto();
        sut.KaraokeUrls.Add(dto);

        // Act
        await sut.RemoveUrlCommand.ExecuteAsync(dto);

        // Assert — error shown, URL still in list
        snackbar.Verify(s => s.ShowErrorAsync("Not found"), Times.Once);
        Assert.Contains(sut.KaraokeUrls, u => u.VideoId == "abc123");
    }
}
