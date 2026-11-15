using MyVocaList.Contracts.DTOs;
using MyVocaList.Domain.ReadModels;
using MyVocaList.Domain.Resolution;
using MyVocaList.Infra.Similarity;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Unit.Services;

/// <summary>
/// L1 — <see cref="SongSuggestionService"/> carries the Deezer track duration on remote rows only
/// (REQ-LYR-01). Same mocks and <c>CreateSut</c> pattern as <see cref="SongSuggestionServiceImageTests"/>.
/// </summary>
public class SongSuggestionServiceDurationTests
{
    private readonly Mock<ISongRepository> _songRepoMock = new();
    private readonly Mock<IArtistRepository> _artistRepoMock = new();
    private readonly Mock<IMusicMetadataProvider> _musicBrainzMock = new();
    private readonly Mock<IMusicMetadataProvider> _deezerMock = new();
    private readonly Mock<INetworkStatus> _networkMock = new();
    private readonly Mock<ILogger<SongSuggestionService>> _loggerMock = new();
    private readonly ISimilarityScorer _scorer = new SimilarityScorer();

    public SongSuggestionServiceDurationTests()
    {
        _musicBrainzMock.SetupGet(p => p.ProviderName).Returns("MusicBrainz");
        _deezerMock.SetupGet(p => p.ProviderName).Returns("Deezer");
        _networkMock.SetupGet(n => n.HasInternetAccess).Returns(true);

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

    private static SongSearchHit Hit(int id, string title, string artist, int artistId)
        => new(id, title, "", artistId, artist, null, null, null, null);

    private void SetupCandidates(string query, params SongSearchHit[] hits)
        => _songRepoMock
            .Setup(r => r.SearchTitleCandidatesAsync(query, SimilarityConstants.PoolSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync(hits.ToList());

    private void SetupDeezer(params MusicSearchResultDto[] results)
        => _deezerMock
            .Setup(p => p.SearchSongsAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(results.ToList());

    [Fact]
    public async Task SearchTitleRemoteAsync_RemoteRow_CarriesProviderDurationSeconds()
    {
        // [AC] REQ-LYR-01: a remote song suggestion copies DurationSeconds from the Deezer result
        SetupDeezer(new MusicSearchResultDto(
            "dz-1", "Deezer", "Radiohead", "Creep", null, null, null, null, "Pablo Honey", 215));
        var sut = CreateSut();

        var result = await sut.SearchTitleRemoteAsync("creep", []);

        var remote = Assert.Single(result.Remote);
        Assert.True(remote.IsRemote);
        Assert.Equal(215, remote.DurationSeconds);
    }

    [Fact]
    public async Task SearchTitleRemoteAsync_RemoteRowWithoutDuration_DurationSecondsIsNull()
    {
        // [AC] REQ-LYR-01: a remote row whose provider result has no duration carries a null DurationSeconds
        SetupDeezer(new MusicSearchResultDto(
            "dz-1", "Deezer", "Radiohead", "Creep", null, null, null, null, "Pablo Honey"));
        var sut = CreateSut();

        var result = await sut.SearchTitleRemoteAsync("creep", []);

        Assert.Null(Assert.Single(result.Remote).DurationSeconds);
    }

    [Fact]
    public async Task SearchTitleLocalAsync_LocalRow_DurationSecondsIsNull()
    {
        // [AC] REQ-LYR-01: a local song suggestion never carries a duration (no lyrics lookup for local picks)
        SetupCandidates("creep", Hit(5, "Creep", "Radiohead", 3));
        var sut = CreateSut();

        var result = await sut.SearchTitleLocalAsync("creep");

        Assert.Null(Assert.Single(result).DurationSeconds);
    }
}
