using System.Net;
using System.Text;
using System.Text.Json;
using MyVocaList.Contracts.DTOs;
using Microsoft.Extensions.Logging.Abstractions;

namespace MyVocaList.Tests.Unit.Services;

/// <summary>
/// L2a — <see cref="LrclibLyricsProvider"/> builds the LRCLIB <c>api/get</c> request, maps the answer to plain
/// lyrics or null, and lets failures surface (REQ-LYR-01, 02, 03, 07). Uses a fake <see cref="HttpMessageHandler"/>; no network.
/// </summary>
public class LrclibLyricsProviderTests
{
    private const string TrackNotFoundBody =
        "{\"code\":404,\"name\":\"TrackNotFound\",\"message\":\"Failed to find specified track\"}";

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(respond(request));
        }
    }

    private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static LrclibLyricsProvider Create(Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        var client = new HttpClient(new StubHandler(respond)) { BaseAddress = new Uri("https://lrclib.net/") };
        return new LrclibLyricsProvider(client, NullLogger<LrclibLyricsProvider>.Instance);
    }

    /// <summary>Calls the provider with a 404 stub and returns the path and query the request carried.</summary>
    private static async Task<string> RequestPathAndQueryAsync(LyricsQuery query)
    {
        string? pathAndQuery = null;
        var provider = Create(request =>
        {
            pathAndQuery = request.RequestUri!.PathAndQuery;
            return Json(TrackNotFoundBody, HttpStatusCode.NotFound);
        });

        await provider.FetchLyricsAsync(query);

        return pathAndQuery!;
    }

    [Fact]
    public async Task FetchLyricsAsync_FullQuery_SendsEscapedTrackArtistAlbumAndDuration()
    {
        // [AC] REQ-LYR-01: the request carries title, artist, album and duration, escaped, on api/get
        var query = new LyricsQuery("Rock & Roll", "Velvet Underground", "The Best Of", 215);

        var pathAndQuery = await RequestPathAndQueryAsync(query);

        Assert.Equal(
            "/api/get?track_name=Rock%20%26%20Roll&artist_name=Velvet%20Underground&album_name=The%20Best%20Of&duration=215",
            pathAndQuery);
    }

    [Fact]
    public async Task FetchLyricsAsync_NullAlbumAndNullDuration_SendsNeitherParameter()
    {
        // [AC] REQ-LYR-01: album and duration are omitted from the request when the query has none
        var query = new LyricsQuery("Creep", "Radiohead");

        var pathAndQuery = await RequestPathAndQueryAsync(query);

        Assert.Equal("/api/get?track_name=Creep&artist_name=Radiohead", pathAndQuery);
    }

    [Fact]
    public async Task FetchLyricsAsync_BlankAlbum_IsNotSent()
    {
        // [AC] REQ-LYR-01: a blank album title is not sent as album_name
        var query = new LyricsQuery("Creep", "Radiohead", "   ", null);

        var pathAndQuery = await RequestPathAndQueryAsync(query);

        Assert.Equal("/api/get?track_name=Creep&artist_name=Radiohead", pathAndQuery);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3601)]
    [InlineData(-5)]
    public async Task FetchLyricsAsync_DurationOutsideOneToOneHour_IsNotSent(int seconds)
    {
        // [AC] REQ-LYR-01: a duration outside 1 to 3600 seconds is not sent
        var query = new LyricsQuery("Creep", "Radiohead", null, seconds);

        var pathAndQuery = await RequestPathAndQueryAsync(query);

        Assert.DoesNotContain("duration", pathAndQuery);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3600)]
    public async Task FetchLyricsAsync_DurationAtBounds_IsSent(int seconds)
    {
        // [AC] REQ-LYR-01: a duration of 1 or 3600 seconds is sent, formatted as a plain integer
        var query = new LyricsQuery("Creep", "Radiohead", null, seconds);

        var pathAndQuery = await RequestPathAndQueryAsync(query);

        Assert.EndsWith($"&duration={seconds}", pathAndQuery);
    }

    [Fact]
    public async Task FetchLyricsAsync_200WithPlainLyrics_ReturnsTextUnchanged()
    {
        // [AC] REQ-LYR-02: a 200 answer with plainLyrics returns that text exactly as received, not trimmed
        const string lyrics = "First line\n\nSecond line  ";
        var provider = Create(_ => Json(
            "{\"id\":1,\"trackName\":\"Creep\",\"plainLyrics\":\"First line\\n\\nSecond line  \",\"instrumental\":false}"));

        var result = await provider.FetchLyricsAsync(new LyricsQuery("Creep", "Radiohead"));

        Assert.Equal(lyrics, result);
    }

    [Fact]
    public async Task FetchLyricsAsync_200Instrumental_ReturnsNull()
    {
        // [AC] REQ-LYR-03: an instrumental record has no lyrics to use and returns null
        var provider = Create(_ => Json(
            "{\"id\":2,\"plainLyrics\":\"ignored\",\"instrumental\":true}"));

        var result = await provider.FetchLyricsAsync(new LyricsQuery("Creep", "Radiohead"));

        Assert.Null(result);
    }

    [Fact]
    public async Task FetchLyricsAsync_200NullPlainLyrics_ReturnsNull()
    {
        // [AC] REQ-LYR-03: a record whose plainLyrics is null returns null
        var provider = Create(_ => Json(
            "{\"id\":3,\"plainLyrics\":null,\"instrumental\":false}"));

        var result = await provider.FetchLyricsAsync(new LyricsQuery("Creep", "Radiohead"));

        Assert.Null(result);
    }

    [Fact]
    public async Task FetchLyricsAsync_200WhitespacePlainLyrics_ReturnsNull()
    {
        // [AC] REQ-LYR-03: a record whose plainLyrics is white space only returns null
        var provider = Create(_ => Json(
            "{\"id\":4,\"plainLyrics\":\" \\n\\t \",\"instrumental\":false}"));

        var result = await provider.FetchLyricsAsync(new LyricsQuery("Creep", "Radiohead"));

        Assert.Null(result);
    }

    [Fact]
    public async Task FetchLyricsAsync_404TrackNotFound_ReturnsNull()
    {
        // [AC] REQ-LYR-03: a 404 TrackNotFound answer is a no-match and returns null without throwing
        var provider = Create(_ => Json(TrackNotFoundBody, HttpStatusCode.NotFound));

        var result = await provider.FetchLyricsAsync(new LyricsQuery("Creep", "Radiohead"));

        Assert.Null(result);
    }

    [Fact]
    public async Task FetchLyricsAsync_500_ThrowsHttpRequestException()
    {
        // [AC] REQ-LYR-07: a 500 answer throws HttpRequestException so the service can log it and end the lookup
        var provider = Create(_ => Json("{\"message\":\"boom\"}", HttpStatusCode.InternalServerError));

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            provider.FetchLyricsAsync(new LyricsQuery("Creep", "Radiohead")));
    }

    [Fact]
    public async Task FetchLyricsAsync_429_ThrowsHttpRequestException()
    {
        // [AC] REQ-LYR-07: a 429 answer throws HttpRequestException so the service can log it and end the lookup
        var provider = Create(_ => Json("{\"message\":\"slow down\"}", (HttpStatusCode)429));

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            provider.FetchLyricsAsync(new LyricsQuery("Creep", "Radiohead")));
    }

    [Fact]
    public async Task FetchLyricsAsync_200UnreadableBody_ThrowsJsonException()
    {
        // [AC] REQ-LYR-07: a 200 body that is not JSON lets the JSON exception propagate to the service
        var provider = Create(_ => Json("<html>not lyrics</html>"));

        await Assert.ThrowsAsync<JsonException>(() =>
            provider.FetchLyricsAsync(new LyricsQuery("Creep", "Radiohead")));
    }

    [Fact]
    public async Task FetchLyricsAsync_CancelledToken_ThrowsOperationCanceledException()
    {
        // [AC] REQ-LYR-07: a cancelled token surfaces as OperationCanceledException, not as a failure
        var provider = Create(_ => Json("{\"plainLyrics\":\"x\",\"instrumental\":false}"));
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            provider.FetchLyricsAsync(new LyricsQuery("Creep", "Radiohead"), cts.Token));
    }
}
