using MyVocaList.Contracts.DTOs;
using MyVocaList.Contracts.DTOs.Suggestions;
using MyVocaList.Domain.Constants;
using MyVocaList.Domain.ReadModels;
using MyVocaList.Domain.Resolution;
using MyVocaList.Infra.Similarity;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Unit.Services;

/// <summary>
/// REQ-MSRC-01/02/10: the suggestion services consult the MusicBrainz provider (<see cref="ProviderNames.MusicBrainz"/>)
/// and nothing else; a provider failure degrades to a status, never an exception.
/// </summary>
public class MusicBrainzSuggestionServiceTests
{
    private readonly Mock<ISongRepository> _songRepoMock = new();
    private readonly Mock<IArtistRepository> _artistRepoMock = new();
    private readonly Mock<IMusicMetadataProvider> _musicBrainzMock = new();
    private readonly Mock<INetworkStatus> _networkMock = new();
    private readonly Mock<ILogger<SongSuggestionService>> _songLoggerMock = new();
    private readonly Mock<ILogger<ArtistSuggestionService>> _artistLoggerMock = new();

    public MusicBrainzSuggestionServiceTests()
    {
        _musicBrainzMock.SetupGet(p => p.ProviderName).Returns(ProviderNames.MusicBrainz);
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

    private SongSuggestionService CreateSongSut(params IMusicMetadataProvider[] providers) => new(
        _songRepoMock.Object, _artistRepoMock.Object,
        PassthroughUnitOfWork.Over(_songRepoMock, _artistRepoMock),
        providers.Length > 0 ? providers : [_musicBrainzMock.Object],
        new SimilarityScorer(), _networkMock.Object, _songLoggerMock.Object);

    private ArtistSuggestionService CreateArtistSut() => new(
        _artistRepoMock.Object,
        PassthroughUnitOfWork.Over(_artistRepoMock),
        [_musicBrainzMock.Object], new SimilarityScorer(), _networkMock.Object, _artistLoggerMock.Object);

    [Fact]
    public async Task SearchTitleRemoteAsync_ITunesAnswers_ReturnsRowsWithoutExternalIdentity()
    {
        // [AC] REQ-ITS-10: the remote title search is served by the iTunes provider; no iTunes id or provider is stored on the row
        var itunesMock = new Mock<IMusicMetadataProvider>();
        itunesMock.SetupGet(p => p.ProviderName).Returns(ProviderNames.ITunes);
        itunesMock
            .Setup(p => p.SearchSongsAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new MusicSearchResultDto("1440803", ProviderNames.ITunes, "Radiohead", "Creep")]);

        var result = await CreateSongSut(itunesMock.Object).SearchTitleRemoteAsync("creep", []);

        Assert.Equal(RemoteSearchStatus.Ok, result.Status);
        var row = Assert.Single(result.Remote);
        Assert.True(row.IsRemote);
        Assert.Null(row.ExternalId);
        Assert.Null(row.ExternalProvider);
    }

    [Fact]
    public async Task SearchTitleRemoteAsync_ProviderThrows_ReturnsUnavailableStatusNoException()
    {
        // [AC] REQ-MSRC-10: a MusicBrainz failure (HTTP 503 / rate limit) ends as a non-blocking status, never an exception
        _musicBrainzMock
            .Setup(p => p.SearchSongsAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new MusicProviderException("503"));

        var result = await CreateSongSut().SearchTitleRemoteAsync("creep", []);

        Assert.Equal(RemoteSearchStatus.Unavailable, result.Status);
        Assert.Empty(result.Remote);
        Assert.Empty(result.Promoted);
    }

    [Fact]
    public async Task SearchTitleRemoteAsync_OnlyDeezerRegistered_DoesNotCallItAndReturnsUnavailable()
    {
        // [AC] REQ-MSRC-01: Deezer is never consulted as a fallback
        var other = new Mock<IMusicMetadataProvider>();
        other.SetupGet(p => p.ProviderName).Returns("Deezer");

        var result = await CreateSongSut(other.Object).SearchTitleRemoteAsync("creep", []);

        Assert.Equal(RemoteSearchStatus.Unavailable, result.Status);
        other.Verify(p => p.SearchSongsAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SearchNameRemoteAsync_MusicBrainzAnswers_ReturnsArtistsWithMbid()
    {
        // [AC] REQ-MSRC-02: the remote artist search is served by the MusicBrainz provider
        _musicBrainzMock
            .Setup(p => p.SearchArtistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new MusicSearchResultDto("mbid-9", ProviderNames.MusicBrainz, "Radiohead", null)]);

        var result = await CreateArtistSut().SearchNameRemoteAsync("radio");

        Assert.Equal(RemoteSearchStatus.Ok, result.Status);
        var row = Assert.Single(result.Remote);
        Assert.Equal("mbid-9", row.ExternalId);
        Assert.Equal(ProviderNames.MusicBrainz, row.ExternalProvider);
    }

    [Fact]
    public async Task SearchNameRemoteAsync_ProviderThrows_ReturnsUnavailableStatusNoException()
    {
        // [AC] REQ-MSRC-10: a MusicBrainz failure ends as a status, never an exception
        _musicBrainzMock
            .Setup(p => p.SearchArtistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new MusicProviderException("503"));

        var result = await CreateArtistSut().SearchNameRemoteAsync("radio");

        Assert.Equal(RemoteSearchStatus.Unavailable, result.Status);
        Assert.Empty(result.Remote);
    }

    [Fact]
    public void AutocompleteDebounce_KeepsExistingValue()
    {
        // [AC] REQ-MSRC-10: remote search keeps the existing suggestion debounce (one request per pause)
        Assert.Equal(300, SearchConstants.AutocompleteDebounceMilliseconds);
    }
}
