using System.Diagnostics;
using MyVocaList.Contracts.DTOs;
using MyVocaList.Domain.Constants;
using MyVocaList.Domain.ServicesInterfaces;
using Microsoft.Extensions.Logging.Abstractions;

namespace MyVocaList.Tests.Unit.Services;

/// <summary>
/// REQ-SLD-20, 22, 23, 29: <see cref="SongLyricsService.LookupLyricsAsync"/> tells a definite "no match" from
/// "no answer". The provider and network status are Moq; the logger is <see cref="NullLogger{T}"/>.
/// The bound test takes about 5 s, as <see cref="SongLyricsServiceTests"/> does.
/// </summary>
public class SongLyricsServiceLookupTests
{
    private static readonly LyricsQuery BareQuery = new("Imagine", "John Lennon");

    private readonly Mock<ILyricsProvider> _provider = new();
    private readonly Mock<INetworkStatus> _network = new();
    private readonly List<(LyricsQuery Query, CancellationToken Ct)> _calls = [];

    public SongLyricsServiceLookupTests()
    {
        _network.SetupGet(n => n.HasInternetAccess).Returns(true);
    }

    private SongLyricsService CreateSut() =>
        new(_provider.Object, _network.Object, NullLogger<SongLyricsService>.Instance);

    /// <summary>Provider answers the successive calls with <paramref name="answers"/>; null once they run out.</summary>
    private void SetupAnswers(params string?[] answers)
    {
        var index = 0;
        _provider
            .Setup(p => p.FetchLyricsAsync(It.IsAny<LyricsQuery>(), It.IsAny<CancellationToken>()))
            .Returns<LyricsQuery, CancellationToken>((q, ct) =>
            {
                _calls.Add((q, ct));
                var answer = index < answers.Length ? answers[index] : null;
                index++;
                return Task.FromResult(answer);
            });
    }

    private void SetupNeverAnswers()
    {
        _provider
            .Setup(p => p.FetchLyricsAsync(It.IsAny<LyricsQuery>(), It.IsAny<CancellationToken>()))
            .Returns<LyricsQuery, CancellationToken>(async (_, ct) =>
            {
                await Task.Delay(Timeout.Infinite, ct);
                return null;
            });
    }

    // [AC] REQ-SLD-20: a usable answer is Found, with the text trimmed
    [Fact]
    public async Task LookupLyricsAsync_Match_ReturnsFoundWithTrimmedLyrics()
    {
        SetupAnswers("  Imagine all the people \r\n");
        var sut = CreateSut();

        var result = await sut.LookupLyricsAsync(BareQuery);

        Assert.Equal(LyricsLookupResult.Found("Imagine all the people"), result);
    }

    // [AC] REQ-SLD-22: the source has no record for the song, so the lookup is a definite NoMatch
    [Fact]
    public async Task LookupLyricsAsync_ProviderHasNoRecord_ReturnsNoMatch()
    {
        SetupAnswers((string?)null);
        var sut = CreateSut();

        var result = await sut.LookupLyricsAsync(BareQuery);

        Assert.Equal(LyricsLookupResult.NoMatch, result);
    }

    // [AC] REQ-SLD-22: blank lyrics from the source are a definite NoMatch
    [Fact]
    public async Task LookupLyricsAsync_BlankAnswer_ReturnsNoMatch()
    {
        SetupAnswers("  \n\t  ");
        var sut = CreateSut();

        var result = await sut.LookupLyricsAsync(BareQuery);

        Assert.Equal(LyricsLookupResult.NoMatch, result);
    }

    // [AC] REQ-SLD-22: lyrics over the length limit are a definite NoMatch
    [Fact]
    public async Task LookupLyricsAsync_TextOverTheLimit_ReturnsNoMatch()
    {
        SetupAnswers(new string('a', SearchConstants.LyricsMaxLength + 1));
        var sut = CreateSut();

        var result = await sut.LookupLyricsAsync(BareQuery);

        Assert.Equal(LyricsLookupResult.NoMatch, result);
    }

    // [AC] REQ-SLD-23: offline, the lookup is NoAnswer and no request is sent
    [Fact]
    public async Task LookupLyricsAsync_Offline_ReturnsNoAnswerWithoutProviderCall()
    {
        _network.SetupGet(n => n.HasInternetAccess).Returns(false);
        var sut = CreateSut();

        var result = await sut.LookupLyricsAsync(BareQuery);

        Assert.Equal(LyricsLookupResult.NoAnswer, result);
        _provider.Verify(p => p.FetchLyricsAsync(It.IsAny<LyricsQuery>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // [AC] REQ-SLD-23: a provider failure is NoAnswer, not a definite no match
    [Fact]
    public async Task LookupLyricsAsync_ProviderThrows_ReturnsNoAnswer()
    {
        _provider
            .Setup(p => p.FetchLyricsAsync(It.IsAny<LyricsQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("connection refused"));
        var sut = CreateSut();

        var result = await sut.LookupLyricsAsync(BareQuery);

        Assert.Equal(LyricsLookupResult.NoAnswer, result);
    }

    // [AC] REQ-SLD-23: the 5,000 ms bound ending the lookup is NoAnswer, not a definite no match
    [Fact]
    public async Task LookupLyricsAsync_BoundReached_ReturnsNoAnswer()
    {
        SetupNeverAnswers();
        var sut = CreateSut();
        var stopwatch = Stopwatch.StartNew();

        var result = await sut.LookupLyricsAsync(BareQuery);

        stopwatch.Stop();
        Assert.Equal(LyricsLookupResult.NoAnswer, result);
        Assert.True(stopwatch.ElapsedMilliseconds >= 4500, $"ended after {stopwatch.ElapsedMilliseconds} ms");
    }

    // [AC] REQ-SLD-29: with title and artist only, the lookup sends exactly one request
    [Fact]
    public async Task LookupLyricsAsync_TitleAndArtistOnly_SendsOneRequest()
    {
        SetupAnswers((string?)null, "unused");
        var sut = CreateSut();

        await sut.LookupLyricsAsync(BareQuery);

        Assert.Single(_calls);
        Assert.Equal("Imagine", _calls[0].Query.Title);
        Assert.Equal("John Lennon", _calls[0].Query.ArtistName);
    }

    // [AC] REQ-SLD-23: caller cancellation is rethrown, it is not turned into NoAnswer
    [Fact]
    public async Task LookupLyricsAsync_CallerCancelled_Rethrows()
    {
        SetupNeverAnswers();
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(100);
        var sut = CreateSut();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sut.LookupLyricsAsync(BareQuery, cts.Token));
    }
}
