using System.Linq;
using Microsoft.EntityFrameworkCore;
using MyVocaList.Contracts.DTOs;
using MyVocaList.Contracts.DTOs.Suggestions;
using MyVocaList.Domain.Constants;
using MyVocaList.Domain.Resolution;
using MyVocaList.Extensions;
using MyVocaList.Infra;
using MyVocaList.Infra.Similarity;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Unit.Services;

/// <summary>
/// Task 6.1 — <see cref="ArtistSuggestionService.SearchNameLocalAsync"/> and
/// <see cref="ArtistSuggestionService.SearchNameRemoteAsync"/> (design § 3.3 artist part, § 4.1,
/// REQ-SFT-03/06/08/09/47/48/53; plan § 0.3 D20/D21). Ranking tests use the real (pure)
/// <see cref="SimilarityScorer"/>; the repository, provider and network status are Moq.
/// <c>Mock&lt;INetworkStatus&gt;</c> defaults to <c>false</c> (= Offline), so the constructor sets it to
/// <c>true</c> explicitly; only the Offline tests set <c>false</c> (plan § 0.3 D21).
/// Constructor order the Builder must implement: (artistRepository, uow, providers, scorer,
/// networkStatus, logger). Deezer is the only provider consulted (REQ-SFT-36).
/// </summary>
public class ArtistSuggestionServiceTests
{
    private readonly Mock<IArtistRepository> _repoMock = new();
    private readonly Mock<IMusicMetadataProvider> _musicBrainzMock = new();
    private readonly Mock<IMusicMetadataProvider> _deezerMock = new();
    private readonly Mock<INetworkStatus> _networkMock = new();
    private readonly Mock<ILogger<ArtistSuggestionService>> _loggerMock = new();
    private readonly ISimilarityScorer _scorer = new SimilarityScorer();

    public ArtistSuggestionServiceTests()
    {
        _musicBrainzMock.SetupGet(p => p.ProviderName).Returns("MusicBrainz");
        _deezerMock.SetupGet(p => p.ProviderName).Returns("Deezer");
        _networkMock.SetupGet(n => n.HasInternetAccess).Returns(true);

        // Moq returns null for Task<IReadOnlyList<T>> by default; make every lookup explicit-empty.
        _repoMock
            .Setup(r => r.SearchNameCandidatesAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Artist>());
        _repoMock
            .Setup(r => r.GetByNamesCollatedAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Artist>());
    }

    // PassthroughUnitOfWork runs each wrapped body inline against the same repo mock (REQ-UOW-38).
    private ArtistSuggestionService CreateSut() => new(
        _repoMock.Object,
        PassthroughUnitOfWork.Over(_repoMock),
        [_musicBrainzMock.Object, _deezerMock.Object],
        _scorer,
        _networkMock.Object,
        _loggerMock.Object);

    private static Artist Stored(int id, string name) => new() { Id = id, Name = name };

    private void SetupDeezer(params MusicSearchResultDto[] results)
        => _deezerMock
            .Setup(p => p.SearchArtistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(results.ToList());

    private static MusicSearchResultDto DeezerArtist(string id, string name)
        => new(id, "Deezer", name, null, null);

    /// <summary>A collated lookup for <paramref name="name"/> finds <paramref name="stored"/>; every other name finds nothing.</summary>
    private void SetupCollatedMatch(string name, Artist stored)
        => _repoMock
            .Setup(r => r.GetByNamesCollatedAsync(
                It.Is<IEnumerable<string>>(names => names.Contains(name)), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Artist> { stored });

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
            p => p.SearchArtistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _musicBrainzMock.Verify(
            p => p.SearchArtistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── SearchNameLocalAsync ─────────────────────────────────────────────

    [Theory]
    [InlineData("a")]
    [InlineData(" a ")]
    [InlineData("")]
    public async Task SearchNameLocalAsync_BelowTwoTrimmedChars_ReturnsEmpty(string query)
    {
        // [AC] REQ-SFT-03: a query under 2 trimmed characters returns nothing and never reaches the repository
        var sut = CreateSut();

        var result = await sut.SearchNameLocalAsync(query);

        Assert.Empty(result);
        _repoMock.Verify(
            r => r.SearchNameCandidatesAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SearchNameLocalAsync_UsesFtsCandidatesRankedMaxFive()
    {
        // [AC] REQ-SFT-47: local names come from the FTS candidate pool (PoolSize), ranked by RankScore, at most 5; the collated prefix query is not used
        var pool = new List<Artist>
        {
            Stored(1, "Zzz Qqq"), Stored(2, "Anitta Band"), Stored(3, "Anita"), Stored(4, "Xylophone"),
            Stored(5, "Anitta"), Stored(6, "Anitta Live"), Stored(7, "Quark"),
        };
        _repoMock
            .Setup(r => r.SearchNameCandidatesAsync("Anitta", SimilarityConstants.PoolSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pool);
        var sut = CreateSut();

        var result = await sut.SearchNameLocalAsync("Anitta");

        Assert.Equal(SearchConstants.MaxSearchResults, result.Count);
        Assert.Equal(
            pool.OrderByDescending(a => _scorer.RankScore("Anitta", a.Name)).Take(SearchConstants.MaxSearchResults).Select(a => a.Id),
            result.Select(r => r.LocalId!.Value));
        Assert.Equal("Anitta", result[0].Name);
        Assert.All(result, r => Assert.False(r.IsRemote));
        _repoMock.Verify(
            r => r.SearchNameCandidatesAsync("Anitta", SimilarityConstants.PoolSize, It.IsAny<CancellationToken>()),
            Times.Once);
        _repoMock.Verify(
            r => r.SearchByNameAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SearchNameLocalAsync_Metalica_ReturnsLocalMetallicaFirst()
    {
        // [AC] REQ-SFT-47: typing "metalica" lists the stored "Metallica" as the first local option, even when the DB order puts it last
        _repoMock
            .Setup(r => r.SearchNameCandidatesAsync("metalica", SimilarityConstants.PoolSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Artist> { Stored(1, "Iron Maiden"), Stored(2, "Metallica") });
        var sut = CreateSut();

        var result = await sut.SearchNameLocalAsync("metalica");

        Assert.Equal(2, result[0].LocalId);
        Assert.Equal("Metallica", result[0].Name);
        Assert.False(result[0].IsRemote);
    }

    // ── SearchNameRemoteAsync ────────────────────────────────────────────

    [Fact]
    public async Task SearchNameRemoteAsync_DeezerMetallica_DroppedWhenLocalMetallicaExists()
    {
        // [AC] REQ-SFT-48: a Deezer "Metallica" is not shown as a separate option when "Metallica" is stored locally
        SetupDeezer(DeezerArtist("dz-1", "Metallica"), DeezerArtist("dz-2", "Megadeth"));
        SetupCollatedMatch("Metallica", Stored(9, "Metallica"));
        var sut = CreateSut();

        var result = await sut.SearchNameRemoteAsync("metallica");

        Assert.Equal(RemoteSearchStatus.Ok, result.Status);
        var kept = Assert.Single(result.Remote);
        Assert.Equal("Megadeth", kept.Name);
        Assert.True(kept.IsRemote);
        Assert.Null(kept.LocalId);
        Assert.Equal("Deezer", kept.ExternalProvider);
    }

    [Theory]
    [InlineData("a")]
    [InlineData("ab")]
    [InlineData(" ab ")]
    public async Task SearchNameRemoteAsync_BelowThreeTrimmedChars_NoProviderCall(string query)
    {
        // [AC] REQ-SFT-03: a query under 3 trimmed characters never calls the provider and is not an error status
        var sut = CreateSut();

        var result = await sut.SearchNameRemoteAsync(query);

        Assert.Empty(result.Remote);
        Assert.Equal(RemoteSearchStatus.Ok, result.Status);
        VerifyNoProviderCall();
    }

    [Fact]
    public async Task SearchNameRemoteAsync_DeezerNameCollatedMatchOfLocal_Dropped()
    {
        // [AC] REQ-SFT-48: a Deezer name that the database collation matches to a local artist (accent/case differences) is dropped; the database decides, not C#
        SetupDeezer(DeezerArtist("dz-1", "Beyoncé"), DeezerArtist("dz-2", "Rihanna"));
        SetupCollatedMatch("Beyoncé", Stored(4, "Beyonce"));
        var sut = CreateSut();

        var result = await sut.SearchNameRemoteAsync("beyonc");

        var kept = Assert.Single(result.Remote);
        Assert.Equal("Rihanna", kept.Name);
        _repoMock.Verify(
            r => r.GetByNamesCollatedAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task SearchNameRemoteAsync_ProviderThrows_StatusUnavailable_LogsWarning()
    {
        // [AC] REQ-SFT-06: a Deezer failure yields Status Unavailable and a Warning log, not an exception
        _deezerMock
            .Setup(p => p.SearchArtistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new MusicProviderException("boom"));
        var sut = CreateSut();

        var result = await sut.SearchNameRemoteAsync("metallica");

        Assert.Equal(RemoteSearchStatus.Unavailable, result.Status);
        Assert.Empty(result.Remote);
        VerifyLog(LogLevel.Warning, Times.AtLeastOnce());
    }

    [Fact]
    public async Task SearchNameRemoteAsync_ProviderSlowerThanBound_StatusUnavailable()
    {
        // [AC] REQ-SFT-08: a call not complete within the bound (3,000 ms) ends as Unavailable
        _deezerMock
            .Setup(p => p.SearchArtistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<string, CancellationToken>(async (_, ct) =>
            {
                await Task.Delay(Timeout.Infinite, ct);
                return new List<MusicSearchResultDto>();
            });
        var sut = CreateSut();

        var result = await sut.SearchNameRemoteAsync("metallica");

        Assert.Equal(RemoteSearchStatus.Unavailable, result.Status);
        Assert.Empty(result.Remote);
    }

    [Fact]
    public async Task SearchNameRemoteAsync_CallerCancelled_Rethrows()
    {
        // [AC] REQ-SFT-09: a cancelled (stale) query rethrows instead of reporting Unavailable
        _deezerMock
            .Setup(p => p.SearchArtistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<string, CancellationToken>(async (_, ct) =>
            {
                await Task.Delay(Timeout.Infinite, ct);
                return new List<MusicSearchResultDto>();
            });
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(100);
        var sut = CreateSut();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => sut.SearchNameRemoteAsync("metallica", cts.Token));
    }

    [Fact]
    public async Task SearchNameRemoteAsync_ZeroResults_StatusOk()
    {
        // [AC] REQ-SFT-06: Deezer answering with zero results is Ok, not Unavailable
        SetupDeezer();
        var sut = CreateSut();

        var result = await sut.SearchNameRemoteAsync("metallica");

        Assert.Equal(RemoteSearchStatus.Ok, result.Status);
        Assert.Empty(result.Remote);
    }

    [Fact]
    public async Task SearchNameRemoteAsync_Offline_NoProviderCall_StatusOffline_LogsInformation()
    {
        // [AC] REQ-SFT-53: offline skips Deezer entirely and reports Offline with an Information log
        _networkMock.SetupGet(n => n.HasInternetAccess).Returns(false);
        var sut = CreateSut();

        var result = await sut.SearchNameRemoteAsync("metallica");

        Assert.Equal(RemoteSearchStatus.Offline, result.Status);
        Assert.Empty(result.Remote);
        VerifyNoProviderCall();
        VerifyLog(LogLevel.Information, Times.AtLeastOnce());
    }

    [Fact]
    public async Task SearchNameRemoteAsync_OnlineButProviderThrows_StatusUnavailableNotOffline()
    {
        // [AC] REQ-SFT-53: with a route but a failing call the status is Unavailable, never Offline
        _deezerMock
            .Setup(p => p.SearchArtistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("no route"));
        var sut = CreateSut();

        var result = await sut.SearchNameRemoteAsync("metallica");

        Assert.Equal(RemoteSearchStatus.Unavailable, result.Status);
        Assert.NotEqual(RemoteSearchStatus.Offline, result.Status);
    }

    [Fact]
    public async Task SearchNameRemoteAsync_ReadsNetworkStatusPerCall()
    {
        // [AC] REQ-SFT-53: the network status is read on every call, never cached
        _networkMock.SetupSequence(n => n.HasInternetAccess).Returns(false).Returns(true);
        SetupDeezer();
        var sut = CreateSut();

        var first = await sut.SearchNameRemoteAsync("metallica");
        var second = await sut.SearchNameRemoteAsync("metallica");

        Assert.Equal(RemoteSearchStatus.Offline, first.Status);
        Assert.Equal(RemoteSearchStatus.Ok, second.Status);
        _deezerMock.Verify(
            p => p.SearchArtistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── DI registration ──────────────────────────────────────────────────

    [Fact]
    public void AddAppServices_ResolvingArtistSuggestionService_Succeeds()
    {
        // [AC] REQ-SFT-47: the artist name search service resolves from AddAppServices
        var services = new ServiceCollection();
        services.AddLogging();
        // Repositories need an AppDbContext registration; it is never queried here.
        services.AddDbContext<AppDbContext>(o => o.UseSqlite("Data Source=:memory:"));
        services.AddAppServices();
        // INetworkStatus is registered in MauiProgram.cs, not AddAppServices — mocked here (D21).
        services.AddSingleton(Mock.Of<INetworkStatus>());
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<IArtistSuggestionService>();

        Assert.NotNull(service);
    }
}
