using MyVocaList.Domain.Resolution;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Unit.Services;

public class ArtistResolutionServiceTests
{
    private readonly Mock<IArtistRepository> _repoMock = new();
    private readonly Mock<IArtistService> _artistServiceMock = new();
    private readonly Mock<ISimilarityScorer> _scorerMock = new();
    private readonly Mock<ILogger<ArtistResolutionService>> _loggerMock = new();

    // The service now takes IUnitOfWork; PassthroughUnitOfWork runs each wrapped body inline
    // against these same mocks, so every existing assertion holds unchanged (plan.md § Task 1.4).
    private ArtistResolutionService CreateSut() => new(
        _repoMock.Object,
        _artistServiceMock.Object,
        _scorerMock.Object,
        PassthroughUnitOfWork.Over(_repoMock, _artistServiceMock),
        _loggerMock.Object);

    // ── ResolveAsync — external-id hit ────────────────────────────────────

    [Fact]
    // [AC] AC-3.1: GIVEN an API artist with (ExternalProvider, ExternalId) matching an existing artist,
    // THEN resolve to that artist.
    public async Task ResolveAsync_ExternalIdHit_ReturnsExactExternalMatch()
    {
        var existingArtist = new Artist { Id = 7, Name = "Anitta" };
        _repoMock
            .Setup(r => r.GetByExternalIdAsync("dz-123", "Deezer", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingArtist);

        var candidate = new ArtistCandidate("Anitta", "Deezer", "dz-123");
        var sut = CreateSut();

        var result = await sut.ResolveAsync(candidate);

        Assert.Equal(ResolutionKind.ExactExternalMatch, result.Kind);
        Assert.Equal(7, result.ExactMatchArtistId);
        Assert.Empty(result.FuzzyCandidates);
    }

    // ── ResolveAsync — exact name match ───────────────────────────────────

    [Fact]
    // [AC] AC-3.2: GIVEN an API artist name exactly matching an existing artist under NOCASE_NOACCENT,
    // THEN resolve to that artist.
    public async Task ResolveAsync_ExactNameHit_ReturnsExactLocalMatch()
    {
        var existingArtist = new Artist { Id = 3, Name = "Madonna" };
        _repoMock
            .Setup(r => r.GetByExternalIdAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Artist)null);
        _repoMock
            .Setup(r => r.GetByNameAsync("Madonna", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingArtist);

        var candidate = new ArtistCandidate("Madonna", null, null);
        var sut = CreateSut();

        var result = await sut.ResolveAsync(candidate);

        Assert.Equal(ResolutionKind.ExactLocalMatch, result.Kind);
        Assert.Equal(3, result.ExactMatchArtistId);
        Assert.Empty(result.FuzzyCandidates);
    }

    // ── ResolveAsync — fuzzy candidates ──────────────────────────────────

    [Fact]
    // [AC] AC-3.3: GIVEN an API artist name within fuzzy threshold, THEN surface candidate(s).
    public async Task ResolveAsync_FuzzyAboveThreshold_ReturnsFuzzyCandidates()
    {
        _repoMock
            .Setup(r => r.GetByExternalIdAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Artist)null);
        _repoMock
            .Setup(r => r.GetByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Artist)null);
        _repoMock
            .Setup(r => r.SearchNameCandidatesAsync("The Beatle", SimilarityConstants.PoolSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Artist> { new Artist { Id = 1, Name = "The Beatles" } });
        _scorerMock
            .Setup(s => s.Score("The Beatle", "The Beatles"))
            .Returns(0.95);

        var candidate = new ArtistCandidate("The Beatle", null, null);
        var sut = CreateSut();

        var result = await sut.ResolveAsync(candidate);

        Assert.Equal(ResolutionKind.FuzzyCandidates, result.Kind);
        Assert.Single(result.FuzzyCandidates);
        Assert.Equal(1, result.FuzzyCandidates[0].ArtistId);
        Assert.Equal(0.95, result.FuzzyCandidates[0].Score);
    }

    [Fact]
    // [AC] AC-3.3: Candidates below threshold are excluded.
    public async Task ResolveAsync_FuzzyBelowThreshold_ReturnsNoMatch()
    {
        _repoMock
            .Setup(r => r.GetByExternalIdAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Artist)null);
        _repoMock
            .Setup(r => r.GetByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Artist)null);
        _repoMock
            .Setup(r => r.SearchNameCandidatesAsync("Foo Something", SimilarityConstants.PoolSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Artist> { new Artist { Id = 5, Name = "Foobar" } });
        _scorerMock
            .Setup(s => s.Score(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(0.50); // below threshold

        var candidate = new ArtistCandidate("Foo Something", null, null);
        var sut = CreateSut();

        var result = await sut.ResolveAsync(candidate);

        Assert.Equal(ResolutionKind.NoMatch, result.Kind);
    }

    // ── ResolveAsync — no match ────────────────────────────────────────────

    [Fact]
    // [AC] AC-3.4: GIVEN no match, THEN return NoMatch.
    public async Task ResolveAsync_NothingMatches_ReturnsNoMatch()
    {
        _repoMock
            .Setup(r => r.GetByExternalIdAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Artist)null);
        _repoMock
            .Setup(r => r.GetByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Artist)null);
        _repoMock
            .Setup(r => r.SearchNameCandidatesAsync("Unknown Artist", SimilarityConstants.PoolSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Artist>());

        var candidate = new ArtistCandidate("Unknown Artist", null, null);
        var sut = CreateSut();

        var result = await sut.ResolveAsync(candidate);

        Assert.Equal(ResolutionKind.NoMatch, result.Kind);
    }

    // ── ResolveAsync — FTS pool ───────────────────────────────────────────

    [Fact]
    // [AC] REQ-SFT-19: GIVEN a typed/API name with a typo ("Metalica"), THEN the near-duplicate
    // candidates come from the ArtistSearch FTS pool and "Metallica" is surfaced with score >= 0.82.
    public async Task ResolveAsync_Metalica_ReturnsFuzzyCandidateMetallica()
    {
        _repoMock
            .Setup(r => r.GetByExternalIdAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Artist)null);
        _repoMock
            .Setup(r => r.GetByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Artist)null);
        _repoMock
            .Setup(r => r.SearchNameCandidatesAsync("Metalica", SimilarityConstants.PoolSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Artist> { new Artist { Id = 9, Name = "Metallica" } });
        _scorerMock
            .Setup(s => s.Score("Metalica", "Metallica"))
            .Returns(0.94);

        var result = await CreateSut().ResolveAsync(new ArtistCandidate("Metalica", null, null));

        Assert.Equal(ResolutionKind.FuzzyCandidates, result.Kind);
        var candidate = Assert.Single(result.FuzzyCandidates);
        Assert.Equal("Metallica", candidate.Name);
        Assert.True(candidate.Score >= SimilarityConstants.DefaultThreshold);
    }

    [Fact]
    // Design §4: empty/whitespace name → empty pool → NoMatch, no repo call for pool.
    public async Task ResolveAsync_EmptyName_ReturnsNoMatchWithoutPoolQuery()
    {
        _repoMock
            .Setup(r => r.GetByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Artist)null);

        var candidate = new ArtistCandidate("   ", null, null);
        var sut = CreateSut();

        var result = await sut.ResolveAsync(candidate);

        Assert.Equal(ResolutionKind.NoMatch, result.Kind);
        _repoMock.Verify(
            r => r.SearchNameCandidatesAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
