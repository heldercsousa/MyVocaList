using MyVocaList.Contracts.DTOs;
using MyVocaList.Contracts.DTOs.Suggestions;
using MyVocaList.Domain.Entity;
using MyVocaList.Domain.RepositoryInterface;
using MyVocaList.Domain.Resolution;
using MyVocaList.Domain.ServicesInterfaces;
using MyVocaList.Infra.Similarity;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Unit.Services;

/// <summary>
/// T4 — image field of <see cref="ArtistSuggestionService"/> (plan § T4, design § 3.4, REQ-ART-24).
/// Same mocks and <c>CreateSut</c> pattern as <see cref="ArtistSuggestionServiceTests"/>.
/// The existing test file is not edited.
/// </summary>
public class ArtistSuggestionServiceImageTests
{
    private readonly Mock<IArtistRepository> _repoMock = new();
    private readonly Mock<IMusicMetadataProvider> _musicBrainzMock = new();
    private readonly Mock<IMusicMetadataProvider> _deezerMock = new();
    private readonly Mock<INetworkStatus> _networkMock = new();
    private readonly Mock<ILogger<ArtistSuggestionService>> _loggerMock = new();
    private readonly ISimilarityScorer _scorer = new SimilarityScorer();

    public ArtistSuggestionServiceImageTests()
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

    // PassthroughUnitOfWork runs each wrapped body inline against the same repo mock (REQ-UOW-38).
    private ArtistSuggestionService CreateSut() => new(
        _repoMock.Object,
        PassthroughUnitOfWork.Over(_repoMock),
        [_musicBrainzMock.Object, _deezerMock.Object],
        _scorer,
        _networkMock.Object,
        _loggerMock.Object);

    private void SetupDeezer(params MusicSearchResultDto[] results)
        => _deezerMock
            .Setup(p => p.SearchArtistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(results.ToList());

    [Fact]
    public async Task SearchNameRemoteAsync_RemoteArtistSuggestion_CarriesArtistImage()
    {
        // [AC] REQ-ART-24: a remote artist suggestion carries the Deezer artist picture as ImageUrl
        SetupDeezer(new MusicSearchResultDto("dz-1", "Deezer", "Metallica", null, null, null, "https://dz/picture.jpg"));
        var sut = CreateSut();

        var result = await sut.SearchNameRemoteAsync("metallica");

        var remote = Assert.Single(result.Remote);
        Assert.True(remote.IsRemote);
        Assert.Equal("https://dz/picture.jpg", remote.ImageUrl);
    }

    [Fact]
    public async Task SearchNameRemoteAsync_RemoteArtistWithoutPicture_ImageUrlIsNull()
    {
        // [AC] REQ-ART-24: a remote artist without a picture carries a null ImageUrl
        SetupDeezer(new MusicSearchResultDto("dz-1", "Deezer", "Metallica", null, null));
        var sut = CreateSut();

        var result = await sut.SearchNameRemoteAsync("metallica");

        var remote = Assert.Single(result.Remote);
        Assert.Null(remote.ImageUrl);
    }

    [Fact]
    public async Task SearchNameLocalAsync_StoredArtistWithImage_LocalSuggestionCarriesTheImage()
    {
        // [AC] REQ-PIC-34: a local artist suggestion carries the stored artist picture (replaces REQ-ART-24's null)
        _repoMock
            .Setup(r => r.SearchNameCandidatesAsync("metallica", SimilarityConstants.PoolSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Artist> { new() { Id = 2, Name = "Metallica", ImageUrl = "https://stored/artist.jpg" } });
        var sut = CreateSut();

        var result = await sut.SearchNameLocalAsync("metallica");

        var local = Assert.Single(result);
        Assert.Equal(2, local.LocalId);
        Assert.Equal("https://stored/artist.jpg", local.ImageUrl);
    }
}
