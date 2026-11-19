using MyVocaList.Contracts.DTOs;
using MyVocaList.Contracts.DTOs.Suggestions;
using MyVocaList.Domain.Constants;
using MyVocaList.Domain.Resolution;
using MyVocaList.Domain.UnitOfWork;
using MyVocaList.Infra.Similarity;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Unit.Services;

/// <summary>
/// Task 6.1 (retargeted from 5.1) — <see cref="ArtistSuggestionService"/> local/remote minimum-length
/// thresholds (REQ-UOW-51/52) and the REQ-UOW-43 no-open-scope-during-network-fetch guarantee, now
/// asserted on <c>SearchNameLocalAsync</c> / <c>SearchNameRemoteAsync</c> (same assertions as the old
/// <c>GetLocalAsync</c> / <c>GetRemoteAsync</c> tests). Network status is set online explicitly
/// (Moq default is false = Offline, plan § 0.3 D21).
/// </summary>
public class ArtistSuggestionThresholdTests
{
    private readonly Mock<IArtistRepository> _repoMock = new();
    private readonly Mock<IMusicMetadataProvider> _musicBrainzMock = new();
    private readonly Mock<IMusicMetadataProvider> _deezerMock = new();
    private readonly Mock<INetworkStatus> _networkMock = new();
    private readonly Mock<ILogger<ArtistSuggestionService>> _loggerMock = new();
    private readonly ISimilarityScorer _scorer = new SimilarityScorer();

    public ArtistSuggestionThresholdTests()
    {
        _musicBrainzMock.SetupGet(p => p.ProviderName).Returns("MusicBrainz");
        _deezerMock.SetupGet(p => p.ProviderName).Returns("Deezer");
        _networkMock.SetupGet(n => n.HasInternetAccess).Returns(true);

        _repoMock
            .Setup(r => r.SearchNameCandidatesAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Artist>());
        _repoMock
            .Setup(r => r.GetByNamesCollatedAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Artist>());
    }

    private ArtistSuggestionService CreateSut(IUnitOfWork uow) => new(
        _repoMock.Object,
        uow,
        [_musicBrainzMock.Object, _deezerMock.Object],
        _scorer,
        _networkMock.Object,
        _loggerMock.Object);

    // ── SearchNameLocalAsync — REQ-UOW-51 ───────────────────────────────────

    [Fact]
    // [AC] REQ-UOW-51: a query shorter than SearchConstants.MinimumLocalQueryLength never reaches
    // the repository (the guard stays outside the wrap, zero ExecuteReadAsync invocations).
    public async Task SearchNameLocalAsync_OneCharQuery_ReturnsEmptyAndNeverCallsRepository()
    {
        var counting = new CountingUnitOfWork(PassthroughUnitOfWork.Over(_repoMock));
        var sut = CreateSut(counting);

        var query = new string('a', SearchConstants.MinimumLocalQueryLength - 1);
        var result = await sut.SearchNameLocalAsync(query);

        Assert.Empty(result);
        Assert.Equal(0, counting.ReadCallCount);
        _repoMock.Verify(
            r => r.SearchNameCandidatesAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    // [AC] REQ-UOW-51: a query at exactly SearchConstants.MinimumLocalQueryLength DOES reach the
    // repository (the guard is exclusive, not inclusive, of the threshold).
    public async Task SearchNameLocalAsync_QueryAtMinimumLength_ReachesRepository()
    {
        var query = new string('a', SearchConstants.MinimumLocalQueryLength);
        var counting = new CountingUnitOfWork(PassthroughUnitOfWork.Over(_repoMock));
        var sut = CreateSut(counting);

        var result = await sut.SearchNameLocalAsync(query);

        Assert.NotNull(result);
        Assert.Equal(1, counting.ReadCallCount);
        _repoMock.Verify(
            r => r.SearchNameCandidatesAsync(query, SimilarityConstants.PoolSize, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── SearchNameRemoteAsync — REQ-UOW-52 ──────────────────────────────────

    [Fact]
    // [AC] REQ-UOW-52: a query shorter than SearchConstants.MinimumRemoteQueryLength never reaches
    // the remote provider.
    public async Task SearchNameRemoteAsync_TwoCharQuery_ReturnsEmptyAndNeverCallsProvider()
    {
        var query = new string('a', SearchConstants.MinimumRemoteQueryLength - 1);
        var counting = new CountingUnitOfWork(PassthroughUnitOfWork.Over(_repoMock));
        var sut = CreateSut(counting);

        var result = await sut.SearchNameRemoteAsync(query);

        Assert.Empty(result.Remote);
        Assert.Equal(0, counting.ReadCallCount);
        _musicBrainzMock.Verify(
            p => p.SearchArtistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _deezerMock.Verify(
            p => p.SearchArtistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    // [AC] REQ-UOW-52: a query at exactly SearchConstants.MinimumRemoteQueryLength DOES reach the
    // remote provider (the guard is exclusive, not inclusive, of the threshold).
    public async Task SearchNameRemoteAsync_QueryAtMinimumRemoteLength_ReachesProvider()
    {
        var query = new string('a', SearchConstants.MinimumRemoteQueryLength);
        _deezerMock
            .Setup(p => p.SearchArtistsAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new MusicSearchResultDto("dz-1", "Deezer", query, null, null) });

        var sut = CreateSut(PassthroughUnitOfWork.Over(_repoMock));

        var result = await sut.SearchNameRemoteAsync(query);

        Assert.Single(result.Remote);
        _deezerMock.Verify(
            p => p.SearchArtistsAsync(query, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── REQ-UOW-43 — no open scope while the remote fetch is in flight ──────

    [Fact]
    // [AC] REQ-UOW-43: no DI scope is held open while the remote provider fetch is in flight — the
    // provider call happens entirely before/outside the ExecuteReadAsync wrap around the DB call.
    public async Task SearchNameRemoteAsync_WhileProviderFetchBlocks_NoScopeIsOpen()
    {
        var providerEntered = new TaskCompletionSource();
        var releaseProvider = new TaskCompletionSource();
        var counting = new CountingUnitOfWork(PassthroughUnitOfWork.Over(_repoMock));

        _deezerMock
            .Setup(p => p.SearchArtistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                providerEntered.SetResult();
                await releaseProvider.Task;
                return new[] { new MusicSearchResultDto("dz-1", "Deezer", "Anitta", null, null) };
            });

        var sut = CreateSut(counting);

        var remoteTask = sut.SearchNameRemoteAsync("Anitta");

        await providerEntered.Task;
        // While the provider call is blocked, the DB-call wrap has not executed yet — zero open
        // scopes and zero completed reads at this point.
        Assert.Equal(0, counting.OpenScopeCount);
        Assert.Equal(0, counting.ReadCallCount);

        releaseProvider.SetResult();
        await remoteTask;

        // After the provider fetch completes, the collated DB lookup runs inside one
        // ExecuteReadAsync and closes its scope — no scope is ever left open, exactly one read.
        Assert.Equal(0, counting.OpenScopeCount);
        Assert.Equal(1, counting.ReadCallCount);
    }

    /// <summary>Wraps a real <see cref="IUnitOfWork"/> to count <see cref="ExecuteReadAsync{TResult}"/>
    /// invocations and currently-open scopes, so a guard's "zero scope creation" / "no scope held
    /// open across the network round-trip" claims can be asserted mechanically instead of by
    /// inspection.</summary>
    private sealed class CountingUnitOfWork(IUnitOfWork inner) : IUnitOfWork
    {
        public int ReadCallCount { get; private set; }
        public int OpenScopeCount { get; private set; }

        public Task<TResult> ExecuteAsync<TResult>(Func<IServiceProvider, Task<TResult>> body, CancellationToken ct = default)
            => inner.ExecuteAsync(body, ct);

        public Task ExecuteAsync(Func<IServiceProvider, Task> body, CancellationToken ct = default)
            => inner.ExecuteAsync(body, ct);

        public async Task<TResult> ExecuteReadAsync<TResult>(Func<IServiceProvider, Task<TResult>> body, CancellationToken ct = default)
        {
            OpenScopeCount++;
            try
            {
                var result = await inner.ExecuteReadAsync(body, ct);
                ReadCallCount++;
                return result;
            }
            finally
            {
                OpenScopeCount--;
            }
        }

        public Task FlushAsync(CancellationToken ct = default) => inner.FlushAsync(ct);
    }
}
