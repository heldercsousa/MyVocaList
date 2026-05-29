namespace MyVocaList.Tests.Unit.Services;

public class SongKaraokeUrlServiceTests
{
    private readonly Mock<ISongKaraokeUrlRepository> _repoMock = new();
    private readonly Mock<ILogger<SongKaraokeUrlService>> _loggerMock = new();

    private SongKaraokeUrlService CreateSut() =>
        new(_repoMock.Object, _loggerMock.Object);

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
        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
