using MyVocaList.Contracts.DTOs;
using MyVocaList.Contracts.DTOs.Suggestions;
using MyVocaList.Domain.ReadModels;
using MyVocaList.Domain.Resolution;
using MyVocaList.Infra.Similarity;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Unit.Services;

/// <summary>
/// T4 — image and album fields of <see cref="SongSuggestionService"/> (plan § T4, design § 3.3–3.4,
/// REQ-ART-06). Same mocks and <c>CreateSut</c> pattern as <see cref="SongSuggestionServiceTests"/>.
/// The existing test file is not edited.
/// </summary>
public class SongSuggestionServiceImageTests
{
    private readonly Mock<ISongRepository> _songRepoMock = new();
    private readonly Mock<IArtistRepository> _artistRepoMock = new();
    private readonly Mock<IMusicMetadataProvider> _musicBrainzMock = new();
    private readonly Mock<IMusicMetadataProvider> _deezerMock = new();
    private readonly Mock<INetworkStatus> _networkMock = new();
    private readonly Mock<ILogger<SongSuggestionService>> _loggerMock = new();
    private readonly ISimilarityScorer _scorer = new SimilarityScorer();

    public SongSuggestionServiceImageTests()
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

    private static SongSearchHit Hit(int id, string title, string artist, int artistId, string? songImage, string? artistImage,
        string? provider = null, string? externalId = null)
        => new(id, title, "", artistId, artist, provider, externalId, songImage, artistImage);

    private void SetupCandidates(string query, params SongSearchHit[] hits)
        => _songRepoMock
            .Setup(r => r.SearchTitleCandidatesAsync(query, SimilarityConstants.PoolSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync(hits.ToList());

    private void SetupDeezer(params MusicSearchResultDto[] results)
        => _deezerMock
            .Setup(p => p.SearchSongsAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(results.ToList());

    [Fact]
    public async Task SearchTitleLocalAsync_HitWithBothImages_OptionCarriesSongAndArtistImage()
    {
        // [AC] REQ-ART-06: a local title option carries the stored song image and the stored artist image, and no album
        SetupCandidates("creep", Hit(5, "Creep", "Radiohead", 3, "https://img/song.jpg", "https://img/artist.jpg"));
        var sut = CreateSut();

        var result = await sut.SearchTitleLocalAsync("creep");

        var option = Assert.Single(result);
        Assert.Equal("https://img/song.jpg", option.ImageUrl);
        Assert.Equal("https://img/artist.jpg", option.ArtistImageUrl);
        Assert.Null(option.AlbumTitle);
        Assert.Equal("https://img/song.jpg", option.RowImageUrl);
    }

    [Fact]
    public async Task SearchTitleLocalAsync_NoSongImage_RowImageFallsBackToArtistImage()
    {
        // [AC] REQ-ART-06: without a song image the row image is the artist image
        SetupCandidates("creep", Hit(5, "Creep", "Radiohead", 3, null, "https://img/artist.jpg"));
        var sut = CreateSut();

        var result = await sut.SearchTitleLocalAsync("creep");

        var option = Assert.Single(result);
        Assert.Null(option.ImageUrl);
        Assert.Equal("https://img/artist.jpg", option.RowImageUrl);
    }

    [Fact]
    public async Task SearchTitleLocalAsync_NoImages_RowImageIsNull()
    {
        // [AC] REQ-ART-06: with neither image the row image is null (the ViewModel shows the placeholder)
        SetupCandidates("creep", Hit(5, "Creep", "Radiohead", 3, null, null));
        var sut = CreateSut();

        var result = await sut.SearchTitleLocalAsync("creep");

        var option = Assert.Single(result);
        Assert.Null(option.RowImageUrl);
    }

    [Fact]
    public async Task SearchTitleRemoteAsync_RemoteTitleOption_CarriesCoverArtistImageAndAlbum()
    {
        // [AC] REQ-ART-06: a remote title option carries the Deezer cover, artist picture and album title
        SetupDeezer(new MusicSearchResultDto(
            "dz-1", "Deezer", "Radiohead", "Creep", null, "Live",
            "https://dz/cover.jpg", "https://dz/artist.jpg", "Pablo Honey"));
        var sut = CreateSut();

        var result = await sut.SearchTitleRemoteAsync("creep", []);

        var remote = Assert.Single(result.Remote);
        Assert.Equal("https://dz/cover.jpg", remote.ImageUrl);
        Assert.Equal("https://dz/artist.jpg", remote.ArtistImageUrl);
        Assert.Equal("Pablo Honey", remote.AlbumTitle);
        Assert.Equal("https://dz/cover.jpg", remote.RowImageUrl);
    }

    [Fact]
    public async Task SearchTitleRemoteAsync_RemoteTitleOptionNoCover_RowImageFallsBackToArtistPicture()
    {
        // [AC] REQ-ART-06: a remote option without a cover shows the artist picture as its row image
        SetupDeezer(new MusicSearchResultDto(
            "dz-1", "Deezer", "Radiohead", "Creep", null, null, null, "https://dz/artist.jpg", null));
        var sut = CreateSut();

        var result = await sut.SearchTitleRemoteAsync("creep", []);

        var remote = Assert.Single(result.Remote);
        Assert.Null(remote.ImageUrl);
        Assert.Equal("https://dz/artist.jpg", remote.RowImageUrl);
        Assert.Null(remote.AlbumTitle);
    }

    [Fact]
    public async Task SearchTitleRemoteAsync_PromotedRow_CarriesStoredImagesNotDeezerImages()
    {
        // [AC] REQ-ART-06: a Deezer row promoted to a local option shows the stored song's images, not Deezer's, and no album
        SetupDeezer(new MusicSearchResultDto(
            "dz-1", "Deezer", "Radiohead", "Creep", null, null,
            "https://dz/cover.jpg", "https://dz/artist.jpg", "Pablo Honey"));
        _songRepoMock
            .Setup(r => r.FindByExternalIdsAsync(
                "Deezer", It.Is<IReadOnlyCollection<string>>(ids => ids.Contains("dz-1")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SongSearchHit>
            {
                Hit(7, "Creep", "Radiohead", 3, "https://stored/song.jpg", "https://stored/artist.jpg", "Deezer", "dz-1"),
            });
        var sut = CreateSut();

        var result = await sut.SearchTitleRemoteAsync("creep", []);

        var promoted = Assert.Single(result.Promoted);
        Assert.Equal(7, promoted.LocalId);
        Assert.False(promoted.IsRemote);
        Assert.Equal("https://stored/song.jpg", promoted.ImageUrl);
        Assert.Equal("https://stored/artist.jpg", promoted.ArtistImageUrl);
        Assert.Null(promoted.AlbumTitle);
        Assert.Empty(result.Remote);
    }
}
