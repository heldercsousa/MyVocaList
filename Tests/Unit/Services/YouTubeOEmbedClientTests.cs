using System.Net;
using Moq.Protected;

namespace MyVocaList.Tests.Unit.Services;

public class YouTubeOEmbedClientTests
{
    private static (YouTubeOEmbedClient sut, Func<HttpRequestMessage?> captured) CreateSut(Func<HttpResponseMessage> response)
    {
        HttpRequestMessage? captured = null;
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => captured = req)
            .ReturnsAsync(response);
        var http = new HttpClient(handler.Object) { BaseAddress = new Uri("https://www.youtube.com/") };
        return (new YouTubeOEmbedClient(http, new Mock<ILogger<YouTubeOEmbedClient>>().Object), () => captured);
    }

    // [AC] REQ-MSRC-14: the title and thumbnail come from the keyless oEmbed endpoint
    [Fact]
    public async Task GetVideoInfoAsync_Success_ReturnsTitleAndThumbnail_FromOEmbedUrl()
    {
        var (sut, captured) = CreateSut(() => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"title\":\"  Song (Karaoke)  \",\"thumbnail_url\":\"https://i.ytimg.com/vi/abc/hqdefault.jpg\"}")
        });

        var info = await sut.GetVideoInfoAsync("dQw4w9WgXcQ");

        Assert.NotNull(info);
        Assert.Equal("Song (Karaoke)", info!.Title);
        Assert.Equal("https://i.ytimg.com/vi/abc/hqdefault.jpg", info.ThumbnailUrl);
        var uri = captured()!.RequestUri!.ToString();
        Assert.StartsWith("https://www.youtube.com/oembed?url=", uri);
        Assert.Contains("format=json", uri);
        Assert.Contains(Uri.EscapeDataString("https://www.youtube.com/watch?v=dQw4w9WgXcQ"), uri);
    }

    // [AC] REQ-MSRC-12: the channel name travels with the title so the cleaning can remove it
    [Fact]
    public async Task GetVideoInfoAsync_Success_ReturnsChannelName()
    {
        var (sut, _) = CreateSut(() => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"title\":\"Song\",\"author_name\":\" Sing King \"}")
        });

        var info = await sut.GetVideoInfoAsync("dQw4w9WgXcQ");

        Assert.Equal("Sing King", info!.AuthorName);
    }

    // [AC] REQ-MSRC-14: offline or on failure the caller gets null (the row falls back to the video id)
    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task GetVideoInfoAsync_ErrorStatus_ReturnsNull(HttpStatusCode status)
    {
        var (sut, _) = CreateSut(() => new HttpResponseMessage(status));

        Assert.Null(await sut.GetVideoInfoAsync("dQw4w9WgXcQ"));
    }

    // [AC] REQ-MSRC-14: a network failure never throws
    [Fact]
    public async Task GetVideoInfoAsync_NetworkFailure_ReturnsNull()
    {
        var (sut, _) = CreateSut(() => throw new HttpRequestException("offline"));

        Assert.Null(await sut.GetVideoInfoAsync("dQw4w9WgXcQ"));
    }

    // [AC] REQ-MSRC-14: a malformed answer is a failure, not a crash
    [Fact]
    public async Task GetVideoInfoAsync_MalformedJson_ReturnsNull()
    {
        var (sut, _) = CreateSut(() => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("<html>") });

        Assert.Null(await sut.GetVideoInfoAsync("dQw4w9WgXcQ"));
    }
}
