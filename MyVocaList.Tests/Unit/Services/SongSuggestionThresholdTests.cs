using MyVocaList.Contracts.DTOs;
using MyVocaList.Domain.Constants;
using MyVocaList.Domain.ReadModels;
using MyVocaList.Domain.Resolution;
using MyVocaList.Domain.UnitOfWork;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Unit.Services;

/// <summary>
/// Task 5.2 / 4.3 — <see cref="SongSuggestionService"/> local/remote minimum query-length thresholds
/// (REQ-UOW-51, REQ-UOW-52) and the REQ-UOW-43 no-open-scope-during-fetch guarantee.
/// </summary>
public class SongSuggestionThresholdTests
{
    private readonly Mock<ISongRepository> _songRepoMock = new();
    private readonly Mock<IArtistRepository> _artistRepoMock = new();
    private readonly Mock<IMusicMetadataProvider> _deezerMock = new();
    private readonly Mock<ISimilarityScorer> _scorerMock = new();
    private readonly Mock<INetworkStatus> _networkStatusMock = new();
    private readonly Mock<ILogger<SongSuggestionService>> _loggerMock = new();

    public SongSuggestionThresholdTests()
    {
        _deezerMock.SetupGet(p => p.ProviderName).Returns("Deezer");
        _networkStatusMock.SetupGet(n => n.HasInternetAccess).Returns(true);
    }

    private SongSuggestionService CreateSut() => new(
        _songRepoMock.Object,
        _artistRepoMock.Object,
        PassthroughUnitOfWork.Over(_songRepoMock, _artistRepoMock),
        [_deezerMock.Object],
        _scorerMock.Object,
        _networkStatusMock.Object,
        _loggerMock.Object);

    // ── SearchTitleLocalAsync — REQ-UOW-51 ──────────────────────────────────────────

    [Fact]
    // [AC] REQ-UOW-51: a 1-character local query short-circuits — empty result, repository never called.
    public async Task SearchTitleLocalAsync_OneCharQuery_ReturnsEmptyAndNeverCallsRepository()
    {
        var sut = CreateSut();

        var result = await sut.SearchTitleLocalAsync("a");

        Assert.Empty(result);
        _songRepoMock.Verify(
            r => r.SearchTitleCandidatesAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    // [AC] REQ-UOW-51: a 2-character local query meets MinimumLocalQueryLength and reaches the repository.
    public async Task SearchTitleLocalAsync_TwoCharQuery_ReachesRepository()
    {
        Assert.Equal(2, SearchConstants.MinimumLocalQueryLength);
        _songRepoMock
            .Setup(r => r.SearchTitleCandidatesAsync("ab", SimilarityConstants.PoolSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<SongSearchHit>)new List<SongSearchHit>());
        var sut = CreateSut();

        var result = await sut.SearchTitleLocalAsync("ab");

        Assert.Empty(result);
        _songRepoMock.Verify(
            r => r.SearchTitleCandidatesAsync("ab", SimilarityConstants.PoolSize, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    // [AC] REQ-UOW-51: a query that trims down to below the local threshold short-circuits on the
    // trimmed length, not the raw (padded) length.
    public async Task SearchTitleLocalAsync_QueryTrimsBelowThreshold_ShortCircuits()
    {
        var sut = CreateSut();

        var result = await sut.SearchTitleLocalAsync("  a  ");

        Assert.Empty(result);
        _songRepoMock.Verify(
            r => r.SearchTitleCandidatesAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── SearchTitleRemoteAsync — REQ-UOW-52 ─────────────────────────────────────────

    [Fact]
    // [AC] REQ-UOW-52: a 2-character remote query is below MinimumRemoteQueryLength — the provider
    // fetch is never issued.
    public async Task SearchTitleRemoteAsync_TwoCharQuery_ProviderNeverInvoked()
    {
        var sut = CreateSut();

        var result = await sut.SearchTitleRemoteAsync("ab", []);

        Assert.Empty(result.Promoted);
        Assert.Empty(result.Remote);
        _deezerMock.Verify(
            p => p.SearchSongsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    // [AC] REQ-UOW-52: a 3-character remote query meets MinimumRemoteQueryLength and reaches the provider.
    public async Task SearchTitleRemoteAsync_ThreeCharQuery_ReachesProvider()
    {
        Assert.Equal(3, SearchConstants.MinimumRemoteQueryLength);
        _deezerMock
            .Setup(p => p.SearchSongsAsync("abc", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MusicSearchResultDto>());
        var sut = CreateSut();

        var result = await sut.SearchTitleRemoteAsync("abc", []);

        Assert.Empty(result.Promoted);
        Assert.Empty(result.Remote);
        _deezerMock.Verify(
            p => p.SearchSongsAsync("abc", null, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── REQ-UOW-43 — no open scope during the fetch ─────────────────────────

    [Fact]
    // [AC] REQ-UOW-43: while the provider fetch is in flight, no ExecuteReadAsync scope is open —
    // the lambda never encloses network I/O.
    public async Task SearchTitleRemoteAsync_ProviderBlocks_NoOpenScopeDuringFetch()
    {
        var fetchTcs = new TaskCompletionSource<IEnumerable<MusicSearchResultDto>>();
        _deezerMock
            .Setup(p => p.SearchSongsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(fetchTcs.Task);
        _songRepoMock
            .Setup(r => r.FindByExternalIdsAsync(It.IsAny<string>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<SongSearchHit>)new List<SongSearchHit>());
        _songRepoMock
            .Setup(r => r.FindByTitleAndArtistNamesAsync(It.IsAny<IReadOnlyCollection<(string Title, string ArtistName, string Version)>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<SongSearchHit>)new List<SongSearchHit>());
        _artistRepoMock
            .Setup(r => r.GetByNamesCollatedAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Artist>());
        var uow = new ScopeTrackingUnitOfWork(_songRepoMock.Object, _artistRepoMock.Object);
        var sut = new SongSuggestionService(
            _songRepoMock.Object,
            _artistRepoMock.Object,
            uow,
            [_deezerMock.Object],
            _scorerMock.Object,
            _networkStatusMock.Object,
            _loggerMock.Object);

        var task = sut.SearchTitleRemoteAsync("Yesterday", []);

        Assert.Equal(0, uow.OpenScopes);

        fetchTcs.SetResult(new List<MusicSearchResultDto>
        {
            new("dz-1", "Deezer", "Beatles", "Yesterday", null)
        });
        var result = await task;

        Assert.Single(result.Remote);
        Assert.Equal(0, uow.OpenScopes);
    }

    /// <summary>Test-only <see cref="IUnitOfWork"/> that counts scopes currently inside
    /// <see cref="ExecuteReadAsync{TResult}"/>, so a test can assert zero are open while a provider
    /// fetch blocks (REQ-UOW-43).</summary>
    private sealed class ScopeTrackingUnitOfWork(ISongRepository songRepository, IArtistRepository artistRepository)
        : IUnitOfWork
    {
        private readonly StubServiceProvider _serviceProvider = new(songRepository, artistRepository);

        public int OpenScopes { get; private set; }

        public Task<TResult> ExecuteAsync<TResult>(
            Func<IServiceProvider, Task<TResult>> body, CancellationToken ct = default)
            => body(_serviceProvider);

        public Task ExecuteAsync(Func<IServiceProvider, Task> body, CancellationToken ct = default)
            => body(_serviceProvider);

        public async Task<TResult> ExecuteReadAsync<TResult>(
            Func<IServiceProvider, Task<TResult>> body, CancellationToken ct = default)
        {
            OpenScopes++;
            try
            {
                return await body(_serviceProvider);
            }
            finally
            {
                OpenScopes--;
            }
        }

        public Task FlushAsync(CancellationToken ct = default) => Task.CompletedTask;

        private sealed class StubServiceProvider(ISongRepository songRepository, IArtistRepository artistRepository)
            : IServiceProvider
        {
            public object? GetService(Type serviceType)
            {
                if (serviceType == typeof(ISongRepository))
                    return songRepository;
                if (serviceType == typeof(IArtistRepository))
                    return artistRepository;
                return null;
            }
        }
    }
}
