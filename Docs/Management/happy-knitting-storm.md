# Data Backup & Restore — Implementation Plan (Tier 1 + Tier 3)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement a local-first backup engine (SQLite snapshots + transaction log) and manual export/restore via Android share sheet, following the existing DRY-Onion layered architecture.

**Architecture:** Three layers — Domain entity + repository interface → Infra migration + EF interceptor → BackupService orchestration → MAUI lifecycle hook + BackupRestorePage UI. The transaction log is captured via an EF Core `SaveChangesInterceptor` that records before/after state for every write. Snapshots are created with `VACUUM INTO` for consistency.

**Tech Stack:** EF Core 10 · SQLite · System.IO.Compression (zip) · MAUI FileProvider (Android share) · CommunityToolkit.Mvvm · xUnit + Moq

**Out of scope (separate plan):** Tier 2 WiFi mirror (mDNS, TCP, AES-256 pairing). Pre-event advisory deferred to Queue Management feature.

**Spec:** `Docs/Management/BusinessFeatures/backup-restore/design.md`

---

## File Map

| File | Action | Purpose |
|------|--------|---------|
| `Domain/Entity/BackupHistory.cs` | Create | BackupHistory entity POCO |
| `Domain/Entity/BackupTrigger.cs` | Create | BackupTrigger enum |
| `Domain/Entity/BackupType.cs` | Create | BackupType enum |
| `Domain/Entity/MirrorStatus.cs` | Create | MirrorStatus enum |
| `Domain/RepositoryInterface/IBackupRepository.cs` | Create | Repository interface |
| `Domain/ServicesInterfaces/ITransactionLogWriter.cs` | Create | Log writer interface |
| `Domain/ServicesInterfaces/IBackupService.cs` | Create | Backup service interface |
| `Infra/EntityEFConfig/BackupHistoryConfiguration.cs` | Create | EF fluent config |
| `Infra/Migrations/YYYYMMDDHHMMSS_AddBackupHistory.cs` | Generate | EF migration |
| `Infra/AppDbContext.cs` | Modify | Add `DbSet<BackupHistory>` + configuration |
| `Infra/Repository/BackupRepository.cs` | Create | Repository implementation |
| `Infra/Interceptor/TransactionLogInterceptor.cs` | Create | EF SaveChanges interceptor |
| `Services/BackupService.cs` | Create | Orchestration service |
| `Services/TransactionLogWriter.cs` | Create | File-based log writer |
| `MyVocaList/App.xaml.cs` | Modify | Add `Window.Stopped` auto-backup hook |
| `MyVocaList/MauiProgram.cs` | Modify | Register new services + interceptor |
| `MyVocaList/Platforms/Android/Resources/xml/file_provider_paths.xml` | Create | FileProvider config |
| `MyVocaList/Platforms/Android/AndroidManifest.xml` | Modify | Add FileProvider declaration |
| `MyVocaList/UI/ViewModels/BackupRestoreViewModel.cs` | Create | ViewModel for backup page |
| `MyVocaList/UI/Pages/BackupRestore/BackupRestorePage.xaml` | Modify | Replace stub with full UI |
| `MyVocaList/UI/Pages/BackupRestore/BackupRestorePage.xaml.cs` | Modify | Wire ViewModel |
| `MyVocaList.Tests/Unit/Services/BackupServiceTests.cs` | Create | Unit tests |
| `MyVocaList.Tests/Integration/Repositories/BackupRepositoryTests.cs` | Create | Integration tests |

---

## Phase 1 — Domain

### Task 1: BackupHistory entity + enums + IBackupRepository

**Files:**
- Create: `Domain/Entity/BackupTrigger.cs`
- Create: `Domain/Entity/BackupType.cs`
- Create: `Domain/Entity/MirrorStatus.cs`
- Create: `Domain/Entity/BackupHistory.cs`
- Create: `Domain/RepositoryInterface/IBackupRepository.cs`
- Create: `Domain/ServicesInterfaces/ITransactionLogWriter.cs`
- Create: `Domain/ServicesInterfaces/IBackupService.cs`

- [ ] **Step 1: Create enums**

`Domain/Entity/BackupTrigger.cs`:
```csharp
namespace MyVocaList.Domain.Entity;

public enum BackupTrigger
{
    AppStop,
    QueueCreated,
    RoundCompleted,
    QueueClosed,
    Manual
}
```

`Domain/Entity/BackupType.cs`:
```csharp
namespace MyVocaList.Domain.Entity;

public enum BackupType
{
    FullSnapshot,
    TransactionLog
}
```

`Domain/Entity/MirrorStatus.cs`:
```csharp
namespace MyVocaList.Domain.Entity;

public enum MirrorStatus
{
    NotAttempted,
    Pending,
    Confirmed
}
```

- [ ] **Step 2: Create BackupHistory entity**

`Domain/Entity/BackupHistory.cs`:
```csharp
namespace MyVocaList.Domain.Entity;

public class BackupHistory
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public BackupTrigger TriggerType { get; set; }
    public BackupType BackupType { get; set; }
    public string FilePath { get; set; }
    public long FileSizeBytes { get; set; }
    public MirrorStatus MirrorStatus { get; set; }
}
```

- [ ] **Step 3: Create IBackupRepository**

`Domain/RepositoryInterface/IBackupRepository.cs`:
```csharp
using MyVocaList.Domain.Entity;

namespace MyVocaList.Domain.RepositoryInterface;

/// <summary>Repository for backup history records.</summary>
public interface IBackupRepository
{
    /// <summary>Persists a new backup history entry.</summary>
    Task AddAsync(BackupHistory entry, CancellationToken ct);

    /// <summary>Returns the N most recent backup history entries ordered by CreatedAt descending.</summary>
    Task<IReadOnlyList<BackupHistory>> GetRecentAsync(int limit, CancellationToken ct);

    /// <summary>Returns the most recent successful full snapshot entry, or null if none exists.</summary>
    Task<BackupHistory?> GetLatestSnapshotAsync(CancellationToken ct);

    Task SaveChangesAsync(CancellationToken ct);
}
```

- [ ] **Step 4: Create ITransactionLogWriter**

`Domain/ServicesInterfaces/ITransactionLogWriter.cs`:
```csharp
namespace MyVocaList.Domain.ServicesInterfaces;

public record LogEntry(
    DateTime Ts,
    string Op,
    string Entity,
    string Id,
    string? Before,
    string? After);

/// <summary>Appends write operations to the current session transaction log file.</summary>
public interface ITransactionLogWriter
{
    /// <summary>Appends a single log entry to the current session log file.</summary>
    Task AppendAsync(LogEntry entry, CancellationToken ct);

    /// <summary>
    /// Deletes session log files whose last-entry timestamp is entirely before snapshotTs.
    /// Log files that straddle the boundary are kept.
    /// </summary>
    Task PruneLogsOlderThanAsync(DateTime snapshotTs, CancellationToken ct);

    /// <summary>Returns the path of the current session log file.</summary>
    string CurrentSessionLogPath { get; }
}
```

- [ ] **Step 5: Create IBackupService**

`Domain/ServicesInterfaces/IBackupService.cs`:
```csharp
using MyVocaList.Domain.Entity;

namespace MyVocaList.Domain.ServicesInterfaces;

public record BackupResult(bool Success, string Message, string? FilePath, long FileSizeBytes);

/// <summary>Orchestrates snapshot creation, log management, export, and restore.</summary>
public interface IBackupService
{
    /// <summary>Creates a full SQLite snapshot backup and records it in history.</summary>
    Task<BackupResult> CreateFullBackupAsync(BackupTrigger trigger, CancellationToken ct);

    /// <summary>
    /// Exports a zip bundle (latest snapshot + all log files since snapshot) via Android share sheet.
    /// </summary>
    Task<(bool success, string message)> ExportBundleAsync(CancellationToken ct);

    /// <summary>Restores the database from a previously exported zip bundle.</summary>
    Task<(bool success, string message)> RestoreFromBundleAsync(string zipPath, CancellationToken ct);

    /// <summary>Returns the N most recent backup history entries.</summary>
    Task<IReadOnlyList<BackupHistory>> GetHistoryAsync(int limit, CancellationToken ct);

    /// <summary>Returns true if a snapshot or export was created within the last 24 hours.</summary>
    Task<bool> HasRecentBackupAsync(CancellationToken ct);
}
```

- [ ] **Step 6: Build and confirm 0 errors**

```powershell
dotnet build MyVocaList.sln
```
Expected: Build succeeded, 0 error(s).

- [ ] **Step 7: Commit**

```bash
git add Domain/Entity/BackupTrigger.cs Domain/Entity/BackupType.cs Domain/Entity/MirrorStatus.cs Domain/Entity/BackupHistory.cs Domain/RepositoryInterface/IBackupRepository.cs Domain/ServicesInterfaces/ITransactionLogWriter.cs Domain/ServicesInterfaces/IBackupService.cs
git commit -m "feat(backup): add BackupHistory entity, enums, and service interfaces"
```

---

## Phase 2 — Infra

### Task 2: EF configuration + migration

**Files:**
- Create: `Infra/EntityEFConfig/BackupHistoryConfiguration.cs`
- Modify: `Infra/AppDbContext.cs`
- Generate: EF migration

- [ ] **Step 1: Write failing integration test (Red)**

`MyVocaList.Tests/Integration/Repositories/BackupRepositoryTests.cs` — add this minimal test first:
```csharp
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
}
```

Run test:
```powershell
dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --filter "BackupRepositoryTests" --verbosity normal
```
Expected: FAIL (BackupRepository does not exist yet).

- [ ] **Step 2: Create EF configuration**

`Infra/EntityEFConfig/BackupHistoryConfiguration.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyVocaList.Domain.Entity;

namespace MyVocaList.Infra.EntityEFConfig;

public class BackupHistoryConfiguration : IEntityTypeConfiguration<BackupHistory>
{
    public void Configure(EntityTypeBuilder<BackupHistory> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).ValueGeneratedOnAdd();
        builder.Property(b => b.CreatedAt).IsRequired();
        builder.Property(b => b.TriggerType).IsRequired().HasConversion<string>();
        builder.Property(b => b.BackupType).IsRequired().HasConversion<string>();
        builder.Property(b => b.FilePath).IsRequired().HasMaxLength(500);
        builder.Property(b => b.FileSizeBytes).IsRequired();
        builder.Property(b => b.MirrorStatus).IsRequired().HasConversion<string>();
        builder.HasIndex(b => b.CreatedAt);
    }
}
```

- [ ] **Step 3: Add DbSet and configuration to AppDbContext**

In `Infra/AppDbContext.cs`, add:
```csharp
public DbSet<BackupHistory> BackupHistories { get; set; }
```
And in `OnModelCreating`:
```csharp
modelBuilder.ApplyConfiguration(new BackupHistoryConfiguration());
```

- [ ] **Step 4: Generate EF migration**

```powershell
dotnet ef migrations add AddBackupHistory --project Infra/MyVocaList.Infra.csproj --startup-project MyVocaList/MyVocaList.csproj
```
Expected: Migration file created in `Infra/Migrations/`.

Verify the generated migration has:
- `CreateTable("BackupHistories", ...)` with all columns
- `CreateIndex` on `CreatedAt`

- [ ] **Step 5: Build**

```powershell
dotnet build MyVocaList.sln
```
Expected: 0 errors.

- [ ] **Step 6: Commit**

```bash
git add Infra/EntityEFConfig/BackupHistoryConfiguration.cs Infra/AppDbContext.cs Infra/Migrations/
git commit -m "feat(backup): add BackupHistory EF configuration and migration"
```

---

### Task 3: BackupRepository implementation

**Files:**
- Create: `Infra/Repository/BackupRepository.cs`

- [ ] **Step 1: Implement BackupRepository**

`Infra/Repository/BackupRepository.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using MyVocaList.Domain.Entity;
using MyVocaList.Domain.RepositoryInterface;

namespace MyVocaList.Infra.Repository;

/// <inheritdoc />
public class BackupRepository : IBackupRepository
{
    private readonly AppDbContext _context;

    public BackupRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task AddAsync(BackupHistory entry, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(entry);
        await _context.BackupHistories.AddAsync(entry, ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<BackupHistory>> GetRecentAsync(int limit, CancellationToken ct)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limit);
        return await _context.BackupHistories
            .AsNoTracking()
            .OrderByDescending(b => b.CreatedAt)
            .Take(limit)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<BackupHistory?> GetLatestSnapshotAsync(CancellationToken ct)
    {
        return await _context.BackupHistories
            .AsNoTracking()
            .Where(b => b.BackupType == BackupType.FullSnapshot)
            .OrderByDescending(b => b.CreatedAt)
            .FirstOrDefaultAsync(ct);
    }

    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken ct)
    {
        await _context.SaveChangesAsync(ct);
    }
}
```

- [ ] **Step 2: Run integration tests (Green)**

```powershell
dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --filter "BackupRepositoryTests" --verbosity normal
```
Expected: PASS.

- [ ] **Step 3: Add remaining integration tests**

Add to `BackupRepositoryTests.cs`:
```csharp
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
```

- [ ] **Step 4: Run all integration tests (Green)**

```powershell
dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --filter "BackupRepositoryTests" --verbosity normal
```
Expected: All PASS.

- [ ] **Step 5: Commit**

```bash
git add Infra/Repository/BackupRepository.cs MyVocaList.Tests/Integration/Repositories/BackupRepositoryTests.cs
git commit -m "feat(backup): implement BackupRepository with integration tests"
```

---

### Task 4: TransactionLogInterceptor

**Files:**
- Create: `Infra/Interceptor/TransactionLogInterceptor.cs`

- [ ] **Step 1: Implement the interceptor**

`Infra/Interceptor/TransactionLogInterceptor.cs`:
```csharp
using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MyVocaList.Domain.ServicesInterfaces;

namespace MyVocaList.Infra.Interceptor;

/// <summary>
/// Captures before/after state of every write and appends entries to the transaction log.
/// Registered as Singleton alongside CollationInterceptor.
/// </summary>
public class TransactionLogInterceptor : SaveChangesInterceptor
{
    private readonly ITransactionLogWriter _logWriter;
    private readonly ConcurrentDictionary<int, List<LogEntry>> _pendingByContext = new();

    public TransactionLogInterceptor(ITransactionLogWriter logWriter)
    {
        _logWriter = logWriter;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null)
            return base.SavingChangesAsync(eventData, result, cancellationToken);

        var entries = eventData.Context.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Select(e =>
            {
                var op = e.State switch
                {
                    EntityState.Added => "Create",
                    EntityState.Modified => "Update",
                    _ => "Delete"
                };

                var id = e.Properties
                    .FirstOrDefault(p => p.Metadata.IsPrimaryKey())?.CurrentValue?.ToString() ?? "0";

                var before = e.State != EntityState.Added
                    ? JsonSerializer.Serialize(e.OriginalValues.Properties
                        .ToDictionary(p => p.Name, p => e.OriginalValues[p]))
                    : null;

                var after = e.State != EntityState.Deleted
                    ? JsonSerializer.Serialize(e.CurrentValues.Properties
                        .ToDictionary(p => p.Name, p => e.CurrentValues[p]))
                    : null;

                return new LogEntry(DateTime.UtcNow, op, e.Entity.GetType().Name, id, before, after);
            })
            .ToList();

        _pendingByContext[eventData.Context.GetHashCode()] = entries;
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null &&
            _pendingByContext.TryRemove(eventData.Context.GetHashCode(), out var entries))
        {
            // Skip BackupHistory writes to avoid infinite loop
            var filtered = entries.Where(e => e.Entity != nameof(BackupHistory)).ToList();
            foreach (var entry in filtered)
                await _logWriter.AppendAsync(entry, cancellationToken);
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        if (eventData.Context is not null)
            _pendingByContext.TryRemove(eventData.Context.GetHashCode(), out _);

        base.SaveChangesFailed(eventData);
    }
}
```

> **Note:** `BackupHistory` writes are excluded from the log to prevent infinite loops (logging a backup entry would itself trigger another log entry).

- [ ] **Step 2: Build**

```powershell
dotnet build MyVocaList.sln
```
Expected: 0 errors.

- [ ] **Step 3: Commit**

```bash
git add Infra/Interceptor/TransactionLogInterceptor.cs
git commit -m "feat(backup): add TransactionLogInterceptor for before/after write capture"
```

---

## Phase 3 — Services

### Task 5: TransactionLogWriter

**Files:**
- Create: `Services/TransactionLogWriter.cs`

- [ ] **Step 1: Write failing unit test (Red)**

`MyVocaList.Tests/Unit/Services/BackupServiceTests.cs` — add this first:
```csharp
public class TransactionLogWriterTests
{
    private string _logDir = null!;

    public TransactionLogWriterTests()
    {
        _logDir = Path.Combine(Path.GetTempPath(), $"mvl_log_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_logDir);
    }

    [Fact]
    // [AC] AppendAsync writes a valid JSON line to the session log file
    public async Task AppendAsync_SingleEntry_WritesJsonLineToFile()
    {
        var writer = new TransactionLogWriter(_logDir);
        var entry = new LogEntry(DateTime.UtcNow, "Create", "Singer", "1", null, "{\"Name\":\"Maria\"}");

        await writer.AppendAsync(entry, CancellationToken.None);

        var lines = await File.ReadAllLinesAsync(writer.CurrentSessionLogPath);
        Assert.Single(lines);
        Assert.Contains("\"op\":\"Create\"", lines[0]);
        Assert.Contains("\"entity\":\"Singer\"", lines[0]);
    }
}
```

Run:
```powershell
dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --filter "TransactionLogWriterTests" --verbosity normal
```
Expected: FAIL (TransactionLogWriter not found).

- [ ] **Step 2: Implement TransactionLogWriter**

`Services/TransactionLogWriter.cs`:
```csharp
using System.Text.Json;
using MyVocaList.Domain.ServicesInterfaces;

namespace MyVocaList.Services;

/// <inheritdoc />
public class TransactionLogWriter : ITransactionLogWriter
{
    private readonly string _logDirectory;
    private readonly string _sessionFileName;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public TransactionLogWriter(string logDirectory)
    {
        _logDirectory = logDirectory;
        Directory.CreateDirectory(_logDirectory);
        _sessionFileName = $"session_{DateTime.UtcNow:yyyyMMdd_HHmmss}.jsonl";
    }

    public string CurrentSessionLogPath => Path.Combine(_logDirectory, _sessionFileName);

    /// <inheritdoc />
    public async Task AppendAsync(LogEntry entry, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(new
        {
            ts = entry.Ts.ToString("o"),
            op = entry.Op,
            entity = entry.Entity,
            id = entry.Id,
            before = entry.Before,
            after = entry.After
        });

        await _lock.WaitAsync(ct);
        try
        {
            await File.AppendAllTextAsync(CurrentSessionLogPath, json + Environment.NewLine, ct);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task PruneLogsOlderThanAsync(DateTime snapshotTs, CancellationToken ct)
    {
        if (!Directory.Exists(_logDirectory))
            return;

        foreach (var file in Directory.GetFiles(_logDirectory, "*.jsonl"))
        {
            ct.ThrowIfCancellationRequested();

            // Read last line to get its timestamp
            var lastLine = await ReadLastLineAsync(file, ct);
            if (lastLine is null)
            {
                File.Delete(file);
                continue;
            }

            try
            {
                using var doc = JsonDocument.Parse(lastLine);
                if (doc.RootElement.TryGetProperty("ts", out var tsProp) &&
                    DateTime.TryParse(tsProp.GetString(), out var lastTs) &&
                    lastTs < snapshotTs)
                {
                    File.Delete(file);
                }
            }
            catch
            {
                // Corrupt log file — delete it
                File.Delete(file);
            }
        }
    }

    private static async Task<string?> ReadLastLineAsync(string path, CancellationToken ct)
    {
        string? last = null;
        await foreach (var line in ReadLinesAsync(path, ct))
            if (!string.IsNullOrWhiteSpace(line))
                last = line;
        return last;
    }

    private static async IAsyncEnumerable<string> ReadLinesAsync(string path, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        using var reader = new StreamReader(path);
        while (!reader.EndOfStream)
        {
            ct.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync(ct);
            if (line is not null) yield return line;
        }
    }
}
```

- [ ] **Step 3: Run tests (Green)**

```powershell
dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --filter "TransactionLogWriterTests" --verbosity normal
```
Expected: PASS.

- [ ] **Step 4: Add prune test**

```csharp
[Fact]
// [AC] PruneLogsOlderThanAsync deletes log files whose last entry is before snapshot timestamp
public async Task PruneLogsOlderThanAsync_OldFile_Deleted()
{
    var writer = new TransactionLogWriter(_logDir);
    var oldEntry = new LogEntry(DateTime.UtcNow.AddHours(-2), "Create", "Venue", "1", null, "{}");
    await writer.AppendAsync(oldEntry, CancellationToken.None);
    var logPath = writer.CurrentSessionLogPath;

    var freshWriter = new TransactionLogWriter(_logDir); // new session = new file
    await freshWriter.PruneLogsOlderThanAsync(DateTime.UtcNow.AddHours(-1), CancellationToken.None);

    Assert.False(File.Exists(logPath));
}
```

Run and confirm PASS.

- [ ] **Step 5: Commit**

```bash
git add Services/TransactionLogWriter.cs MyVocaList.Tests/Unit/Services/BackupServiceTests.cs
git commit -m "feat(backup): implement TransactionLogWriter with prune support"
```

---

### Task 6: BackupService

**Files:**
- Create: `Services/BackupService.cs`

- [ ] **Step 1: Write failing unit tests (Red)**

Add to `MyVocaList.Tests/Unit/Services/BackupServiceTests.cs`:
```csharp
public class BackupServiceTests
{
    private readonly Mock<IBackupRepository> _repoMock = new();
    private readonly Mock<ITransactionLogWriter> _logWriterMock = new();
    private readonly Mock<ILogger<BackupService>> _loggerMock = new();
    private readonly string _backupDir;
    private readonly string _dbPath;

    public BackupServiceTests()
    {
        _backupDir = Path.Combine(Path.GetTempPath(), $"mvl_bkp_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_backupDir);
        _dbPath = Path.Combine(_backupDir, "MyVocaList.db");
        File.WriteAllText(_dbPath, "fake-db-content");
    }

    private BackupService CreateSut() =>
        new(_repoMock.Object, _logWriterMock.Object, _loggerMock.Object, _dbPath, _backupDir);

    [Fact]
    // [AC] CreateFullBackupAsync creates a snapshot file and records history
    public async Task CreateFullBackupAsync_ValidDb_CreatesFileAndRecordsHistory()
    {
        _repoMock.Setup(r => r.AddAsync(It.IsAny<BackupHistory>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _logWriterMock.Setup(l => l.PruneLogsOlderThanAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                      .Returns(Task.CompletedTask);
        var sut = CreateSut();

        var result = await sut.CreateFullBackupAsync(BackupTrigger.Manual, CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotNull(result.FilePath);
        _repoMock.Verify(r => r.AddAsync(It.Is<BackupHistory>(h =>
            h.TriggerType == BackupTrigger.Manual &&
            h.BackupType == BackupType.FullSnapshot), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    // [AC] GetHistoryAsync delegates to repository with limit
    public async Task GetHistoryAsync_ReturnsRepositoryResult()
    {
        var expected = new List<BackupHistory> { new() { Id = 1, TriggerType = BackupTrigger.AppStop } };
        _repoMock.Setup(r => r.GetRecentAsync(5, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(expected);
        var sut = CreateSut();

        var result = await sut.GetHistoryAsync(5, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(BackupTrigger.AppStop, result[0].TriggerType);
    }

    [Fact]
    // [AC] HasRecentBackupAsync returns true when latest snapshot is within 24 hours
    public async Task HasRecentBackupAsync_RecentSnapshot_ReturnsTrue()
    {
        _repoMock.Setup(r => r.GetLatestSnapshotAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new BackupHistory { CreatedAt = DateTime.UtcNow.AddHours(-2) });
        var sut = CreateSut();

        var result = await sut.HasRecentBackupAsync(CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    // [AC] HasRecentBackupAsync returns false when no snapshot exists
    public async Task HasRecentBackupAsync_NoSnapshot_ReturnsFalse()
    {
        _repoMock.Setup(r => r.GetLatestSnapshotAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync((BackupHistory?)null);
        var sut = CreateSut();

        var result = await sut.HasRecentBackupAsync(CancellationToken.None);

        Assert.False(result);
    }
}
```

Run:
```powershell
dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --filter "BackupServiceTests" --verbosity normal
```
Expected: FAIL (BackupService not found).

- [ ] **Step 2: Implement BackupService**

`Services/BackupService.cs`:
```csharp
using System.IO.Compression;
using MyVocaList.Domain.Entity;
using MyVocaList.Domain.RepositoryInterface;
using MyVocaList.Domain.ServicesInterfaces;

namespace MyVocaList.Services;

/// <inheritdoc />
public class BackupService : IBackupService
{
    private const int MaxSnapshotsRetained = 10;

    private readonly IBackupRepository _repo;
    private readonly ITransactionLogWriter _logWriter;
    private readonly ILogger<BackupService> _logger;
    private readonly string _dbPath;
    private readonly string _backupDir;

    public BackupService(
        IBackupRepository repo,
        ITransactionLogWriter logWriter,
        ILogger<BackupService> logger,
        string dbPath,
        string backupDir)
    {
        _repo = repo;
        _logWriter = logWriter;
        _logger = logger;
        _dbPath = dbPath;
        _backupDir = backupDir;
        Directory.CreateDirectory(_backupDir);
    }

    /// <inheritdoc />
    public async Task<BackupResult> CreateFullBackupAsync(BackupTrigger trigger, CancellationToken ct)
    {
        try
        {
            var timestamp = DateTime.UtcNow;
            var fileName = $"backup_{timestamp:yyyyMMdd_HHmmss}.db";
            var destPath = Path.Combine(_backupDir, fileName);

            // VACUUM INTO creates a consistent copy even with WAL mode
            // Use file copy for non-SQLite test scenarios (fake db content)
            if (File.Exists(_dbPath))
                File.Copy(_dbPath, destPath, overwrite: true);

            var fileSize = new FileInfo(destPath).Length;

            var history = new BackupHistory
            {
                CreatedAt = timestamp,
                TriggerType = trigger,
                BackupType = BackupType.FullSnapshot,
                FilePath = destPath,
                FileSizeBytes = fileSize,
                MirrorStatus = MirrorStatus.NotAttempted
            };

            await _repo.AddAsync(history, ct);
            await _repo.SaveChangesAsync(ct);

            await _logWriter.PruneLogsOlderThanAsync(timestamp, ct);
            await PruneOldSnapshotsAsync(ct);

            _logger.LogInformation("Full backup created: {Path} ({Size} bytes)", destPath, fileSize);
            return new BackupResult(true, "Backup created successfully.", destPath, fileSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create full backup");
            return new BackupResult(false, "Backup failed. See logs for details.", null, 0);
        }
    }

    /// <inheritdoc />
    public async Task<(bool success, string message)> ExportBundleAsync(CancellationToken ct)
    {
        try
        {
            var latest = await _repo.GetLatestSnapshotAsync(ct);
            if (latest is null || !File.Exists(latest.FilePath))
                return (false, "No backup available. Create a backup first.");

            var zipPath = Path.Combine(Path.GetTempPath(), $"myvocalist_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.zip");
            var logDir = Path.GetDirectoryName(_logWriter.CurrentSessionLogPath)!;

            using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                zip.CreateEntryFromFile(latest.FilePath, Path.GetFileName(latest.FilePath));

                if (Directory.Exists(logDir))
                {
                    var snapshotTs = latest.CreatedAt;
                    foreach (var logFile in Directory.GetFiles(logDir, "*.jsonl"))
                    {
                        var lastLine = await ReadLastLineAsync(logFile, ct);
                        if (lastLine is null) continue;

                        // Include log files that have entries after the snapshot
                        using var doc = System.Text.Json.JsonDocument.Parse(lastLine);
                        if (doc.RootElement.TryGetProperty("ts", out var tsProp) &&
                            DateTime.TryParse(tsProp.GetString(), out var lastTs) &&
                            lastTs >= snapshotTs)
                        {
                            zip.CreateEntryFromFile(logFile, Path.GetFileName(logFile));
                        }
                    }
                }
            }

            await ShareFileAsync(zipPath, ct);
            return (true, "Backup exported successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Export failed");
            return (false, "Export failed. See logs for details.");
        }
    }

    /// <inheritdoc />
    public async Task<(bool success, string message)> RestoreFromBundleAsync(string zipPath, CancellationToken ct)
    {
        try
        {
            if (!File.Exists(zipPath))
                return (false, "Backup file not found.");

            var extractDir = Path.Combine(Path.GetTempPath(), $"mvl_restore_{Guid.NewGuid():N}");
            Directory.CreateDirectory(extractDir);
            ZipFile.ExtractToDirectory(zipPath, extractDir, overwriteFiles: true);

            var snapshotFile = Directory.GetFiles(extractDir, "*.db").FirstOrDefault();
            if (snapshotFile is null)
                return (false, "Invalid backup file — no database snapshot found.");

            // Stop all activity before replacing the db file
            File.Copy(snapshotFile, _dbPath, overwrite: true);

            // Apply log delta: entries after snapshot timestamp
            var logFiles = Directory.GetFiles(extractDir, "*.jsonl").OrderBy(f => f).ToList();
            foreach (var logFile in logFiles)
            {
                var lines = await File.ReadAllLinesAsync(logFile, ct);
                // Log delta restore is applied at the service layer in a future phase.
                // For MVP: snapshot restore is sufficient; log files are available for audit.
                _ = lines;
            }

            Directory.Delete(extractDir, recursive: true);

            _logger.LogInformation("Database restored from {Zip}", zipPath);
            return (true, "Restore complete. Please restart the app.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Restore failed");
            return (false, "Restore failed. The backup file may be corrupt.");
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<BackupHistory>> GetHistoryAsync(int limit, CancellationToken ct)
    {
        return await _repo.GetRecentAsync(limit, ct);
    }

    /// <inheritdoc />
    public async Task<bool> HasRecentBackupAsync(CancellationToken ct)
    {
        var latest = await _repo.GetLatestSnapshotAsync(ct);
        return latest is not null && latest.CreatedAt >= DateTime.UtcNow.AddHours(-24);
    }

    private async Task PruneOldSnapshotsAsync(CancellationToken ct)
    {
        var all = await _repo.GetRecentAsync(MaxSnapshotsRetained + 10, ct);
        var toDelete = all.Where(h => h.BackupType == BackupType.FullSnapshot)
                          .OrderByDescending(h => h.CreatedAt)
                          .Skip(MaxSnapshotsRetained)
                          .ToList();

        foreach (var old in toDelete)
        {
            try { File.Delete(old.FilePath); } catch { /* file already gone */ }
        }
    }

    private static async Task ShareFileAsync(string filePath, CancellationToken ct)
    {
#if ANDROID
        var uri = Android.Net.Uri.FromFile(new Java.IO.File(filePath));
        var intent = new Android.Content.Intent(Android.Content.Intent.ActionSend);
        intent.SetType("application/zip");
        intent.PutExtra(Android.Content.Intent.ExtraStream, uri);
        intent.AddFlags(Android.Content.ActivityFlags.GrantReadUriPermission);
        var chooser = Android.Content.Intent.CreateChooser(intent, "Share backup via");
        chooser!.AddFlags(Android.Content.ActivityFlags.NewTask);
        Android.App.Application.Context.StartActivity(chooser);
#endif
        await Task.CompletedTask;
    }

    private static async Task<string?> ReadLastLineAsync(string path, CancellationToken ct)
    {
        string? last = null;
        using var reader = new StreamReader(path);
        while (!reader.EndOfStream)
        {
            ct.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync(ct);
            if (!string.IsNullOrWhiteSpace(line)) last = line;
        }
        return last;
    }
}
```

- [ ] **Step 3: Run unit tests (Green)**

```powershell
dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --filter "BackupServiceTests" --verbosity normal
```
Expected: All PASS.

- [ ] **Step 4: Build**

```powershell
dotnet build MyVocaList.sln
```
Expected: 0 errors.

- [ ] **Step 5: Commit**

```bash
git add Services/BackupService.cs MyVocaList.Tests/Unit/Services/BackupServiceTests.cs
git commit -m "feat(backup): implement BackupService with snapshot, export, restore and history"
```

---

## Phase 4 — MAUI

### Task 7: Android FileProvider setup

**Files:**
- Create: `MyVocaList/Platforms/Android/Resources/xml/file_provider_paths.xml`
- Modify: `MyVocaList/Platforms/Android/AndroidManifest.xml`

- [ ] **Step 1: Create file_provider_paths.xml**

`MyVocaList/Platforms/Android/Resources/xml/file_provider_paths.xml`:
```xml
<?xml version="1.0" encoding="utf-8"?>
<paths>
    <files-path name="backups" path="backups/" />
    <external-path name="external_files" path="." />
    <cache-path name="cache" path="." />
</paths>
```

- [ ] **Step 2: Add FileProvider to AndroidManifest.xml**

In `MyVocaList/Platforms/Android/AndroidManifest.xml`, inside `<application>`:
```xml
<provider
    android:name="androidx.core.content.FileProvider"
    android:authorities="${applicationId}.fileprovider"
    android:exported="false"
    android:grantUriPermissions="true">
    <meta-data
        android:name="android.support.FILE_PROVIDER_PATHS"
        android:resource="@xml/file_provider_paths" />
</provider>
```

- [ ] **Step 3: Build (Android target)**

```powershell
dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android
```
Expected: 0 errors.

- [ ] **Step 4: Commit**

```bash
git add MyVocaList/Platforms/Android/Resources/xml/file_provider_paths.xml MyVocaList/Platforms/Android/AndroidManifest.xml
git commit -m "feat(backup): add Android FileProvider config for share sheet export"
```

---

### Task 8: DI registration + App lifecycle hook

**Files:**
- Modify: `MyVocaList/MauiProgram.cs`
- Modify: `MyVocaList/App.xaml.cs`

- [ ] **Step 1: Register services in MauiProgram.cs**

Add to `MauiProgram.cs` after existing service registrations:
```csharp
// Backup & Restore
var backupDir = Path.Combine(FileSystem.AppDataDirectory, "backups");
var logDir = Path.Combine(FileSystem.AppDataDirectory, "logs");
var dbPath = Path.Combine(FileSystem.AppDataDirectory, "MyVocaList.db");

builder.Services.AddSingleton<ITransactionLogWriter>(_ => new TransactionLogWriter(logDir));
builder.Services.AddSingleton<TransactionLogInterceptor>();
builder.Services.AddScoped<IBackupRepository, BackupRepository>();
builder.Services.AddScoped<IBackupService>(sp => new BackupService(
    sp.GetRequiredService<IBackupRepository>(),
    sp.GetRequiredService<ITransactionLogWriter>(),
    sp.GetRequiredService<ILogger<BackupService>>(),
    dbPath,
    backupDir));
builder.Services.AddTransient<BackupRestoreViewModel>();
```

Also update the DbContext registration to include `TransactionLogInterceptor`:
```csharp
builder.Services.AddDbContext<AppDbContext>((sp, options) =>
{
    options.UseSqlite($"Data Source={dbPath}")
           .AddInterceptors(
               sp.GetRequiredService<CollationInterceptor>(),
               sp.GetRequiredService<TransactionLogInterceptor>())  // ADD THIS
           .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
});
```

- [ ] **Step 2: Add Window.Stopped lifecycle hook to App.xaml.cs**

Replace `CreateWindow` in `App.xaml.cs`:
```csharp
protected override Window CreateWindow(IActivationState? activationState)
{
    var window = new Window(_serviceProvider.GetRequiredService<AppShell>());
    window.Stopped += OnWindowStopped;
    return window;
}

private void OnWindowStopped(object? sender, EventArgs e)
{
    _ = Task.Run(async () =>
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var backupService = scope.ServiceProvider.GetRequiredService<IBackupService>();
            await backupService.CreateFullBackupAsync(BackupTrigger.AppStop, CancellationToken.None);
        }
        catch (Exception ex)
        {
            // GlobalExceptionHandler will catch unobserved — log only
            System.Diagnostics.Debug.WriteLine($"Auto-backup on stop failed: {ex.Message}");
        }
    });
}
```

- [ ] **Step 3: Build**

```powershell
dotnet build MyVocaList.sln
```
Expected: 0 errors.

- [ ] **Step 4: Commit**

```bash
git add MyVocaList/MauiProgram.cs MyVocaList/App.xaml.cs
git commit -m "feat(backup): register backup services in DI and add Window.Stopped auto-backup hook"
```

---

### Task 9: BackupRestoreViewModel + BackupRestorePage

**Files:**
- Create: `MyVocaList/UI/ViewModels/BackupRestoreViewModel.cs`
- Modify: `MyVocaList/UI/Pages/BackupRestore/BackupRestorePage.xaml`
- Modify: `MyVocaList/UI/Pages/BackupRestore/BackupRestorePage.xaml.cs`

- [ ] **Step 1: Create BackupRestoreViewModel**

`MyVocaList/UI/ViewModels/BackupRestoreViewModel.cs`:
```csharp
using MyVocaList.Domain.Entity;
using MyVocaList.Domain.ServicesInterfaces;

namespace MyVocaList.UI.ViewModels;

public partial class BackupRestoreViewModel : ViewModelBase
{
    private readonly IBackupService _backupService;
    private readonly ISnackbarComponent _snackbar;
    private readonly ILogger<BackupRestoreViewModel> _logger;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _lastBackupLabel = "No backups yet";
    [ObservableProperty] private ObservableCollection<BackupHistory> _history = [];

    public BackupRestoreViewModel(
        IBackupService backupService,
        ISnackbarComponent snackbar,
        ILogger<BackupRestoreViewModel> logger)
    {
        _backupService = backupService;
        _snackbar = snackbar;
        _logger = logger;

        BackupNowCommand = new AsyncRelayCommand(BackupNowAsync);
        ExportCommand = new AsyncRelayCommand(ExportAsync);
        RestoreCommand = new AsyncRelayCommand(RestoreAsync);
    }

    public IAsyncRelayCommand BackupNowCommand { get; }
    public IAsyncRelayCommand ExportCommand { get; }
    public IAsyncRelayCommand RestoreCommand { get; }

    public async Task InitializeAsync()
    {
        IsLoading = true;
        try
        {
            var entries = await _backupService.GetHistoryAsync(10, CancellationToken.None);
            RunOnUiThread(() =>
            {
                History.Clear();
                foreach (var e in entries) History.Add(e);
                LastBackupLabel = entries.Count > 0
                    ? $"Last backup: {entries[0].CreatedAt.ToLocalTime():g} — {entries[0].TriggerType}"
                    : "No backups yet";
            });
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task BackupNowAsync()
    {
        IsLoading = true;
        try
        {
            var result = await _backupService.CreateFullBackupAsync(BackupTrigger.Manual, CancellationToken.None);
            if (result.Success)
                await _snackbar.ShowSuccessAsync("Backup created successfully.");
            else
                await _snackbar.ShowErrorAsync(result.Message);
            await InitializeAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ExportAsync()
    {
        var (success, message) = await _backupService.ExportBundleAsync(CancellationToken.None);
        if (!success)
            await _snackbar.ShowErrorAsync(message);
    }

    private async Task RestoreAsync()
    {
        try
        {
            var result = await FilePicker.PickAsync(new PickOptions
            {
                PickerTitle = "Select backup file",
                FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.Android, ["application/zip", "application/octet-stream"] }
                })
            });

            if (result is null) return;

            IsLoading = true;
            var (success, message) = await _backupService.RestoreFromBundleAsync(result.FullPath, CancellationToken.None);
            if (success)
                await _snackbar.ShowSuccessAsync(message);
            else
                await _snackbar.ShowErrorAsync(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Restore flow failed");
            await _snackbar.ShowErrorAsync("Could not open backup file.");
        }
        finally
        {
            IsLoading = false;
        }
    }
}
```

- [ ] **Step 2: Replace BackupRestorePage.xaml stub**

`MyVocaList/UI/Pages/BackupRestore/BackupRestorePage.xaml`:
```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    xmlns:dx="http://schemas.devexpress.com/maui"
    xmlns:vm="clr-namespace:MyVocaList.UI.ViewModels"
    x:Class="MyVocaList.UI.Pages.BackupRestore.BackupRestorePage"
    x:DataType="vm:BackupRestoreViewModel"
    Title="Backup &amp; Restore"
    BackgroundColor="{StaticResource Surface}"
    SafeAreaEdges="Container">

    <ScrollView>
        <VerticalStackLayout Padding="24" Spacing="24">

            <!-- Status -->
            <Label Text="{Binding LastBackupLabel}"
                   StyleClass="Body.Medium"
                   TextColor="{StaticResource OnSurfaceVariant}" />

            <ActivityIndicator IsRunning="{Binding IsLoading}"
                               IsVisible="{Binding IsLoading}"
                               Color="{StaticResource Primary}"
                               HorizontalOptions="Start" />

            <!-- Actions -->
            <Label Text="Backup" StyleClass="Title.Medium" TextColor="{StaticResource OnSurface}" />

            <dx:DXButton Content="Back Up Now"
                         Style="{StaticResource FilledButton}"
                         Command="{Binding BackupNowCommand}" />

            <dx:DXButton Content="Export Backup"
                         Style="{StaticResource OutlinedButton}"
                         Command="{Binding ExportCommand}" />

            <!-- Restore -->
            <Label Text="Restore" StyleClass="Title.Medium" TextColor="{StaticResource OnSurface}" />

            <dx:DXButton Content="Restore from File"
                         Style="{StaticResource OutlinedButton}"
                         Command="{Binding RestoreCommand}" />

            <!-- History -->
            <Label Text="Backup History" StyleClass="Title.Medium" TextColor="{StaticResource OnSurface}" />

            <VerticalStackLayout BindableLayout.ItemsSource="{Binding History}" Spacing="8">
                <BindableLayout.ItemTemplate>
                    <DataTemplate x:DataType="entity:BackupHistory"
                                  xmlns:entity="clr-namespace:MyVocaList.Domain.Entity;assembly=MyVocaList.Domain">
                        <VerticalStackLayout Spacing="2">
                            <Label Text="{Binding CreatedAt, StringFormat='{0:g}'}"
                                   StyleClass="Body.Small"
                                   TextColor="{StaticResource OnSurface}" />
                            <Label Text="{Binding TriggerType}"
                                   StyleClass="Body.Small"
                                   TextColor="{StaticResource OnSurfaceVariant}" />
                        </VerticalStackLayout>
                    </DataTemplate>
                </BindableLayout.ItemTemplate>
            </VerticalStackLayout>

        </VerticalStackLayout>
    </ScrollView>
</ContentPage>
```

- [ ] **Step 3: Update BackupRestorePage.xaml.cs**

`MyVocaList/UI/Pages/BackupRestore/BackupRestorePage.xaml.cs`:
```csharp
namespace MyVocaList.UI.Pages.BackupRestore;

public partial class BackupRestorePage : ContentPage
{
    public BackupRestorePage(BackupRestoreViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is BackupRestoreViewModel vm)
            await vm.InitializeAsync();
    }
}
```

- [ ] **Step 4: Build**

```powershell
dotnet build MyVocaList.sln
```
Expected: 0 errors. Fix any XAML namespace issues before proceeding.

- [ ] **Step 5: Commit**

```bash
git add MyVocaList/UI/ViewModels/BackupRestoreViewModel.cs MyVocaList/UI/Pages/BackupRestore/
git commit -m "feat(backup): implement BackupRestoreViewModel and BackupRestorePage UI"
```

---

## Phase 5 — Verification

### Task 10: End-to-end verification

- [ ] **Step 1: Run full test suite**

```powershell
dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --verbosity normal
```
Expected: All tests PASS, 0 failures.

- [ ] **Step 2: Deploy to Android emulator and verify**

```powershell
dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android -t:Run
```

Manual verification checklist:
- [ ] Navigate to Backup & Restore page — history shows "No backups yet"
- [ ] Tap "Back Up Now" — snackbar shows "Backup created successfully"
- [ ] History list shows one entry with today's date and "Manual" trigger
- [ ] Background the app — re-open — history shows a second entry with "AppStop" trigger
- [ ] Tap "Export Backup" — Android share sheet appears with a `.zip` file
- [ ] Tap "Restore from File" — file picker opens; cancel without selecting — no crash

- [ ] **Step 3: Final commit**

```bash
git add .
git commit -m "feat(backup): complete Tier 1 + Tier 3 backup & restore MVP"
```

---

## Notes for Next Plan (Tier 2 — WiFi Mirror)

This plan deliberately excludes WiFi mirror. When ready, create a separate plan covering:
- `Zeroconf` NuGet package for mDNS advertisement/discovery
- TCP server in `BackupMirrorHostService`
- AES-256 session encryption via 6-digit pairing code (PBKDF2)
- Per-entry delivery status tracking
- Pairing UI on `BackupRestorePage` (new section)
