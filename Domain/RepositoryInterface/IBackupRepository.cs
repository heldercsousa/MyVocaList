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
