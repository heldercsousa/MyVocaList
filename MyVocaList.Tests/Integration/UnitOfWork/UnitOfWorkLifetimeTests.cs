using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyVocaList.Domain.RepositoryInterface;
using MyVocaList.Domain.UnitOfWork;
using MyVocaList.Infra;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Integration.UnitOfWork;

/// <summary>
/// Task 1.2a — lifetime, concurrency and interceptor coverage for <see cref="Infra.UnitOfWork.UnitOfWork"/>.
/// All tests use the no-signal <c>ExecuteAsync(Func&lt;IServiceProvider, Task&gt;, ct)</c> overload,
/// which always saves and needs no <c>ResultSignalsSuccess</c> inspection (still a
/// <see cref="NotImplementedException"/> stub — Task 1.2b's scope).
/// </summary>
public class UnitOfWorkLifetimeTests
{
    // [AC] REQ-UOW-02: three sequential ExecuteAsync calls capture three distinct AppDbContext
    // references from inside their bodies, and each is disposed after its unit of work returns.
    [Fact]
    public async Task ExecuteAsync_ThreeSequentialCalls_CaptureThreeDistinctDisposedContexts()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var uow = host.Resolve<IUnitOfWork>();

        AppDbContext? first = null, second = null, third = null;

        await uow.ExecuteAsync(async sp =>
        {
            first = sp.GetRequiredService<AppDbContext>();
            await Task.CompletedTask;
        });
        await uow.ExecuteAsync(async sp =>
        {
            second = sp.GetRequiredService<AppDbContext>();
            await Task.CompletedTask;
        });
        await uow.ExecuteAsync(async sp =>
        {
            third = sp.GetRequiredService<AppDbContext>();
            await Task.CompletedTask;
        });

        Assert.NotSame(first, second);
        Assert.NotSame(second, third);
        Assert.NotSame(first, third);

        Assert.Throws<ObjectDisposedException>(() => first!.Artists.Local.Count);
        Assert.Throws<ObjectDisposedException>(() => second!.Artists.Local.Count);
        Assert.Throws<ObjectDisposedException>(() => third!.Artists.Local.Count);
    }

    // [AC] REQ-UOW-05: two overlapping units of work see distinct contexts and no
    // InvalidOperationException. Interleaved via TaskCompletionSource, never Task.WhenAll.
    [Fact]
    public async Task ExecuteAsync_TwoOverlappingUnitsOfWork_SeeDistinctContexts()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var uow = host.Resolve<IUnitOfWork>();

        // RunContinuationsAsynchronously is load-bearing here: a default (synchronous) TCS would
        // resume "await firstEntered.Task" on the SAME call stack as the SetResult() call inside
        // the first body — i.e. nested inside ExecuteAsync's synchronous prefix, before the first
        // ExecuteAsync call has even returned. That reentrant nesting would make the "second" call
        // see the first's ambient scope still published and silently JOIN it, defeating the test.
        var firstEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirst = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        AppDbContext? firstContext = null, secondContext = null;

        var firstTask = uow.ExecuteAsync(async sp =>
        {
            firstContext = sp.GetRequiredService<AppDbContext>();
            firstEntered.SetResult();
            await releaseFirst.Task;
        });

        await firstEntered.Task;

        // Real SQLite (rollback-journal mode, non-WAL) allows only one open WRITE transaction at
        // a time — a second concurrent ExecuteAsync (write) here would block on the first's still-
        // open transaction and eventually throw SqliteException "database is locked", which is
        // real single-writer behavior, not an IUnitOfWork defect. ExecuteReadAsync opens no
        // transaction (Revision 12), so it exercises genuine context overlap without that
        // contention, while still proving REQ-UOW-05: distinct AppDbContext, no exception.
        var secondTask = uow.ExecuteReadAsync(async sp =>
        {
            secondContext = sp.GetRequiredService<AppDbContext>();
            await Task.CompletedTask;
            return true;
        });
        await secondTask;

        releaseFirst.SetResult();
        await firstTask;

        Assert.NotNull(firstContext);
        Assert.NotNull(secondContext);
        Assert.NotSame(firstContext, secondContext);
    }

    // [AC] REQ-UOW-06: a body that throws leaves the NEXT unit of work able to write successfully.
    [Fact]
    public async Task ExecuteAsync_BodyThrows_NextUnitOfWorkStillWrites()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var uow = host.Resolve<IUnitOfWork>();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            uow.ExecuteAsync(async sp =>
            {
                var repo = sp.GetRequiredService<IArtistRepository>();
                await repo.AddAsync(NewArtist("Doomed Artist"), CancellationToken.None);
                throw new InvalidOperationException("injected failure");
            }));

        await uow.ExecuteAsync(async sp =>
        {
            var repo = sp.GetRequiredService<IArtistRepository>();
            await repo.AddAsync(NewArtist("Recovering Artist"), CancellationToken.None);
        });

        var found = await host.Db.Artists.FirstOrDefaultAsync(a => a.Name == "Recovering Artist");
        Assert.NotNull(found);
        var doomed = await host.Db.Artists.FirstOrDefaultAsync(a => a.Name == "Doomed Artist");
        Assert.Null(doomed);
    }

    // [AC] REQ-UOW-14: a NOCASE_NOACCENT-collated query still works inside a unit of work (the
    // CollationInterceptor is still attached), and host.Log.Entries gains an entry for a save.
    [Fact]
    public async Task ExecuteAsync_CollatedQuery_WorksInsideUnitOfWork_AndLogsEntry()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var uow = host.Resolve<IUnitOfWork>();

        await uow.ExecuteAsync(async sp =>
        {
            var repo = sp.GetRequiredService<IArtistRepository>();
            await repo.AddAsync(NewArtist("Café Tacvba"), CancellationToken.None);
        });

        var match = await host.Db.Artists.FirstOrDefaultAsync(a => a.Name == "cafe tacvba");
        Assert.NotNull(match);
        Assert.Contains(host.Log.Entries, e => e.Contains("Artist"));
    }

    // [AC] REQ-UOW-15: after two sequential writes, the second host.Log.Entries entry does NOT
    // contain the first write's entity — the interceptor sees only its own unit of work's entries.
    [Fact]
    public async Task ExecuteAsync_TwoSequentialWrites_SecondLogEntryDoesNotContainFirstEntity()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var uow = host.Resolve<IUnitOfWork>();

        await uow.ExecuteAsync(async sp =>
        {
            var repo = sp.GetRequiredService<IArtistRepository>();
            await repo.AddAsync(NewArtist("First Sequential Artist"), CancellationToken.None);
        });

        var entriesAfterFirst = host.Log.Entries.Count;

        await uow.ExecuteAsync(async sp =>
        {
            var repo = sp.GetRequiredService<IArtistRepository>();
            await repo.AddAsync(NewArtist("Second Sequential Artist"), CancellationToken.None);
        });

        var newEntries = host.Log.Entries.Skip(entriesAfterFirst).ToList();
        Assert.NotEmpty(newEntries);
        Assert.DoesNotContain(newEntries, e => e.Contains("First Sequential Artist"));
        Assert.Contains(newEntries, e => e.Contains("Second Sequential Artist"));
    }

    // [AC] REQ-UOW-34: a write nested inside ExecuteReadAsync does NOT join the (non-existent)
    // ambient scope — it opens its OWN scope, and the mutation IS persisted. This is the
    // regression test for 4th-pass finding BL-E: under the withdrawn Revision 10 design the write
    // would have silently joined a scope that never calls SaveChangesAsync (same shape as
    // BUG-071). Asserting NotSame alone would not catch that — only reading the row back through a
    // fresh scope after the outer read returns proves the write was not lost.
    [Fact]
    public async Task ExecuteReadAsync_NestedWrite_OpensOwnScope_AndPersists()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var uow = host.Resolve<IUnitOfWork>();

        AppDbContext? readContext = null;
        AppDbContext? writeContext = null;

        await uow.ExecuteReadAsync(async sp =>
        {
            readContext = sp.GetRequiredService<AppDbContext>();

            await uow.ExecuteAsync(async innerSp =>
            {
                writeContext = innerSp.GetRequiredService<AppDbContext>();
                var repo = innerSp.GetRequiredService<IArtistRepository>();
                await repo.AddAsync(NewArtist("Read Then Write Artist"), CancellationToken.None);
            });

            return true;
        });

        Assert.NotSame(readContext, writeContext);

        // Read back through a FRESH scope — proves the write was actually saved, not just that a
        // distinct context was created.
        var found = await uow.ExecuteReadAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            return await db.Artists.FirstOrDefaultAsync(a => a.Name == "Read Then Write Artist");
        });
        Assert.NotNull(found);
    }

    // [AC] REQ-UOW-34: a standalone ExecuteReadAsync never publishes the ambient scope — a sibling
    // operation started after it sees no ambient scope (each opens its own distinct context),
    // proving the read did not leak an AsyncLocal publication for anything to join.
    [Fact]
    public async Task ExecuteReadAsync_Standalone_DoesNotPublishAmbientScope()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var uow = host.Resolve<IUnitOfWork>();

        AppDbContext? firstReadContext = null;
        AppDbContext? secondReadContext = null;

        await uow.ExecuteReadAsync(async sp =>
        {
            firstReadContext = sp.GetRequiredService<AppDbContext>();
            await Task.CompletedTask;
            return true;
        });

        await uow.ExecuteReadAsync(async sp =>
        {
            secondReadContext = sp.GetRequiredService<AppDbContext>();
            await Task.CompletedTask;
            return true;
        });

        Assert.NotNull(firstReadContext);
        Assert.NotNull(secondReadContext);
        Assert.NotSame(firstReadContext, secondReadContext);
    }

    // [AC] REQ-UOW-34: a read nested inside ExecuteAsync (a write) JOINS the write's scope — same
    // AppDbContext instance — and the outer write still commits successfully.
    [Fact]
    public async Task ExecuteAsync_NestedRead_JoinsSameContext_AndOuterWriteCommits()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var uow = host.Resolve<IUnitOfWork>();

        AppDbContext? writeContext = null;
        AppDbContext? nestedReadContext = null;

        await uow.ExecuteAsync(async sp =>
        {
            writeContext = sp.GetRequiredService<AppDbContext>();
            var repo = sp.GetRequiredService<IArtistRepository>();
            await repo.AddAsync(NewArtist("Write Then Read Artist"), CancellationToken.None);

            await uow.ExecuteReadAsync(async innerSp =>
            {
                nestedReadContext = innerSp.GetRequiredService<AppDbContext>();
                await Task.CompletedTask;
                return true;
            });
        });

        Assert.Same(writeContext, nestedReadContext);

        var found = await host.Db.Artists.FirstOrDefaultAsync(a => a.Name == "Write Then Read Artist");
        Assert.NotNull(found);
    }

    // [AC] REQ-UOW-22: a write nested inside ExecuteAsync (a write) JOINS the outer scope — same
    // AppDbContext instance, one commit — and if the OUTER body throws after the inner write has
    // already run, the rollback undoes the inner write too (all-or-nothing across the joined
    // scope, matching REQ-UOW-22's atomicity guarantee for the compound nested-write case).
    [Fact]
    public async Task ExecuteAsync_NestedWrite_JoinsSameContext_OuterThrowRollsBackInnerToo()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var uow = host.Resolve<IUnitOfWork>();

        AppDbContext? outerContext = null;
        AppDbContext? innerContext = null;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            uow.ExecuteAsync(async sp =>
            {
                outerContext = sp.GetRequiredService<AppDbContext>();

                await uow.ExecuteAsync(async innerSp =>
                {
                    innerContext = innerSp.GetRequiredService<AppDbContext>();
                    var innerRepo = innerSp.GetRequiredService<IArtistRepository>();
                    await innerRepo.AddAsync(NewArtist("Inner Doomed Artist"), CancellationToken.None);
                });

                throw new InvalidOperationException("injected outer failure after inner completed");
            }));

        Assert.Same(outerContext, innerContext);

        var found = await host.Db.Artists.FirstOrDefaultAsync(a => a.Name == "Inner Doomed Artist");
        Assert.Null(found);
    }

    private static Artist NewArtist(string name) => new()
    {
        Name = name,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };
}
