using MyVocaList.Contracts.DTOs;
using MyVocaList.Contracts.DTOs.Suggestions;
using MyVocaList.Domain.Constants;
using MyVocaList.Domain.ReadModels;
using MyVocaList.Domain.Resolution;
using MyVocaList.Domain.UnitOfWork;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Unit.Services;

/// <summary>
/// Task 5.1 — <see cref="ArtistSuggestionService"/> local/remote minimum-length thresholds
/// (REQ-UOW-51/52) and the REQ-UOW-43 no-open-scope-during-network-fetch guarantee.
/// </summary>
public class ArtistSuggestionThresholdTests
{
    private readonly Mock<IArtistRepository> _repoMock = new();
    private readonly Mock<IMusicMetadataProvider> _musicBrainzMock = new();
    private readonly Mock<IMusicMetadataProvider> _deezerMock = new();
    private readonly Mock<ISimilarityScorer> _scorerMock = new();
    private readonly Mock<ILogger<ArtistSuggestionService>> _loggerMock = new();

    public ArtistSuggestionThresholdTests()
    {
        _musicBrainzMock.Setup(p => p.ProviderName).Returns("MusicBrainz");
        _deezerMock.Setup(p => p.ProviderName).Returns("Deezer");
    }

    private ArtistSuggestionService CreateSut(IUnitOfWork uow) => new(
        _repoMock.Object,
        uow,
        new[] { _musicBrainzMock.Object, _deezerMock.Object },
        _scorerMock.Object,
        _loggerMock.Object);

    // ── GetLocalAsync — REQ-UOW-51 ──────────────────────────────────────────

    [Fact]
    // [AC] REQ-UOW-51: a query shorter than SearchConstants.MinimumLocalQueryLength never reaches
    // the repository (the guard stays outside the wrap, zero ExecuteReadAsync invocations).
    public async Task GetLocalAsync_OneCharQuery_ReturnsEmptyAndNeverCallsRepository()
    {
        var counting = new CountingUnitOfWork(PassthroughUnitOfWork.Over(_repoMock));
        var sut = CreateSut(counting);

        var query = new string('a', SearchConstants.MinimumLocalQueryLength - 1);
        var result = await sut.GetLocalAsync(query);

        Assert.Empty(result);
        Assert.Equal(0, counting.ReadCallCount);
        _repoMock.Verify(
            r => r.SearchByNameAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    // [AC] REQ-UOW-51: a query at exactly SearchConstants.MinimumLocalQueryLength DOES reach the
    // repository (the guard is exclusive, not inclusive, of the threshold).
    public async Task GetLocalAsync_QueryAtMinimumLength_ReachesRepository()
    {
        var query = new string('a', SearchConstants.MinimumLocalQueryLength);
        _repoMock
            .Setup(r => r.SearchByNameAsync(query, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ArtistListItem>());
        _repoMock
            .Setup(r => r.GetByNameAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Artist?)null);

        var counting = new CountingUnitOfWork(PassthroughUnitOfWork.Over(_repoMock));
        var sut = CreateSut(counting);

        var result = await sut.GetLocalAsync(query);

        Assert.NotNull(result);
        Assert.Equal(1, counting.ReadCallCount);
        _repoMock.Verify(
            r => r.SearchByNameAsync(query, It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── GetRemoteAsync — REQ-UOW-52 ──────────────────────────────────────────

    [Fact]
    // [AC] REQ-UOW-52: a query shorter than SearchConstants.MinimumRemoteQueryLength never reaches
    // the remote provider.
    public async Task GetRemoteAsync_TwoCharQuery_ReturnsEmptyAndNeverCallsProvider()
    {
        var query = new string('a', SearchConstants.MinimumRemoteQueryLength - 1);
        var counting = new CountingUnitOfWork(PassthroughUnitOfWork.Over(_repoMock));
        var sut = CreateSut(counting);

        var result = await sut.GetRemoteAsync(query, []);

        Assert.Empty(result);
        Assert.Equal(0, counting.ReadCallCount);
        _musicBrainzMock.Verify(
            p => p.SearchArtistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _deezerMock.Verify(
            p => p.SearchArtistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    // [AC] REQ-UOW-52: a query at exactly SearchConstants.MinimumRemoteQueryLength DOES reach the
    // remote provider (the guard is exclusive, not inclusive, of the threshold).
    public async Task GetRemoteAsync_QueryAtMinimumRemoteLength_ReachesProvider()
    {
        var query = new string('a', SearchConstants.MinimumRemoteQueryLength);
        _musicBrainzMock
            .Setup(p => p.SearchArtistsAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new MusicSearchResultDto("mb-1", "MusicBrainz", query, null, null) });
        _repoMock
            .Setup(r => r.GetByNamesCollatedAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Artist>());

        var sut = CreateSut(PassthroughUnitOfWork.Over(_repoMock));

        var result = await sut.GetRemoteAsync(query, []);

        Assert.Single(result);
        _musicBrainzMock.Verify(
            p => p.SearchArtistsAsync(query, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── REQ-UOW-43 — no open scope while the remote fetch is in flight ──────

    [Fact]
    // [AC] REQ-UOW-43: no DI scope is held open while the remote provider fetch is in flight — the
    // provider call happens entirely before/outside the ExecuteReadAsync wrap around the DB call.
    public async Task GetRemoteAsync_WhileProviderFetchBlocks_NoScopeIsOpen()
    {
        var providerEntered = new TaskCompletionSource();
        var releaseProvider = new TaskCompletionSource();
        var counting = new CountingUnitOfWork(PassthroughUnitOfWork.Over(_repoMock));

        _musicBrainzMock
            .Setup(p => p.SearchArtistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                providerEntered.SetResult();
                await releaseProvider.Task;
                return new[] { new MusicSearchResultDto("mb-1", "MusicBrainz", "Anitta", null, null) };
            });

        var sut = CreateSut(counting);

        var remoteTask = sut.GetRemoteAsync("Anitta", []);

        await providerEntered.Task;
        // While the provider call is blocked, the DB-call wrap has not executed yet (it happens
        // after dedup filtering completes) — zero open scopes and zero completed reads at this point.
        Assert.Equal(0, counting.OpenScopeCount);
        Assert.Equal(0, counting.ReadCallCount);

        releaseProvider.SetResult();
        await remoteTask;

        // After the provider fetch completes, the DB call runs and closes its scope — no scope is
        // ever left open, and exactly one read was issued.
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
