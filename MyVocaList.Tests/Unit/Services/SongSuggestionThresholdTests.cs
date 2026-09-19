using MyVocaList.Contracts.DTOs;
using MyVocaList.Domain.Constants;
using MyVocaList.Domain.UnitOfWork;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Unit.Services;

/// <summary>
/// Task 5.2 — <see cref="SongSuggestionService"/> local/remote minimum query-length thresholds
/// (REQ-UOW-51, REQ-UOW-52) and the REQ-UOW-43 no-open-scope-during-fetch guarantee.
/// </summary>
public class SongSuggestionThresholdTests
{
    private readonly Mock<ISongRepository> _songRepoMock = new();
    private readonly Mock<IArtistRepository> _artistRepoMock = new();
    private readonly Mock<IMusicMetadataProvider> _musicBrainzMock = new();
    private readonly Mock<IMusicMetadataProvider> _deezerMock = new();
    private readonly Mock<ISimilarityScorer> _scorerMock = new();
    private readonly Mock<ILogger<SongSuggestionService>> _loggerMock = new();

    public SongSuggestionThresholdTests()
    {
        _musicBrainzMock.SetupGet(p => p.ProviderName).Returns("MusicBrainz");
        _deezerMock.SetupGet(p => p.ProviderName).Returns("Deezer");
    }

    private SongSuggestionService CreateSut() => new(
        _songRepoMock.Object,
        _artistRepoMock.Object,
        PassthroughUnitOfWork.Over(_songRepoMock, _artistRepoMock),
        [_musicBrainzMock.Object, _deezerMock.Object],
        _scorerMock.Object,
        _loggerMock.Object);

    // ── GetLocalAsync — REQ-UOW-51 ──────────────────────────────────────────

    [Fact]
    // [AC] REQ-UOW-51: a 1-character local query short-circuits — empty result, repository never called.
    public async Task GetLocalAsync_OneCharQuery_ReturnsEmptyAndNeverCallsRepository()
    {
        var sut = CreateSut();

        var result = await sut.GetLocalAsync("a");

        Assert.Empty(result);
        _songRepoMock.Verify(
            r => r.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    // [AC] REQ-UOW-51: a 2-character local query meets MinimumLocalQueryLength and reaches the repository.
    public async Task GetLocalAsync_TwoCharQuery_ReachesRepository()
    {
        Assert.Equal(2, SearchConstants.MinimumLocalQueryLength);
        _songRepoMock
            .Setup(r => r.GetPagedAsync(1, 5, "ab", It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<SongListItemDto>(), 0));
        var sut = CreateSut();

        var result = await sut.GetLocalAsync("ab");

        Assert.Empty(result);
        _songRepoMock.Verify(
            r => r.GetPagedAsync(1, 5, "ab", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    // [AC] REQ-UOW-51: a query that trims down to below the local threshold short-circuits on the
    // trimmed length, not the raw (padded) length.
    public async Task GetLocalAsync_QueryTrimsBelowThreshold_ShortCircuits()
    {
        var sut = CreateSut();

        var result = await sut.GetLocalAsync("  a  ");

        Assert.Empty(result);
        _songRepoMock.Verify(
            r => r.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── GetRemoteAsync — REQ-UOW-52 ─────────────────────────────────────────

    [Fact]
    // [AC] REQ-UOW-52: a 2-character remote query is below MinimumRemoteQueryLength — the provider
    // fetch is never issued.
    public async Task GetRemoteAsync_TwoCharQuery_ProviderNeverInvoked()
    {
        var sut = CreateSut();

        var result = await sut.GetRemoteAsync("ab", null, []);

        Assert.Empty(result);
        _musicBrainzMock.Verify(
            p => p.SearchSongsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _deezerMock.Verify(
            p => p.SearchSongsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    // [AC] REQ-UOW-52: a 3-character remote query meets MinimumRemoteQueryLength and reaches the provider.
    public async Task GetRemoteAsync_ThreeCharQuery_ReachesProvider()
    {
        Assert.Equal(3, SearchConstants.MinimumRemoteQueryLength);
        _musicBrainzMock
            .Setup(p => p.SearchSongsAsync("abc", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MusicSearchResultDto>());
        _deezerMock
            .Setup(p => p.SearchSongsAsync("abc", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MusicSearchResultDto>());
        var sut = CreateSut();

        var result = await sut.GetRemoteAsync("abc", null, []);

        Assert.Empty(result);
        _musicBrainzMock.Verify(
            p => p.SearchSongsAsync("abc", null, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── REQ-UOW-43 — no open scope during the fetch ─────────────────────────

    [Fact]
    // [AC] REQ-UOW-43: while the provider fetch is in flight, no ExecuteReadAsync scope is open —
    // the lambda never encloses network I/O.
    public async Task GetRemoteAsync_ProviderBlocks_NoOpenScopeDuringFetch()
    {
        var fetchTcs = new TaskCompletionSource<IEnumerable<MusicSearchResultDto>>();
        _musicBrainzMock
            .Setup(p => p.SearchSongsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(fetchTcs.Task);
        _songRepoMock
            .Setup(r => r.GetByTitlesCollatedAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Song>());
        _artistRepoMock
            .Setup(r => r.GetByNamesCollatedAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Artist>());
        var uow = new ScopeTrackingUnitOfWork(_songRepoMock.Object, _artistRepoMock.Object);
        var sut = new SongSuggestionService(
            _songRepoMock.Object,
            _artistRepoMock.Object,
            uow,
            [_musicBrainzMock.Object, _deezerMock.Object],
            _scorerMock.Object,
            _loggerMock.Object);

        var task = sut.GetRemoteAsync("Yesterday", null, []);

        Assert.Equal(0, uow.OpenScopes);

        fetchTcs.SetResult(new List<MusicSearchResultDto>
        {
            new("mb-1", "MusicBrainz", "Beatles", "Yesterday", null)
        });
        var result = await task;

        Assert.Single(result);
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
