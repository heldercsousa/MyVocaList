using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;

namespace MyVocaList.Tests.Unit.Services;

/// <summary>
/// L1 — <see cref="DeezerProvider"/> maps the track <c>duration</c> (seconds) to
/// <see cref="MusicSearchResultDto.DurationSeconds"/> (REQ-LYR-01). Uses a fake <see cref="HttpMessageHandler"/>; no network.
/// </summary>
public class DeezerProviderDurationTests
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

    /// <summary>A <c>search/track</c> body with one row; <paramref name="durationJson"/> is the raw JSON fragment or null for absent.</summary>
    private static string TrackBody(string? durationJson)
    {
        var sb = new StringBuilder("{\"data\":[{\"id\":42,\"title\":\"Creep\",\"title_short\":\"Creep\",");
        if (durationJson is not null) sb.Append("\"duration\":").Append(durationJson).Append(',');
        sb.Append("\"artist\":{\"id\":7,\"name\":\"Radiohead\"}}]}");
        return sb.ToString();
    }

    [Fact]
    public async Task SearchSongsAsync_DurationPresent_MapsToDurationSeconds()
    {
        // [AC] REQ-LYR-01: a search/track row with "duration": 215 yields DurationSeconds 215
        var provider = Create(_ => Json(TrackBody("215")));

        var results = (await provider.SearchSongsAsync("creep")).ToList();

        Assert.Equal(215, Assert.Single(results).DurationSeconds);
    }

    [Fact]
    public async Task SearchSongsAsync_DurationMissing_DurationSecondsIsNull()
    {
        // [AC] REQ-LYR-01: a row without a duration field yields a null DurationSeconds
        var provider = Create(_ => Json(TrackBody(null)));

        var results = (await provider.SearchSongsAsync("creep")).ToList();

        Assert.Null(Assert.Single(results).DurationSeconds);
    }

    [Fact]
    public async Task SearchSongsAsync_DurationZero_DurationSecondsIsNull()
    {
        // [AC] REQ-LYR-01: a duration of 0 is not a usable length and yields a null DurationSeconds
        var provider = Create(_ => Json(TrackBody("0")));

        var results = (await provider.SearchSongsAsync("creep")).ToList();

        Assert.Null(Assert.Single(results).DurationSeconds);
    }

    [Fact]
    public async Task SearchSongsAsync_DurationNegative_DurationSecondsIsNull()
    {
        // [AC] REQ-LYR-01: a negative duration is not a usable length and yields a null DurationSeconds
        var provider = Create(_ => Json(TrackBody("-1")));

        var results = (await provider.SearchSongsAsync("creep")).ToList();

        Assert.Null(Assert.Single(results).DurationSeconds);
    }
}
