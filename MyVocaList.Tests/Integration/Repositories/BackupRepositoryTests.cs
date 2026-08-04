using MyVocaList.Infra;
using MyVocaList.Infra.Repository;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Integration.Repositories;

public class BackupRepositoryTests : IAsyncLifetime
{
    private AppDbContext _db = null!;
    private BackupRepository _repo = null!;

    public async Task InitializeAsync()
    {
        _db = TestDbContextFactory.Create();
        await _db.Database.EnsureCreatedAsync();
        _repo = new BackupRepository(_db);
    }

    public async Task DisposeAsync()
    {
        await _db.Database.EnsureDeletedAsync();
        await _db.DisposeAsync();
    }

    [Fact]
    // [AC] BackupHistory table exists and AddAsync persists an entry
    public async Task AddAsync_ValidEntry_PersistedAndReturnedByGetRecent()
    {
        var entry = new BackupHistory
        {
            CreatedAt = DateTime.UtcNow,
            TriggerType = BackupTrigger.Manual,
            BackupType = BackupType.FullSnapshot,
            FilePath = "/data/backup_test.db",
            FileSizeBytes = 1024,
            MirrorStatus = MirrorStatus.NotAttempted
        };

        await _repo.AddAsync(entry, CancellationToken.None);
        await _repo.SaveChangesAsync(CancellationToken.None);

        var results = await _repo.GetRecentAsync(10, CancellationToken.None);

        Assert.Single(results);
        Assert.Equal(BackupTrigger.Manual, results[0].TriggerType);
        Assert.Equal(1024, results[0].FileSizeBytes);
    }

    [Fact]
    // [AC] GetRecentAsync returns entries ordered by CreatedAt descending
    public async Task GetRecentAsync_MultipleEntries_ReturnsOrderedByCreatedAtDesc()
    {
        var older = new BackupHistory { CreatedAt = DateTime.UtcNow.AddHours(-2), TriggerType = BackupTrigger.AppStop, BackupType = BackupType.FullSnapshot, FilePath = "/old.db", FileSizeBytes = 100, MirrorStatus = MirrorStatus.NotAttempted };
        var newer = new BackupHistory { CreatedAt = DateTime.UtcNow, TriggerType = BackupTrigger.Manual, BackupType = BackupType.FullSnapshot, FilePath = "/new.db", FileSizeBytes = 200, MirrorStatus = MirrorStatus.NotAttempted };

        await _repo.AddAsync(older, CancellationToken.None);
        await _repo.AddAsync(newer, CancellationToken.None);
        await _repo.SaveChangesAsync(CancellationToken.None);

        var results = await _repo.GetRecentAsync(10, CancellationToken.None);

        Assert.Equal(2, results.Count);
        Assert.Equal("/new.db", results[0].FilePath);
        Assert.Equal("/old.db", results[1].FilePath);
    }

    [Fact]
    // [AC] GetRecentAsync respects the limit parameter
    public async Task GetRecentAsync_LimitApplied_ReturnsOnlyRequestedCount()
    {
        for (int i = 0; i < 5; i++)
        {
            await _repo.AddAsync(new BackupHistory { CreatedAt = DateTime.UtcNow.AddMinutes(-i), TriggerType = BackupTrigger.AppStop, BackupType = BackupType.FullSnapshot, FilePath = $"/b{i}.db", FileSizeBytes = 100, MirrorStatus = MirrorStatus.NotAttempted }, CancellationToken.None);
        }
        await _repo.SaveChangesAsync(CancellationToken.None);

        var results = await _repo.GetRecentAsync(3, CancellationToken.None);

        Assert.Equal(3, results.Count);
    }

    [Fact]
    // [AC] GetLatestSnapshotAsync returns only FullSnapshot entries
    public async Task GetLatestSnapshotAsync_MixedTypes_ReturnsOnlySnapshot()
    {
        var log = new BackupHistory { CreatedAt = DateTime.UtcNow, TriggerType = BackupTrigger.AppStop, BackupType = BackupType.TransactionLog, FilePath = "/log.jsonl", FileSizeBytes = 50, MirrorStatus = MirrorStatus.NotAttempted };
        var snap = new BackupHistory { CreatedAt = DateTime.UtcNow.AddMinutes(-1), TriggerType = BackupTrigger.Manual, BackupType = BackupType.FullSnapshot, FilePath = "/snap.db", FileSizeBytes = 2048, MirrorStatus = MirrorStatus.NotAttempted };

        await _repo.AddAsync(log, CancellationToken.None);
        await _repo.AddAsync(snap, CancellationToken.None);
        await _repo.SaveChangesAsync(CancellationToken.None);

        var result = await _repo.GetLatestSnapshotAsync(CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(BackupType.FullSnapshot, result.BackupType);
    }
}
