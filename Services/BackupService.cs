using Microsoft.Extensions.DependencyInjection;
using MyVocaList.Domain.Entity;
using MyVocaList.Domain.RepositoryInterface;
using MyVocaList.Domain.ServicesInterfaces;
using MyVocaList.Domain.UnitOfWork;
using System.IO.Compression;
using System.Text.Json;

namespace MyVocaList.Services;

/// <inheritdoc />
public class BackupService : IBackupService
{
    private const int MaxSnapshotsRetained = 10;

    private readonly IBackupRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly ITransactionLogWriter _logWriter;
    private readonly ILogger<BackupService> _logger;
    private readonly string _dbPath;
    private readonly string _backupDir;

    public BackupService(
        IBackupRepository repo,
        IUnitOfWork uow,
        ITransactionLogWriter logWriter,
        ILogger<BackupService> logger,
        string dbPath,
        string backupDir)
    {
        _repo = repo;
        _uow = uow;
        _logWriter = logWriter;
        _logger = logger;
        _dbPath = dbPath;
        _backupDir = backupDir;
        Directory.CreateDirectory(_backupDir);
    }

    /// <inheritdoc />
    public Task<BackupResult> CreateFullBackupAsync(BackupTrigger trigger, CancellationToken ct)
        => _uow.ExecuteAsync<BackupResult>(async sp =>
        {
            // REQ-UOW-28: resolved from the lambda's own scope — never the constructor field.
            var backupRepository = sp.GetRequiredService<IBackupRepository>();

            try
            {
                var timestamp = DateTime.UtcNow;
                var fileName = $"backup_{timestamp:yyyyMMdd_HHmmss}.db";
                var destPath = Path.Combine(_backupDir, fileName);

                // Use file copy; VACUUM INTO is applied in Phase 4 via platform-specific EF Core connection
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

                await backupRepository.AddAsync(history, ct);
                // SaveChangesAsync deleted — the single save is owned by IUnitOfWork (REQ-UOW-10).

                await _logWriter.PruneLogsOlderThanAsync(timestamp, ct);
                await PruneOldSnapshotsAsync(backupRepository, ct);

                _logger.LogInformation("Full backup created: {Path} ({Size} bytes)", destPath, fileSize);
                return new BackupResult(true, "Backup created successfully.", destPath, fileSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create full backup");
                return new BackupResult(false, "Backup failed. See logs for details.", null, 0);
            }
        }, ct);

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

                        using var doc = JsonDocument.Parse(lastLine);
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

            File.Copy(snapshotFile, _dbPath, overwrite: true);

            // Log delta restore: log files are available for audit; full replay deferred to future phase
            var logFiles = Directory.GetFiles(extractDir, "*.jsonl").OrderBy(f => f).ToList();
            foreach (var logFile in logFiles)
            {
                var lines = await File.ReadAllLinesAsync(logFile, ct);
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

    private static async Task PruneOldSnapshotsAsync(IBackupRepository backupRepository, CancellationToken ct)
    {
        var all = await backupRepository.GetRecentAsync(MaxSnapshotsRetained + 10, ct);
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
