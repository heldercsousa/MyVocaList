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
}
