using System.Globalization;
using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using MyVocaList.Domain.Constants;

namespace MyVocaList.Tests.Unit.Services;

/// <summary>
/// T6 — <see cref="DeezerProvider"/> carries the track artist id in <see cref="MusicSearchResultDto.ExternalArtistId"/>,
/// keeps <see cref="SearchConstants.RemoteSongFetchCount"/> track rows, and keeps the artist search at five rows
/// (REQ-SLD-06, REQ-SLD-10, REQ-SLD-11). Uses a fake <see cref="HttpMessageHandler"/>; no network.
/// </summary>
public class DeezerProviderArtistIdTests
{
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

    private static DeezerProvider Create(Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        var client = new HttpClient(new StubHandler(respond)) { BaseAddress = new Uri("https://api.deezer.com/") };
        return new DeezerProvider(client, NullLogger<DeezerProvider>.Instance);
    }

    /// <summary>One <c>search/track</c> row; <paramref name="artistJson"/> is the raw artist object or null for an absent artist.</summary>
    private static string TrackRow(long id, string? artistJson) =>
        "{\"id\":" + id.ToString(CultureInfo.InvariantCulture) + ",\"title\":\"Creep\",\"title_short\":\"Creep\""
        + (artistJson is null ? string.Empty : ",\"artist\":" + artistJson) + "}";

    private static string TrackBody(params string[] rows) => "{\"data\":[" + string.Join(",", rows) + "]}";

    [Fact]
    public async Task SearchSongsAsync_RowWithArtistId_CarriesItAsText()
    {
        // [AC] REQ-SLD-06: a track row whose artist object has id 5288900 yields ExternalArtistId "5288900"
        var provider = Create(_ => Json(TrackBody(TrackRow(42, "{\"id\":5288900,\"name\":\"Radiohead\"}"))));

        var results = (await provider.SearchSongsAsync("creep")).ToList();

        Assert.Equal("5288900", Assert.Single(results).ExternalArtistId);
    }

    [Theory]
    [InlineData("{\"name\":\"Radiohead\"}")]
    [InlineData(null)]
    public async Task SearchSongsAsync_RowWithoutArtistId_CarriesNull(string? artistJson)
    {
        // [AC] REQ-SLD-11: a track row with no artist id (artist object without id, or no artist object) yields a null ExternalArtistId
        var provider = Create(_ => Json(TrackBody(TrackRow(42, artistJson))));

        var results = (await provider.SearchSongsAsync("creep")).ToList();

        Assert.Null(Assert.Single(results).ExternalArtistId);
    }

    [Fact]
    public async Task SearchSongsAsync_UrlIsUnchanged()
    {
        // [AC] REQ-SLD-10: the track request for "creep" is still the plain query, with no limit parameter
        string? requested = null;
        var provider = Create(request =>
        {
            requested = request.RequestUri?.PathAndQuery;
            return Json(TrackBody());
        });

        await provider.SearchSongsAsync("creep");

        Assert.Equal("/search/track?q=creep", requested);
    }

    [Fact]
    public async Task SearchSongsAsync_AnswerOfThirtyRows_KeepsTwentyFive()
    {
        // [AC] REQ-SLD-10: an answer of 30 track rows keeps RemoteSongFetchCount (25) rows
        var rows = Enumerable.Range(1, 30).Select(i => TrackRow(i, "{\"id\":7,\"name\":\"Radiohead\"}")).ToArray();
        var provider = Create(_ => Json(TrackBody(rows)));

        var results = (await provider.SearchSongsAsync("creep")).ToList();

        Assert.Equal(SearchConstants.RemoteSongFetchCount, results.Count);
    }

    [Fact]
    public async Task SearchArtistsAsync_StillAsksPlainQueryAndKeepsFive()
    {
        // [AC] REQ-SLD-10: the artist search still requests the plain query and keeps five rows
        string? requested = null;
        var artists = Enumerable.Range(1, 8)
            .Select(i => "{\"id\":" + i.ToString(CultureInfo.InvariantCulture) + ",\"name\":\"Artist " + i.ToString(CultureInfo.InvariantCulture) + "\"}");
        var provider = Create(request =>
        {
            requested = request.RequestUri?.PathAndQuery;
            return Json("{\"data\":[" + string.Join(",", artists) + "]}");
        });

        var results = (await provider.SearchArtistsAsync("creep")).ToList();

        Assert.Equal("/search/artist?q=creep", requested);
        Assert.Equal(5, results.Count);
    }
}
