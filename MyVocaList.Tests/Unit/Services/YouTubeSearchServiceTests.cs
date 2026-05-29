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

    [Fact]
    // [AC] AC-2.5: API key present but search returns no items → empty
    public async Task SearchAsync_ApiKeyPresent_EmptyApiResponse_ReturnsEmpty()
    {
        var httpClient = new HttpClient(new FakeHttpMessageHandler(
            System.Net.HttpStatusCode.OK,
            """{"items":[]}"""));
        _httpMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var sut = CreateSut(storedKey: "valid-key");

        var results = await sut.SearchAsync("test query");

        Assert.Empty(results);
    }

    [Fact]
    public async Task ValidateApiKeyAsync_SuccessResponse_ReturnsTrue()
    {
        var httpClient = new HttpClient(new FakeHttpMessageHandler(System.Net.HttpStatusCode.OK));
        _httpMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var sut = CreateSut(storedKey: "any-key");

        var result = await sut.ValidateApiKeyAsync("valid-key");

        Assert.True(result);
    }

    [Fact]
    public async Task ValidateApiKeyAsync_FailureResponse_ReturnsFalse()
    {
        var httpClient = new HttpClient(new FakeHttpMessageHandler(System.Net.HttpStatusCode.Forbidden));
        _httpMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        var sut = CreateSut(storedKey: "any-key");

        var result = await sut.ValidateApiKeyAsync("bad-key");

        Assert.False(result);
    }
}

file sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly System.Net.HttpStatusCode _statusCode;
    private readonly string _content;

    public FakeHttpMessageHandler(System.Net.HttpStatusCode statusCode, string content = "")
    {
        _statusCode = statusCode;
        _content = content;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_content, System.Text.Encoding.UTF8, "application/json")
        });
}
