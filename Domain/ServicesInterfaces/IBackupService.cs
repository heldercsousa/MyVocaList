using MyVocaList.Domain.Entity;
using MyVocaList.Domain.UnitOfWork;

namespace MyVocaList.Domain.ServicesInterfaces;

/// <remarks>Implements <see cref="IUnitOfWorkOutcome"/> so
/// <c>CreateFullBackupAsync</c>'s wrap in <see cref="IUnitOfWork.ExecuteAsync{TResult}"/> can
/// recognise the success/failure signal (REQ-UOW-24/25/27).</remarks>
public record BackupResult(bool Success, string Message, string? FilePath, long FileSizeBytes)
    : IUnitOfWorkOutcome;

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
