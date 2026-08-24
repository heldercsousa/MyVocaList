using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyVocaList.Domain.Entity;
using MyVocaList.Domain.RepositoryInterface;
using MyVocaList.Domain.UnitOfWork;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Integration.UnitOfWork;

/// <summary>
/// Task 1.2b — save-skip signal detection (<c>ResultSignalsSuccess</c>, `design.md § 6b`,
/// Revision 8/9): the value-returning <c>ExecuteAsync&lt;TResult&gt;</c> commits only when the
/// result signals success, rolls back on a failure signal (including
/// <c>ExecuteUpdateAsync</c>/<c>ExecuteDeleteAsync</c> bulk operations, REQ-UOW-33), and
/// fail-closed throws when the result carries no recognised signal at all (REQ-UOW-27).
/// </summary>
public class SaveSkipTests
{
    // ---------------------------------------------------------------- REQ-UOW-25 (success path)

    // [AC] REQ-UOW-25: a body that mutates an entity and returns a ValueTuple whose first element
    // is bool true SHALL have SaveChangesAsync called and the mutation persisted.
    [Fact]
    public async Task ExecuteAsync_SuccessTuple_CommitsMutation()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var uow = host.Resolve<IUnitOfWork>();

        var (success, message, artist) = await uow.ExecuteAsync(async sp =>
        {
            var repo = sp.GetRequiredService<IArtistRepository>();
            var entity = NewArtist("Success Tuple Artist");
            await repo.AddAsync(entity, CancellationToken.None);
            return (true, string.Empty, (Artist?)entity);
        });

        Assert.True(success, message);
        Assert.NotNull(artist);

        // Fresh scope read — proves the row is in the database, not merely tracked.
        var found = await ReadArtistAsync(uow, "Success Tuple Artist");
        Assert.NotNull(found);
    }

    // [AC] REQ-UOW-25: the IUnitOfWorkOutcome shape of the same criterion — Success == true
    // SHALL persist the mutation.
    [Fact]
    public async Task ExecuteAsync_OutcomeWithSuccessTrue_CommitsMutation()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var uow = host.Resolve<IUnitOfWork>();

        var outcome = await uow.ExecuteAsync(async sp =>
        {
            var repo = sp.GetRequiredService<IArtistRepository>();
            await repo.AddAsync(NewArtist("Success Outcome Artist"), CancellationToken.None);
            return new ProbeOutcome(true, string.Empty);
        });

        Assert.True(outcome.Success);

        var found = await ReadArtistAsync(uow, "Success Outcome Artist");
        Assert.NotNull(found);
    }

    // ---------------------------------------------------------------- REQ-UOW-24 (failure path)

    // [AC] REQ-UOW-24: a body that mutates an entity and then returns a ValueTuple whose first
    // element is bool false SHALL NOT call SaveChangesAsync — the mutation SHALL NOT be persisted.
    [Fact]
    public async Task ExecuteAsync_FailureTuple_DoesNotPersistMutation()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var uow = host.Resolve<IUnitOfWork>();

        var (success, message, artist) = await uow.ExecuteAsync(async sp =>
        {
            var repo = sp.GetRequiredService<IArtistRepository>();
            await repo.AddAsync(NewArtist("Failure Tuple Artist"), CancellationToken.None);
            return (false, "a later validation check failed", (Artist?)null);
        });

        Assert.False(success);
        Assert.Equal("a later validation check failed", message);
        Assert.Null(artist);

        var found = await ReadArtistAsync(uow, "Failure Tuple Artist");
        Assert.Null(found);
    }

    // [AC] REQ-UOW-24: the IUnitOfWorkOutcome shape of the same criterion — Success == false
    // SHALL NOT persist the mutation.
    [Fact]
    public async Task ExecuteAsync_OutcomeWithSuccessFalse_DoesNotPersistMutation()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var uow = host.Resolve<IUnitOfWork>();

        var outcome = await uow.ExecuteAsync(async sp =>
        {
            var repo = sp.GetRequiredService<IArtistRepository>();
            await repo.AddAsync(NewArtist("Failure Outcome Artist"), CancellationToken.None);
            return new ProbeOutcome(false, "a later validation check failed");
        });

        Assert.False(outcome.Success);

        var found = await ReadArtistAsync(uow, "Failure Outcome Artist");
        Assert.Null(found);
    }

    // [AC] REQ-UOW-24: an UPDATE to an already-persisted entity is equally skipped — the failure
    // tuple leaves the ORIGINAL value on a fresh read (the spec's SongService.UpdateSongAsync
    // exemplar, expressed on Artist so no service wrap is required at this phase).
    [Fact]
    public async Task ExecuteAsync_FailureTupleAfterUpdate_LeavesOriginalValue()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var uow = host.Resolve<IUnitOfWork>();

        var id = await SeedArtistAsync(uow, "Original Update Name");

        var (success, _) = await uow.ExecuteAsync(async sp =>
        {
            var repo = sp.GetRequiredService<IArtistRepository>();
            var entity = await repo.GetByIdAsync(id, CancellationToken.None);
            entity.Name = "Mutated Update Name";
            await repo.UpdateAsync(entity, CancellationToken.None);
            return (false, "duplicate name");
        });

        Assert.False(success);

        Assert.NotNull(await ReadArtistAsync(uow, "Original Update Name"));
        Assert.Null(await ReadArtistAsync(uow, "Mutated Update Name"));
    }

    // ---------------------------------------------------------------- REQ-UOW-26 (no signal)

    // [AC] REQ-UOW-26: the no-signal overload (Func<IServiceProvider, Task>) SHALL call
    // SaveChangesAsync unconditionally whenever the body completes without throwing — there is no
    // result to inspect, so ResultSignalsSuccess is never consulted.
    [Fact]
    public async Task ExecuteAsync_NoSignalOverload_AlwaysSaves()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var uow = host.Resolve<IUnitOfWork>();

        await uow.ExecuteAsync(async sp =>
        {
            var repo = sp.GetRequiredService<IArtistRepository>();
            await repo.AddAsync(NewArtist("No Signal Artist"), CancellationToken.None);
        });

        Assert.NotNull(await ReadArtistAsync(uow, "No Signal Artist"));
    }

    // ---------------------------------------------------------------- REQ-UOW-27 (fail-closed)

    // [AC] REQ-UOW-27: a TResult that is neither a ValueTuple with a leading bool nor an
    // IUnitOfWorkOutcome SHALL make ExecuteAsync throw InvalidOperationException before any
    // SaveChangesAsync is attempted; the message names the offending type and both valid fixes.
    [Fact]
    public async Task ExecuteAsync_UnmarkedNamedResult_ThrowsAndDoesNotPersist()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var uow = host.Resolve<IUnitOfWork>();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            uow.ExecuteAsync(async sp =>
            {
                var repo = sp.GetRequiredService<IArtistRepository>();
                await repo.AddAsync(NewArtist("Unmarked Result Artist"), CancellationToken.None);
                return new UnmarkedResult(42);
            }));

        Assert.Contains(nameof(UnmarkedResult), ex.Message);
        Assert.Contains(nameof(IUnitOfWorkOutcome), ex.Message);
        Assert.Contains("ExecuteAsync", ex.Message);

        Assert.Null(await ReadArtistAsync(uow, "Unmarked Result Artist"));
    }

    // [AC] REQ-UOW-27: a primitive result (int) carries no success signal either — fail-closed,
    // never a silent commit.
    [Fact]
    public async Task ExecuteAsync_PrimitiveResult_ThrowsAndDoesNotPersist()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var uow = host.Resolve<IUnitOfWork>();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            uow.ExecuteAsync(async sp =>
            {
                var repo = sp.GetRequiredService<IArtistRepository>();
                await repo.AddAsync(NewArtist("Primitive Result Artist"), CancellationToken.None);
                return 42;
            }));

        Assert.Contains(nameof(Int32), ex.Message);
        Assert.Null(await ReadArtistAsync(uow, "Primitive Result Artist"));
    }

    // [AC] REQ-UOW-27: a ValueTuple whose FIRST element is not a bool is not a success signal —
    // it must throw, never be treated as success by virtue of merely being a tuple.
    [Fact]
    public async Task ExecuteAsync_TupleWithNonBoolFirstElement_ThrowsAndDoesNotPersist()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var uow = host.Resolve<IUnitOfWork>();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            uow.ExecuteAsync(async sp =>
            {
                var repo = sp.GetRequiredService<IArtistRepository>();
                await repo.AddAsync(NewArtist("Non Bool Tuple Artist"), CancellationToken.None);
                return (42, "not a signal");
            }));

        Assert.Null(await ReadArtistAsync(uow, "Non Bool Tuple Artist"));
    }

    // [AC] REQ-UOW-27: the empty ValueTuple has Length == 0, so it has no leading bool to read —
    // fail-closed throw, not an implicit success.
    [Fact]
    public async Task ExecuteAsync_EmptyTuple_ThrowsAndDoesNotPersist()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var uow = host.Resolve<IUnitOfWork>();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            uow.ExecuteAsync(async sp =>
            {
                var repo = sp.GetRequiredService<IArtistRepository>();
                await repo.AddAsync(NewArtist("Empty Tuple Artist"), CancellationToken.None);
                return default(ValueTuple);
            }));

        Assert.Null(await ReadArtistAsync(uow, "Empty Tuple Artist"));
    }

    // ------------------------------------------------ REQ-UOW-24/27 (real BackupResult exemplar)

    // [AC] REQ-UOW-24: the real IUnitOfWorkOutcome exemplar named by the spec —
    // BackupService.CreateFullBackupAsync returning a BackupResult with Success == false SHALL NOT
    // persist the BackupHistory row the body already added (Task 4.5).
    [Fact]
    public async Task BackupService_CreateFullBackupAsync_FailureResult_DoesNotPersistHistoryRow()
    {
        await using var host = UnitOfWorkTestHost.Create(services =>
            services.AddScoped<IBackupRepository, MyVocaList.Infra.Repository.BackupRepository>());
        var uow = host.Resolve<IUnitOfWork>();

        var backupDir = Path.Combine(Path.GetTempPath(), $"mvl_bkp_fail_{Guid.NewGuid():N}");
        Directory.CreateDirectory(backupDir);
        var dbPath = Path.Combine(backupDir, "fake_source.db");
        File.WriteAllText(dbPath, "fake-db-content");

        // Fault injected after the AddAsync mutation — proves the mutation is rolled back, not
        // merely never attempted.
        var faultyLogWriter = new Mock<ITransactionLogWriter>();
        faultyLogWriter
            .Setup(l => l.PruneLogsOlderThanAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("simulated post-mutation failure"));

        var backupService = new BackupService(
            host.Resolve<IBackupRepository>(),
            uow,
            faultyLogWriter.Object,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<BackupService>.Instance,
            dbPath,
            backupDir);

        var result = await backupService.CreateFullBackupAsync(BackupTrigger.Manual, CancellationToken.None);

        Assert.False(result.Success);

        var historyCount = await uow.ExecuteReadAsync(async sp =>
        {
            var db = sp.GetRequiredService<Infra.AppDbContext>();
            return await db.BackupHistories.CountAsync();
        });
        Assert.Equal(0, historyCount);
    }

    // [AC] REQ-UOW-27: the positive counterpart — now that BackupResult implements
    // IUnitOfWorkOutcome, the wrap does NOT throw InvalidOperationException, and a successful
    // backup's BackupHistory row IS persisted (Task 4.5).
    [Fact]
    public async Task BackupService_CreateFullBackupAsync_SuccessResult_DoesNotThrowAndPersistsHistoryRow()
    {
        await using var host = UnitOfWorkTestHost.Create(services =>
            services.AddScoped<IBackupRepository, MyVocaList.Infra.Repository.BackupRepository>());
        var uow = host.Resolve<IUnitOfWork>();

        var backupDir = Path.Combine(Path.GetTempPath(), $"mvl_bkp_ok_{Guid.NewGuid():N}");
        Directory.CreateDirectory(backupDir);
        var dbPath = Path.Combine(backupDir, "fake_source.db");
        File.WriteAllText(dbPath, "fake-db-content");

        var backupService = new BackupService(
            host.Resolve<IBackupRepository>(),
            uow,
            host.Log,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<BackupService>.Instance,
            dbPath,
            backupDir);

        var result = await backupService.CreateFullBackupAsync(BackupTrigger.Manual, CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotNull(result.FilePath);

        var historyCount = await uow.ExecuteReadAsync(async sp =>
        {
            var db = sp.GetRequiredService<Infra.AppDbContext>();
            return await db.BackupHistories.CountAsync();
        });
        Assert.Equal(1, historyCount);
    }

    // ---------------------------------------------------------------- REQ-UOW-33 (bulk ops)

    // [AC] REQ-UOW-33: WHEN the body returns a failure signal after an ExecuteDeleteAsync has
    // already run inside it, THEN ExecuteAsync SHALL roll back the transaction, undoing that bulk
    // delete — the row still exists. This is the test that proves the withdrawn
    // "ExecuteUpdate/ExecuteDelete are exempt from atomicity" carve-out was a design gap.
    [Fact]
    public async Task ExecuteAsync_FailureTupleAfterExecuteDelete_RollsBackTheDelete()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var uow = host.Resolve<IUnitOfWork>();

        var id = await SeedArtistAsync(uow, "Bulk Delete Artist");

        var (success, _) = await uow.ExecuteAsync(async sp =>
        {
            var repo = sp.GetRequiredService<IArtistRepository>();
            await repo.DeleteAsync([id], CancellationToken.None);
            return (false, "deletion refused by a later check");
        });

        Assert.False(success);
        Assert.NotNull(await ReadArtistAsync(uow, "Bulk Delete Artist"));
    }

    // [AC] REQ-UOW-33: the ExecuteUpdateAsync counterpart — a failure signal after
    // IncrementPlayCountAsync leaves the play count unchanged.
    [Fact]
    public async Task ExecuteAsync_FailureTupleAfterExecuteUpdate_RollsBackTheUpdate()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var uow = host.Resolve<IUnitOfWork>();

        const string videoId = "vid-req-uow-33";
        var songId = await SeedSongWithKaraokeUrlAsync(uow, "Bulk Update Artist", "Bulk Update Song", videoId);

        var (success, _) = await uow.ExecuteAsync(async sp =>
        {
            var repo = sp.GetRequiredService<ISongKaraokeUrlRepository>();
            await repo.IncrementPlayCountAsync(songId, videoId, CancellationToken.None);
            return (false, "play could not be recorded");
        });

        Assert.False(success);

        var playCount = await uow.ExecuteReadAsync(async sp =>
        {
            var db = sp.GetRequiredService<Infra.AppDbContext>();
            var url = await db.SongKaraokeUrls.FirstAsync(u => u.SongId == songId && u.VideoId == videoId);
            return url.PlayCount;
        });
        Assert.Equal(0, playCount);
    }

    // ---------------------------------------------------------------- helpers

    // ------------------------------------------------------ REQ-UOW-35 (flush without commit)

    // [AC] REQ-UOW-35: FlushAsync makes the database-generated key readable INSIDE the body
    // (REQ-UOW-09's "two saves inside one unit of work" branch) — and the flushed row survives
    // when the unit of work goes on to succeed.
    [Fact]
    public async Task FlushAsync_InsideSuccessfulUnitOfWork_MaterialisesGeneratedKeyAndPersists()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var uow = host.Resolve<IUnitOfWork>();

        var (success, _, idSeenInsideBody) = await uow.ExecuteAsync(async sp =>
        {
            var repo = sp.GetRequiredService<IArtistRepository>();
            var entity = NewArtist("Flush Success Artist");
            await repo.AddAsync(entity, CancellationToken.None);

            Assert.Equal(0, entity.Id);          // not yet materialised
            await uow.FlushAsync(CancellationToken.None);
            Assert.NotEqual(0, entity.Id);       // materialised by the flush, before the body returns

            return (true, string.Empty, entity.Id);
        });

        Assert.True(success);
        Assert.NotEqual(0, idSeenInsideBody);

        var found = await ReadArtistAsync(uow, "Flush Success Artist");
        Assert.NotNull(found);
        Assert.Equal(idSeenInsideBody, found!.Id);
    }

    // [AC] REQ-UOW-35: a flush is NOT a commit — the transaction stays open, so a subsequent
    // failure tuple SHALL roll the already-flushed row back. This is the atomicity guarantee.
    [Fact]
    public async Task FlushAsync_ThenFailureSignal_RollsFlushedRowBack()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var uow = host.Resolve<IUnitOfWork>();

        var (success, _, flushedId) = await uow.ExecuteAsync(async sp =>
        {
            var repo = sp.GetRequiredService<IArtistRepository>();
            var entity = NewArtist("Flush Then Failure Artist");
            await repo.AddAsync(entity, CancellationToken.None);

            await uow.FlushAsync(CancellationToken.None);
            Assert.NotEqual(0, entity.Id);   // the flush really did write it

            // ...and only now does the unit of work decide it has failed.
            return (false, "a later validation check failed", entity.Id);
        });

        Assert.False(success);
        Assert.NotEqual(0, flushedId);

        // Fresh scope read — the flushed row must NOT have survived the rollback.
        Assert.Null(await ReadArtistAsync(uow, "Flush Then Failure Artist"));
    }

    // [AC] REQ-UOW-35: same guarantee via the exception path — a throw after a flush SHALL roll the
    // already-flushed row back.
    [Fact]
    public async Task FlushAsync_ThenThrow_RollsFlushedRowBack()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var uow = host.Resolve<IUnitOfWork>();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            uow.ExecuteAsync<(bool, string, int)>(async sp =>
            {
                var repo = sp.GetRequiredService<IArtistRepository>();
                var entity = NewArtist("Flush Then Throw Artist");
                await repo.AddAsync(entity, CancellationToken.None);

                await uow.FlushAsync(CancellationToken.None);
                Assert.NotEqual(0, entity.Id);

                throw new InvalidOperationException("simulated post-flush failure");
            }));

        Assert.Null(await ReadArtistAsync(uow, "Flush Then Throw Artist"));
    }

    // [AC] REQ-UOW-35: calling FlushAsync outside a unit of work (no ambient scope) SHALL throw —
    // fail-closed, consistent with REQ-UOW-27's refusal to guess. ExecuteReadAsync does not open one
    // (REQ-UOW-34), so a flush from inside a standalone read throws too.
    [Fact]
    public async Task FlushAsync_OutsideUnitOfWork_Throws()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var uow = host.Resolve<IUnitOfWork>();

        await Assert.ThrowsAsync<InvalidOperationException>(() => uow.FlushAsync(CancellationToken.None));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            uow.ExecuteReadAsync<int>(async _ =>
            {
                await uow.FlushAsync(CancellationToken.None);
                return 0;
            }));
    }

    private static async Task<Artist?> ReadArtistAsync(IUnitOfWork uow, string name)
        => await uow.ExecuteReadAsync(async sp =>
        {
            var db = sp.GetRequiredService<Infra.AppDbContext>();
            return await db.Artists.FirstOrDefaultAsync(a => a.Name == name);
        });

    private static async Task<int> SeedArtistAsync(IUnitOfWork uow, string name)
    {
        var entity = NewArtist(name);
        await uow.ExecuteAsync(async sp =>
        {
            var repo = sp.GetRequiredService<IArtistRepository>();
            await repo.AddAsync(entity, CancellationToken.None);
        });
        return entity.Id;
    }

    private static async Task<int> SeedSongWithKaraokeUrlAsync(
        IUnitOfWork uow, string artistName, string songTitle, string videoId)
    {
        var artistId = await SeedArtistAsync(uow, artistName);

        var song = new Song
        {
            ArtistId = artistId,
            Title = songTitle,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await uow.ExecuteAsync(async sp =>
        {
            var songRepo = sp.GetRequiredService<ISongRepository>();
            await songRepo.AddAsync(song, CancellationToken.None);
        });

        await uow.ExecuteAsync(async sp =>
        {
            var urlRepo = sp.GetRequiredService<ISongKaraokeUrlRepository>();
            await urlRepo.AddAsync(new SongKaraokeUrl
            {
                VideoId = videoId,
                SongId = song.Id,
                PlayCount = 0,
                AddedAt = DateTime.UtcNow
            }, CancellationToken.None);
        });

        return song.Id;
    }

    private static Artist NewArtist(string name) => new()
    {
        Name = name,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    /// <summary>Test-local stand-in for a named result type that opts in to the success signal.
    /// The spec's exemplar is <c>BackupResult</c>, but <c>IBackupRepository</c> is registered in
    /// <c>MauiProgram.cs</c> rather than <c>AddAppServices()</c>, so the real type is Task 4.5's
    /// obligation; this synthetic record covers the shape now.</summary>
    private sealed record ProbeOutcome(bool Success, string Message) : IUnitOfWorkOutcome;

    /// <summary>A named result type that deliberately does NOT implement
    /// <see cref="IUnitOfWorkOutcome"/> — the REQ-UOW-27 fail-closed probe.</summary>
    private sealed record UnmarkedResult(int Value);
}
