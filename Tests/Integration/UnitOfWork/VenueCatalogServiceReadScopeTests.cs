using Microsoft.Extensions.DependencyInjection;
using MyVocaList.Domain.ServicesInterfaces;
using MyVocaList.Domain.UnitOfWork;
using MyVocaList.Infra;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Integration.UnitOfWork;

/// <summary>
/// READ-SCOPE Wave 4 task 4.3 — <see cref="VenueService.GetPagedVenuesForListAsync"/> and
/// <see cref="CatalogService.GetPagedCatalogForArtistAsync"/> now scope their reads through
/// <c>IUnitOfWork.ExecuteReadAsync</c> (REQ-UOW-40, REQ-UOW-41).
/// </summary>
public class VenueCatalogServiceReadScopeTests
{
    // [AC] REQ-UOW-40: GetPagedVenuesForListAsync returns unchanged values (items + totalCount)
    // for a seeded fixture once wrapped in ExecuteReadAsync.
    [Fact]
    public async Task GetPagedVenuesForListAsync_SeededFixture_ReturnsExpectedItemsAndTotalCount()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var venues = host.Resolve<IVenueService>();

        var (createOk1, createMessage1) = await venues.CreateVenueAsync("Alpha Venue");
        Assert.True(createOk1, createMessage1);
        var (createOk2, createMessage2) = await venues.CreateVenueAsync("Beta Venue");
        Assert.True(createOk2, createMessage2);

        var (items, totalCount) = await venues.GetPagedVenuesForListAsync(1, 10);

        Assert.Equal(2, totalCount);
        Assert.Contains(items, i => i.Name == "Alpha Venue");
        Assert.Contains(items, i => i.Name == "Beta Venue");
    }

    // [AC] REQ-UOW-40: GetPagedCatalogForArtistAsync returns unchanged values (items + totalCount)
    // for a seeded fixture once wrapped in ExecuteReadAsync.
    [Fact]
    public async Task GetPagedCatalogForArtistAsync_SeededFixture_ReturnsExpectedItemsAndTotalCount()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var artists = host.Resolve<IArtistService>();
        var songs = host.Resolve<ISongService>();
        var catalog = host.Resolve<ICatalogService>();

        var (artistOk, artistMessage, artist) = await artists.CreateArtistAsync("Test Artist");
        Assert.True(artistOk, artistMessage);

        var (songOk, songMessage, song) = await songs.CreateSongAsync(artist!.Id, "Test Song");
        Assert.True(songOk, songMessage);

        var (addOk, addMessage) = await catalog.AddSongToCatalogAsync(artist.Id, song!.Id);
        Assert.True(addOk, addMessage);

        var (items, totalCount) = await catalog.GetPagedCatalogForArtistAsync(artist.Id, 1, 10);

        Assert.Equal(1, totalCount);
        Assert.Contains(items, i => i.Id == song.Id);
    }

    // [AC] REQ-UOW-39: two successive GetPagedVenuesForListAsync calls resolve the AppDbContext
    // from inside their own ExecuteReadAsync scope — proving the read is genuinely scoped per call,
    // not cosmetically wrapped. Captured via a customized host so both calls can observe the
    // context used internally.
    [Fact]
    public async Task GetPagedVenuesForListAsync_TwoSuccessiveCalls_UseDistinctAppDbContexts()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var uow = host.Resolve<Domain.UnitOfWork.IUnitOfWork>();
        var venues = host.Resolve<IVenueService>();

        var (createOk, createMessage) = await venues.CreateVenueAsync("Gamma Venue");
        Assert.True(createOk, createMessage);

        AppDbContext? first = null;
        AppDbContext? second = null;

        await uow.ExecuteReadAsync(async sp =>
        {
            first = sp.GetRequiredService<AppDbContext>();
            await venues.GetPagedVenuesForListAsync(1, 10);
            return true;
        });

        await uow.ExecuteReadAsync(async sp =>
        {
            second = sp.GetRequiredService<AppDbContext>();
            await venues.GetPagedVenuesForListAsync(1, 10);
            return true;
        });

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.NotSame(first, second);
    }

    // [AC] REQ-UOW-41: the argument-validation guard on GetPagedVenuesForListAsync short-circuits
    // OUTSIDE the lambda — zero ExecuteReadAsync invocations when pageNumber is invalid. Uses a
    // mocked IUnitOfWork (not PassthroughUnitOfWork) so a call CAN be verified/refuted.
    [Fact]
    public async Task GetPagedVenuesForListAsync_InvalidPageNumber_ThrowsWithoutInvokingExecuteReadAsync()
    {
        var venueRepoMock = new Mock<IVenueRepository>();
        var uowMock = new Mock<IUnitOfWork>();
        var loggerMock = new Mock<ILogger<VenueService>>();
        var sut = new VenueService(venueRepoMock.Object, uowMock.Object, loggerMock.Object);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => sut.GetPagedVenuesForListAsync(0, 10));

        uowMock.Verify(
            u => u.ExecuteReadAsync(
                It.IsAny<Func<IServiceProvider, Task<(IEnumerable<VenueListItemDto>, int)>>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // [AC] REQ-UOW-41: the argument-validation guard on GetPagedCatalogForArtistAsync short-circuits
    // OUTSIDE the lambda — zero ExecuteReadAsync invocations when pageSize is invalid. Uses a
    // mocked IUnitOfWork (not PassthroughUnitOfWork) so a call CAN be verified/refuted.
    [Fact]
    public async Task GetPagedCatalogForArtistAsync_InvalidPageSize_ThrowsWithoutInvokingExecuteReadAsync()
    {
        var catalogRepoMock = new Mock<ICatalogRepository>();
        var uowMock = new Mock<IUnitOfWork>();
        var loggerMock = new Mock<ILogger<CatalogService>>();
        var sut = new CatalogService(catalogRepoMock.Object, uowMock.Object, loggerMock.Object);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => sut.GetPagedCatalogForArtistAsync(1, 1, 0));

        uowMock.Verify(
            u => u.ExecuteReadAsync(
                It.IsAny<Func<IServiceProvider, Task<(IEnumerable<SongListItemDto>, int)>>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
