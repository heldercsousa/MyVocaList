using MyVocaList.Contracts.DTOs;
using MyVocaList.Contracts.DTOs.List;
using MyVocaList.Navigation;
using MyVocaList.Tests.Infrastructure;
using MyVocaList.UI.Services;

namespace MyVocaList.Tests.Unit.ViewModels;

/// <summary>REQ-MSRC-14: the Song form's YouTube rows show the video title and thumbnail, filled lazily. Moq services, no database.</summary>
public class SongFormViewModelVideoInfoTests
{
    private const int SongId = 7;
    private readonly Mock<ISongKaraokeUrlService> _urlServiceMock = new();
    private readonly Mock<ILogger<SongFormViewModel>> _loggerMock = new();

    private SongFormViewModel CreateSut()
    {
        var songService = new Mock<ISongService>();
        songService.Setup(s => s.GetSongByIdAsync(SongId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Song { Id = SongId, ArtistId = 8, Title = "Blitzkrieg Bop", OriginalArtist = new Artist { Id = 8, Name = "Ramones" } });
        var sut = new SongFormViewModel(
            new Mock<IArtistService>().Object,
            songService.Object,
            new Mock<ISnackbarComponent>().Object,
            _loggerMock.Object,
            _urlServiceMock.Object,
            new Mock<ISharedLinkInbox>().Object,
            new Mock<ISharedVideoService>().Object,
            new Mock<INavigationService>().Object,
            new Mock<ISongSuggestionService>().Object,
            TestImageStore.PassThrough().Object,
            new Mock<IPhotoPicker>().Object,
            new Mock<ISongLyricsService>().Object);
        sut.CompleteHydration();
        return sut;
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        for (var i = 0; i < 500 && !condition(); i++)
            await Task.Delay(10);
    }

    private static SongKaraokeUrlDto Row(string videoId, string? title = null, string? thumb = null) =>
        new(videoId, SongId, 0, null, null, DateTime.UtcNow, null, false, title, thumb);

    // [AC] REQ-MSRC-14: existing rows are filled lazily when the form opens; the row shows the title and thumbnail
    [Fact]
    public async Task EditMode_RowWithoutTitle_IsFilledLazilyOnLoad()
    {
        _urlServiceMock.Setup(s => s.GetUrlsForSongAsync(SongId, It.IsAny<CancellationToken>())).ReturnsAsync([Row("aaaaaaaaaaa")]);
        _urlServiceMock.Setup(s => s.FillMissingVideoInfoAsync(SongId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([Row("aaaaaaaaaaa", "Filled title", "https://i.ytimg.com/a.jpg")]);
        var sut = CreateSut();

        sut.SongIdRaw = SongId.ToString();
        await WaitUntilAsync(() => sut.KaraokeUrls.Any(u => u.Title == "Filled title"));

        var row = Assert.Single(sut.KaraokeUrls);
        Assert.Equal("Filled title", row.DisplayTitle);
        Assert.True(row.HasThumbnail);
    }

    // [AC] REQ-MSRC-14: a row that already has its title is not looked up again
    [Fact]
    public async Task EditMode_RowsAllTitled_NoLazyFillRuns()
    {
        _urlServiceMock.Setup(s => s.GetUrlsForSongAsync(SongId, It.IsAny<CancellationToken>())).ReturnsAsync([Row("aaaaaaaaaaa", "Known", "t")]);
        var sut = CreateSut();

        sut.SongIdRaw = SongId.ToString();
        await WaitUntilAsync(() => sut.KaraokeUrls.Count == 1);
        await Task.Delay(100);

        _urlServiceMock.Verify(s => s.FillMissingVideoInfoAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // [AC] REQ-MSRC-14: offline or on failure the row falls back to the video id; the failure is logged, never shown
    [Fact]
    public async Task EditMode_LazyFillThrows_RowFallsBackToVideoId_AndFailureIsLogged()
    {
        _urlServiceMock.Setup(s => s.GetUrlsForSongAsync(SongId, It.IsAny<CancellationToken>())).ReturnsAsync([Row("aaaaaaaaaaa")]);
        _urlServiceMock.Setup(s => s.FillMissingVideoInfoAsync(SongId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("offline"));
        var sut = CreateSut();

        sut.SongIdRaw = SongId.ToString();
        await WaitUntilAsync(() => _loggerMock.Invocations.Any(i => i.Method.Name == nameof(ILogger.Log)));

        var row = Assert.Single(sut.KaraokeUrls);
        Assert.Equal("aaaaaaaaaaa", row.DisplayTitle);
        Assert.False(row.HasThumbnail);
        Assert.Contains(_loggerMock.Invocations, i => i.Method.Name == nameof(ILogger.Log) && (LogLevel)i.Arguments[0] == LogLevel.Warning);
    }

    // [AC] REQ-MSRC-14: the title and thumbnail fall back to the video id when none is known
    [Fact]
    public void Dto_WithoutTitle_DisplaysVideoId()
    {
        var row = Row("aaaaaaaaaaa");

        Assert.Equal("aaaaaaaaaaa", row.DisplayTitle);
        Assert.False(row.HasThumbnail);
    }
}
