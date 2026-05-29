namespace MyVocaList.Tests.Unit.Services;

public class YouTubeSearchServiceTests
{
    private readonly Mock<IHttpClientFactory> _httpMock = new();
    private readonly Mock<ILogger<YouTubeSearchService>> _loggerMock = new();

    private YouTubeSearchService CreateSut(string? storedKey = null)
    {
        var secureStorageMock = new Mock<ISecureStorageWrapper>();
        secureStorageMock.Setup(s => s.GetAsync("youtube_api_key")).ReturnsAsync(storedKey);
        return new YouTubeSearchService(_httpMock.Object, secureStorageMock.Object, _loggerMock.Object);
    }

    [Fact]
    // [AC] AC-2.5: no key → returns empty result set
    public async Task SearchAsync_NoApiKey_ReturnsEmpty()
    {
        var sut = CreateSut(storedKey: null);

        var results = await sut.SearchAsync("test query");

        Assert.Empty(results);
    }
}
