using MyVocaList.Contracts.DTOs;
using MyVocaList.Domain.Constants;
using MyVocaList.Domain.ReadModels;
using MyVocaList.Domain.Resolution;
using MyVocaList.Infra.Similarity;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Unit.Services;

/// <summary>
/// MusicBrainz title suggestions are distinct per (title, artist name) on the fetched page (REQ-MSRC-17).
/// </summary>
public class SongSuggestionServiceDistinctTests
{
    private readonly Mock<ISongRepository> _songRepoMock = new();
    private readonly Mock<IArtistRepository> _artistRepoMock = new();
    private readonly Mock<IMusicMetadataProvider> _providerMock = new();
    private readonly Mock<INetworkStatus> _networkMock = new();
    private readonly Mock<ILogger<SongSuggestionService>> _loggerMock = new();

    public SongSuggestionServiceDistinctTests()
    {
        _providerMock.SetupGet(p => p.ProviderName).Returns(ProviderNames.MusicBrainz);
        _networkMock.SetupGet(n => n.HasInternetAccess).Returns(true);
        _songRepoMock
            .Setup(r => r.FindByExternalIdsAsync(It.IsAny<string>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SongSearchHit>());
        _songRepoMock
            .Setup(r => r.FindByTitleAndArtistNamesAsync(
                It.IsAny<IReadOnlyCollection<(string Title, string ArtistName)>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SongSearchHit>());
        _artistRepoMock
            .Setup(r => r.GetByNamesCollatedAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Artist>());
    }

    private SongSuggestionService CreateSut() => new(
        _songRepoMock.Object, _artistRepoMock.Object,
        PassthroughUnitOfWork.Over(_songRepoMock, _artistRepoMock),
        [_providerMock.Object], new SimilarityScorer(), _networkMock.Object, _loggerMock.Object);

    private void Setup(params MusicSearchResultDto[] results)
        => _providerMock
            .Setup(p => p.SearchSongsAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(results.ToList());

    private static MusicSearchResultDto Row(string id, string artist, string title, string? artistId)
        => new(ExternalId: id, Provider: ProviderNames.MusicBrainz, ArtistName: artist, SongTitle: title, ExternalArtistId: artistId);

    // [AC] REQ-MSRC-17: the same (title, artist name) under different provider artist ids is one row.
    // Rows without an artist id stay unmerged (REQ-SLD-11).
    [Fact]
    public async Task SearchTitleRemoteAsync_SameTitleAndArtistNameWithDifferentArtistIds_OneRow()
    {
        Setup(
            Row("r1", "Cazuza", "Beber até morrer", "a1"),
            Row("r2", "Cazuza", "Beber até morrer", "a2"),
            Row("r3", "Cazuza", "Beber até morrer", "a3"));

        var result = await CreateSut().SearchTitleRemoteAsync("beber até morrer", []);

        Assert.Single(result.Remote);
    }

    // [AC] REQ-MSRC-17: a different artist with the same title stays a separate row.
    [Fact]
    public async Task SearchTitleRemoteAsync_SameTitleDifferentArtistName_TwoRows()
    {
        Setup(
            Row("r1", "Cazuza", "Beber até morrer", "a1"),
            Row("r2", "Cazuza", "Beber até morrer", "a2"),
            Row("r3", "Outro", "Beber até morrer", "a3"));

        var result = await CreateSut().SearchTitleRemoteAsync("beber até morrer", []);

        Assert.Equal(2, result.Remote.Count);
    }
}
