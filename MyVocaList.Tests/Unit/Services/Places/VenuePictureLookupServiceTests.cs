using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using MyVocaList.Contracts.DTOs.Venues;
using MyVocaList.Domain.ServicesInterfaces;
using MyVocaList.Services.Places;

namespace MyVocaList.Tests.Unit.Services.Places;

/// <summary>
/// V11 — <see cref="VenuePictureLookupService"/> resolution order, request shape, offline and failure handling
/// (REQ-VEN-38, 39). Uses a fake <see cref="HttpMessageHandler"/>; no network.
/// </summary>
public class VenuePictureLookupServiceTests
{
    private const string WikidataClaimsPrefix = "/w/api.php?action=wbgetclaims&entity=";

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public List<Uri> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Requests.Add(request.RequestUri!);
            return Task.FromResult(respond(request));
        }
    }

    private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static INetworkStatus Online() => Mock.Of<INetworkStatus>(network => network.HasInternetAccess == true);

    private static INetworkStatus Offline() => Mock.Of<INetworkStatus>(network => network.HasInternetAccess == false);

    private static (VenuePictureLookupService Service, StubHandler Handler) Create(
        Func<HttpRequestMessage, HttpResponseMessage> respond,
        INetworkStatus? network = null)
    {
        var handler = new StubHandler(respond);
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://www.wikidata.org/") };
        return (new VenuePictureLookupService(client, network ?? Online(), NullLogger<VenuePictureLookupService>.Instance), handler);
    }

    private static string ClaimsWith(string property, string fileName) =>
        "{\"claims\":{\"" + property + "\":[{\"mainsnak\":{\"datavalue\":{\"value\":\"" + fileName + "\",\"type\":\"string\"}}}]}}";

    private const string EmptyClaims = "{\"claims\":{}}";

    [Fact]
    public async Task ResolveAsync_LogoStatementP154_ReturnsEscapedCommonsFilePathAtWidth512()
    {
        // [AC] REQ-VEN-38: the Wikidata logo statement P154 gives the Commons file address with width 512; spaces become underscores and the name is escaped
        var (service, handler) = Create(_ => Json(ClaimsWith("P154", "São Paulo Bar.png")));

        var uri = await service.ResolveAsync(new VenuePictureSource("Q918151", null, null));

        Assert.Equal("https://commons.wikimedia.org/wiki/Special:FilePath/S%C3%A3o_Paulo_Bar.png?width=512", uri?.AbsoluteUri);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task ResolveAsync_WikidataRequest_UsesClaimsApiWithEntityId()
    {
        // [AC] REQ-VEN-38: the Wikidata request is w/api.php?action=wbgetclaims&entity=<id>&format=json
        var (service, handler) = Create(_ => Json(EmptyClaims));

        await service.ResolveAsync(new VenuePictureSource("Q918151", null, null));

        var uri = Assert.Single(handler.Requests);
        Assert.Equal(WikidataClaimsPrefix + "Q918151&format=json", uri.PathAndQuery);
    }

    [Fact]
    public async Task ResolveAsync_NoP154_FallsBackToImageStatementP18()
    {
        // [AC] REQ-VEN-38: without a logo statement, the image statement P18 is used
        var (service, _) = Create(_ => Json(ClaimsWith("P18", "Bar photo.jpg")));

        var uri = await service.ResolveAsync(new VenuePictureSource("Q918151", null, null));

        Assert.Equal("https://commons.wikimedia.org/wiki/Special:FilePath/Bar_photo.jpg?width=512", uri?.AbsoluteUri);
    }

    [Fact]
    public async Task ResolveAsync_NoClaimsForWikidataId_FallsToCommonsFileWithoutSecondRequest()
    {
        // [AC] REQ-VEN-38: when the Wikidata answer has no picture, the Commons file name is used
        var (service, handler) = Create(_ => Json(EmptyClaims));

        var uri = await service.ResolveAsync(new VenuePictureSource("Q918151", "Bar.png", null));

        Assert.Equal("https://commons.wikimedia.org/wiki/Special:FilePath/Bar.png?width=512", uri?.AbsoluteUri);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task ResolveAsync_NoClaimsAndNoCommonsFile_FallsToImageAddress()
    {
        // [AC] REQ-VEN-38: when neither Wikidata nor Commons gives a picture, the image address is used
        var (service, _) = Create(_ => Json(EmptyClaims));

        var uri = await service.ResolveAsync(new VenuePictureSource("Q918151", null, "https://example.org/bar.jpg"));

        Assert.Equal("https://example.org/bar.jpg", uri?.AbsoluteUri);
    }

    [Fact]
    public async Task ResolveAsync_CommonsFileOnly_ReturnsCommonsFilePathWithoutRequest()
    {
        // [AC] REQ-VEN-38: a Commons file name is resolved to its Special:FilePath address without a request
        var (service, handler) = Create(_ => Json(EmptyClaims));

        var uri = await service.ResolveAsync(new VenuePictureSource(null, "Bar Logo.png", null));

        Assert.Equal("https://commons.wikimedia.org/wiki/Special:FilePath/Bar_Logo.png?width=512", uri?.AbsoluteUri);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task ResolveAsync_ImageAddressOnly_ReturnsTheAddressWithoutRequest()
    {
        // [AC] REQ-VEN-38: an image address is returned as the picture address without a request
        var (service, handler) = Create(_ => Json(EmptyClaims));

        var uri = await service.ResolveAsync(new VenuePictureSource(null, null, "https://example.org/bar.jpg"));

        Assert.Equal("https://example.org/bar.jpg", uri?.AbsoluteUri);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task ResolveAsync_EmptySource_ReturnsNullWithoutRequest()
    {
        // [AC] REQ-VEN-38: given an empty picture source, no picture request is sent
        var (service, handler) = Create(_ => Json(EmptyClaims));

        var uri = await service.ResolveAsync(new VenuePictureSource(null, null, null));

        Assert.Null(uri);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task ResolveAsync_Offline_ReturnsNullWithoutRequest()
    {
        // [AC] REQ-VEN-39: with no internet access the picture is skipped without a request
        var (service, handler) = Create(_ => Json(ClaimsWith("P154", "Bar.png")), Offline());

        var uri = await service.ResolveAsync(new VenuePictureSource("Q918151", null, null));

        Assert.Null(uri);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task ResolveAsync_Timeout_ReturnsNullWithoutThrowing()
    {
        // [AC] REQ-VEN-39: an answer that is too slow (timeout) gives no picture and no error
        var (service, _) = Create(_ => throw new TaskCanceledException("timeout"));

        var uri = await service.ResolveAsync(new VenuePictureSource("Q918151", null, null));

        Assert.Null(uri);
    }

    [Fact]
    public async Task ResolveAsync_HttpErrorStatus_ReturnsNullWithoutThrowing()
    {
        // [AC] REQ-VEN-39: an HTTP error from Wikidata gives no picture and no error
        var (service, _) = Create(_ => Json("{}", HttpStatusCode.InternalServerError));

        var uri = await service.ResolveAsync(new VenuePictureSource("Q918151", null, null));

        Assert.Null(uri);
    }

    [Fact]
    public async Task ResolveAsync_MalformedJson_ReturnsNullWithoutThrowing()
    {
        // [AC] REQ-VEN-39: a malformed answer from Wikidata gives no picture and no error
        var (service, _) = Create(_ => Json("{not json"));

        var uri = await service.ResolveAsync(new VenuePictureSource("Q918151", null, null));

        Assert.Null(uri);
    }
}
