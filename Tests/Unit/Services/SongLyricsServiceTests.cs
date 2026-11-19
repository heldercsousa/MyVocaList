using System.Diagnostics;
using MyVocaList.Contracts.DTOs;
using MyVocaList.Domain.Constants;
using MyVocaList.Domain.ServicesInterfaces;
using Microsoft.Extensions.Logging.Abstractions;

namespace MyVocaList.Tests.Unit.Services;

/// <summary>
/// L2b: <see cref="SongLyricsService"/> (design § 2.3, REQ-LYR-02, 03, 05, 06, 07, 08). The provider and network
/// status are Moq; the logger is <see cref="NullLogger{T}"/>. The bound test takes about 5 s, as the
/// 3,000 ms bound test in <see cref="SongSuggestionServiceTests"/> does (no shortening hook exists).
/// </summary>
public class SongLyricsServiceTests
{
    private static readonly LyricsQuery BareQuery = new("Imagine", "John Lennon");
    private static readonly LyricsQuery FullQuery = new("Imagine", "John Lennon", "Imagine", 183);

    private readonly Mock<ILyricsProvider> _provider = new();
    private readonly Mock<INetworkStatus> _network = new();
    private readonly List<(LyricsQuery Query, CancellationToken Ct)> _calls = [];

    public SongLyricsServiceTests()
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

    [Fact]
    public async Task FindLyricsAsync_Offline_ReturnsNullWithoutProviderCall()
    {
        // [AC] REQ-LYR-07: offline, the lookup starts no HTTP request to the lyrics source
        _network.SetupGet(n => n.HasInternetAccess).Returns(false);
        var sut = CreateSut();

        var result = await sut.FindLyricsAsync(FullQuery);

        Assert.Null(result);
        _provider.Verify(p => p.FetchLyricsAsync(It.IsAny<LyricsQuery>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("", "John Lennon")]
    [InlineData("   ", "John Lennon")]
    [InlineData("Imagine", "")]
    [InlineData("Imagine", "  ")]
    public async Task FindLyricsAsync_BlankTitleOrArtist_ReturnsNullWithoutProviderCall(string title, string artist)
    {
        // [AC] REQ-LYR-01: a lookup needs a title and an artist; a blank one starts no request
        var sut = CreateSut();

        var result = await sut.FindLyricsAsync(new LyricsQuery(title, artist, "Imagine", 183));

        Assert.Null(result);
        Assert.Empty(_calls);
    }

    [Fact]
    public async Task FindLyricsAsync_MatchWithSurroundingWhiteSpace_ReturnsTrimmedText()
    {
        // [AC] REQ-LYR-02: the matched plain lyrics are returned trimmed
        SetupAnswers("  \n Imagine all the people \r\n  ");
        var sut = CreateSut();

        var result = await sut.FindLyricsAsync(BareQuery);

        Assert.Equal("Imagine all the people", result);
    }

    [Fact]
    public async Task FindLyricsAsync_BareQueryNoMatch_ReturnsNullAfterOneCall()
    {
        // [AC] REQ-LYR-08: a query with neither album nor duration is never repeated
        SetupAnswers((string?)null);
        var sut = CreateSut();

        var result = await sut.FindLyricsAsync(BareQuery);

        Assert.Null(result);
        Assert.Single(_calls);
    }

    [Theory]
    [InlineData("Imagine", 183)]
    [InlineData("Imagine", null)]
    [InlineData(null, 183)]
    public async Task FindLyricsAsync_NoMatchWithAlbumOrDuration_SecondAttemptHasTitleAndArtistOnly(
        string? album, int? duration)
    {
        // [AC] REQ-LYR-08: after a no-match on a query with album or duration, one second request carries title and artist only
        SetupAnswers((string?)null, "Imagine all the people");
        var sut = CreateSut();

        var result = await sut.FindLyricsAsync(new LyricsQuery("Imagine", "John Lennon", album, duration));

        Assert.Equal("Imagine all the people", result);
        Assert.Equal(2, _calls.Count);
        Assert.Null(_calls[1].Query.AlbumTitle);
        Assert.Null(_calls[1].Query.DurationSeconds);
        Assert.Equal("Imagine", _calls[1].Query.Title);
        Assert.Equal("John Lennon", _calls[1].Query.ArtistName);
    }

    [Fact]
    public async Task FindLyricsAsync_BlankAlbumAndNoDuration_IsNeverRepeated()
    {
        // [AC] REQ-LYR-08: a blank album counts as no album, so the query with no duration is not repeated
        SetupAnswers((string?)null, "unused");
        var sut = CreateSut();

        var result = await sut.FindLyricsAsync(new LyricsQuery("Imagine", "John Lennon", "   ", null));

        Assert.Null(result);
        Assert.Single(_calls);
    }

    [Fact]
    public async Task FindLyricsAsync_BothAttemptsNoMatch_ReturnsNullAfterExactlyTwoCalls()
    {
        // [AC] REQ-LYR-03: no record on either attempt leaves the result null, with no message
        SetupAnswers((string?)null, (string?)null);
        var sut = CreateSut();

        var result = await sut.FindLyricsAsync(FullQuery);

        Assert.Null(result);
        Assert.Equal(2, _calls.Count);
    }

    [Fact]
    public async Task FindLyricsAsync_FirstAttemptReturnsText_OneCallOnly()
    {
        // [AC] REQ-LYR-08: a non-null first answer is never followed by a second call
        SetupAnswers("Imagine all the people");
        var sut = CreateSut();

        var result = await sut.FindLyricsAsync(FullQuery);

        Assert.Equal("Imagine all the people", result);
        Assert.Single(_calls);
    }

    [Fact]
    public async Task FindLyricsAsync_TextOfExactlyMaxLength_IsKept()
    {
        // [AC] REQ-LYR-05: lyrics of exactly 10,000 characters are kept
        var text = new string('a', SearchConstants.LyricsMaxLength);
        SetupAnswers(text);
        var sut = CreateSut();

        var result = await sut.FindLyricsAsync(BareQuery);

        Assert.Equal(text, result);
    }

    [Fact]
    public async Task FindLyricsAsync_TextOneOverMaxLength_ReturnsNull()
    {
        // [AC] REQ-LYR-05: lyrics longer than 10,000 characters are not used
        SetupAnswers(new string('a', SearchConstants.LyricsMaxLength + 1));
        var sut = CreateSut();

        var result = await sut.FindLyricsAsync(BareQuery);

        Assert.Null(result);
    }

    [Fact]
    public async Task FindLyricsAsync_WhiteSpaceOnlyAnswer_ReturnsNullWithoutSecondCall()
    {
        // [AC] REQ-LYR-03: blank plain lyrics are no match, and a non-null answer is not retried (REQ-LYR-08)
        SetupAnswers("  \n\t  ");
        var sut = CreateSut();

        var result = await sut.FindLyricsAsync(FullQuery);

        Assert.Null(result);
        Assert.Single(_calls);
    }

    [Fact]
    public async Task FindLyricsAsync_ProviderThrowsHttpRequestException_ReturnsNullWithoutException()
    {
        // [AC] REQ-LYR-07: a source failure ends the lookup with no match and no exception
        _provider
            .Setup(p => p.FetchLyricsAsync(It.IsAny<LyricsQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("connection refused"));
        var sut = CreateSut();

        var result = await sut.FindLyricsAsync(BareQuery);

        Assert.Null(result);
    }

    [Fact]
    public async Task FindLyricsAsync_ProviderNeverAnswers_ReturnsNullAfterBound()
    {
        // [AC] REQ-LYR-06: a lookup that does not end within 5,000 ms ends with no match
        _provider
            .Setup(p => p.FetchLyricsAsync(It.IsAny<LyricsQuery>(), It.IsAny<CancellationToken>()))
            .Returns<LyricsQuery, CancellationToken>(async (_, ct) =>
            {
                await Task.Delay(Timeout.Infinite, ct);
                return null;
            });
        var sut = CreateSut();
        var stopwatch = Stopwatch.StartNew();

        var result = await sut.FindLyricsAsync(BareQuery);

        stopwatch.Stop();
        Assert.Null(result);
        Assert.True(stopwatch.ElapsedMilliseconds >= 4500, $"ended after {stopwatch.ElapsedMilliseconds} ms");
    }

    [Fact]
    public async Task FindLyricsAsync_BoundCoversBothAttempts_SecondAttemptRunsUnderSameLinkedToken()
    {
        // [AC] REQ-LYR-06: the bound covers the whole lookup; the second attempt uses the same linked token
        _provider
            .Setup(p => p.FetchLyricsAsync(It.IsAny<LyricsQuery>(), It.IsAny<CancellationToken>()))
            .Returns<LyricsQuery, CancellationToken>(async (q, ct) =>
            {
                _calls.Add((q, ct));
                if (_calls.Count == 1)
                    return null;
                await Task.Delay(Timeout.Infinite, ct);
                return null;
            });
        var sut = CreateSut();

        var result = await sut.FindLyricsAsync(FullQuery);

        Assert.Null(result);
        Assert.Equal(2, _calls.Count);
        Assert.Equal(_calls[0].Ct, _calls[1].Ct);
        Assert.True(_calls[1].Ct.IsCancellationRequested, "the second attempt was ended by the bound");
    }

    [Fact]
    public async Task FindLyricsAsync_CallerCancelledBeforeCall_RethrowsOperationCanceled()
    {
        // [AC] REQ-LYR-07: caller cancellation is the only exception allowed out of the service
        _provider
            .Setup(p => p.FetchLyricsAsync(It.IsAny<LyricsQuery>(), It.IsAny<CancellationToken>()))
            .Returns<LyricsQuery, CancellationToken>(async (_, ct) =>
            {
                await Task.Delay(Timeout.Infinite, ct);
                return null;
            });
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var sut = CreateSut();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sut.FindLyricsAsync(BareQuery, cts.Token));
    }

    [Fact]
    public async Task FindLyricsAsync_CallerCancelledDuringProviderCall_RethrowsOperationCanceled()
    {
        // [AC] REQ-LYR-07: a caller cancellation during the provider call propagates, it is not turned into null
        _provider
            .Setup(p => p.FetchLyricsAsync(It.IsAny<LyricsQuery>(), It.IsAny<CancellationToken>()))
            .Returns<LyricsQuery, CancellationToken>(async (_, ct) =>
            {
                await Task.Delay(Timeout.Infinite, ct);
                return null;
            });
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(100);
        var sut = CreateSut();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sut.FindLyricsAsync(BareQuery, cts.Token));
    }
}
