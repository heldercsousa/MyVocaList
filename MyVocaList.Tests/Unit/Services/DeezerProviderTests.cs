using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;

namespace MyVocaList.Tests.Unit.Services;

/// <summary>
/// Task 4.1a — <see cref="DeezerProvider"/> plain query, failure propagation and title/version mapping
/// (REQ-SFT-06/09/12/35/47). Uses a fake <see cref="HttpMessageHandler"/>; no network.
/// </summary>
public class DeezerProviderTests
{
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

    private static (DeezerProvider Provider, StubHandler Handler) Create(Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        var handler = new StubHandler(respond);
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.deezer.com/") };
        return (new DeezerProvider(client, NullLogger<DeezerProvider>.Instance), handler);
    }

    private static string TrackBody(string title, string? titleShort, string? titleVersion)
    {
        var sb = new StringBuilder("{\"data\":[{\"id\":42,");
        sb.Append("\"title\":").Append(JsonSerializer.Serialize(title)).Append(',');
        if (titleShort is not null) sb.Append("\"title_short\":").Append(JsonSerializer.Serialize(titleShort)).Append(',');
        if (titleVersion is not null) sb.Append("\"title_version\":").Append(JsonSerializer.Serialize(titleVersion)).Append(',');
        sb.Append("\"artist\":{\"id\":7,\"name\":\"Radiohead\"}}]}");
        return sb.ToString();
    }

    [Fact]
    public async Task SearchSongsAsync_SendsPlainQueryWithoutQualifiers()
    {
        // [AC] REQ-SFT-35: song search sends the plain query search/track?q= without track:/artist: qualifiers
        var (provider, handler) = Create(_ => Json("{\"data\":[]}"));

        await provider.SearchSongsAsync("dont cry", "Guns N' Roses");

        var uri = Assert.Single(handler.Requests);
        Assert.Equal("/search/track?q=dont%20cry", uri.PathAndQuery);
    }

    [Fact]
    public async Task SearchArtistsAsync_SendsPlainArtistQuery()
    {
        // [AC] REQ-SFT-47: artist search sends the plain query search/artist?q=
        var (provider, handler) = Create(_ => Json("{\"data\":[]}"));

        await provider.SearchArtistsAsync("metalica");

        var uri = Assert.Single(handler.Requests);
        Assert.Equal("/search/artist?q=metalica", uri.PathAndQuery);
    }

    [Fact]
    public async Task SearchSongsAsync_Http500_Throws()
    {
        // [AC] REQ-SFT-06: a transport/HTTP failure throws instead of returning empty
        var (provider, _) = Create(_ => Json("{}", HttpStatusCode.InternalServerError));

        await Assert.ThrowsAsync<HttpRequestException>(() => provider.SearchSongsAsync("creep"));
    }

    [Fact]
    public async Task SearchSongsAsync_MalformedJson_Throws()
    {
        // [AC] REQ-SFT-06: a malformed JSON body throws (JsonException propagates unwrapped)
        var (provider, _) = Create(_ => Json("{not json"));

        await Assert.ThrowsAsync<JsonException>(() => provider.SearchSongsAsync("creep"));
    }

    [Fact]
    public async Task SearchSongsAsync_Http200WithErrorObject_ThrowsMusicProviderException()
    {
        // [AC] REQ-SFT-06: a Deezer error body returned with HTTP 200 throws MusicProviderException
        var (provider, _) = Create(_ => Json("{\"error\":{\"type\":\"Exception\",\"message\":\"Quota limit exceeded\",\"code\":4}}"));

        var ex = await Assert.ThrowsAsync<MusicProviderException>(() => provider.SearchSongsAsync("creep"));
        Assert.Contains("Quota limit exceeded", ex.Message);
    }

    [Fact]
    public async Task SearchSongsAsync_CallerCancelled_PropagatesOperationCanceled()
    {
        // [AC] REQ-SFT-09: caller cancellation propagates as OperationCanceledException
        var (provider, _) = Create(_ => Json("{\"data\":[]}"));
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => provider.SearchSongsAsync("creep", null, cts.Token));
    }

    [Fact]
    public async Task SearchSongsAsync_EmptyData_ReturnsEmpty()
    {
        // [AC] REQ-SFT-06: an empty data array yields an empty result without throwing
        var (provider, _) = Create(_ => Json("{\"data\":[]}"));

        var result = await provider.SearchSongsAsync("zzzz");

        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchSongsAsync_MapsIdTitleShortVersionArtistName()
    {
        // [AC] REQ-SFT-12: Title = title_short, Version = normalised title_version, plus id and artist name
        var (provider, _) = Create(_ => Json(TrackBody("Creep (Live)", "Creep", "(Live)")));

        var item = Assert.Single(await provider.SearchSongsAsync("creep"));

        Assert.Equal("Creep", item.SongTitle);
        Assert.Equal("Live", item.Version);
        Assert.Equal("42", item.ExternalId);
        Assert.Equal("Radiohead", item.ArtistName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task SearchSongsAsync_TitleShortMissing_FallsBackToTitle(string? titleShort)
    {
        // [AC] REQ-SFT-12: missing/empty title_short falls back to title; missing title_version yields empty Version
        var (provider, _) = Create(_ => Json(TrackBody("Creep", titleShort, null)));

        var item = Assert.Single(await provider.SearchSongsAsync("creep"));

        Assert.Equal("Creep", item.SongTitle);
        Assert.Equal("", item.Version);
    }

    [Theory]
    [InlineData("(Live)", "Live")]
    [InlineData(" [Remastered 2011] ", "Remastered 2011")]
    [InlineData("Live at Wembley", "Live at Wembley")]
    [InlineData("((x))", "(x)")]
    [InlineData("(Live) (2011)", "(Live) (2011)")]
    [InlineData("(Live]", "(Live]")]
    [InlineData("(Live [2011])", "Live [2011]")]
    [InlineData("", "")]
    public async Task SearchSongsAsync_NormalisesVersion(string raw, string expected)
    {
        // [AC] REQ-SFT-12: outer brackets are stripped only when the opener's same-type closer is the last character
        var (provider, _) = Create(_ => Json(TrackBody("Creep", "Creep", raw)));

        var item = Assert.Single(await provider.SearchSongsAsync("creep"));

        Assert.Equal(expected, item.Version);
    }
}
