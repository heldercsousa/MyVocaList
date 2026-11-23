using MyVocaList.Contracts.DTOs;
using MyVocaList.Contracts.DTOs.Suggestions;
using MyVocaList.Domain.Constants;
using MyVocaList.Domain.ReadModels;
using MyVocaList.Domain.Resolution;
using MyVocaList.Infra.Similarity;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Unit.Services;

/// <summary>
/// One Deezer row per song group in <see cref="SongSuggestionService.SearchTitleRemoteAsync"/>
/// (REQ-SLD-06 to 11). Same fixture as <c>SongSuggestionServiceTests</c>: Moq repositories and provider,
/// <c>PassthroughUnitOfWork.Over</c>, no database.
/// </summary>
public class SongSuggestionServiceGroupingTests
{
    private readonly Mock<ISongRepository> _songRepoMock = new();
    private readonly Mock<IArtistRepository> _artistRepoMock = new();
    private readonly Mock<IMusicMetadataProvider> _musicBrainzMock = new();
    private readonly Mock<IMusicMetadataProvider> _deezerMock = new();
    private readonly Mock<INetworkStatus> _networkMock = new();
    private readonly Mock<ILogger<SongSuggestionService>> _loggerMock = new();
    private readonly ISimilarityScorer _scorer = new SimilarityScorer();

    public SongSuggestionServiceGroupingTests()
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
                It.IsAny<IReadOnlyCollection<(string Title, string ArtistName)>>(), It.IsAny<CancellationToken>()))
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

    // Expression trees cannot contain tuple literals, so Moq predicates build the pair through a method.
    private static (string Title, string ArtistName) Pair(string title, string artist)
        => (title, artist);

    private void SetupDeezer(params MusicSearchResultDto[] results)
        => _deezerMock
            .Setup(p => p.SearchSongsAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(results.ToList());

    // Deezer row: (externalId, artistName, title, deezer artist id).
    private static MusicSearchResultDto Row(string externalId, string artistName, string? title,
        string? artistId = "1")
        => new(ExternalId: externalId, Provider: "Deezer", ArtistName: artistName, SongTitle: title, ExternalArtistId: artistId);

    // [AC] REQ-SLD-06: several rows of one song group (same Deezer artist id and title) give one row.
    [Fact]
    public async Task SearchTitleRemoteAsync_AlbumRepeatsOfOneSong_OneRow()
    {
        SetupDeezer(
            Row("dz-1", "Radiohead", "Creep", artistId: "1"),
            Row("dz-2", "Radiohead", "Creep", artistId: "1"),
            Row("dz-3", "Radiohead", "Creep", artistId: "1"));
        var sut = CreateSut();

        var result = await sut.SearchTitleRemoteAsync("creep", []);

        var remote = Assert.Single(result.Remote);
        Assert.Equal("Creep", remote.Title);
    }

    // [AC] REQ-SLD-06: the same title by two Deezer artists stays as two rows.
    [Fact]
    public async Task SearchTitleRemoteAsync_SameTitleTwoArtists_TwoRows()
    {
        SetupDeezer(
            Row("dz-1", "Radiohead", "Creep", artistId: "1"),
            Row("dz-2", "Stone Temple Pilots", "Creep", artistId: "2"));
        var sut = CreateSut();

        var result = await sut.SearchTitleRemoteAsync("creep", []);

        Assert.Equal(2, result.Remote.Count);
    }

    // [AC] REQ-SLD-06: grouping is ordinal, so "Creep" and "creep" stay apart.
    [Fact]
    public async Task SearchTitleRemoteAsync_TitlesDifferingOnlyInCase_TwoRows()
    {
        SetupDeezer(
            Row("dz-1", "Radiohead", "Creep", artistId: "1"),
            Row("dz-2", "Radiohead", "creep", artistId: "1"));
        var sut = CreateSut();

        var result = await sut.SearchTitleRemoteAsync("creep", []);

        Assert.Equal(2, result.Remote.Count);
    }

    // [AC] REQ-SLD-06: groups are listed in the order of their first row in Deezer's answer.
    [Fact]
    public async Task SearchTitleRemoteAsync_GroupsKeepDeezerOrder()
    {
        SetupDeezer(
            Row("dz-1", "Radiohead", "Creep", artistId: "1"),
            Row("dz-2", "Radiohead", "Karma Police", artistId: "2"),
            Row("dz-3", "Radiohead", "Creep", artistId: "1"),
            Row("dz-4", "Radiohead", "Zebra", artistId: "3"));
        var sut = CreateSut();

        var result = await sut.SearchTitleRemoteAsync("radiohead", []);

        Assert.Equal(new List<string> { "Creep", "Karma Police", "Zebra" }, result.Remote.Select(r => r.Title).ToList());
    }

    // [AC] REQ-SLD-07 (reduced by REQ-RMV-08): the kept row of a group is the first row, in Deezer's order.
    [Fact]
    public async Task SearchTitleRemoteAsync_LabelledThenPlain_KeepsTheFirstRow()
    {
        SetupDeezer(
            Row("dz-live", "Radiohead", "Creep"),
            Row("dz-plain", "Radiohead", "Creep"));
        var sut = CreateSut();

        var result = await sut.SearchTitleRemoteAsync("creep", []);

        var kept = Assert.Single(result.Remote);
        Assert.Equal("dz-live", kept.ExternalId);
    }

    // [AC] REQ-SLD-07 (reduced by REQ-RMV-08): a group of several labelled-by-Deezer rows still keeps its first row.
    [Fact]
    public async Task SearchTitleRemoteAsync_SeveralRowsOfOneSong_KeepsTheFirst()
    {
        SetupDeezer(
            Row("dz-1", "Radiohead", "Creep"),
            Row("dz-2", "Radiohead", "Creep"));
        var sut = CreateSut();

        var result = await sut.SearchTitleRemoteAsync("creep", []);

        var kept = Assert.Single(result.Remote);
        Assert.Equal("dz-1", kept.ExternalId);
    }

    // [AC] REQ-RMV-08: the library check uses the (Artist, Title) pair of the kept row; no version is passed.
    [Fact]
    public async Task SearchTitleRemoteAsync_LibraryCheckUsesArtistAndTitle()
    {
        SetupDeezer(Row("dz-1", "Radiohead", "Creep"));
        var sut = CreateSut();

        await sut.SearchTitleRemoteAsync("creep", []);

        _songRepoMock.Verify(
            r => r.FindByTitleAndArtistNamesAsync(
                It.Is<IReadOnlyCollection<(string Title, string ArtistName)>>(
                    items => items.Contains(Pair("Creep", "Radiohead")) && items.Count == 1),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // [AC] REQ-SLD-09 (reduced by REQ-RMV-08): a library song matching (artist, title) hides the whole group.
    [Fact]
    public async Task SearchTitleRemoteAsync_LibrarySongMatchesArtistAndTitle_HidesTheGroup()
    {
        SetupDeezer(
            Row("dz-1", "Radiohead", "Creep"),
            Row("dz-2", "Radiohead", "Creep"));
        _songRepoMock
            .Setup(r => r.FindByTitleAndArtistNamesAsync(
                It.Is<IReadOnlyCollection<(string Title, string ArtistName)>>(
                    items => items.Contains(Pair("Creep", "Radiohead"))),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SongSearchHit> { new(9, "Creep", 3, "Radiohead", null, null) });
        var sut = CreateSut();

        var result = await sut.SearchTitleRemoteAsync("creep", []);

        var promoted = Assert.Single(result.Promoted);
        Assert.Equal(9, promoted.LocalId);
        Assert.Empty(result.Remote);
    }

    // [AC] REQ-SLD-10: all rows of the answer are grouped before the limit of five remote rows applies.
    [Fact]
    public async Task SearchTitleRemoteAsync_TwentyFiveRowsOfSixSongs_ShowsFive()
    {
        var rows = Enumerable.Range(0, 25)
            .Select(i => Row($"dz-{i}", "Radiohead", $"Song {i % 6}", artistId: "1"))
            .ToArray();
        SetupDeezer(rows);
        var sut = CreateSut();

        var result = await sut.SearchTitleRemoteAsync("song", []);

        Assert.Equal(SearchConstants.MaxSearchResults, result.Remote.Count);
        Assert.Equal(5, result.Remote.Select(r => r.Title).Distinct().Count());
    }

    // [AC] REQ-SLD-11: rows without a Deezer artist id are never merged, even with the same title.
    [Fact]
    public async Task SearchTitleRemoteAsync_RowsWithoutArtistId_NeverMerged()
    {
        SetupDeezer(
            Row("dz-1", "Radiohead", "Creep", artistId: null),
            Row("dz-2", "Radiohead", "Creep", artistId: null));
        var sut = CreateSut();

        var result = await sut.SearchTitleRemoteAsync("creep", []);

        Assert.Equal(2, result.Remote.Count);
    }
}
