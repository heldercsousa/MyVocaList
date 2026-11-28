using MyVocaList.Contracts.DTOs;
using MyVocaList.Contracts.DTOs.Suggestions;
using MyVocaList.Domain.Constants;
using MyVocaList.Domain.ReadModels;
using MyVocaList.Domain.Resolution;
using MyVocaList.Infra.Similarity;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Unit.Services;

/// <summary>iTunes title suggestions: retry, token re-rank, no stored identity, post-pick MusicBrainz ids (REQ-ITS-02, 10, 11, 14, 16).</summary>
public class SongSuggestionITunesTests
{
    private readonly Mock<ISongRepository> _songRepoMock = new();
    private readonly Mock<IArtistRepository> _artistRepoMock = new();
    private readonly Mock<IMusicMetadataProvider> _itunesMock = new();
    private readonly Mock<IMusicMetadataProvider> _musicBrainzMock = new();
    private readonly Mock<INetworkStatus> _networkMock = new();
    private readonly Mock<ILogger<SongSuggestionService>> _loggerMock = new();

    public SongSuggestionITunesTests()
    {
        _itunesMock.SetupGet(p => p.ProviderName).Returns(ProviderNames.ITunes);
        _musicBrainzMock.SetupGet(p => p.ProviderName).Returns(ProviderNames.MusicBrainz);
        _networkMock.SetupGet(n => n.HasInternetAccess).Returns(true);
        _songRepoMock
            .Setup(r => r.FindByTitleAndArtistNamesAsync(
                It.IsAny<IReadOnlyCollection<(string Title, string ArtistName)>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SongSearchHit>());
        _artistRepoMock
            .Setup(r => r.GetByNamesCollatedAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Artist>());
    }

    private SongSuggestionService CreateSut() => new(
        _songRepoMock.Object,
        _artistRepoMock.Object,
        PassthroughUnitOfWork.Over(_songRepoMock, _artistRepoMock),
        [_itunesMock.Object, _musicBrainzMock.Object],
        new SimilarityScorer(),
        _networkMock.Object,
        _loggerMock.Object);

    private static MusicSearchResultDto Row(string id, string artist, string title) =>
        new(id, ProviderNames.ITunes, artist, title);

    [Fact]
    public async Task SearchTitleRemoteAsync_FewerThanThreeRows_RetriesWithLastWordDropped()
    {
        // [AC] REQ-ITS-14: "Don't crie" returns one row, so the retry for "Don't" adds its rows after the first answer's rows
        _itunesMock.Setup(p => p.SearchSongsAsync("Don't crie", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([Row("1", "Band A", "Dance Floor")]);
        _itunesMock.Setup(p => p.SearchSongsAsync("Don't", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([Row("2", "Guns N' Roses", "Don't Cry"), Row("3", "Band B", "Don't Stop")]);

        var result = await CreateSut().SearchTitleRemoteAsync("Don't crie", []);

        Assert.Equal(3, result.Remote.Count);
        _itunesMock.Verify(p => p.SearchSongsAsync("Don't", null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SearchTitleRemoteAsync_TypoQuery_RanksDontCryFirstDespiteItunesOrder()
    {
        // [AC] REQ-ITS-16: "Don't crie" ranks the "Don't Cry" row above the earlier "Dance Floor" row
        _itunesMock.Setup(p => p.SearchSongsAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([Row("1", "Band A", "Dance Floor"), Row("2", "Guns N' Roses", "Don't Cry"), Row("3", "Band C", "Dance Hall")]);

        var result = await CreateSut().SearchTitleRemoteAsync("Don't crie", []);

        Assert.Equal("Don't Cry", result.Remote[0].Title);
    }

    [Fact]
    public async Task SearchTitleRemoteAsync_ITunesRow_CarriesNoExternalIdentity()
    {
        // [AC] REQ-ITS-10: no iTunes id or provider is stored; the row is remote with no external identity
        _itunesMock.Setup(p => p.SearchSongsAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([Row("1440803", "Guns N' Roses", "Don't Cry"), Row("2", "x", "y")]);

        var result = await CreateSut().SearchTitleRemoteAsync("dont cry", []);

        var row = result.Remote.First(r => r.Title == "Don't Cry");
        Assert.Null(row.ExternalId);
        Assert.Null(row.ExternalProvider);
    }

    [Fact]
    public async Task ResolveMusicBrainzIdsAsync_ExactMatch_ReturnsArtistAndReleaseMbids()
    {
        // [AC] REQ-ITS-11: after a pick, the MusicBrainz row with the same title and artist supplies the artist and release ids
        _musicBrainzMock.Setup(p => p.SearchSongsAsync("Don't Cry", "Guns N' Roses", It.IsAny<CancellationToken>()))
            .ReturnsAsync([new MusicSearchResultDto("rec-1", ProviderNames.MusicBrainz, "Guns N' Roses", "Don't Cry",
                ExternalArtistId: "artist-mbid", ReleaseMbid: "release-mbid")]);

        var ids = await CreateSut().ResolveMusicBrainzIdsAsync("Don't Cry", "Guns N' Roses");

        Assert.Equal(("artist-mbid", "release-mbid"), ids);
    }

    [Fact]
    public async Task ResolveMusicBrainzIdsAsync_ProviderFails_ReturnsNoIdsWithoutThrowing()
    {
        // [AC] REQ-ITS-11: the lookup is failure-silent; the pick still completes without ids
        _musicBrainzMock.Setup(p => p.SearchSongsAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new MusicProviderException("503"));

        var ids = await CreateSut().ResolveMusicBrainzIdsAsync("Don't Cry", "Guns N' Roses");

        Assert.Equal((null, null), ids);
    }
}
