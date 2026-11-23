using MyVocaList.Domain.UnitOfWork;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Unit.Services;

public class CatalogServiceTests
{
    private readonly Mock<ICatalogRepository> _catalogRepoMock = new();
    private readonly Mock<IArtistRepository> _artistRepoMock = new();
    private readonly Mock<ILogger<CatalogService>> _loggerMock = new();

    // The service now takes IUnitOfWork; PassthroughUnitOfWork runs each wrapped body inline
    // against this same mock, so every existing assertion keeps its original meaning
    // (`plan.md` Task 4.1).
    private CatalogService CreateSut() => new(
        _catalogRepoMock.Object,
        PassthroughUnitOfWork.Over(_catalogRepoMock, _artistRepoMock),
        _loggerMock.Object);

    // ── AddSongToCatalogAsync ─────────────────────────────────────────────

    [Fact]
    public async Task AddSongToCatalogAsync_NewEntry_ReturnsSuccess()
    {
        _catalogRepoMock.Setup(r => r.ExistsAsync(1, 10, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(false);
        _catalogRepoMock.Setup(r => r.AddAsync(It.IsAny<Catalog>(), It.IsAny<CancellationToken>()))
                        .Returns(Task.CompletedTask);
        var sut = CreateSut();

        var (success, message) = await sut.AddSongToCatalogAsync(1, 10);

        Assert.True(success);
        _catalogRepoMock.Verify(r => r.AddAsync(It.IsAny<Catalog>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddSongToCatalogAsync_Duplicate_ReturnsFalse()
    {
        _catalogRepoMock.Setup(r => r.ExistsAsync(1, 10, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(true);
        var sut = CreateSut();

        var (success, message) = await sut.AddSongToCatalogAsync(1, 10);

        Assert.False(success);
        _catalogRepoMock.Verify(r => r.AddAsync(It.IsAny<Catalog>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── RemoveSongFromCatalogAsync ────────────────────────────────────────

    private void GivenArtist(string name = "Queen")
        => _artistRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                          .ReturnsAsync(new Artist { Id = 1, Name = name });

    [Fact]
    public async Task RemoveSongFromCatalogAsync_DerivedRow_RefusesAndKeepsRow()
    {
        // [AC] REQ-CATPROP-06: a derived row is refused with the author message and the row remains
        GivenArtist();
        _catalogRepoMock.Setup(r => r.ExistsAsync(1, 10, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _catalogRepoMock.Setup(r => r.IsDerivedAsync(1, 10, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var (success, message) = await CreateSut().RemoveSongFromCatalogAsync(1, 10);

        Assert.False(success);
        Assert.Equal("Queen wrote this song, so it stays in their catalog.", message);
        _catalogRepoMock.Verify(r => r.RemoveAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RemoveSongFromCatalogAsync_CoverRow_RemovesAndSucceeds()
    {
        // [AC] REQ-CATPROP-06: a cover row is removed and the call succeeds
        GivenArtist();
        _catalogRepoMock.Setup(r => r.ExistsAsync(1, 10, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _catalogRepoMock.Setup(r => r.IsDerivedAsync(1, 10, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var (success, message) = await CreateSut().RemoveSongFromCatalogAsync(1, 10);

        Assert.True(success);
        Assert.Equal("Song removed from catalog.", message);
        _catalogRepoMock.Verify(r => r.RemoveAsync(1, 10, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveSongFromCatalogAsync_MissingRow_ReturnsNotInCatalog()
    {
        // [AC] REQ-CATPROP-06: a row that does not exist returns "Song is not in this catalog."
        GivenArtist();
        _catalogRepoMock.Setup(r => r.ExistsAsync(1, 10, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var (success, message) = await CreateSut().RemoveSongFromCatalogAsync(1, 10);

        Assert.False(success);
        Assert.Equal("Song is not in this catalog.", message);
        _catalogRepoMock.Verify(r => r.RemoveAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── RemoveSongsFromCatalogAsync ───────────────────────────────────────

    [Fact]
    public async Task RemoveSongsFromCatalogAsync_EmptyList_ReturnsNoSongsSelectedWithoutUnitOfWork()
    {
        // [AC] REQ-CATPROP-07: an empty id list returns "No songs selected." (checked outside the unit of work)
        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var sut = new CatalogService(_catalogRepoMock.Object, uow.Object, _loggerMock.Object);

        var result = await sut.RemoveSongsFromCatalogAsync(1, Array.Empty<int>());

        Assert.Equal((false, "No songs selected.", 0, 0), result);
    }

    [Fact]
    public async Task RemoveSongsFromCatalogAsync_UnknownArtist_ReturnsArtistNotFound()
    {
        // [AC] REQ-CATPROP-07: an unknown artist returns "Artist not found."
        _artistRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((Artist)null);

        var result = await CreateSut().RemoveSongsFromCatalogAsync(1, [10]);

        Assert.Equal((false, "Artist not found.", 0, 0), result);
        _catalogRepoMock.Verify(r => r.RemoveCoversAsync(It.IsAny<int>(), It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(0, 0, false, "Nothing to remove from the catalog.")]
    [InlineData(0, 1, false, "Queen wrote this song, so it stays in their catalog.")]
    [InlineData(0, 3, false, "Queen wrote these songs, so they stay in their catalog.")]
    [InlineData(1, 0, true, "Removed 1 song from the catalog.")]
    [InlineData(2, 0, true, "Removed 2 songs from the catalog.")]
    [InlineData(2, 1, true, "Removed 2 songs from the catalog. 1 kept: Queen wrote it.")]
    [InlineData(2, 3, true, "Removed 2 songs from the catalog. 3 kept: Queen wrote them.")]
    public async Task RemoveSongsFromCatalogAsync_Counts_ProduceExactMessage(int removed, int kept, bool expectedSuccess, string expectedMessage)
    {
        // [AC] REQ-CATPROP-07: success is removed > 0 and the message follows the specified table
        GivenArtist();
        _catalogRepoMock.Setup(r => r.RemoveCoversAsync(1, It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync((removed, kept));

        var result = await CreateSut().RemoveSongsFromCatalogAsync(1, [10, 11, 12]);

        Assert.Equal((expectedSuccess, expectedMessage, removed, kept), result);
    }

    // ── GetPagedCatalogForArtistAsync ─────────────────────────────────────

    [Fact]
    public async Task GetPagedCatalogForArtistAsync_ReturnsRepositoryResult()
    {
        var items = new List<SongListItemDto>
        {
            new(1, "Bohemian Rhapsody", 1, "Queen", null, null)
        };
        _catalogRepoMock.Setup(r => r.GetPagedByArtistAsync(1, 1, 20, null, It.IsAny<CancellationToken>()))
                        .ReturnsAsync((items.AsEnumerable(), 1));
        var sut = CreateSut();

        var (result, totalCount) = await sut.GetPagedCatalogForArtistAsync(1, 1, 20);

        Assert.Single(result);
        Assert.Equal(1, totalCount);
    }
}
