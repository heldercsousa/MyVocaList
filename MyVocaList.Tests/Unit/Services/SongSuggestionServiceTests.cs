using MyVocaList.Contracts.DTOs;
using MyVocaList.Contracts.DTOs.Suggestions;
using MyVocaList.Domain.Resolution;

namespace MyVocaList.Tests.Unit.Services;

public class SongSuggestionServiceTests
{
    private readonly Mock<ISongRepository> _songRepoMock = new();
    private readonly Mock<IArtistRepository> _artistRepoMock = new();
    private readonly Mock<IMusicMetadataProvider> _musicBrainzMock = new();
    private readonly Mock<IMusicMetadataProvider> _deezerMock = new();
    private readonly Mock<ISimilarityScorer> _scorerMock = new();
    private readonly Mock<ILogger<SongSuggestionService>> _loggerMock = new();

    public SongSuggestionServiceTests()
    {
        _musicBrainzMock.SetupGet(p => p.ProviderName).Returns("MusicBrainz");
        _deezerMock.SetupGet(p => p.ProviderName).Returns("Deezer");
    }

    private SongSuggestionService CreateSut() => new(
        _songRepoMock.Object,
        _artistRepoMock.Object,
        [_musicBrainzMock.Object, _deezerMock.Object],
        _scorerMock.Object,
        _loggerMock.Object);

    // ── GetLocalAsync ────────────────────────────────────────────────────

    [Fact]
    // [AC] REQ-FORMUX-22: local title suggestions with artist supporting text
    public async Task GetLocalAsync_TermMatchesRegisteredSongs_ReturnsTitleAndArtistName()
    {
        _songRepoMock
            .Setup(r => r.GetPagedAsync(1, 5, "Bohemian", It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<SongListItemDto>
            {
                new(1, "Bohemian Rhapsody", 10, "Queen", null, null, false)
            }, 1));
        var sut = CreateSut();

        var result = await sut.GetLocalAsync("Bohemian");

        Assert.Single(result);
        Assert.Equal(1, result[0].LocalId);
        Assert.Equal("Bohemian Rhapsody", result[0].Title);
        Assert.Equal("Queen", result[0].ArtistName);
        Assert.Equal(10, result[0].LocalArtistId);
        Assert.False(result[0].IsRemote);
    }

    [Fact]
    // [AC] REQ-FORMUX-22
    public async Task GetLocalAsync_ManyMatches_ReturnsAtMostFive()
    {
        var items = Enumerable.Range(1, 5)
            .Select(i => new SongListItemDto(i, $"Song {i}", 10, "Queen", null, null, false))
            .ToList();
        _songRepoMock
            .Setup(r => r.GetPagedAsync(1, 5, "Song", It.IsAny<CancellationToken>()))
            .ReturnsAsync((items, 7));
        var sut = CreateSut();

        var result = await sut.GetLocalAsync("Song");

        Assert.Equal(5, result.Count);
    }

    // ── GetRemoteAsync ───────────────────────────────────────────────────

    [Fact]
    // [AC] REQ-FORMUX-22: artistHint pass-through
    public async Task GetRemoteAsync_ArtistHintProvided_PassedToProvider()
    {
        _musicBrainzMock
            .Setup(p => p.SearchSongsAsync("Yesterday", "Beatles", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MusicSearchResultDto>());
        _deezerMock
            .Setup(p => p.SearchSongsAsync("Yesterday", "Beatles", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MusicSearchResultDto>());
        var sut = CreateSut();

        await sut.GetRemoteAsync("Yesterday", "Beatles", []);

        _musicBrainzMock.Verify(
            p => p.SearchSongsAsync("Yesterday", "Beatles", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    // [AC] REQ-FORMUX-03
    public async Task GetRemoteAsync_ResultTitleCollationEqualToLocal_IsExcluded()
    {
        var raw = new List<MusicSearchResultDto>
        {
            new("mb-1", "MusicBrainz", "Queen", "Bohemian Rhapsody", null)
        };
        _musicBrainzMock
            .Setup(p => p.SearchSongsAsync("Bohemian", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(raw);
        _songRepoMock
            .Setup(r => r.GetByTitlesCollatedAsync(
                It.Is<IEnumerable<string>>(t => t.Contains("Bohemian Rhapsody")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Song> { new() { Id = 1, Title = "Bohemian Rhapsody" } });
        var sut = CreateSut();

        var result = await sut.GetRemoteAsync("Bohemian", null, []);

        Assert.Empty(result);
        _songRepoMock.Verify(
            r => r.GetByTitlesCollatedAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    // [AC] REQ-FORMUX-23: local artist resolved for remote rows
    public async Task GetRemoteAsync_RemoteArtistExistsLocally_LocalArtistIdResolved()
    {
        var raw = new List<MusicSearchResultDto>
        {
            new("mb-2", "MusicBrainz", "Queen", "Somebody To Love", null)
        };
        _musicBrainzMock
            .Setup(p => p.SearchSongsAsync("Somebody", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(raw);
        _songRepoMock
            .Setup(r => r.GetByTitlesCollatedAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Song>());
        _artistRepoMock
            .Setup(r => r.GetByNamesCollatedAsync(
                It.Is<IEnumerable<string>>(n => n.Contains("Queen")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Artist> { new() { Id = 42, Name = "Queen" } });
        var sut = CreateSut();

        var result = await sut.GetRemoteAsync("Somebody", null, []);

        Assert.Single(result);
        Assert.Equal(42, result[0].LocalArtistId);
    }

    [Fact]
    // [AC] REQ-FORMUX-05
    public async Task GetRemoteAsync_AllProvidersFail_ReturnsEmptyAndLogs()
    {
        _musicBrainzMock
            .Setup(p => p.SearchSongsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));
        _deezerMock
            .Setup(p => p.SearchSongsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));
        var sut = CreateSut();

        var result = await sut.GetRemoteAsync("Anything", null, []);

        Assert.Empty(result);
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    // [AC] REQ-FORMUX-02
    public async Task GetRemoteAsync_MusicBrainzEmpty_FallsBackToDeezer()
    {
        _musicBrainzMock
            .Setup(p => p.SearchSongsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MusicSearchResultDto>());
        _deezerMock
            .Setup(p => p.SearchSongsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MusicSearchResultDto>
            {
                new("dz-1", "Deezer", "Adele", "Hello", null)
            });
        _songRepoMock
            .Setup(r => r.GetByTitlesCollatedAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Song>());
        _artistRepoMock
            .Setup(r => r.GetByNamesCollatedAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Artist>());
        var sut = CreateSut();

        var result = await sut.GetRemoteAsync("Hello", null, []);

        Assert.Single(result);
        Assert.Equal("Hello", result[0].Title);
        _deezerMock.Verify(
            p => p.SearchSongsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
