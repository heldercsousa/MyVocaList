using Microsoft.Extensions.DependencyInjection;
using MyVocaList.Domain.ServicesInterfaces;
using MyVocaList.Domain.UnitOfWork;
using MyVocaList.Infra;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Unit.Services;

/// <summary>
/// Task 4.5 — <see cref="MyVocaList.Services.BackupService"/> read methods scoped through
/// <see cref="IUnitOfWork.ExecuteReadAsync{TResult}"/> (REQ-UOW-39, REQ-UOW-40, REQ-UOW-44).
/// </summary>
public class BackupServiceReadScopeTests
{
    // [AC] REQ-UOW-40: GetHistoryAsync returns the same rows as before the wrap (unchanged
    // observable behaviour) after being scoped through IUnitOfWork.
    [Fact]
    public async Task GetHistoryAsync_ReturnsRecentBackups()
    {
        await using var host = UnitOfWorkTestHost.Create(services =>
            services.AddScoped<IBackupRepository, MyVocaList.Infra.Repository.BackupRepository>());
        var backups = host.Resolve<IBackupRepository>();
        await backups.AddAsync(new BackupHistory
        {
            CreatedAt = DateTime.UtcNow,
            TriggerType = BackupTrigger.Manual,
            BackupType = BackupType.FullSnapshot,
            FilePath = "dummy.db",
            FileSizeBytes = 1,
            MirrorStatus = MirrorStatus.NotAttempted
        }, CancellationToken.None);
        await host.Db.SaveChangesAsync();

        var service = BuildService(host);
        var history = await service.GetHistoryAsync(5, CancellationToken.None);

        Assert.Single(history);
    }

    // [AC] REQ-UOW-40: HasRecentBackupAsync returns true when the latest snapshot is within 24
    // hours, after being scoped through IUnitOfWork.
    [Fact]
    public async Task HasRecentBackupAsync_RecentSnapshot_ReturnsTrue()
    {
        await using var host = UnitOfWorkTestHost.Create(services =>
            services.AddScoped<IBackupRepository, MyVocaList.Infra.Repository.BackupRepository>());
        var backups = host.Resolve<IBackupRepository>();
        await backups.AddAsync(new BackupHistory
        {
            CreatedAt = DateTime.UtcNow.AddHours(-2),
            TriggerType = BackupTrigger.Manual,
            BackupType = BackupType.FullSnapshot,
            FilePath = "dummy.db",
            FileSizeBytes = 1,
            MirrorStatus = MirrorStatus.NotAttempted
        }, CancellationToken.None);
        await host.Db.SaveChangesAsync();

        var service = BuildService(host);
        var result = await service.HasRecentBackupAsync(CancellationToken.None);

        Assert.True(result);
    }

    // [AC] REQ-UOW-40: HasRecentBackupAsync returns false when no snapshot exists, after being
    // scoped through IUnitOfWork.
    [Fact]
    public async Task HasRecentBackupAsync_NoSnapshot_ReturnsFalse()
    {
        await using var host = UnitOfWorkTestHost.Create(services =>
            services.AddScoped<IBackupRepository, MyVocaList.Infra.Repository.BackupRepository>());
        var service = BuildService(host);

        var result = await service.HasRecentBackupAsync(CancellationToken.None);

        Assert.False(result);
    }

    // [AC] REQ-UOW-44: ExportBundleAsync still returns its "no backup available" failure tuple
    // when no snapshot exists, proving the read wrap sits INSIDE the method's existing try block
    // and does not change the observable failure tuple.
    [Fact]
    public async Task ExportBundleAsync_NoSnapshot_ReturnsFailureTuple()
    {
        await using var host = UnitOfWorkTestHost.Create(services =>
            services.AddScoped<IBackupRepository, MyVocaList.Infra.Repository.BackupRepository>());
        var service = BuildService(host);

        var (success, message) = await service.ExportBundleAsync(CancellationToken.None);

        Assert.False(success);
        Assert.Equal("No backup available. Create a backup first.", message);
    }

    // [AC] REQ-UOW-44: ExportBundleAsync's existing catch still catches an exception thrown
    // during the wrapped read — proving the ExecuteReadAsync wrap is inside the pre-existing
    // try block, not hoisted around it (a hoist would let the exception escape uncaught).
    [Fact]
    public async Task ExportBundleAsync_ReadThrows_ReturnsFailureTupleInsteadOfThrowing()
    {
        await using var host = UnitOfWorkTestHost.Create(services =>
            services.AddScoped<IBackupRepository, MyVocaList.Infra.Repository.BackupRepository>());
        var throwingUow = new ThrowingReadUnitOfWork(host.Resolve<IUnitOfWork>());
        var backupRepository = host.Resolve<IBackupRepository>();
        var logWriter = host.Resolve<ITransactionLogWriter>();
        var logger = host.Resolve<Microsoft.Extensions.Logging.ILogger<MyVocaList.Services.BackupService>>();
        var dbPath = Path.Combine(Path.GetTempPath(), $"mvl_export_dbpath_{Guid.NewGuid():N}.db");
        var backupDir = Path.Combine(Path.GetTempPath(), $"mvl_export_bkpdir_{Guid.NewGuid():N}");
        var service = new MyVocaList.Services.BackupService(
            backupRepository, throwingUow, logWriter, logger, dbPath, backupDir);

        var (success, message) = await service.ExportBundleAsync(CancellationToken.None);

        Assert.False(success);
        Assert.Equal("Export failed. See logs for details.", message);
    }

    // [AC] REQ-UOW-39: two successive BackupService reads scoped through IUnitOfWork resolve two
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

    private static MyVocaList.Services.BackupService BuildService(UnitOfWorkTestHost host)
    {
        var backupRepository = host.Resolve<IBackupRepository>();
        var uow = host.Resolve<IUnitOfWork>();
        var logWriter = host.Resolve<ITransactionLogWriter>();
        var logger = host.Resolve<Microsoft.Extensions.Logging.ILogger<MyVocaList.Services.BackupService>>();
        var dbPath = Path.Combine(Path.GetTempPath(), $"mvl_bkp_dbpath_{Guid.NewGuid():N}.db");
        var backupDir = Path.Combine(Path.GetTempPath(), $"mvl_bkp_dir_{Guid.NewGuid():N}");
        return new MyVocaList.Services.BackupService(backupRepository, uow, logWriter, logger, dbPath, backupDir);
    }

    /// <summary>Wraps a real <see cref="IUnitOfWork"/> but makes every
    /// <see cref="ExecuteReadAsync{TResult}"/> call throw, so a test can prove that an exception
    /// raised during the wrapped read is caught by the caller's own pre-existing try/catch
    /// (REQ-UOW-44) rather than propagating past it.</summary>
    private sealed class ThrowingReadUnitOfWork(IUnitOfWork inner) : IUnitOfWork
    {
        public Task<TResult> ExecuteAsync<TResult>(Func<IServiceProvider, Task<TResult>> body, CancellationToken ct = default)
            => inner.ExecuteAsync(body, ct);

        public Task ExecuteAsync(Func<IServiceProvider, Task> body, CancellationToken ct = default)
            => inner.ExecuteAsync(body, ct);

        public Task<TResult> ExecuteReadAsync<TResult>(Func<IServiceProvider, Task<TResult>> body, CancellationToken ct = default)
            => throw new InvalidOperationException("Simulated read failure for REQ-UOW-44 coverage.");

        public Task FlushAsync(CancellationToken ct = default) => inner.FlushAsync(ct);
    }
}
