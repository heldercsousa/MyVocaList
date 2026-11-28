using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;

namespace MyVocaList.Tests.Unit.Services;

/// <summary>ITunesSearchProvider: request shape, mapping, artwork size and the 20-calls-per-minute budget (REQ-ITS-01..03, 06, 10, 12).</summary>
public class ITunesSearchProviderTests
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

    private const string GunsRow = """
        {"resultCount":1,"results":[{"wrapperType":"track","kind":"song","trackId":1440803,
        "trackName":"Don't Cry","artistName":"Guns N' Roses","collectionName":"Use Your Illusion I",
        "artworkUrl100":"https://is1.mzstatic.com/image/thumb/Music/100x100bb.jpg","trackTimeMillis":284000}]}
        """;

    private static HttpResponseMessage Json(string body) =>
        new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static (ITunesSearchProvider Provider, StubHandler Handler, FakeTime Time) Create(
        Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        var handler = new StubHandler(respond);
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://itunes.apple.com/") };
        var time = new FakeTime();
        return (new ITunesSearchProvider(client, new ITunesRequestBudget(time), NullLogger<ITunesSearchProvider>.Instance), handler, time);
    }

    [Fact]
    public async Task SearchSongsAsync_SendsKeylessSongQueryWithLimit25()
    {
        // [AC] REQ-ITS-01: keyless song search, limit 15, term URL-encoded
        var (provider, handler, _) = Create(_ => Json("""{"resultCount":0,"results":[]}"""));

        await provider.SearchSongsAsync("Don't cry");

        var uri = Assert.Single(handler.Requests);
        Assert.Equal("/search", uri.AbsolutePath);
        Assert.Contains("entity=song", uri.Query);
        Assert.Contains("limit=25", uri.Query);
        Assert.Contains("term=Don%27t%20cry", uri.Query);
        Assert.DoesNotContain("key", uri.Query);
    }

    [Fact]
    public async Task SearchSongsAsync_MapsTrackToRowWithArtworkUrlAt250()
    {
        // [AC] REQ-ITS-10: artwork URL rewritten to the 250 size, referenced only (no file)
        var (provider, _, _) = Create(_ => Json(GunsRow));

        var rows = (await provider.SearchSongsAsync("dont cry gun")).ToList();

        var row = Assert.Single(rows);
        Assert.Equal("Don't Cry", row.SongTitle);
        Assert.Equal("Guns N' Roses", row.ArtistName);
        Assert.Equal("Use Your Illusion I", row.AlbumTitle);
        Assert.Equal(284, row.DurationSeconds);
        Assert.Equal("https://is1.mzstatic.com/image/thumb/Music/250x250bb.jpg", row.ImageUrl);
        Assert.Equal("1440803", row.ExternalId);
        Assert.Equal("iTunes", row.Provider);
        Assert.Null(row.ExternalArtistId);
        Assert.Null(row.ReleaseMbid);
    }

    [Fact]
    public async Task SearchArtistsAsync_IsNotSupported_ReturnsEmptyWithoutCalling()
    {
        // [AC] REQ-ITS-01: iTunes serves title suggestions only; artist search stays on MusicBrainz
        var (provider, handler, _) = Create(_ => Json(GunsRow));

        var rows = await provider.SearchArtistsAsync("Guns");

        Assert.Empty(rows);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task SearchSongsAsync_BeyondTwentyCallsInAMinute_SkipsCallAndReturnsEmpty()
    {
        // [AC] REQ-ITS-06: at most 20 calls per minute; the 21st call is skipped
        var (provider, handler, time) = Create(_ => Json(GunsRow));
        for (var i = 0; i < 20; i++)
            await provider.SearchSongsAsync("dont cry");

        var rows = await provider.SearchSongsAsync("dont cry");

        Assert.Empty(rows);
        Assert.Equal(20, handler.Requests.Count);
    }

    [Fact]
    public async Task SearchSongsAsync_AfterOneMinute_BudgetRecovers()
    {
        // [AC] REQ-ITS-06: the budget is a sliding one-minute window
        var (provider, handler, time) = Create(_ => Json(GunsRow));
        for (var i = 0; i < 20; i++)
            await provider.SearchSongsAsync("dont cry");

        time.Advance(TimeSpan.FromSeconds(61));
        var rows = await provider.SearchSongsAsync("dont cry");

        Assert.Single(rows);
        Assert.Equal(21, handler.Requests.Count);
    }

    private sealed class FakeTime : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 11, 28, 12, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan by) => _now += by;
    }
}
