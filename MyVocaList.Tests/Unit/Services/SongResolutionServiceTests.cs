using MyVocaList.Domain.Resolution;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Unit.Services;

public class SongResolutionServiceTests
{
    private readonly Mock<ISongRepository> _songRepoMock = new();
    private readonly Mock<IArtistResolutionService> _artistResolutionMock = new();
    private readonly Mock<ISongService> _songServiceMock = new();
    private readonly Mock<ISimilarityScorer> _scorerMock = new();
    private readonly Mock<ILogger<SongResolutionService>> _loggerMock = new();

    // The service now takes IUnitOfWork; PassthroughUnitOfWork runs each wrapped body inline
    // against these same mocks, so every existing assertion holds unchanged (plan.md § Task 1.4).
    private SongResolutionService CreateSut() => new(
        _songRepoMock.Object,
        _artistResolutionMock.Object,
        _songServiceMock.Object,
        _scorerMock.Object,
        PassthroughUnitOfWork.Over(_songRepoMock, _artistResolutionMock, _songServiceMock),
        _loggerMock.Object);

    // ── Helper defaults ────────────────────────────────────────────────────

    private static ArtistResolution ArtistExactLocal(int id) =>
        new(ResolutionKind.ExactLocalMatch, id, []);

    private static ArtistResolution ArtistNoMatch() =>
        new(ResolutionKind.NoMatch, null, []);

    private SongCandidate MakeCandidate(
        string title = "Bohemian Rhapsody",
        string version = "",
        string? externalProvider = null,
        string? externalId = null,
        string? featured = null,
        string? lyrics = null) =>
        new(title, version, featured, lyrics,
            new ArtistCandidate("Queen", null, null),
            externalProvider, externalId);

    // ── ResolveAsync — artist first (INV-1) ───────────────────────────────

    [Fact]
    // [AC] AC-2.5: artist resolved FIRST before song matching.
    public async Task ResolveAsync_ArtistUnresolved_ReturnsNoMatchWithoutSongLookup()
    {
        _artistResolutionMock
            .Setup(a => a.ResolveAsync(It.IsAny<ArtistCandidate>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ArtistNoMatch());

        var candidate = MakeCandidate();
        var sut = CreateSut();

        var result = await sut.ResolveAsync(candidate);

        Assert.Equal(ResolutionKind.NoMatch, result.Kind);
        // No song repo calls when artist is unresolved (except external-id check)
        _songRepoMock.Verify(
            r => r.FindIdByTitleVersionForArtistAsync(
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── ResolveAsync — external id match ──────────────────────────────────

    [Fact]
    // [AC] AC-2.1: API result with matching (ExternalProvider, ExternalId) → ExactExternalMatch.
    public async Task ResolveAsync_ExternalIdHit_ReturnsExactExternalMatch()
    {
        _artistResolutionMock
            .Setup(a => a.ResolveAsync(It.IsAny<ArtistCandidate>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ArtistExactLocal(10));

        var existingSong = new Song
        {
            Id = 42,
            Title = "Bohemian Rhapsody",
            Version = ""
        };
        _songRepoMock
            .Setup(r => r.GetByExternalIdAsync("dz-99", "Deezer", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSong);

        var candidate = MakeCandidate(externalProvider: "Deezer", externalId: "dz-99");
        var sut = CreateSut();

        var result = await sut.ResolveAsync(candidate);

        Assert.Equal(ResolutionKind.ExactExternalMatch, result.Kind);
        Assert.Equal(42, result.ExactMatchSongId);
    }

    // ── ResolveAsync — exact local match ──────────────────────────────────

    [Fact]
    // [AC] AC-2.2: No external match but exact (ArtistId, Title, Version) local match.
    public async Task ResolveAsync_ExactLocalHit_ReturnsExactLocalMatch()
    {
        _artistResolutionMock
            .Setup(a => a.ResolveAsync(It.IsAny<ArtistCandidate>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ArtistExactLocal(10));

        _songRepoMock
            .Setup(r => r.GetByExternalIdAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Song)null);

        _songRepoMock
            .Setup(r => r.FindIdByTitleVersionForArtistAsync(10, "Bohemian Rhapsody", "", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        var candidate = MakeCandidate();
        var sut = CreateSut();

        var result = await sut.ResolveAsync(candidate);

        Assert.Equal(ResolutionKind.ExactLocalMatch, result.Kind);
        Assert.Equal(5, result.ExactMatchSongId);
    }

    [Fact]
    // [AC] REQ-SFT-42: an accent-only title difference returns an exact match carrying the stored song's id.
    public async Task ResolveAsync_AccentOnlyTitleDifference_ReturnsExactLocalMatchWithId()
    {
        _artistResolutionMock
            .Setup(a => a.ResolveAsync(It.IsAny<ArtistCandidate>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ArtistExactLocal(7));
        _songRepoMock
            .Setup(r => r.FindIdByTitleVersionForArtistAsync(7, "Nao Sei", "", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);

        var sut = CreateSut();

        var result = await sut.ResolveAsync(MakeCandidate(title: "Nao Sei"));

        Assert.Equal(ResolutionKind.ExactLocalMatch, result.Kind);
        Assert.Equal(42, result.ExactMatchSongId);
    }

    // ── ResolveAsync — fuzzy candidates ──────────────────────────────────

    [Fact]
    // [AC] AC-2.3: Fuzzy candidates surfaced when no exact match.
    public async Task ResolveAsync_FuzzyAboveThreshold_ReturnsFuzzyCandidates()
    {
        _artistResolutionMock
            .Setup(a => a.ResolveAsync(It.IsAny<ArtistCandidate>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ArtistExactLocal(10));

        _songRepoMock
            .Setup(r => r.GetByExternalIdAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Song)null);

        _songRepoMock
            .Setup(r => r.FindIdByTitleVersionForArtistAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int?)null);

        var fuzzySong = new Song { Id = 7, Title = "Bohemian Rhapsdy", Version = "" };
        _songRepoMock
            .Setup(r => r.GetFuzzyCandidatePoolAsync(10, "Bohemian", SimilarityConstants.PoolSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Song> { fuzzySong });

        _scorerMock
            .Setup(s => s.Score("Bohemian Rhapsody", "Bohemian Rhapsdy"))
            .Returns(0.90);

        var candidate = MakeCandidate();
        var sut = CreateSut();

        var result = await sut.ResolveAsync(candidate);

        Assert.Equal(ResolutionKind.FuzzyCandidates, result.Kind);
        Assert.Single(result.FuzzyCandidates);
        Assert.Equal(7, result.FuzzyCandidates[0].SongId);
        Assert.Equal(0.90, result.FuzzyCandidates[0].Score);
    }

    [Fact]
    // [AC] AC-2.3: No fuzzy candidates → NoMatch.
    public async Task ResolveAsync_NoMatchAtAll_ReturnsNoMatch()
    {
        _artistResolutionMock
            .Setup(a => a.ResolveAsync(It.IsAny<ArtistCandidate>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ArtistExactLocal(10));

        _songRepoMock
            .Setup(r => r.GetByExternalIdAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Song)null);

        _songRepoMock
            .Setup(r => r.FindIdByTitleVersionForArtistAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int?)null);

        _songRepoMock
            .Setup(r => r.GetFuzzyCandidatePoolAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Song>());

        var candidate = MakeCandidate();
        var sut = CreateSut();

        var result = await sut.ResolveAsync(candidate);

        Assert.Equal(ResolutionKind.NoMatch, result.Kind);
    }

    // ── ResolveAsync — titlePrefixToken ───────────────────────────────────

    [Fact]
    // Design §4 N1: title prefix token is first whitespace token, capped at 12 chars.
    public async Task ResolveAsync_LongTitle_UsesPrefixTokenForFuzzyPool()
    {
        _artistResolutionMock
            .Setup(a => a.ResolveAsync(It.IsAny<ArtistCandidate>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ArtistExactLocal(10));

        _songRepoMock
            .Setup(r => r.GetByExternalIdAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Song)null);

        _songRepoMock
            .Setup(r => r.FindIdByTitleVersionForArtistAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int?)null);

        string capturedToken = null;
        _songRepoMock
            .Setup(r => r.GetFuzzyCandidatePoolAsync(10, It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback<int, string, int, CancellationToken>((_, token, _, _) => capturedToken = token)
            .ReturnsAsync(new List<Song>());

        // First token "VeryLongFirs" is 20 chars but capped at 12
        var candidate = MakeCandidate(title: "VeryLongFirstToken More");
        var sut = CreateSut();

        await sut.ResolveAsync(candidate);

        Assert.Equal("VeryLongFirs", capturedToken); // 12 chars
    }
}
