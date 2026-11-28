using MyVocaList.Contracts.DTOs;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Unit.Services;

public class SongKaraokeUrlServiceTests
{
    private readonly Mock<ISongKaraokeUrlRepository> _repoMock = new();
    private readonly Mock<IYouTubeOEmbedClient> _oEmbedMock = new();
    private readonly Mock<ILogger<SongKaraokeUrlService>> _loggerMock = new();

    private SongKaraokeUrlService CreateSut() =>
        new(_repoMock.Object, PassthroughUnitOfWork.Over(_repoMock), _oEmbedMock.Object, _loggerMock.Object);

    // ── ExtractVideoId ───────────────────────────────────────────────────────

    [Theory]
    // [AC] AC-2.6: accepts all 4 YouTube URL formats
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://youtu.be/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/embed/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://youtube.com/shorts/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    public void ExtractVideoId_ValidFormats_ReturnsId(string url, string expectedId)
    {
        var sut = CreateSut();
        Assert.Equal(expectedId, sut.ExtractVideoId(url));
    }

    [Theory]
    // [AC] AC-2.7: invalid URLs return null
    [InlineData("https://vimeo.com/12345")]
    [InlineData("not-a-url")]
    [InlineData("")]
    [InlineData(null)]
    public void ExtractVideoId_InvalidFormats_ReturnsNull(string? url)
    {
        var sut = CreateSut();
        Assert.Null(sut.ExtractVideoId(url!));
    }

    // ── AddUrlAsync ──────────────────────────────────────────────────────────

    [Fact]
    // [AC] AC-1.9: duplicate video ID per song is rejected
    public async Task AddUrlAsync_DuplicateVideoId_ReturnsFalse()
    {
        _repoMock.Setup(r => r.ExistsAsync(1, "dQw4w9WgXcQ", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);
        var sut = CreateSut();

        var (success, message, dto) = await sut.AddUrlAsync(1, "https://youtu.be/dQw4w9WgXcQ");

        Assert.False(success);
        Assert.Contains("already saved", message, StringComparison.OrdinalIgnoreCase);
        Assert.Null(dto);
    }

    [Fact]
    // [AC] AC-2.7: invalid URL format returns error
    public async Task AddUrlAsync_InvalidUrl_ReturnsFalse()
    {
        var sut = CreateSut();
        var (success, message, dto) = await sut.AddUrlAsync(1, "https://vimeo.com/12345");

        Assert.False(success);
        Assert.Contains("valid YouTube URL", message, StringComparison.OrdinalIgnoreCase);
        Assert.Null(dto);
        _repoMock.Verify(r => r.AddAsync(It.IsAny<SongKaraokeUrl>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    // [AC] AC-2.4: valid URL is saved and returned
    public async Task AddUrlAsync_ValidUrl_PersistsAndReturnsDto()
    {
        _repoMock.Setup(r => r.ExistsAsync(1, "dQw4w9WgXcQ", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);
        var sut = CreateSut();

        var (success, _, dto) = await sut.AddUrlAsync(1, "https://youtu.be/dQw4w9WgXcQ");

        Assert.True(success);
        Assert.NotNull(dto);
        Assert.Equal("dQw4w9WgXcQ", dto!.VideoId);
        _repoMock.Verify(r => r.AddAsync(It.Is<SongKaraokeUrl>(u => u.VideoId == "dQw4w9WgXcQ"), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── REQ-MSRC-14: video title and thumbnail ───────────────────────────────

    [Fact]
    // [AC] REQ-MSRC-14: both come from oEmbed and are stored on the row when it is added
    public async Task AddUrlAsync_OEmbedAnswers_StoresTitleAndThumbnailOnTheRow()
    {
        _oEmbedMock.Setup(o => o.GetVideoInfoAsync("dQw4w9WgXcQ", It.IsAny<CancellationToken>()))
                   .ReturnsAsync(new YouTubeVideoInfo("Never Gonna Give You Up", "https://i.ytimg.com/vi/x.jpg"));
        var sut = CreateSut();

        var (success, _, dto) = await sut.AddUrlAsync(1, "https://youtu.be/dQw4w9WgXcQ");

        Assert.True(success);
        Assert.Equal("Never Gonna Give You Up", dto!.Title);
        Assert.Equal("https://i.ytimg.com/vi/x.jpg", dto.ThumbnailUrl);
        _repoMock.Verify(r => r.AddAsync(
            It.Is<SongKaraokeUrl>(u => u.Title == "Never Gonna Give You Up" && u.ThumbnailUrl == "https://i.ytimg.com/vi/x.jpg"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    // [AC] REQ-MSRC-14: offline or on failure the row is still saved, with no title (falls back to the video id)
    public async Task AddUrlAsync_OEmbedFails_StillSavesRowWithoutTitle()
    {
        _oEmbedMock.Setup(o => o.GetVideoInfoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync((YouTubeVideoInfo?)null);
        var sut = CreateSut();

        var (success, _, dto) = await sut.AddUrlAsync(1, "https://youtu.be/dQw4w9WgXcQ");

        Assert.True(success);
        Assert.Null(dto!.Title);
        Assert.Equal("dQw4w9WgXcQ", dto.DisplayTitle);
        Assert.False(dto.HasThumbnail);
    }

    [Fact]
    // [AC] REQ-MSRC-14: existing rows are filled lazily, in place; rows that already have a title are not fetched again
    public async Task FillMissingVideoInfoAsync_FillsOnlyRowsWithoutTitle_InPlace()
    {
        var missing = new SongKaraokeUrl { SongId = 1, VideoId = "aaaaaaaaaaa", AddedAt = DateTime.UtcNow };
        var known = new SongKaraokeUrl { SongId = 1, VideoId = "bbbbbbbbbbb", AddedAt = DateTime.UtcNow, Title = "Known" };
        _repoMock.Setup(r => r.GetBySongIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([missing, known]);
        _oEmbedMock.Setup(o => o.GetVideoInfoAsync("aaaaaaaaaaa", It.IsAny<CancellationToken>()))
                   .ReturnsAsync(new YouTubeVideoInfo("Filled", "https://i.ytimg.com/a.jpg"));
        var sut = CreateSut();

        var filled = await sut.FillMissingVideoInfoAsync(1);

        var row = Assert.Single(filled);
        Assert.Equal("aaaaaaaaaaa", row.VideoId);
        Assert.Equal("Filled", row.Title);
        _repoMock.Verify(r => r.UpdateVideoInfoAsync(1, "aaaaaaaaaaa", "Filled", "https://i.ytimg.com/a.jpg", It.IsAny<CancellationToken>()), Times.Once);
        _repoMock.Verify(r => r.UpdateVideoInfoAsync(1, "bbbbbbbbbbb", It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        _oEmbedMock.Verify(o => o.GetVideoInfoAsync("bbbbbbbbbbb", It.IsAny<CancellationToken>()), Times.Never);
        _repoMock.Verify(r => r.RemoveAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    // [AC] REQ-MSRC-14: a failed lookup leaves the row untouched
    public async Task FillMissingVideoInfoAsync_LookupFails_ChangesNothing()
    {
        _repoMock.Setup(r => r.GetBySongIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync([new SongKaraokeUrl { SongId = 1, VideoId = "aaaaaaaaaaa", AddedAt = DateTime.UtcNow }]);
        _oEmbedMock.Setup(o => o.GetVideoInfoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync((YouTubeVideoInfo?)null);
        var sut = CreateSut();

        var filled = await sut.FillMissingVideoInfoAsync(1);

        Assert.Empty(filled);
        _repoMock.Verify(r => r.UpdateVideoInfoAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── ExtractVideoId — FsCheck property-based tests ────────────────────────

    private static Gen<string> ValidVideoIdGen =>
        Gen.ArrayOf(11, Gen.Elements<char>("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_-".ToCharArray()))
           .Select(chars => new string(chars));

    [Property]
    // [AC] AC-2.6: any valid 11-char video ID survives the round-trip through all 4 URL formats
    public Property ExtractVideoId_WatchUrl_RoundTrips()
    {
        return Prop.ForAll(
            Arb.From(ValidVideoIdGen),
            videoId =>
            {
                var sut = CreateSut();
                return sut.ExtractVideoId($"https://www.youtube.com/watch?v={videoId}") == videoId;
            });
    }

    [Property]
    public Property ExtractVideoId_ShortUrl_RoundTrips()
    {
        return Prop.ForAll(
            Arb.From(ValidVideoIdGen),
            videoId =>
            {
                var sut = CreateSut();
                return sut.ExtractVideoId($"https://youtu.be/{videoId}") == videoId;
            });
    }

    [Property]
    public Property ExtractVideoId_EmbedUrl_RoundTrips()
    {
        return Prop.ForAll(
            Arb.From(ValidVideoIdGen),
            videoId =>
            {
                var sut = CreateSut();
                return sut.ExtractVideoId($"https://www.youtube.com/embed/{videoId}") == videoId;
            });
    }

    [Property]
    public Property ExtractVideoId_ShortsUrl_RoundTrips()
    {
        return Prop.ForAll(
            Arb.From(ValidVideoIdGen),
            videoId =>
            {
                var sut = CreateSut();
                return sut.ExtractVideoId($"https://youtube.com/shorts/{videoId}") == videoId;
            });
    }

    // ── RemoveUrlAsync ───────────────────────────────────────────────────────

    [Fact]
    // [AC] implied: remove returns failure when URL does not exist
    public async Task RemoveUrlAsync_UrlNotFound_ReturnsFalse()
    {
        _repoMock.Setup(r => r.ExistsAsync(1, "dQw4w9WgXcQ", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);
        var sut = CreateSut();

        var (success, message) = await sut.RemoveUrlAsync(1, "dQw4w9WgXcQ");

        Assert.False(success);
        Assert.NotEmpty(message);
        _repoMock.Verify(r => r.RemoveAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    // [AC] AC-1.5: removing a saved URL succeeds
    public async Task RemoveUrlAsync_ExistingUrl_RemovesAndReturnsSuccess()
    {
        _repoMock.Setup(r => r.ExistsAsync(1, "dQw4w9WgXcQ", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);
        var sut = CreateSut();

        var (success, _) = await sut.RemoveUrlAsync(1, "dQw4w9WgXcQ");

        Assert.True(success);
        _repoMock.Verify(r => r.RemoveAsync(1, "dQw4w9WgXcQ", It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── GetUrlsForSongAsync ──────────────────────────────────────────────────

    [Fact]
    // [AC] AC-1.7: zero URLs is valid — returns empty list
    public async Task GetUrlsForSongAsync_NoUrls_ReturnsEmptyList()
    {
        _repoMock.Setup(r => r.GetBySongIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync([]);
        var sut = CreateSut();

        var result = await sut.GetUrlsForSongAsync(1);

        Assert.Empty(result);
    }

    [Fact]
    // [AC] AC-1.4: first URL (highest play count via repo ordering) is marked IsSuggested
    public async Task GetUrlsForSongAsync_MultipleUrls_FirstIsSuggested()
    {
        var urls = new List<SongKaraokeUrl>
        {
            new() { VideoId = "AAAAAAAAAAA", SongId = 1, PlayCount = 5, AddedAt = DateTime.UtcNow },
            new() { VideoId = "BBBBBBBBBBB", SongId = 1, PlayCount = 2, AddedAt = DateTime.UtcNow }
        };
        _repoMock.Setup(r => r.GetBySongIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(urls);
        var sut = CreateSut();

        var result = await sut.GetUrlsForSongAsync(1);

        Assert.Equal(2, result.Count);
        Assert.True(result[0].IsSuggested);
        Assert.False(result[1].IsSuggested);
        Assert.Equal("AAAAAAAAAAA", result[0].VideoId);
    }
}
