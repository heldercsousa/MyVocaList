using MyVocaList.Domain.Resolution;
using MyVocaList.Infra.Similarity;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Unit.Services;

/// <summary>
/// Level A tests for <c>ISongService.SaveFromFormAsync</c> (SFT task 3.10, REQ-SFT-51): every Song-form
/// Save decision lives in the Service. <c>SaveFromFormAsync</c> calls the real
/// <c>CreateSongWithUrlsAsync</c> / <c>UpdateSongAsync</c> on the same <see cref="SongService"/>, so the
/// write path is observed through the repository mocks, and the resolution collaborators are resolved
/// from the unit-of-work <c>sp</c> (<see cref="PassthroughUnitOfWork"/>), never from constructor fields.
/// </summary>
public class SongServiceSaveFromFormTests
{
    private readonly Mock<ISongRepository> _songRepoMock = new();
    private readonly Mock<IArtistRepository> _artistRepoMock = new();
    private readonly Mock<ISongKaraokeUrlRepository> _urlRepoMock = new();
    private readonly Mock<ISongKaraokeUrlService> _urlServiceMock = new();
    private readonly Mock<IArtistService> _artistServiceMock = new();
    private readonly Mock<IArtistResolutionService> _artistResolutionMock = new();
    private readonly Mock<ISongResolutionService> _songResolutionMock = new();
    private readonly Mock<ILogger<SongService>> _loggerMock = new();

    public SongServiceSaveFromFormTests()
    {
        // Mock defaults would read as failures: pin the benign defaults, tests override what they probe.
        _artistServiceMock
            .Setup(a => a.ValidateNameInput(It.IsAny<string>()))
            .Returns((true, string.Empty));
        _artistServiceMock
            .Setup(a => a.CreateArtistAsync(
                It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string name, string? _, string? __, CancellationToken ___) =>
                (true, string.Empty, (Artist?)new Artist { Id = NewArtistId, Name = name }));

        _artistResolutionMock
            .Setup(r => r.ResolveAsync(It.IsAny<ArtistCandidate>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ArtistResolution(ResolutionKind.NoMatch, null, []));
        _songResolutionMock
            .Setup(r => r.ResolveAsync(It.IsAny<SongCandidate>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SongResolution(ResolutionKind.NoMatch, null, []));

        _artistRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) => new Artist { Id = id, Name = $"Stored artist {id}" });

        _songRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Song>(), It.IsAny<CancellationToken>()))
            .Callback<Song, CancellationToken>((song, _) =>
            {
                song.Id = SavedSongIdValue;
                AddedSong = song;
            })
            .Returns(Task.CompletedTask);
    }

    private const int NewArtistId = 12;
    private const int SavedSongIdValue = 99;
    private const string DeezerProvider = "Deezer";
    private const string DeezerTrackId = "3135556";

    private Song? AddedSong { get; set; }

    /// <summary>
    /// The resolution services and the artist service are NOT constructor parameters: they live in the
    /// unit-of-work <c>sp</c> (plan § 3.10, UnitOfWorkReadScopeTests). The constructor is unchanged.
    /// </summary>
    private SongService CreateSut(IArtistResolutionService? artistResolution = null) => new(
        _songRepoMock.Object,
        _artistRepoMock.Object,
        _urlRepoMock.Object,
        _urlServiceMock.Object,
        PassthroughUnitOfWork.Over(
            _songRepoMock, _artistRepoMock, _urlRepoMock, _urlServiceMock, _artistServiceMock,
            artistResolution ?? _artistResolutionMock.Object, _songResolutionMock),
        _loggerMock.Object);

    private static SongFormSaveRequest NewRequest(
        int? songId = null,
        string title = "Don't Cry",
        string version = "",
        int? artistId = null,
        string? artistText = "Guns N' Roses",
        string? provider = null,
        string? externalId = null,
        ArtistDecision? artistDecision = null,
        SongDecision songDecision = SongDecision.None)
        => new(
            songId, title, version, FeaturedArtists: null, Lyrics: null,
            artistId, artistText, provider, externalId,
            PendingUrls: [],
            artistDecision ?? new ArtistDecision.None(),
            songDecision);

    private void SetupArtistResolution(ResolutionKind kind, int? exactId = null,
        params (int ArtistId, string Name, double Score)[] fuzzy)
        => _artistResolutionMock
            .Setup(r => r.ResolveAsync(It.IsAny<ArtistCandidate>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ArtistResolution(kind, exactId, fuzzy));

    private void VerifyNothingWritten()
    {
        _songRepoMock.Verify(r => r.AddAsync(It.IsAny<Song>(), It.IsAny<CancellationToken>()), Times.Never);
        _songRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Song>(), It.IsAny<CancellationToken>()), Times.Never);
        _artistRepoMock.Verify(r => r.AddAsync(It.IsAny<Artist>(), It.IsAny<CancellationToken>()), Times.Never);
        _artistServiceMock.Verify(a => a.CreateArtistAsync(
            It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private void VerifyArtistResolutionNeverCalled()
        => _artistResolutionMock.Verify(
            r => r.ResolveAsync(It.IsAny<ArtistCandidate>(), It.IsAny<CancellationToken>()), Times.Never);

    private void VerifySongGuardNeverCalled()
        => _songResolutionMock.Verify(
            r => r.ResolveAsync(It.IsAny<SongCandidate>(), It.IsAny<CancellationToken>()), Times.Never);

    private void VerifyLog(LogLevel level, Times times)
        => _loggerMock.Verify(
            l => l.Log(
                level,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times);

    private void AssertArtistNotFoundWritesNothing(SongFormSaveOutcome outcome)
    {
        Assert.Equal(SongFormSaveKind.Failed, outcome.Kind);
        Assert.Equal(SaveField.Artist, outcome.FailedField);
        Assert.Equal("Artist not found", outcome.Message);
        Assert.Null(outcome.SavedSongId);
        VerifySongGuardNeverCalled();
        VerifyNothingWritten();
    }

    // ── Rule 1: validation ────────────────────────────────────────────────

    [Fact]
    // [AC] REQ-SFT-15: an empty or whitespace Title fails on the Title field and saves nothing.
    public async Task SaveFromForm_EmptyTitle_FailsOnTitle()
    {
        var outcome = await CreateSut().SaveFromFormAsync(NewRequest(title: "   ", artistId: 7));

        Assert.Equal(SongFormSaveKind.Failed, outcome.Kind);
        Assert.Equal(SaveField.Title, outcome.FailedField);
        Assert.False(string.IsNullOrWhiteSpace(outcome.Message));
        VerifyNothingWritten();
    }

    [Fact]
    // [AC] REQ-SFT-21: Save with an empty or whitespace Artist (no id, no text) fails with
    // "Artist is required" on the Artist field and saves nothing.
    public async Task SaveFromForm_EmptyArtist_FailsArtistRequired()
    {
        var outcome = await CreateSut().SaveFromFormAsync(NewRequest(artistId: null, artistText: "   "));

        Assert.Equal(SongFormSaveKind.Failed, outcome.Kind);
        Assert.Equal(SaveField.Artist, outcome.FailedField);
        Assert.Equal("Artist is required", outcome.Message);
        VerifyArtistResolutionNeverCalled();
        VerifyNothingWritten();
    }

    [Fact]
    // [AC] REQ-SFT-20: a typed artist name that violates the IArtistService rules (here 61 characters)
    // fails on the Artist field with the service's message, before any resolution or write.
    public async Task SaveFromForm_NewArtistNameInvalid_FailsOnArtist()
    {
        var tooLong = new string('a', 61);
        const string serviceMessage = "Artist name is too long. Maximum 60 characters.";
        _artistServiceMock
            .Setup(a => a.ValidateNameInput(tooLong))
            .Returns((false, serviceMessage));

        var outcome = await CreateSut().SaveFromFormAsync(NewRequest(artistId: null, artistText: tooLong));

        Assert.Equal(SongFormSaveKind.Failed, outcome.Kind);
        Assert.Equal(SaveField.Artist, outcome.FailedField);
        Assert.Equal(serviceMessage, outcome.Message);
        VerifyArtistResolutionNeverCalled();
        VerifyNothingWritten();
    }

    [Fact]
    // [AC] REQ-SFT-12: a pick-filled Version longer than 60 characters fails on the Version field and
    // saves nothing (D21).
    public async Task SaveFromForm_VersionOver60_FailsOnVersionField()
    {
        var outcome = await CreateSut().SaveFromFormAsync(
            NewRequest(version: new string('v', 61), artistId: 7));

        Assert.Equal(SongFormSaveKind.Failed, outcome.Kind);
        Assert.Equal(SaveField.Version, outcome.FailedField);
        Assert.False(string.IsNullOrWhiteSpace(outcome.Message));
        VerifyArtistResolutionNeverCalled();
        VerifySongGuardNeverCalled();
        VerifyNothingWritten();
    }

    // ── Rule 2: artist ────────────────────────────────────────────────────

    [Fact]
    // [AC] REQ-SFT-52: a typed artist whose name is a collated match of a stored artist reuses that
    // artist silently; no second artist is ever created.
    public async Task SaveFromForm_TypedArtistExactCollatedMatch_ReusesExistingNeverCreates()
    {
        SetupArtistResolution(ResolutionKind.ExactLocalMatch, exactId: 7);

        var outcome = await CreateSut().SaveFromFormAsync(NewRequest(artistId: null, artistText: "guns n' roses"));

        Assert.Equal(SongFormSaveKind.Saved, outcome.Kind);
        Assert.Equal(7, AddedSong!.ArtistId);
        _artistServiceMock.Verify(a => a.CreateArtistAsync(
            It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        _artistRepoMock.Verify(r => r.AddAsync(It.IsAny<Artist>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    // [AC] REQ-SFT-18: an artist filled by a Deezer pick with no local id is resolved by collation
    // like a typed one: an exact match is reused silently (step 1), the song keeps its Deezer identity.
    public async Task SaveFromForm_DeezerFilledArtistExactCollatedMatch_ReusesExisting()
    {
        SetupArtistResolution(ResolutionKind.ExactLocalMatch, exactId: 4);

        var outcome = await CreateSut().SaveFromFormAsync(
            NewRequest(artistId: null, artistText: "Guns N' Roses", provider: DeezerProvider, externalId: DeezerTrackId));

        Assert.Equal(SongFormSaveKind.Saved, outcome.Kind);
        Assert.Equal(4, AddedSong!.ArtistId);
        Assert.Equal(DeezerProvider, AddedSong.ExternalProvider);
        Assert.Equal(DeezerTrackId, AddedSong.ExternalId);
        _artistServiceMock.Verify(a => a.CreateArtistAsync(
            It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    // [AC] REQ-SFT-52: a padded / differently cased / accented variant ("  métallica ") is decided by
    // the DB collation inside IArtistResolutionService, never by a C# comparison: the service hands the
    // typed text to the resolver (case and accent untouched) and reuses the id it returns.
    public async Task SaveFromForm_TypedArtistPaddedCaseAccentVariant_ReusesExistingArtist()
    {
        _artistResolutionMock
            .Setup(r => r.ResolveAsync(
                It.Is<ArtistCandidate>(c => c.Name.Trim() == "métallica"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ArtistResolution(ResolutionKind.ExactLocalMatch, 9, []));

        var outcome = await CreateSut().SaveFromFormAsync(NewRequest(artistId: null, artistText: "  métallica "));

        Assert.Equal(SongFormSaveKind.Saved, outcome.Kind);
        Assert.Equal(9, AddedSong!.ArtistId);
        _artistServiceMock.Verify(a => a.CreateArtistAsync(
            It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    // [AC] REQ-SFT-19: a near-duplicate typed artist ("Metalica") returns ArtistChoiceNeeded with at
    // most 3 candidates, highest score first; nothing is written and the song guard has not run.
    public async Task SaveFromForm_TypedArtistNearDuplicate_ReturnsArtistChoiceNeededTopThreeBestFirst()
    {
        SetupArtistResolution(ResolutionKind.FuzzyCandidates, exactId: null,
            (11, "Metalika", 0.85), (12, "Metallica", 0.94), (13, "Metalica Band", 0.88), (14, "Metallic", 0.90));

        var outcome = await CreateSut().SaveFromFormAsync(NewRequest(artistId: null, artistText: "Metalica"));

        Assert.Equal(SongFormSaveKind.ArtistChoiceNeeded, outcome.Kind);
        Assert.Equal([12, 14, 13], outcome.ArtistCandidates.Select(c => c.ArtistId));
        Assert.Equal("Metallica", outcome.ArtistCandidates[0].Name);
        Assert.Equal(0.94, outcome.ArtistCandidates[0].Score);
        VerifySongGuardNeverCalled();
        VerifyNothingWritten();
    }

    [Fact]
    // [AC] REQ-SFT-19: the artist-choice outcome carries the typed name trimmed (padding removed; case and
    // accents untouched) so the sheet can show "Create '<typed>'" from the outcome alone.
    public async Task SaveFromForm_TypedArtistNearDuplicate_OutcomeCarriesTrimmedTypedName()
    {
        _artistResolutionMock
            .Setup(r => r.ResolveAsync(It.Is<ArtistCandidate>(c => c.Name == "Metalica"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ArtistResolution(ResolutionKind.FuzzyCandidates, null, [(11, "Metallica", 0.9)]));

        var outcome = await CreateSut().SaveFromFormAsync(NewRequest(artistId: null, artistText: "  Metalica "));

        Assert.Equal(SongFormSaveKind.ArtistChoiceNeeded, outcome.Kind);
        Assert.Equal("Metalica", outcome.TypedName);
    }

    [Fact]
    // [AC] REQ-SFT-19: end to end with the real ArtistResolutionService and the real SimilarityScorer
    // (3.9c follow-up W1): "Metalica" against the stored "Metallica" yields ArtistChoiceNeeded with
    // Metallica scoring >= 0.82, and nothing is saved yet.
    public async Task SaveFromForm_Metalica_RealResolutionAndScorer_ReturnsArtistChoiceNeeded()
    {
        _artistRepoMock
            .Setup(r => r.SearchNameCandidatesAsync("Metalica", SimilarityConstants.PoolSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Artist> { new() { Id = 9, Name = "Metallica" } });
        var realResolution = new ArtistResolutionService(
            _artistRepoMock.Object,
            _artistServiceMock.Object,
            new SimilarityScorer(),
            PassthroughUnitOfWork.Over(_artistRepoMock, _artistServiceMock),
            new Mock<ILogger<ArtistResolutionService>>().Object);

        var outcome = await CreateSut(realResolution).SaveFromFormAsync(
            NewRequest(artistId: null, artistText: "Metalica"));

        Assert.Equal(SongFormSaveKind.ArtistChoiceNeeded, outcome.Kind);
        var candidate = Assert.Single(outcome.ArtistCandidates);
        Assert.Equal(9, candidate.ArtistId);
        Assert.Equal("Metallica", candidate.Name);
        Assert.True(candidate.Score >= SimilarityConstants.DefaultThreshold);
        VerifyNothingWritten();
    }

    [Fact]
    // [AC] REQ-SFT-19: the user's "Use 'existing'" decision skips artist resolution entirely and the
    // song is saved with that artist id.
    public async Task SaveFromForm_UseExistingDecision_SkipsArtistResolution_UsesId()
    {
        var outcome = await CreateSut().SaveFromFormAsync(NewRequest(
            artistId: null, artistText: "Metalica", artistDecision: new ArtistDecision.UseExisting(7)));

        Assert.Equal(SongFormSaveKind.Saved, outcome.Kind);
        Assert.Equal(7, AddedSong!.ArtistId);
        VerifyArtistResolutionNeverCalled();
        _artistServiceMock.Verify(a => a.CreateArtistAsync(
            It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    // [AC] REQ-SFT-19: the user's "Create '<typed>'" decision skips artist resolution and creates the
    // artist through the write path (IArtistService.CreateArtistAsync inside the song's unit of work).
    public async Task SaveFromForm_CreateTypedDecision_CreatesNewArtistViaWritePath()
    {
        var outcome = await CreateSut().SaveFromFormAsync(NewRequest(
            artistId: null, artistText: "Metalica", artistDecision: new ArtistDecision.CreateTyped()));

        Assert.Equal(SongFormSaveKind.Saved, outcome.Kind);
        VerifyArtistResolutionNeverCalled();
        _artistServiceMock.Verify(a => a.CreateArtistAsync(
            "Metalica", null, null, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(NewArtistId, AddedSong!.ArtistId);
    }

    [Fact]
    // [AC] REQ-SFT-18: with no collated match and no near-duplicate, a new artist is created in the same
    // write as the song (via IArtistService, never a direct artist-repository add).
    public async Task SaveFromForm_NoArtistMatch_CreatesNewArtistInSameWrite()
    {
        SetupArtistResolution(ResolutionKind.NoMatch);

        var outcome = await CreateSut().SaveFromFormAsync(NewRequest(artistId: null, artistText: "Brand New Band"));

        Assert.Equal(SongFormSaveKind.Saved, outcome.Kind);
        _artistServiceMock.Verify(a => a.CreateArtistAsync(
            "Brand New Band", null, null, It.IsAny<CancellationToken>()), Times.Once);
        _artistRepoMock.Verify(r => r.AddAsync(It.IsAny<Artist>(), It.IsAny<CancellationToken>()), Times.Never);
        _songRepoMock.Verify(r => r.AddAsync(It.IsAny<Song>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(NewArtistId, AddedSong!.ArtistId);
    }

    [Fact]
    // [AC] REQ-SFT-18: an exact artist kind without an id is not a match; the artist is created in the
    // song's write as if no match existed.
    public async Task SaveFromForm_ExactArtistKindWithoutId_FallsThroughToCreate()
    {
        SetupArtistResolution(ResolutionKind.ExactLocalMatch, exactId: null);

        var outcome = await CreateSut().SaveFromFormAsync(NewRequest(artistId: null, artistText: "Brand New Band"));

        Assert.Equal(SongFormSaveKind.Saved, outcome.Kind);
        _artistServiceMock.Verify(a => a.CreateArtistAsync(
            "Brand New Band", null, null, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(NewArtistId, AddedSong!.ArtistId);
    }

    [Fact]
    // [AC] REQ-SFT-18: a fuzzy artist kind with an empty candidate list offers no choice; it is treated as
    // no match and the artist is created in the song's write.
    public async Task SaveFromForm_FuzzyArtistKindWithEmptyList_FallsThroughToCreate()
    {
        SetupArtistResolution(ResolutionKind.FuzzyCandidates);

        var outcome = await CreateSut().SaveFromFormAsync(NewRequest(artistId: null, artistText: "Brand New Band"));

        Assert.Equal(SongFormSaveKind.Saved, outcome.Kind);
        _artistServiceMock.Verify(a => a.CreateArtistAsync(
            "Brand New Band", null, null, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(NewArtistId, AddedSong!.ArtistId);
    }

    [Fact]
    // [AC] REQ-SFT-41: the artist lookup for a song save never carries the song's external identity.
    public async Task SaveFromForm_ArtistResolution_NeverReceivesSongExternalIdentity()
    {
        await CreateSut().SaveFromFormAsync(NewRequest(
            artistId: null, artistText: "Guns N' Roses", provider: DeezerProvider, externalId: DeezerTrackId));

        _artistResolutionMock.Verify(r => r.ResolveAsync(
            It.Is<ArtistCandidate>(c => c.ExternalId == null && c.ExternalProvider == null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── Rule 3: song guard (add mode) ─────────────────────────────────────

    [Fact]
    // [AC] REQ-SFT-22: an exact duplicate (collated artist, title, version) returns SongChoiceNeeded
    // with IsExactSongMatch = true and the stored song as the candidate; nothing is written.
    public async Task SaveFromForm_ExactDuplicateSong_ReturnsSongChoiceNeededExact()
    {
        _songResolutionMock
            .Setup(r => r.ResolveAsync(It.IsAny<SongCandidate>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SongResolution(ResolutionKind.ExactLocalMatch, 55, []));
        _songRepoMock
            .Setup(r => r.GetByIdAsync(55, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Song
            {
                Id = 55, ArtistId = 7, Title = "Don't Cry", Version = "",
                OriginalArtist = new Artist { Id = 7, Name = "Guns N' Roses" }
            });

        var outcome = await CreateSut().SaveFromFormAsync(NewRequest(artistId: 7));

        Assert.Equal(SongFormSaveKind.SongChoiceNeeded, outcome.Kind);
        Assert.True(outcome.IsExactSongMatch);
        Assert.Equal(55, Assert.Single(outcome.SongCandidates).SongId);
        Assert.Null(outcome.SavedSongId);
        VerifyNothingWritten();
    }

    [Fact]
    // [AC] REQ-SFT-23: possible duplicates (fuzzy, no exact) return SongChoiceNeeded with
    // IsExactSongMatch = false and the resolver's candidates in order; nothing is written.
    public async Task SaveFromForm_FuzzyDuplicateSong_ReturnsSongChoiceNeededNotExact()
    {
        var fuzzy = new List<SongMatch>
        {
            new(61, "Dont Cry", "", "Guns N' Roses", 0.96),
            new(62, "Don't Cry (Acoustic)", "Acoustic", "Guns N' Roses", 0.88)
        };
        _songResolutionMock
            .Setup(r => r.ResolveAsync(It.IsAny<SongCandidate>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SongResolution(ResolutionKind.FuzzyCandidates, null, fuzzy));

        var outcome = await CreateSut().SaveFromFormAsync(NewRequest(artistId: 7));

        Assert.Equal(SongFormSaveKind.SongChoiceNeeded, outcome.Kind);
        Assert.False(outcome.IsExactSongMatch);
        Assert.Equal([61, 62], outcome.SongCandidates.Select(c => c.SongId));
        VerifyNothingWritten();
    }

    [Fact]
    // [AC] REQ-SFT-25: a NEW artist does not skip the song guard: the artist is not created yet, the
    // external-identity check (the song guard receives the song's identity) finds the held song, and
    // the outcome is the exact-duplicate choice with no artist and no song created.
    public async Task SaveFromForm_NewArtistWithHeldIdentity_ReturnsSongChoiceNeededExact_CreatesNothing()
    {
        SetupArtistResolution(ResolutionKind.NoMatch);
        _songResolutionMock
            .Setup(r => r.ResolveAsync(
                It.Is<SongCandidate>(c => c.ExternalProvider == DeezerProvider && c.ExternalId == DeezerTrackId
                                          && c.Artist.Name == "Guns N' Roses" && c.Artist.ExternalId == null),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SongResolution(ResolutionKind.ExactExternalMatch, 55, []));
        _songRepoMock
            .Setup(r => r.GetByIdAsync(55, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Song
            {
                Id = 55, ArtistId = 3, Title = "Don't Cry", Version = "",
                OriginalArtist = new Artist { Id = 3, Name = "Guns N Roses" }
            });

        var outcome = await CreateSut().SaveFromFormAsync(NewRequest(
            artistId: null, artistText: "Guns N' Roses", provider: DeezerProvider, externalId: DeezerTrackId));

        Assert.Equal(SongFormSaveKind.SongChoiceNeeded, outcome.Kind);
        Assert.True(outcome.IsExactSongMatch);
        Assert.Equal(55, Assert.Single(outcome.SongCandidates).SongId);
        VerifyNothingWritten();
    }

    [Fact]
    // [AC] REQ-SFT-22: for a picked (existing) artist the song guard identifies the artist by its
    // STORED name (the picked artist may arrive without ArtistText), so the artist-scoped checks run
    // against the right artist.
    public async Task SaveFromForm_PickedArtist_SongGuardCandidateCarriesStoredArtistName()
    {
        _artistRepoMock
            .Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Artist { Id = 7, Name = "Queen" });

        await CreateSut().SaveFromFormAsync(NewRequest(artistId: 7, artistText: null, title: "Radio Ga Ga"));

        _songResolutionMock.Verify(r => r.ResolveAsync(
            It.Is<SongCandidate>(c => c.Artist.Name == "Queen" && c.Title == "Radio Ga Ga"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    // [AC] REQ-SFT-51: a picked artist id with no stored row fails on the Artist field with "Artist not
    // found" in the song guard; nothing is written.
    public async Task SaveFromForm_PickedArtistMissingRow_FailsOnArtistNotFound()
    {
        _artistRepoMock
            .Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Artist?)null);

        var outcome = await CreateSut().SaveFromFormAsync(NewRequest(artistId: 7));

        AssertArtistNotFoundWritesNothing(outcome);
    }

    [Fact]
    // [AC] REQ-SFT-51: a "Use existing" decision whose artist id has no stored row fails on the Artist
    // field with "Artist not found" in the song guard; nothing is written.
    public async Task SaveFromForm_UseExistingArtistMissingRow_FailsOnArtistNotFound()
    {
        _artistRepoMock
            .Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Artist?)null);

        var outcome = await CreateSut().SaveFromFormAsync(NewRequest(
            artistId: null, artistText: "Metalica", artistDecision: new ArtistDecision.UseExisting(7)));

        AssertArtistNotFoundWritesNothing(outcome);
    }

    [Fact]
    // [AC] REQ-SFT-51: an exact collated artist match whose stored row has gone fails on the Artist field
    // with "Artist not found" in the song guard; nothing is written.
    public async Task SaveFromForm_ResolverExactArtistMissingRow_FailsOnArtistNotFound()
    {
        SetupArtistResolution(ResolutionKind.ExactLocalMatch, exactId: 7);
        _artistRepoMock
            .Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Artist?)null);

        var outcome = await CreateSut().SaveFromFormAsync(NewRequest(artistId: null, artistText: "guns n' roses"));

        AssertArtistNotFoundWritesNothing(outcome);
    }

    [Fact]
    // [AC] REQ-SFT-22: an exact duplicate whose stored song can no longer be loaded is treated as no match
    // (Warning logged); the write's own checks decide and the song is saved.
    public async Task SaveFromForm_ExactDuplicateSongNotLoadable_TreatedAsNoMatchAndWrites()
    {
        _songResolutionMock
            .Setup(r => r.ResolveAsync(It.IsAny<SongCandidate>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SongResolution(ResolutionKind.ExactLocalMatch, 55, []));
        _songRepoMock
            .Setup(r => r.GetByIdAsync(55, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Song?)null);

        var outcome = await CreateSut().SaveFromFormAsync(NewRequest(artistId: 7));

        Assert.Equal(SongFormSaveKind.Saved, outcome.Kind);
        Assert.Equal(SavedSongIdValue, outcome.SavedSongId);
        _songRepoMock.Verify(r => r.AddAsync(It.IsAny<Song>(), It.IsAny<CancellationToken>()), Times.Once);
        VerifyLog(LogLevel.Warning, Times.Once());
    }

    [Fact]
    // [AC] REQ-SFT-22: an exact song kind without an id is not a duplicate; the save proceeds to the write.
    public async Task SaveFromForm_ExactSongKindWithoutId_ProceedsToWrite()
    {
        _songResolutionMock
            .Setup(r => r.ResolveAsync(It.IsAny<SongCandidate>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SongResolution(ResolutionKind.ExactLocalMatch, null, []));

        var outcome = await CreateSut().SaveFromFormAsync(NewRequest(artistId: 7));

        Assert.Equal(SongFormSaveKind.Saved, outcome.Kind);
        _songRepoMock.Verify(r => r.AddAsync(It.IsAny<Song>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── Rules 3-4: song decisions ─────────────────────────────────────────

    [Fact]
    // [AC] REQ-SFT-22: "Save as new version" with an empty Version fails on the Version field
    // (AC-1.2 validation); the song guard does not re-run and nothing is written.
    public async Task SaveFromForm_SaveAsNewVersionEmptyVersion_FailsOnVersion()
    {
        var outcome = await CreateSut().SaveFromFormAsync(NewRequest(
            version: "", artistId: 7, songDecision: SongDecision.SaveAsNewVersion));

        Assert.Equal(SongFormSaveKind.Failed, outcome.Kind);
        Assert.Equal(SaveField.Version, outcome.FailedField);
        Assert.False(string.IsNullOrWhiteSpace(outcome.Message));
        VerifySongGuardNeverCalled();
        VerifyNothingWritten();
    }

    [Fact]
    // [AC] REQ-SFT-22: "Save as new version" with a Version skips the song guard and writes the song.
    public async Task SaveFromForm_SaveAsNewVersionDecision_SkipsSongGuard_Writes()
    {
        var outcome = await CreateSut().SaveFromFormAsync(NewRequest(
            version: "Live", artistId: 7, songDecision: SongDecision.SaveAsNewVersion));

        Assert.Equal(SongFormSaveKind.Saved, outcome.Kind);
        VerifySongGuardNeverCalled();
        _songRepoMock.Verify(r => r.AddAsync(It.IsAny<Song>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal("Live", AddedSong!.Version);
    }

    [Fact]
    // [AC] REQ-SFT-23: "Save as new song" skips the song guard and needs no Version.
    public async Task SaveFromForm_SaveAsNewSongDecision_SkipsSongGuard_NoVersionRequired()
    {
        var outcome = await CreateSut().SaveFromFormAsync(NewRequest(
            version: "", artistId: 7, songDecision: SongDecision.SaveAsNewSong));

        Assert.Equal(SongFormSaveKind.Saved, outcome.Kind);
        VerifySongGuardNeverCalled();
        _songRepoMock.Verify(r => r.AddAsync(It.IsAny<Song>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(string.Empty, AddedSong!.Version);
    }

    // ── Rule 5: write ─────────────────────────────────────────────────────

    [Fact]
    // [AC] REQ-SFT-27: edit mode runs no song guard and updates through UpdateSongAsync with a null
    // external identity (keep): the stored ExternalId/ExternalProvider survive a title edit even when
    // the request carries different values.
    public async Task SaveFromForm_EditMode_RunsNoSongGuard_CallsUpdateWithNullIdentity()
    {
        var stored = new Song
        {
            Id = 50, ArtistId = 3, Title = "Old title", Version = "",
            ExternalProvider = DeezerProvider, ExternalId = DeezerTrackId,
            OriginalArtist = new Artist { Id = 3, Name = "Stored artist 3" }
        };
        _songRepoMock
            .Setup(r => r.GetByIdAsync(50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stored);

        var outcome = await CreateSut().SaveFromFormAsync(NewRequest(
            songId: 50, title: "Corrected title", artistId: 3,
            provider: DeezerProvider, externalId: "999"));

        Assert.Equal(SongFormSaveKind.Saved, outcome.Kind);
        VerifySongGuardNeverCalled();
        _songRepoMock.Verify(r => r.UpdateAsync(
            It.Is<Song>(s => s.Id == 50 && s.Title == "Corrected title"
                             && s.ExternalProvider == DeezerProvider && s.ExternalId == DeezerTrackId),
            It.IsAny<CancellationToken>()), Times.Once);
        _songRepoMock.Verify(r => r.AddAsync(It.IsAny<Song>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    // [AC] REQ-SFT-51: a failure tuple from the write path (here the duplicate (artist, title, version)
    // refusal) comes back as Failed carrying the write's message, with no saved id.
    public async Task SaveFromForm_WriteReturnsFailureTuple_ReturnsFailedWithMessage()
    {
        _songRepoMock
            .Setup(r => r.ExistsByTitleVersionForArtistAsync(7, "Don't Cry", "", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var outcome = await CreateSut().SaveFromFormAsync(NewRequest(artistId: 7));

        Assert.Equal(SongFormSaveKind.Failed, outcome.Kind);
        Assert.Equal("A song with this title and version already exists for this artist", outcome.Message);
        Assert.Null(outcome.SavedSongId);
    }

    [Fact]
    // [AC] REQ-SFT-26: an external identity already held by another song fails with
    // "This music-database song is already in your library" and writes nothing.
    public async Task SaveFromForm_IdentityHeldByAnotherSong_FailsWithMessage()
    {
        _songRepoMock
            .Setup(r => r.ExistsByExternalIdentityAsync(
                DeezerProvider, DeezerTrackId, It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var outcome = await CreateSut().SaveFromFormAsync(NewRequest(
            artistId: 7, provider: DeezerProvider, externalId: DeezerTrackId));

        Assert.Equal(SongFormSaveKind.Failed, outcome.Kind);
        Assert.Equal("This music-database song is already in your library", outcome.Message);
        VerifyNothingWritten();
    }

    [Fact]
    // [AC] REQ-SFT-51: a successful save returns Saved with the new song's id and no failed field.
    public async Task SaveFromForm_Success_ReturnsSavedWithId()
    {
        var outcome = await CreateSut().SaveFromFormAsync(NewRequest(artistId: 7));

        Assert.Equal(SongFormSaveKind.Saved, outcome.Kind);
        Assert.Equal(SavedSongIdValue, outcome.SavedSongId);
        Assert.Equal(SaveField.None, outcome.FailedField);
        Assert.False(string.IsNullOrWhiteSpace(outcome.Message));
    }

    [Fact]
    // [AC] REQ-SFT-46: SaveFromFormAsync writes only through CreateSongWithUrlsAsync / UpdateSongAsync:
    // an add-mode save stages exactly one song through the create path (one AddAsync, no UpdateAsync) and
    // creates its artist only through IArtistService, never a direct artist-repository write. (The two
    // write methods live on the SUT itself, so they are observed through the repository mocks.)
    public async Task SaveFromForm_WritesOnlyThroughCreateOrUpdate()
    {
        SetupArtistResolution(ResolutionKind.NoMatch);

        var outcome = await CreateSut().SaveFromFormAsync(NewRequest(artistId: null, artistText: "Brand New Band"));

        Assert.Equal(SongFormSaveKind.Saved, outcome.Kind);
        _songRepoMock.Verify(r => r.AddAsync(It.IsAny<Song>(), It.IsAny<CancellationToken>()), Times.Once);
        _songRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Song>(), It.IsAny<CancellationToken>()), Times.Never);
        _artistRepoMock.Verify(r => r.AddAsync(It.IsAny<Artist>(), It.IsAny<CancellationToken>()), Times.Never);
        _artistRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Artist>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Write-time failures: which field owns the refusal (D2) ────────────

    [Fact]
    // [AC] REQ-SFT-20: when IArtistService refuses the artist created inside the song write (e.g. a collated
    // duplicate), the save fails on the Artist field with the service's message and the song is not saved.
    public async Task SaveFromForm_CreatedArtistRefused_FailsOnArtistAndSavesNothing()
    {
        const string serviceMessage = "An artist with this name already exists";
        _artistServiceMock
            .Setup(a => a.CreateArtistAsync("Brand New Band", null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, serviceMessage, (Artist?)null));

        var outcome = await CreateSut().SaveFromFormAsync(NewRequest(artistId: null, artistText: "Brand New Band"));

        Assert.Equal(SongFormSaveKind.Failed, outcome.Kind);
        Assert.Equal(SaveField.Artist, outcome.FailedField);
        Assert.Equal(serviceMessage, outcome.Message);
        Assert.Null(outcome.SavedSongId);
        _songRepoMock.Verify(r => r.AddAsync(It.IsAny<Song>(), It.IsAny<CancellationToken>()), Times.Never);
        _songRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Song>(), It.IsAny<CancellationToken>()), Times.Never);
        _artistRepoMock.Verify(r => r.AddAsync(It.IsAny<Artist>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    // [AC] REQ-SFT-51: a song-level refusal after the new artist was created in the same write (here the
    // duplicate (artist, title, version) check) is not an Artist failure: Failed with SaveField.None and no
    // saved id.
    public async Task SaveFromForm_NewArtistThenSongRefused_FailsWithFieldNone()
    {
        _songRepoMock
            .Setup(r => r.ExistsByTitleVersionForArtistAsync(NewArtistId, "Don't Cry", "", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var outcome = await CreateSut().SaveFromFormAsync(NewRequest(artistId: null, artistText: "Brand New Band"));

        Assert.Equal(SongFormSaveKind.Failed, outcome.Kind);
        Assert.Equal(SaveField.None, outcome.FailedField);
        Assert.Equal("A song with this title and version already exists for this artist", outcome.Message);
        Assert.Null(outcome.SavedSongId);
        _artistServiceMock.Verify(a => a.CreateArtistAsync(
            "Brand New Band", null, null, It.IsAny<CancellationToken>()), Times.Once);
        _songRepoMock.Verify(r => r.AddAsync(It.IsAny<Song>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    // [AC] REQ-SFT-26: a new artist saved as a new song (the user skipped the duplicate guard) whose external
    // identity is already held fails with the identity message, SaveField.None, and creates no artist.
    public async Task SaveFromForm_NewArtistIdentityHeldAfterGuardSkipped_FailsWithFieldNone()
    {
        _songRepoMock
            .Setup(r => r.ExistsByExternalIdentityAsync(
                DeezerProvider, DeezerTrackId, It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var outcome = await CreateSut().SaveFromFormAsync(NewRequest(
            artistId: null, artistText: "Guns N' Roses", provider: DeezerProvider, externalId: DeezerTrackId,
            songDecision: SongDecision.SaveAsNewSong));

        Assert.Equal(SongFormSaveKind.Failed, outcome.Kind);
        Assert.Equal(SaveField.None, outcome.FailedField);
        Assert.Equal("This music-database song is already in your library", outcome.Message);
        Assert.Null(outcome.SavedSongId);
        VerifyNothingWritten();
    }
}
