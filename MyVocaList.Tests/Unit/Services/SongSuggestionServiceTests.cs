using MyVocaList.Contracts.DTOs;
using MyVocaList.Contracts.DTOs.Suggestions;
using MyVocaList.Domain.ReadModels;
using MyVocaList.Domain.Resolution;
using MyVocaList.Infra.Similarity;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Unit.Services;

/// <summary>
/// Task 4.2 — <see cref="SongSuggestionService.SearchTitleLocalAsync"/> and
/// <see cref="SongSuggestionService.SearchTitleRemoteAsync"/> (design § 3.3, REQ-SFT-02..09, 12, 13, 30-32, 35, 36, 53).
/// Ranking tests use the real (pure) <see cref="SimilarityScorer"/>; repositories, provider and
/// network status are Moq. <c>Mock&lt;INetworkStatus&gt;</c> defaults to <c>false</c> (= Offline), so the
/// constructor sets it to <c>true</c> explicitly; only the Offline tests set <c>false</c> (plan § 0.3 D21).
/// Constructor order the Builder must implement: (songRepository, artistRepository, uow, providers,
/// scorer, networkStatus, logger).
/// </summary>
public class SongSuggestionServiceTests
{
    private readonly Mock<ISongRepository> _songRepoMock = new();
    private readonly Mock<IArtistRepository> _artistRepoMock = new();
    private readonly Mock<IMusicMetadataProvider> _musicBrainzMock = new();
    private readonly Mock<IMusicMetadataProvider> _deezerMock = new();
    private readonly Mock<INetworkStatus> _networkMock = new();
    private readonly Mock<ILogger<SongSuggestionService>> _loggerMock = new();
    private readonly ISimilarityScorer _scorer = new SimilarityScorer();

    public SongSuggestionServiceTests()
    {
        _musicBrainzMock.SetupGet(p => p.ProviderName).Returns("MusicBrainz");
        _deezerMock.SetupGet(p => p.ProviderName).Returns("Deezer");
        _networkMock.SetupGet(n => n.HasInternetAccess).Returns(true);

        // Moq returns null for Task<IReadOnlyList<T>> by default; make every lookup explicit-empty.
        _songRepoMock
            .Setup(r => r.SearchTitleCandidatesAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SongSearchHit>());
        _songRepoMock
            .Setup(r => r.FindByExternalIdsAsync(It.IsAny<string>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SongSearchHit>());
        _songRepoMock
            .Setup(r => r.FindByTitleAndArtistNamesAsync(
                It.IsAny<IReadOnlyCollection<(string Title, string ArtistName, string Version)>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SongSearchHit>());
        _artistRepoMock
            .Setup(r => r.GetByNamesCollatedAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Artist>());
    }

    // PassthroughUnitOfWork runs each wrapped body inline against these same mocks (REQ-UOW-38/49).
    private SongSuggestionService CreateSut() => new(
        _songRepoMock.Object,
        _artistRepoMock.Object,
        PassthroughUnitOfWork.Over(_songRepoMock, _artistRepoMock),
        [_musicBrainzMock.Object, _deezerMock.Object],
        _scorer,
        _networkMock.Object,
        _loggerMock.Object);

    private static SongSearchHit Hit(int id, string title, string artist, int artistId = 1, string version = "",
        string? provider = null, string? externalId = null)
        => new(id, title, version, artistId, artist, provider, externalId);

    // Expression trees cannot contain tuple literals, so Moq predicates build the triple through a method.
    private static (string Title, string ArtistName, string Version) Triple(string title, string artist, string version)
        => (title, artist, version);

    private void SetupCandidates(string query, params SongSearchHit[] hits)
        => _songRepoMock
            .Setup(r => r.SearchTitleCandidatesAsync(query, SimilarityConstants.PoolSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync(hits.ToList());

    private void SetupDeezer(params MusicSearchResultDto[] results)
        => _deezerMock
            .Setup(p => p.SearchSongsAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(results.ToList());

    private void VerifyLog(LogLevel level, Times times)
        => _loggerMock.Verify(
            l => l.Log(
                level,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times);

    private void VerifyNoProviderCall()
    {
        _deezerMock.Verify(
            p => p.SearchSongsAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        _musicBrainzMock.Verify(
            p => p.SearchSongsAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── SearchTitleLocalAsync ────────────────────────────────────────────

    [Theory]
    [InlineData("a")]
    [InlineData("  a ")]
    [InlineData("")]
    public async Task SearchTitleLocalAsync_BelowTwoTrimmedChars_ReturnsEmptyNoRepositoryCall(string query)
    {
        // [AC] REQ-SFT-03: fewer than 2 trimmed characters starts no local search
        var sut = CreateSut();

        var result = await sut.SearchTitleLocalAsync(query);

        Assert.Empty(result);
        _songRepoMock.Verify(
            r => r.SearchTitleCandidatesAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static readonly SongSearchHit[] DontCryPool =
    [
        Hit(1, "Dont Look Back", "Boston", 10),
        Hit(2, "Don't Cry", "Guns N' Roses", 11),
        Hit(3, "Boys Don't Cry", "The Cure", 12),
        Hit(4, "Cry", "Cyndi Lauper", 13),
        Hit(5, "Crying", "Roy Orbison", 14)
    ];

    [Fact]
    public async Task SearchTitleLocalAsync_DontCry_DontCryFirst()
    {
        // [AC] REQ-SFT-30: "dont cry" ranks "Don't Cry" first although the DB order puts it second
        SetupCandidates("dont cry", DontCryPool);
        var sut = CreateSut();

        var result = await sut.SearchTitleLocalAsync("dont cry");

        Assert.Equal(2, result[0].LocalId);
        Assert.Equal("Don't Cry", result[0].Title);
    }

    [Fact]
    public async Task SearchTitleLocalAsync_DonCry_DontCryFirst()
    {
        // [AC] REQ-SFT-30: the truncated-word query "don cry" still ranks "Don't Cry" first
        SetupCandidates("don cry", DontCryPool);
        var sut = CreateSut();

        var result = await sut.SearchTitleLocalAsync("don cry");

        Assert.Equal(2, result[0].LocalId);
    }

    [Fact]
    public async Task SearchTitleLocalAsync_GunsCry_DontCryFirstViaArtist()
    {
        // [AC] REQ-SFT-32: artist words in the query rank by title + artist, so Guns N' Roses' "Don't Cry" wins
        SetupCandidates(
            "guns cry",
            Hit(4, "Cry", "Cyndi Lauper", 13),
            Hit(5, "Crying", "Roy Orbison", 14),
            Hit(3, "Boys Don't Cry", "The Cure", 12),
            Hit(2, "Don't Cry", "Guns N' Roses", 11));
        var sut = CreateSut();

        var result = await sut.SearchTitleLocalAsync("guns cry");

        Assert.Equal(2, result[0].LocalId);
    }

    [Fact]
    public async Task SearchTitleLocalAsync_TakesAtMostFive()
    {
        // [AC] REQ-SFT-04: at most MaxSearchResults local options
        var hits = Enumerable.Range(1, 7).Select(i => Hit(i, $"Song {i}", "Queen", 10)).ToArray();
        SetupCandidates("song", hits);
        var sut = CreateSut();

        var result = await sut.SearchTitleLocalAsync("song");

        Assert.Equal(5, result.Count);
    }

    [Fact]
    public async Task SearchTitleLocalAsync_MakesNoProviderCall()
    {
        // [AC] REQ-SFT-07: local options never wait on, or call, a remote provider
        SetupCandidates("creep", Hit(1, "Creep", "Radiohead", 3));
        var sut = CreateSut();

        await sut.SearchTitleLocalAsync("creep");

        VerifyNoProviderCall();
    }

    [Fact]
    public async Task SearchTitleLocalAsync_MapsVersionAndArtist()
    {
        // [AC] REQ-SFT-04: a local option carries the stored version, artist name and ids
        SetupCandidates("creep", Hit(5, "Creep", "Radiohead", 3, "Live"));
        var sut = CreateSut();

        var result = await sut.SearchTitleLocalAsync("creep");

        var option = Assert.Single(result);
        Assert.Equal(5, option.LocalId);
        Assert.Equal("Creep", option.Title);
        Assert.Equal("Live", option.Version);
        Assert.Equal("Radiohead", option.ArtistName);
        Assert.Equal("Radiohead", option.LocalArtistName);
        Assert.Equal(3, option.LocalArtistId);
        Assert.False(option.IsRemote);
    }

    // ── SearchTitleRemoteAsync ───────────────────────────────────────────

    [Theory]
    [InlineData("ab")]
    [InlineData(" ab ")]
    public async Task SearchTitleRemoteAsync_BelowThreeTrimmedChars_NoProviderCall(string query)
    {
        // [AC] REQ-SFT-03: fewer than 3 trimmed characters starts no Deezer search
        var sut = CreateSut();

        var result = await sut.SearchTitleRemoteAsync(query, []);

        Assert.Empty(result.Remote);
        Assert.Empty(result.Promoted);
        Assert.Equal(RemoteSearchStatus.Ok, result.Status);
        VerifyNoProviderCall();
    }

    [Fact]
    public async Task SearchTitleRemoteAsync_ExternalIdentityMatchNotShown_Promoted()
    {
        // [AC] REQ-SFT-05: a Deezer hit already in the library by external id is promoted as the local row
        SetupDeezer(new MusicSearchResultDto("dz-1", "Deezer", "Radiohead", "Creep", null));
        _songRepoMock
            .Setup(r => r.FindByExternalIdsAsync(
                "Deezer", It.Is<IReadOnlyCollection<string>>(ids => ids.Contains("dz-1")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SongSearchHit> { Hit(7, "Creep", "Radiohead", 3, "", "Deezer", "dz-1") });
        var sut = CreateSut();

        var result = await sut.SearchTitleRemoteAsync("creep", []);

        var promoted = Assert.Single(result.Promoted);
        Assert.Equal(7, promoted.LocalId);
        Assert.False(promoted.IsRemote);
        Assert.Empty(result.Remote);
    }

    [Fact]
    public async Task SearchTitleRemoteAsync_CollatedTitleArtistEmptyVersionMatch_Promoted()
    {
        // [AC] REQ-SFT-05: with no external id match, a collated (title, artist, "") match is promoted
        // (a null provider Version is coalesced to "" before the triple is built)
        SetupDeezer(new MusicSearchResultDto("dz-1", "Deezer", "Radiohead", "Creep", null, null));
        _songRepoMock
            .Setup(r => r.FindByTitleAndArtistNamesAsync(
                It.Is<IReadOnlyCollection<(string Title, string ArtistName, string Version)>>(
                    items => items.Contains(Triple("Creep", "Radiohead", ""))),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SongSearchHit> { Hit(8, "creep", "RADIOHEAD", 3) });
        var sut = CreateSut();

        var result = await sut.SearchTitleRemoteAsync("creep", []);

        var promoted = Assert.Single(result.Promoted);
        Assert.Equal(8, promoted.LocalId);
        Assert.Empty(result.Remote);
    }

    [Fact]
    public async Task SearchTitleRemoteAsync_CollatedTitleArtistVersionMatch_Promoted()
    {
        // [AC] REQ-SFT-05: the version takes part in the match (Deezer "Creep"/"Live" vs stored "Creep"/"Live")
        SetupDeezer(new MusicSearchResultDto("dz-1", "Deezer", "Radiohead", "Creep", null, "Live"));
        _songRepoMock
            .Setup(r => r.FindByTitleAndArtistNamesAsync(
                It.Is<IReadOnlyCollection<(string Title, string ArtistName, string Version)>>(
                    items => items.Contains(Triple("Creep", "Radiohead", "Live"))),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SongSearchHit> { Hit(9, "Creep", "Radiohead", 3, "Live") });
        var sut = CreateSut();

        var result = await sut.SearchTitleRemoteAsync("creep", []);

        var promoted = Assert.Single(result.Promoted);
        Assert.Equal(9, promoted.LocalId);
        Assert.Empty(result.Remote);
    }

    [Fact]
    public async Task SearchTitleRemoteAsync_MatchAlreadyShownLocally_DroppedNotPromoted()
    {
        // [AC] REQ-SFT-05: a match already listed among the local options is dropped, not promoted twice
        SetupDeezer(new MusicSearchResultDto("dz-1", "Deezer", "Radiohead", "Creep", null));
        _songRepoMock
            .Setup(r => r.FindByExternalIdsAsync(
                It.IsAny<string>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SongSearchHit> { Hit(7, "Creep", "Radiohead", 3, "", "Deezer", "dz-1") });
        var sut = CreateSut();

        var result = await sut.SearchTitleRemoteAsync("creep", [7]);

        Assert.Empty(result.Promoted);
        Assert.Empty(result.Remote);
    }

    [Fact]
    public async Task SearchTitleRemoteAsync_OtherArtistSameTitle_StaysRemote()
    {
        // [AC] REQ-SFT-05: dedup is artist-scoped; the same title by another artist stays a remote option
        SetupDeezer(new MusicSearchResultDto("dz-1", "Deezer", "Radiohead", "Creep", null));
        // The library holds "Creep" by Stone Temple Pilots only: the (Creep, Radiohead, "") lookup finds nothing.
        var sut = CreateSut();

        var result = await sut.SearchTitleRemoteAsync("creep", []);

        var remote = Assert.Single(result.Remote);
        Assert.Equal("Radiohead", remote.ArtistName);
        Assert.Empty(result.Promoted);
        _songRepoMock.Verify(
            r => r.FindByTitleAndArtistNamesAsync(
                It.Is<IReadOnlyCollection<(string Title, string ArtistName, string Version)>>(
                    items => items.Contains(Triple("Creep", "Radiohead", ""))),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SearchTitleRemoteAsync_ProviderThrows_StatusUnavailable_LogsWarning()
    {
        // [AC] REQ-SFT-06: a Deezer failure yields Status Unavailable and a Warning log, not an exception
        _deezerMock
            .Setup(p => p.SearchSongsAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new MusicProviderException("boom"));
        var sut = CreateSut();

        var result = await sut.SearchTitleRemoteAsync("creep", []);

        Assert.Equal(RemoteSearchStatus.Unavailable, result.Status);
        Assert.Empty(result.Remote);
        Assert.Empty(result.Promoted);
        VerifyLog(LogLevel.Warning, Times.AtLeastOnce());
    }

    [Fact]
    public async Task SearchTitleRemoteAsync_ProviderSlowerThanBound_StatusUnavailable()
    {
        // [AC] REQ-SFT-08: a call not complete within the bound (3,000 ms) ends as Unavailable
        _deezerMock
            .Setup(p => p.SearchSongsAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns<string, string?, CancellationToken>(async (_, _, ct) =>
            {
                await Task.Delay(Timeout.Infinite, ct);
                return new List<MusicSearchResultDto>();
            });
        var sut = CreateSut();

        var result = await sut.SearchTitleRemoteAsync("creep", []);

        Assert.Equal(RemoteSearchStatus.Unavailable, result.Status);
        Assert.Empty(result.Remote);
    }

    [Fact]
    public async Task SearchTitleRemoteAsync_CallerCancelled_Rethrows()
    {
        // [AC] REQ-SFT-09: a cancelled (stale) query rethrows instead of reporting Unavailable
        _deezerMock
            .Setup(p => p.SearchSongsAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns<string, string?, CancellationToken>(async (_, _, ct) =>
            {
                await Task.Delay(Timeout.Infinite, ct);
                return new List<MusicSearchResultDto>();
            });
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(100);
        var sut = CreateSut();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => sut.SearchTitleRemoteAsync("creep", [], cts.Token));
    }

    [Fact]
    public async Task SearchTitleRemoteAsync_ZeroResults_StatusOk()
    {
        // [AC] REQ-SFT-06: Deezer answering with zero results is Ok, not Unavailable
        SetupDeezer();
        var sut = CreateSut();

        var result = await sut.SearchTitleRemoteAsync("creep", []);

        Assert.Equal(RemoteSearchStatus.Ok, result.Status);
        Assert.Empty(result.Remote);
        Assert.Empty(result.Promoted);
    }

    [Fact]
    public async Task SearchTitleRemoteAsync_Offline_NoProviderCall_StatusOffline_LogsInformation()
    {
        // [AC] REQ-SFT-53: offline skips Deezer entirely and reports Offline with an Information log
        _networkMock.SetupGet(n => n.HasInternetAccess).Returns(false);
        var sut = CreateSut();

        var result = await sut.SearchTitleRemoteAsync("creep", []);

        Assert.Equal(RemoteSearchStatus.Offline, result.Status);
        Assert.Empty(result.Remote);
        Assert.Empty(result.Promoted);
        VerifyNoProviderCall();
        VerifyLog(LogLevel.Information, Times.AtLeastOnce());
    }

    [Fact]
    public async Task SearchTitleRemoteAsync_OnlineButProviderThrows_StatusUnavailableNotOffline()
    {
        // [AC] REQ-SFT-53: with a route but a failing call the status is Unavailable, never Offline
        _deezerMock
            .Setup(p => p.SearchSongsAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("no route"));
        var sut = CreateSut();

        var result = await sut.SearchTitleRemoteAsync("creep", []);

        Assert.Equal(RemoteSearchStatus.Unavailable, result.Status);
        Assert.NotEqual(RemoteSearchStatus.Offline, result.Status);
    }

    [Fact]
    public async Task SearchTitleRemoteAsync_ReadsNetworkStatusPerCall()
    {
        // [AC] REQ-SFT-53: the network status is read on every call, never cached
        _networkMock.SetupSequence(n => n.HasInternetAccess).Returns(false).Returns(true);
        SetupDeezer();
        var sut = CreateSut();

        var first = await sut.SearchTitleRemoteAsync("creep", []);
        var second = await sut.SearchTitleRemoteAsync("creep", []);

        Assert.Equal(RemoteSearchStatus.Offline, first.Status);
        Assert.Equal(RemoteSearchStatus.Ok, second.Status);
        _deezerMock.Verify(
            p => p.SearchSongsAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SearchTitleRemoteAsync_RemoteRowCarriesVersion()
    {
        // [AC] REQ-SFT-12: a remote row carries the provider's normalised version ("" when none)
        SetupDeezer(
            new MusicSearchResultDto("dz-1", "Deezer", "Radiohead", "Creep", null, "Live"),
            new MusicSearchResultDto("dz-2", "Deezer", "Radiohead", "Karma Police", null));
        var sut = CreateSut();

        var result = await sut.SearchTitleRemoteAsync("radiohead", []);

        Assert.Equal(2, result.Remote.Count);
        var creep = result.Remote.Single(r => r.ExternalId == "dz-1");
        var karma = result.Remote.Single(r => r.ExternalId == "dz-2");
        Assert.Equal("Live", creep.Version);
        Assert.Equal("", karma.Version);
        Assert.True(creep.IsRemote);
        Assert.Equal("Deezer", creep.ExternalProvider);
        Assert.Equal("Creep", creep.Title);
        Assert.Equal("Radiohead", creep.ArtistName);
    }

    [Fact]
    public async Task SearchTitleRemoteAsync_RemoteRowArtistIsLocal_CarriesLocalArtistIdAndName()
    {
        // [AC] REQ-SFT-13: a Deezer artist that collates equal to a local artist carries its id and stored spelling
        SetupDeezer(new MusicSearchResultDto("dz-1", "Deezer", "radiohead", "Creep", null));
        _artistRepoMock
            .Setup(r => r.GetByNamesCollatedAsync(
                It.Is<IEnumerable<string>>(n => n.Contains("radiohead")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Artist> { new() { Id = 42, Name = "Radiohead" } });
        var sut = CreateSut();

        var result = await sut.SearchTitleRemoteAsync("creep", []);

        var remote = Assert.Single(result.Remote);
        Assert.Equal(42, remote.LocalArtistId);
        Assert.Equal("Radiohead", remote.LocalArtistName);
    }

    [Fact]
    public async Task SearchTitleRemoteAsync_QueriesDeezerOnlyWithoutHint()
    {
        // [AC] REQ-SFT-35: Deezer is queried with the plain term and no artist hint; REQ-SFT-36 no other provider is called
        SetupDeezer();
        var sut = CreateSut();

        await sut.SearchTitleRemoteAsync("creep", []);

        _deezerMock.Verify(
            p => p.SearchSongsAsync("creep", null, It.IsAny<CancellationToken>()), Times.Once);
        _musicBrainzMock.Verify(
            p => p.SearchSongsAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SearchTitleRemoteAsync_TakesAtMostFiveRemote()
    {
        // [AC] REQ-SFT-04: at most MaxSearchResults remote options
        SetupDeezer(Enumerable.Range(1, 8)
            .Select(i => new MusicSearchResultDto($"dz-{i}", "Deezer", "Queen", $"Song {i}", null))
            .ToArray());
        var sut = CreateSut();

        var result = await sut.SearchTitleRemoteAsync("song", []);

        Assert.Equal(5, result.Remote.Count);
    }
}
