using System.Diagnostics;
using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;

namespace MyVocaList.Tests.Unit.Services;

/// <summary>MusicBrainzProvider, rate limiter and user agent (REQ-MSRC-02, REQ-MSRC-10). Fake handler; no network.</summary>
public class MusicBrainzProviderTests
{
    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public List<Uri> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request.RequestUri!);
            return Task.FromResult(respond(request));
        }
    }

    private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static (MusicBrainzProvider Provider, StubHandler Handler) Create(Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        var handler = new StubHandler(respond);
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://musicbrainz.org/ws/2/") };
        return (new MusicBrainzProvider(client, new MusicBrainzRateLimiter(), NullLogger<MusicBrainzProvider>.Instance), handler);
    }

    [Fact]
    public async Task SearchSongsAsync_WithArtistHint_SendsLuceneQuery()
    {
        // [AC] REQ-MSRC-02: song search queries MusicBrainz recordings by title and artist
        var (provider, handler) = Create(_ => Json("""{"recordings":[]}"""));

        await provider.SearchSongsAsync("x", "y");

        var uri = Assert.Single(handler.Requests);
        Assert.Equal("/ws/2/recording/", uri.AbsolutePath);
        Assert.Contains("query=recording%3A%22x%22%20AND%20artist%3A%22y%22", uri.Query);
        Assert.Contains("fmt=json", uri.Query);
    }

    [Fact]
    public async Task SearchSongsAsync_FirstRelease_CarriesReleaseMbid()
    {
        // [AC] REQ-MSRC-18: the recording's first release id travels so the cover URL is built with no extra call
        var (provider, _) = Create(_ => Json("""{"recordings":[{"id":"r1","title":"T","artist-credit":[{"name":"A","artist":{"id":"a1","name":"A"}}],"releases":[{"id":"76df3287-6cda-33eb-8e9a-044b5e15ffdd","title":"Alb"}]}]}"""));

        var result = Assert.Single(await provider.SearchSongsAsync("t"));

        Assert.Equal("76df3287-6cda-33eb-8e9a-044b5e15ffdd", result.ReleaseMbid);
    }

    [Fact]
    public async Task SearchArtistsAsync_MapsMbidAndName()
    {
        // [AC] REQ-MSRC-02: artist results carry the MBID as ExternalId and the MusicBrainz provider name
        var (provider, _) = Create(_ => Json("""{"artists":[{"id":"mbid-1","name":"Abba"}]}"""));

        var result = Assert.Single(await provider.SearchArtistsAsync("abba"));

        Assert.Equal("mbid-1", result.ExternalId);
        Assert.Equal("Abba", result.ArtistName);
        Assert.Equal("MusicBrainz", result.Provider);
    }

    [Fact]
    public async Task SearchSongsAsync_Http503_ThrowsMusicProviderException()
    {
        // [AC] REQ-MSRC-10: rate-limited response surfaces as MusicProviderException
        var (provider, _) = Create(_ => Json("{}", HttpStatusCode.ServiceUnavailable));

        await Assert.ThrowsAsync<MusicProviderException>(() => provider.SearchSongsAsync("x"));
    }

    [Fact]
    public async Task SearchSongsAsync_Http503ThenOk_RetriesOnceAndReturnsResults()
    {
        // [AC] REQ-MSRC-10: one transient 503 (server-side rate limit) is retried after the limiter wait, not surfaced
        var calls = 0;
        var (provider, handler) = Create(_ => ++calls == 1
            ? Json("{}", HttpStatusCode.ServiceUnavailable)
            : Json("""{"recordings":[{"id":"r1","title":"Mama","length":1000,"artist-credit":[{"name":"Queen","artist":{"id":"a1","name":"Queen"}}]}]}"""));

        var result = Assert.Single(await provider.SearchSongsAsync("mama"));

        Assert.Equal("r1", result.ExternalId);
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task WaitAsync_TwoCalls_SecondWaitsAtLeast1000ms()
    {
        // [AC] REQ-MSRC-10: at most one request per second through the shared limiter
        var limiter = new MusicBrainzRateLimiter();
        await limiter.WaitAsync();
        var sw = Stopwatch.StartNew();
        await limiter.WaitAsync();
        sw.Stop();

        Assert.True(sw.ElapsedMilliseconds >= 950, $"waited {sw.ElapsedMilliseconds} ms");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public void BuildUserAgent_MissingContact_ReturnsBareProductToken(string? contact)
    {
        // [AC] REQ-MSRC-10: user agent without contact is the bare product token
        Assert.Equal("MyVocaList/1.2", MusicBrainzProvider.BuildUserAgent("1.2", contact));
    }
}
