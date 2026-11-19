using Microsoft.Extensions.DependencyInjection;
using MyVocaList.Domain.ServicesInterfaces;
using MyVocaList.Domain.UnitOfWork;
using MyVocaList.Infra;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Integration.UnitOfWork;

/// <summary>
/// Task 4.1 — <see cref="MyVocaList.Services.PersonService"/> read methods scoped through
/// <see cref="IUnitOfWork.ExecuteReadAsync{TResult}"/> (REQ-UOW-39, REQ-UOW-40, REQ-UOW-41).
/// </summary>
public class PersonServiceReadScopeTests
{
    // [AC] REQ-UOW-40: GetPersonByIdAsync returns the same value as before the wrap (unchanged
    // observable behaviour) after being scoped through IUnitOfWork.
    [Fact]
    public async Task GetPersonByIdAsync_ReturnsExpectedPerson()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var persons = host.Resolve<IPersonService>();

        var (createOk, createMessage, created) = await persons.CreatePersonAsync("Jane Doe");
        Assert.True(createOk, createMessage);

        var found = await persons.GetPersonByIdAsync(created!.Id);

        Assert.NotNull(found);
        Assert.Equal("Jane Doe", found!.FullName);
    }

    // [AC] REQ-UOW-40: GetPersonByIdAsync returns null for an id that does not exist.
    [Fact]
    public async Task GetPersonByIdAsync_UnknownId_ReturnsNull()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var persons = host.Resolve<IPersonService>();

        var found = await persons.GetPersonByIdAsync(999_999);

        Assert.Null(found);
    }

    // [AC] REQ-UOW-40: GetPersonByNameAsync returns the matching person after being scoped
    // through IUnitOfWork.
    [Fact]
    public async Task GetPersonByNameAsync_ReturnsExpectedPerson()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var persons = host.Resolve<IPersonService>();

        var (createOk, createMessage, created) = await persons.CreatePersonAsync("John Smith");
        Assert.True(createOk, createMessage);

        var found = await persons.GetPersonByNameAsync("John Smith");

        Assert.NotNull(found);
        Assert.Equal(created!.Id, found!.Id);
    }

    // [AC] REQ-UOW-40: SearchPersonsAsync still returns matching persons after being scoped
    // through IUnitOfWork.
    [Fact]
    public async Task SearchPersonsAsync_ReturnsMatchingPersons()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var persons = host.Resolve<IPersonService>();

        await persons.CreatePersonAsync("Amanda Silva");
        await persons.CreatePersonAsync("Amanda Rocha");
        await persons.CreatePersonAsync("Bruno Costa");

        var results = (await persons.SearchPersonsAsync("Amanda")).ToList();

        Assert.Equal(2, results.Count);
        Assert.All(results, p => Assert.StartsWith("Amanda", p.FullName));
    }

    // [AC] REQ-UOW-41: SearchPersonsAsync short-circuits for a too-short search term WITHOUT
    // invoking IUnitOfWork.ExecuteReadAsync — the guard must never create a DI scope.
    [Fact]
    public async Task SearchPersonsAsync_TooShortTerm_ShortCircuitsWithoutExecutingRead()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var counting = new CountingUnitOfWork(host.Resolve<IUnitOfWork>());
        var personRepository = host.Resolve<MyVocaList.Domain.RepositoryInterface.IPersonRepository>();
        var logger = host.Resolve<Microsoft.Extensions.Logging.ILogger<MyVocaList.Services.PersonService>>();
        var service = new MyVocaList.Services.PersonService(personRepository, counting, logger);

        var results = await service.SearchPersonsAsync("a");

        Assert.Empty(results);
        Assert.Equal(0, counting.ReadCallCount);
    }

    // [AC] REQ-UOW-40: SearchPersonsStartsWithAsync still returns matching persons after being
    // scoped through IUnitOfWork.
    [Fact]
    public async Task SearchPersonsStartsWithAsync_ReturnsMatchingPersons()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var persons = host.Resolve<IPersonService>();

        await persons.CreatePersonAsync("Carla Nunes");
        await persons.CreatePersonAsync("Bruno Costa");

        var results = (await persons.SearchPersonsStartsWithAsync("Carla")).ToList();

        Assert.Single(results);
        Assert.Equal("Carla Nunes", results[0].FullName);
    }

    // [AC] REQ-UOW-41: SearchPersonsStartsWithAsync short-circuits for a too-short search term
    // WITHOUT invoking IUnitOfWork.ExecuteReadAsync — the guard must never create a DI scope.
    [Fact]
    public async Task SearchPersonsStartsWithAsync_TooShortTerm_ShortCircuitsWithoutExecutingRead()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var counting = new CountingUnitOfWork(host.Resolve<IUnitOfWork>());
        var personRepository = host.Resolve<MyVocaList.Domain.RepositoryInterface.IPersonRepository>();
        var logger = host.Resolve<Microsoft.Extensions.Logging.ILogger<MyVocaList.Services.PersonService>>();
        var service = new MyVocaList.Services.PersonService(personRepository, counting, logger);

        var results = await service.SearchPersonsStartsWithAsync("b");

        Assert.Empty(results);
        Assert.Equal(0, counting.ReadCallCount);
    }

    // [AC] REQ-UOW-40: GetPagedPersonsForListAsync returns the same page/total shape as before the
    // wrap after being scoped through IUnitOfWork.
    [Fact]
    public async Task GetPagedPersonsForListAsync_ReturnsExpectedPageAndTotal()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var persons = host.Resolve<IPersonService>();

        await persons.CreatePersonAsync("Ana Lima");
        await persons.CreatePersonAsync("Beto Alves");
        await persons.CreatePersonAsync("Caio Reis");

        var (items, totalCount) = await persons.GetPagedPersonsForListAsync(1, 2);

        Assert.Equal(3, totalCount);
        Assert.Equal(2, items.Count());
    }

    // [AC] REQ-UOW-39: two successive PersonService reads scoped through IUnitOfWork resolve two
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
