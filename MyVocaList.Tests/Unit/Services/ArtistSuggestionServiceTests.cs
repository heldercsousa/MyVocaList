using MyVocaList.Contracts.DTOs;
using MyVocaList.Contracts.DTOs.Suggestions;
using MyVocaList.Domain.ReadModels;
using MyVocaList.Domain.Resolution;

namespace MyVocaList.Tests.Unit.Services;

public class ArtistSuggestionServiceTests
{
    private readonly Mock<IArtistRepository> _repoMock = new();
    private readonly Mock<IMusicMetadataProvider> _musicBrainzMock = new();
    private readonly Mock<IMusicMetadataProvider> _deezerMock = new();
    private readonly Mock<ISimilarityScorer> _scorerMock = new();
    private readonly Mock<ILogger<ArtistSuggestionService>> _loggerMock = new();

    public ArtistSuggestionServiceTests()
    {
        _musicBrainzMock.Setup(p => p.ProviderName).Returns("MusicBrainz");
        _deezerMock.Setup(p => p.ProviderName).Returns("Deezer");
    }

    private ArtistSuggestionService CreateSut() => new(
        _repoMock.Object,
        new[] { _musicBrainzMock.Object, _deezerMock.Object },
        _scorerMock.Object,
        _loggerMock.Object);

    // ── GetLocalAsync ────────────────────────────────────────────────────

    [Fact]
    // [AC] REQ-FORMUX-01: suggestions require >= 2 chars
    public async Task GetLocalAsync_TermUnderTwoChars_ReturnsEmpty()
    {
        var sut = CreateSut();

        var result = await sut.GetLocalAsync("a");

        Assert.Empty(result);
        _repoMock.Verify(r => r.SearchByNameAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    // [AC] REQ-FORMUX-01: up to 5 local rows
    public async Task GetLocalAsync_ManyMatches_ReturnsAtMostFive()
    {
        var items = Enumerable.Range(1, 5)
            .Select(i => new ArtistListItem(i, $"Artist {i}", "musicbrainz", false, 0))
            .ToList();
        _repoMock
            .Setup(r => r.SearchByNameAsync("Art", 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(items);
        _repoMock
            .Setup(r => r.GetByNameAsync("Art", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Artist)null);

        var sut = CreateSut();

        var result = await sut.GetLocalAsync("Art");

        Assert.Equal(5, result.Count);
        Assert.All(result, r => Assert.False(r.IsRemote));
    }

    // ── GetRemoteAsync — provider order (REQ-FORMUX-02) ────────────────────

    [Fact]
    // [AC] REQ-FORMUX-02: provider order (AC-4.2 in force)
    public async Task GetRemoteAsync_MusicBrainzReturnsResults_DeezerNeverCalled()
    {
        _musicBrainzMock
            .Setup(p => p.SearchArtistsAsync("Anitta", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new MusicSearchResultDto("mb-1", "MusicBrainz", "Anitta", null, null) });

        var sut = CreateSut();

        var result = await sut.GetRemoteAsync("Anitta", []);

        Assert.Single(result);
        _deezerMock.Verify(p => p.SearchArtistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    // [AC] REQ-FORMUX-02
    public async Task GetRemoteAsync_MusicBrainzEmpty_FallsBackToDeezer()
    {
        _musicBrainzMock
            .Setup(p => p.SearchArtistsAsync("Anitta", It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _deezerMock
            .Setup(p => p.SearchArtistsAsync("Anitta", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new MusicSearchResultDto("dz-1", "Deezer", "Anitta", null, null) });

        var sut = CreateSut();

        var result = await sut.GetRemoteAsync("Anitta", []);

        Assert.Single(result);
        Assert.Equal("Deezer", result[0].ExternalProvider);
    }

    [Fact]
    // [AC] REQ-FORMUX-02
    public async Task GetRemoteAsync_MusicBrainzThrows_FallsBackToDeezer()
    {
        _musicBrainzMock
            .Setup(p => p.SearchArtistsAsync("Anitta", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("network down"));
        _deezerMock
            .Setup(p => p.SearchArtistsAsync("Anitta", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new MusicSearchResultDto("dz-1", "Deezer", "Anitta", null, null) });

        var sut = CreateSut();

        var result = await sut.GetRemoteAsync("Anitta", []);

        Assert.Single(result);
        Assert.Equal("Deezer", result[0].ExternalProvider);
    }

    // ── GetRemoteAsync — dedup tier (a): external id ───────────────────────

    [Fact]
    // [AC] REQ-FORMUX-03: dedup tier (a)
    public async Task GetRemoteAsync_ResultSharesExternalIdWithLocal_IsExcluded()
    {
        _musicBrainzMock
            .Setup(p => p.SearchArtistsAsync("Anitta", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new MusicSearchResultDto("mb-1", "MusicBrainz", "Anitta", null, null) });
        var local = new List<ArtistSuggestionDto>
        {
            new(1, "Anitta", "mb-1", "MusicBrainz", false, false)
        };

        var sut = CreateSut();

        var result = await sut.GetRemoteAsync("Anitta", local);

        Assert.Empty(result);
    }

    // ── GetRemoteAsync — dedup tier (b): collation-equal name (batch) ─────

    [Fact]
    // [AC] REQ-FORMUX-03: dedup tier (b)
    public async Task GetRemoteAsync_ResultNameCollationEqualToLocalDb_IsExcluded()
    {
        _musicBrainzMock
            .Setup(p => p.SearchArtistsAsync("Anitta", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new MusicSearchResultDto("mb-1", "MusicBrainz", "Anitta", null, null) });
        _repoMock
            .Setup(r => r.GetByNamesCollatedAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Artist> { new Artist { Id = 9, Name = "Anitta" } });

        var sut = CreateSut();

        var result = await sut.GetRemoteAsync("Anitta", []);

        Assert.Empty(result);
        _repoMock.Verify(
            r => r.GetByNamesCollatedAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── GetRemoteAsync — dedup tier (c): similarity threshold ─────────────

    [Fact]
    // [AC] REQ-FORMUX-03: dedup tier (c)
    public async Task GetRemoteAsync_ResultSimilarAboveThresholdToLocal_IsExcluded()
    {
        _musicBrainzMock
            .Setup(p => p.SearchArtistsAsync("Anita", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new MusicSearchResultDto("mb-1", "MusicBrainz", "Anita", null, null) });
        _repoMock
            .Setup(r => r.GetByNamesCollatedAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Artist>());
        var local = new List<ArtistSuggestionDto> { new(1, "Anitta", null, null, false, false) };
        _scorerMock.Setup(s => s.Score("Anita", "Anitta")).Returns(0.9);

        var sut = CreateSut();

        var result = await sut.GetRemoteAsync("Anita", local);

        Assert.Empty(result);
    }

    [Fact]
    // [AC] REQ-FORMUX-03: dedup tier (c) — below threshold is kept
    public async Task GetRemoteAsync_ResultBelowThreshold_IsKept()
    {
        _musicBrainzMock
            .Setup(p => p.SearchArtistsAsync("Anita", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new MusicSearchResultDto("mb-1", "MusicBrainz", "Anita", null, null) });
        _repoMock
            .Setup(r => r.GetByNamesCollatedAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Artist>());
        var local = new List<ArtistSuggestionDto> { new(1, "Anitta", null, null, false, false) };
        _scorerMock.Setup(s => s.Score("Anita", "Anitta")).Returns(0.5);

        var sut = CreateSut();

        var result = await sut.GetRemoteAsync("Anita", local);

        Assert.Single(result);
    }

    // ── GetRemoteAsync — all providers fail ────────────────────────────────

    [Fact]
    // [AC] REQ-FORMUX-05: silent local-only degradation, logged
    public async Task GetRemoteAsync_AllProvidersFail_ReturnsEmptyAndLogs()
    {
        _musicBrainzMock
            .Setup(p => p.SearchArtistsAsync("Anitta", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("mb down"));
        _deezerMock
            .Setup(p => p.SearchArtistsAsync("Anitta", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("dz down"));

        var sut = CreateSut();

        var result = await sut.GetRemoteAsync("Anitta", []);

        Assert.Empty(result);
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.AtLeastOnce);
    }

    // ── GetRemoteAsync — cancellation ──────────────────────────────────────

    [Fact]
    // [AC] REQ-FORMUX-02: stale lookups cancellable (Failure modes: stale results discarded)
    public async Task GetRemoteAsync_Cancelled_ThrowsOperationCanceled()
    {
        using var cts = new CancellationTokenSource();
        _musicBrainzMock
            .Setup(p => p.SearchArtistsAsync("Anitta", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var sut = CreateSut();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => sut.GetRemoteAsync("Anitta", [], cts.Token));
    }

    // ── FilterSimilar ────────────────────────────────────────────────────

    [Fact]
    // [AC] REQ-FORMUX-10: similar = >= 0.82 AND not exact, cache-only
    public void FilterSimilar_ScoreAtThresholdNonExact_IsSimilar()
    {
        var cached = new List<ArtistSuggestionDto> { new(1, "Anitta", null, null, false, false) };
        _scorerMock.Setup(s => s.Score("Anita", "Anitta")).Returns(SimilarityConstants.DefaultThreshold);

        var sut = CreateSut();

        var result = sut.FilterSimilar("Anita", cached);

        Assert.Single(result);
    }

    [Fact]
    // [AC] REQ-FORMUX-10
    public void FilterSimilar_ScoreBelowThreshold_IsNotSimilar()
    {
        var cached = new List<ArtistSuggestionDto> { new(1, "Anitta", null, null, false, false) };
        _scorerMock.Setup(s => s.Score("Anita", "Anitta")).Returns(0.5);

        var sut = CreateSut();

        var result = sut.FilterSimilar("Anita", cached);

        Assert.Empty(result);
    }

    [Fact]
    // [AC] REQ-FORMUX-10
    public void FilterSimilar_ExactMatch_IsNotSimilar()
    {
        var cached = new List<ArtistSuggestionDto> { new(1, "Anitta", null, null, false, true) };
        _scorerMock.Setup(s => s.Score("Anitta", "Anitta")).Returns(1.0);

        var sut = CreateSut();

        var result = sut.FilterSimilar("Anitta", cached);

        Assert.Empty(result);
    }
}
