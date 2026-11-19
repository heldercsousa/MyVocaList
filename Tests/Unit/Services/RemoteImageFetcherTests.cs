using System.Net;
using MyVocaList.Services;

namespace MyVocaList.Tests.Unit.Services;

/// <summary>
/// V12 — <see cref="RemoteImageFetcher"/> downloads a venue picture for the form (REQ-VEN-38, 39).
/// Uses a fake <see cref="HttpMessageHandler"/>; no network.
/// </summary>
public class RemoteImageFetcherTests
{
    private static readonly Uri ImageAddress = new("https://upload.wikimedia.org/wikipedia/commons/a/ab/Bar.jpg");

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public int Requests { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests++;
            return Task.FromResult(respond(request));
        }
    }

    private static (RemoteImageFetcher Fetcher, StubHandler Handler) Create(Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        var handler = new StubHandler(respond);
        var client = new HttpClient(handler);
        return (new RemoteImageFetcher(client, Microsoft.Extensions.Logging.Abstractions.NullLogger<RemoteImageFetcher>.Instance), handler);
    }

    private static HttpResponseMessage Image(byte[] bytes) =>
        new(HttpStatusCode.OK) { Content = new ByteArrayContent(bytes) { Headers = { ContentType = new("image/jpeg") } } };

    [Fact]
    // [AC] REQ-VEN-38: the downloaded picture bytes are handed over for import, unchanged.
    public async Task FetchAsync_ImageAnswer_ReturnsItsBytes()
    {
        var bytes = new byte[] { 1, 2, 3, 4 };
        var (fetcher, _) = Create(_ => Image(bytes));

        await using var stream = await fetcher.FetchAsync(ImageAddress);

        Assert.NotNull(stream);
        using var copy = new MemoryStream();
        await stream!.CopyToAsync(copy);
        Assert.Equal(bytes, copy.ToArray());
    }

    [Fact]
    // [AC] REQ-VEN-39: an answer that is not an image is skipped: no stream.
    public async Task FetchAsync_NotAnImage_ReturnsNull()
    {
        var (fetcher, _) = Create(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("<html></html>", System.Text.Encoding.UTF8, "text/html"),
        });

        Assert.Null(await fetcher.FetchAsync(ImageAddress));
    }

    [Fact]
    // [AC] REQ-VEN-39: an HTTP error answer is skipped: no stream.
    public async Task FetchAsync_HttpError_ReturnsNull()
    {
        var (fetcher, _) = Create(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

        Assert.Null(await fetcher.FetchAsync(ImageAddress));
    }

    [Fact]
    // [AC] REQ-VEN-39: a failed request (no internet, connection error) is skipped: no stream, no throw.
    public async Task FetchAsync_RequestFails_ReturnsNull()
    {
        var (fetcher, _) = Create(_ => throw new HttpRequestException("no connection"));

        Assert.Null(await fetcher.FetchAsync(ImageAddress));
    }

    [Fact]
    // [AC] REQ-VEN-38: a picture is downloaded only from an https address; any other address sends no request.
    public async Task FetchAsync_NotHttps_ReturnsNullWithoutRequest()
    {
        var (fetcher, handler) = Create(_ => Image([1]));

        Assert.Null(await fetcher.FetchAsync(new Uri("http://example.org/bar.jpg")));
        Assert.Equal(0, handler.Requests);
    }
}
