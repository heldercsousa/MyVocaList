using MyVocaList.Domain.Resolution;

namespace MyVocaList.Tests.Unit.Services;

public class SongResolutionServiceTests
{
    private readonly Mock<ISongRepository> _songRepoMock = new();
    private readonly Mock<IArtistResolutionService> _artistResolutionMock = new();
    private readonly Mock<ISongService> _songServiceMock = new();
    private readonly Mock<ISimilarityScorer> _scorerMock = new();
    private readonly Mock<ILogger<SongResolutionService>> _loggerMock = new();

    private SongResolutionService CreateSut() => new(
        _songRepoMock.Object,
        _artistResolutionMock.Object,
        _songServiceMock.Object,
        _scorerMock.Object,
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
            r => r.ExistsByTitleVersionForArtistAsync(
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
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
            Version = "",
            HasManualEdits = false
        };
        _songRepoMock
            .Setup(r => r.GetByExternalIdAsync("dz-99", "Deezer", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSong);

        var candidate = MakeCandidate(externalProvider: "Deezer", externalId: "dz-99");
        var sut = CreateSut();

        var result = await sut.ResolveAsync(candidate);

        Assert.Equal(ResolutionKind.ExactExternalMatch, result.Kind);
        Assert.Equal(42, result.ExactMatchSongId);
        Assert.Empty(result.FieldDiffs);
        Assert.False(result.TargetHasManualEdits);
    }

    [Fact]
    // [AC] AC-4.2: When target HasManualEdits, FieldDiffs are populated for ExactExternalMatch.
    public async Task ResolveAsync_ExternalIdHitWithManualEdits_PopulatesFieldDiffs()
    {
        _artistResolutionMock
            .Setup(a => a.ResolveAsync(It.IsAny<ArtistCandidate>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ArtistExactLocal(10));

        var existingSong = new Song
        {
            Id = 42,
            Title = "Bohemian Rhapsody — Edited",
            Version = "",
            Lyrics = "edited lyrics",
            HasManualEdits = true
        };
        _songRepoMock
            .Setup(r => r.GetByExternalIdAsync("dz-99", "Deezer", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSong);

        var candidate = MakeCandidate(
            title: "Bohemian Rhapsody",
            externalProvider: "Deezer",
            externalId: "dz-99",
            lyrics: "original lyrics");
        var sut = CreateSut();

        var result = await sut.ResolveAsync(candidate);

        Assert.Equal(ResolutionKind.ExactExternalMatch, result.Kind);
        Assert.True(result.TargetHasManualEdits);
        Assert.NotEmpty(result.FieldDiffs);
        // FieldDiffs should include Title and Lyrics (both differ), NOT ArtistId
        Assert.Contains(result.FieldDiffs, d => d.Field == "Title");
        Assert.Contains(result.FieldDiffs, d => d.Field == "Lyrics");
        Assert.DoesNotContain(result.FieldDiffs, d => d.Field == "ArtistId");
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
            .Setup(r => r.ExistsByTitleVersionForArtistAsync(10, "Bohemian Rhapsody", "", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var matchedSong = new Song { Id = 5, Title = "Bohemian Rhapsody", Version = "", HasManualEdits = false };
        _songRepoMock
            .Setup(r => r.GetFuzzyCandidatePoolAsync(10, It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Song> { matchedSong });

        var candidate = MakeCandidate();
        var sut = CreateSut();

        var result = await sut.ResolveAsync(candidate);

        Assert.Equal(ResolutionKind.ExactLocalMatch, result.Kind);
        Assert.Equal(5, result.ExactMatchSongId);
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
            .Setup(r => r.ExistsByTitleVersionForArtistAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

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
            .Setup(r => r.ExistsByTitleVersionForArtistAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

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
            .Setup(r => r.ExistsByTitleVersionForArtistAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

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

    // ── FieldDiffs restricted to mergeable set (N4) ───────────────────────

    [Fact]
    // [AC] N4: FieldDiffs only contain Title, FeaturedArtists, Lyrics, Version — never ArtistId.
    public async Task ResolveAsync_ExternalMatch_FieldDiffsRestrictedToMergeableSet()
    {
        _artistResolutionMock
            .Setup(a => a.ResolveAsync(It.IsAny<ArtistCandidate>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ArtistExactLocal(10));

        var existingSong = new Song
        {
            Id = 42,
            Title = "Different Title",
            Version = "v1",
            FeaturedArtists = "someone",
            Lyrics = "old lyrics",
            HasManualEdits = true
        };
        _songRepoMock
            .Setup(r => r.GetByExternalIdAsync("dz-1", "Deezer", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSong);

        var candidate = MakeCandidate(
            title: "New Title",
            version: "v2",
            featured: "new featured",
            lyrics: "new lyrics",
            externalProvider: "Deezer",
            externalId: "dz-1");
        var sut = CreateSut();

        var result = await sut.ResolveAsync(candidate);

        Assert.All(result.FieldDiffs, d =>
            Assert.True(
                d.Field is "Title" or "FeaturedArtists" or "Lyrics" or "Version",
                $"Unexpected field in FieldDiffs: {d.Field}"));
        Assert.DoesNotContain(result.FieldDiffs, d => d.Field == "ArtistId");
    }

    // ── CommitAsync — CreateNewVersion requires non-empty Version ─────────

    [Fact]
    // [AC] AC-1.2: CreateNewVersion with empty Version returns failure.
    public async Task CommitAsync_CreateNewVersion_EmptyVersion_ReturnsFalse()
    {
        var candidate = MakeCandidate(version: "   ");
        var sut = CreateSut();

        var (success, message, song) = await sut.CommitAsync(
            candidate, ResolutionChoice.CreateNewVersion, null, null);

        Assert.False(success);
        Assert.NotEmpty(message);
        Assert.Null(song);
    }

    [Fact]
    // [AC] AC-1.2: CreateNewVersion with whitespace-only Version returns failure.
    public async Task CommitAsync_CreateNewVersion_WhitespaceVersion_ReturnsFalse()
    {
        var candidate = MakeCandidate(version: "");
        var sut = CreateSut();

        var (success, message, song) = await sut.CommitAsync(
            candidate, ResolutionChoice.CreateNewVersion, null, null);

        Assert.False(success);
        Assert.Null(song);
    }

    // ── CommitAsync — UpdateExisting with manual edits (AC-4.2) ──────────

    [Fact]
    // [AC] AC-4.2: UpdateExisting with HasManualEdits=true applies ONLY acceptedFields.
    public async Task CommitAsync_UpdateExisting_ManualEdits_AppliesOnlyAcceptedFields()
    {
        var song = new Song
        {
            Id = 10,
            Title = "Original Title",
            Version = "",
            FeaturedArtists = "Original Featured",
            Lyrics = "Original Lyrics",
            HasManualEdits = true
        };
        _songRepoMock
            .Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(song);
        _songServiceMock
            .Setup(s => s.UpdateSongAsync(
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, "Updated"));

        var candidate = MakeCandidate(
            title: "New Title",
            featured: "New Featured",
            lyrics: "New Lyrics");

        // Only accept "Title" — FeaturedArtists and Lyrics should NOT change
        var acceptedFields = new List<string> { "Title" };
        var sut = CreateSut();

        var (success, _, _) = await sut.CommitAsync(
            candidate, ResolutionChoice.UpdateExisting, 10, acceptedFields);

        Assert.True(success);
        // Verify UpdateSongAsync was called with the new title but original featured/lyrics
        _songServiceMock.Verify(s => s.UpdateSongAsync(
            10,
            "New Title",          // accepted
            "Original Featured",  // NOT accepted — unchanged
            "Original Lyrics",    // NOT accepted — unchanged
            true,
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    // [AC] AC-4.1: UpdateExisting with HasManualEdits=false overwrites all non-empty API fields.
    public async Task CommitAsync_UpdateExisting_NoManualEdits_OverwritesNonEmptyFields()
    {
        var song = new Song
        {
            Id = 10,
            Title = "Old Title",
            Version = "",
            FeaturedArtists = "Old Featured",
            Lyrics = "Old Lyrics",
            HasManualEdits = false
        };
        _songRepoMock
            .Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(song);
        _songServiceMock
            .Setup(s => s.UpdateSongAsync(
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, "Updated"));

        var candidate = MakeCandidate(
            title: "New Title",
            featured: "New Featured",
            lyrics: "New Lyrics");

        var sut = CreateSut();

        var (success, _, _) = await sut.CommitAsync(
            candidate, ResolutionChoice.UpdateExisting, 10, null);

        Assert.True(success);
        // All non-empty API fields should overwrite
        _songServiceMock.Verify(s => s.UpdateSongAsync(
            10,
            "New Title",
            "New Featured",
            "New Lyrics",
            false,
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── CommitAsync — AttachExternalId sets identity only ─────────────────

    [Fact]
    // [AC] AC-2.2: AttachExternalId attaches external identity without changing other fields.
    public async Task CommitAsync_AttachExternalId_SetsExternalIdentityOnly()
    {
        var song = new Song
        {
            Id = 5,
            Title = "My Song",
            Version = "",
            FeaturedArtists = null,
            Lyrics = null,
            ExternalId = null,
            HasManualEdits = false
        };
        _songRepoMock
            .Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(song);
        _songServiceMock
            .Setup(s => s.UpdateSongAsync(
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, "Attached"));

        var candidate = MakeCandidate(externalProvider: "Deezer", externalId: "dz-5");
        var sut = CreateSut();

        var (success, message, _) = await sut.CommitAsync(
            candidate, ResolutionChoice.AttachExternalId, 5, null);

        Assert.True(success);
        // Verify only external identity params are set; title/featured/lyrics unchanged
        _songServiceMock.Verify(s => s.UpdateSongAsync(
            5,
            "My Song",  // unchanged
            null,       // unchanged
            null,       // unchanged
            false,      // unchanged
            "dz-5", "Deezer",
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── CommitAsync — CreateNew resolves+creates artist first ─────────────

    [Fact]
    // [AC] AC-2.5: CreateNew resolves the artist first, then creates the song with that artistId.
    public async Task CommitAsync_CreateNew_ResolvesArtistThenCreatesSong()
    {
        _artistResolutionMock
            .Setup(a => a.ResolveAsync(It.IsAny<ArtistCandidate>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ArtistExactLocal(7));

        var createdSong = new Song { Id = 88, Title = "New Song" };
        _songServiceMock
            .Setup(s => s.CreateSongAsync(7, "Bohemian Rhapsody", null, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, "Created", createdSong));

        var candidate = MakeCandidate();
        var sut = CreateSut();

        var (success, _, song) = await sut.CommitAsync(
            candidate, ResolutionChoice.CreateNew, null, null);

        Assert.True(success);
        Assert.Equal(88, song?.Id);
        _songServiceMock.Verify(s => s.CreateSongAsync(7, "Bohemian Rhapsody", null, null, null, null, It.IsAny<CancellationToken>()), Times.Once);
    }
}
