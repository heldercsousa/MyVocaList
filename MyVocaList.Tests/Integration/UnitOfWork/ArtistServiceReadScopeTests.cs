using Microsoft.Extensions.DependencyInjection;
using MyVocaList.Domain.Constants;
using MyVocaList.Domain.ServicesInterfaces;
using MyVocaList.Domain.UnitOfWork;
using MyVocaList.Infra;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Integration.UnitOfWork;

/// <summary>
/// Task 4.6 — <see cref="MyVocaList.Services.ArtistService"/>'s
/// <see cref="MyVocaList.Services.ArtistService.GetPagedArtistsForListAsync"/> and
/// <see cref="MyVocaList.Services.ArtistService.SearchArtistsByNameAsync"/> scoped through
/// <see cref="IUnitOfWork.ExecuteReadAsync{TResult}"/> (REQ-UOW-40, REQ-UOW-41, REQ-UOW-51).
/// </summary>
public class ArtistServiceReadScopeTests
{
    // [AC] REQ-UOW-40: GetPagedArtistsForListAsync returns the same page/total shape as before
    // the wrap after being scoped through IUnitOfWork.
    [Fact]
    public async Task GetPagedArtistsForListAsync_ReturnsExpectedPageAndTotal()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var artists = host.Resolve<IArtistService>();

        await artists.CreateArtistAsync("Ana Lima");
        await artists.CreateArtistAsync("Beto Alves");
        await artists.CreateArtistAsync("Caio Reis");

        var (items, totalCount) = await artists.GetPagedArtistsForListAsync(1, 2);

        Assert.Equal(3, totalCount);
        Assert.Equal(2, items.Count());
    }

    // [AC] REQ-UOW-40: SearchArtistsByNameAsync still returns matching artists after being scoped
    // through IUnitOfWork.
    [Fact]
    public async Task SearchArtistsByNameAsync_ReturnsMatchingArtists()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var artists = host.Resolve<IArtistService>();

        await artists.CreateArtistAsync("Amanda Silva");
        await artists.CreateArtistAsync("Amanda Rocha");
        await artists.CreateArtistAsync("Bruno Costa");

        var results = (await artists.SearchArtistsByNameAsync("Amanda")).ToList();

        Assert.Equal(2, results.Count);
        Assert.All(results, a => Assert.StartsWith("Amanda", a.Name));
    }

    // [AC] REQ-UOW-51: SearchArtistsByNameAsync short-circuits for a query shorter than
    // SearchConstants.MinimumLocalQueryLength WITHOUT invoking IUnitOfWork.ExecuteReadAsync —
    // the guard must never create a DI scope.
    [Fact]
    public async Task SearchArtistsByNameAsync_TooShortQuery_ShortCircuitsWithoutExecutingRead()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var counting = new CountingUnitOfWork(host.Resolve<IUnitOfWork>());
        var artistRepository = host.Resolve<MyVocaList.Domain.RepositoryInterface.IArtistRepository>();
        var songRepository = host.Resolve<MyVocaList.Domain.RepositoryInterface.ISongRepository>();
        var catalogRepository = host.Resolve<MyVocaList.Domain.RepositoryInterface.ICatalogRepository>();
        var logger = host.Resolve<Microsoft.Extensions.Logging.ILogger<MyVocaList.Services.ArtistService>>();
        var service = new MyVocaList.Services.ArtistService(
            artistRepository, songRepository, catalogRepository, counting, logger);

        Assert.True("a".Length < SearchConstants.MinimumLocalQueryLength);
        var results = await service.SearchArtistsByNameAsync("a");

        Assert.Empty(results);
        Assert.Equal(0, counting.ReadCallCount);
    }

    // [AC] REQ-UOW-51: a query at exactly SearchConstants.MinimumLocalQueryLength DOES reach the
    // database (the guard is exclusive, not inclusive, of the threshold).
    [Fact]
    public async Task SearchArtistsByNameAsync_QueryAtMinimumLength_ReachesDatabase()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var counting = new CountingUnitOfWork(host.Resolve<IUnitOfWork>());
        var artistRepository = host.Resolve<MyVocaList.Domain.RepositoryInterface.IArtistRepository>();
        var songRepository = host.Resolve<MyVocaList.Domain.RepositoryInterface.ISongRepository>();
        var catalogRepository = host.Resolve<MyVocaList.Domain.RepositoryInterface.ICatalogRepository>();
        var logger = host.Resolve<Microsoft.Extensions.Logging.ILogger<MyVocaList.Services.ArtistService>>();
        var service = new MyVocaList.Services.ArtistService(
            artistRepository, songRepository, catalogRepository, counting, logger);

        var query = new string('a', SearchConstants.MinimumLocalQueryLength);
        Assert.Equal(SearchConstants.MinimumLocalQueryLength, query.Length);

        var results = await service.SearchArtistsByNameAsync(query);

        Assert.NotNull(results);
        Assert.Equal(1, counting.ReadCallCount);
    }

    // [AC] REQ-UOW-39: two successive ArtistService reads scoped through IUnitOfWork resolve two
    // distinct AppDbContext instances — proving the read is genuinely scoped, not cosmetically
    // wrapped (mirrors BUG-078's root cause of a single captive context serving every read).
    [Fact]
    public async Task SuccessiveReads_ResolveDistinctAppDbContextInstances()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var uow = host.Resolve<IUnitOfWork>();

        var firstContext = await uow.ExecuteReadAsync(sp => Task.FromResult(sp.GetRequiredService<AppDbContext>()));
        var secondContext = await uow.ExecuteReadAsync(sp => Task.FromResult(sp.GetRequiredService<AppDbContext>()));

        Assert.NotSame(firstContext, secondContext);
    }

    /// <summary>Wraps a real <see cref="IUnitOfWork"/> to count <see cref="ExecuteReadAsync{TResult}"/>
    /// invocations, so a guard's "zero scope creation" claim can be asserted mechanically instead of
    /// by inspection.</summary>
    private sealed class CountingUnitOfWork(IUnitOfWork inner) : IUnitOfWork
    {
        public int ReadCallCount { get; private set; }

        public Task<TResult> ExecuteAsync<TResult>(Func<IServiceProvider, Task<TResult>> body, CancellationToken ct = default)
            => inner.ExecuteAsync(body, ct);

        public Task ExecuteAsync(Func<IServiceProvider, Task> body, CancellationToken ct = default)
            => inner.ExecuteAsync(body, ct);

        public Task<TResult> ExecuteReadAsync<TResult>(Func<IServiceProvider, Task<TResult>> body, CancellationToken ct = default)
        {
            ReadCallCount++;
            return inner.ExecuteReadAsync(body, ct);
        }

        public Task FlushAsync(CancellationToken ct = default) => inner.FlushAsync(ct);
    }
}
