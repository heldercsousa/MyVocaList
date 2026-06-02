namespace MyVocaList.Tests.Unit.Services;

public class CatalogServiceTests
{
    private readonly Mock<ICatalogRepository> _catalogRepoMock = new();
    private readonly Mock<ILogger<CatalogService>> _loggerMock = new();

    private CatalogService CreateSut() => new(_catalogRepoMock.Object, _loggerMock.Object);

    // ── AddSongToCatalogAsync ─────────────────────────────────────────────

    [Fact]
    public async Task AddSongToCatalogAsync_NewEntry_ReturnsSuccess()
    {
        _catalogRepoMock.Setup(r => r.ExistsAsync(1, 10, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(false);
        _catalogRepoMock.Setup(r => r.AddAsync(It.IsAny<Catalog>(), It.IsAny<CancellationToken>()))
                        .Returns(Task.CompletedTask);
        _catalogRepoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
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

    [Fact]
    public async Task RemoveSongFromCatalogAsync_AlwaysReturnsSuccess()
    {
        _catalogRepoMock.Setup(r => r.RemoveAsync(1, 10, It.IsAny<CancellationToken>()))
                        .Returns(Task.CompletedTask);
        var sut = CreateSut();

        var (success, message) = await sut.RemoveSongFromCatalogAsync(1, 10);

        Assert.True(success);
    }

    // ── GetPagedCatalogForArtistAsync ─────────────────────────────────────

    [Fact]
    public async Task GetPagedCatalogForArtistAsync_ReturnsRepositoryResult()
    {
        var items = new List<SongListItemDto>
        {
            new(1, "Bohemian Rhapsody", 1, "Queen", null, null, false)
        };
        _catalogRepoMock.Setup(r => r.GetPagedByArtistAsync(1, 1, 20, null, It.IsAny<CancellationToken>()))
                        .ReturnsAsync((items.AsEnumerable(), 1));
        var sut = CreateSut();

        var (result, totalCount) = await sut.GetPagedCatalogForArtistAsync(1, 1, 20);

        Assert.Single(result);
        Assert.Equal(1, totalCount);
    }
}
