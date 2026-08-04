using Microsoft.EntityFrameworkCore;
using MyVocaList.Domain.Interfaces;
using QueueEntry = MyVocaList.Domain.Entities.QueueEntry;

namespace MyVocaList.Infra.Repositories;

/// <summary>
/// Repository implementation for QueueEntry entity
/// </summary>
public sealed class QueueRepository : IQueueRepository
{
    private readonly AppDbContext _dbContext;

    public QueueRepository(AppDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<QueueEntry?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await _dbContext.QueueEntries
            .Include(qe => qe.Person)
            .Include(qe => qe.Song)
            .Include(qe => qe.Event)
            .FirstOrDefaultAsync(qe => qe.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<QueueEntry>> GetByEventIdAsync(int eventId, CancellationToken cancellationToken)
    {
        return await _dbContext.QueueEntries
            .Where(qe => qe.EventId == eventId)
            .Include(qe => qe.Person)
            .Include(qe => qe.Song)
            .OrderBy(qe => qe.Position)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(QueueEntry entity, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entity);

        // Auto-assign position: find max position for this event and add 1
        var maxPosition = await _dbContext.QueueEntries
            .Where(qe => qe.EventId == entity.EventId)
            .MaxAsync(qe => (int?)qe.Position, cancellationToken) ?? 0;

        entity.Position = maxPosition + 1;
        entity.CreatedAt = DateTime.UtcNow;
        entity.ModifiedAt = DateTime.UtcNow;

        await _dbContext.QueueEntries.AddAsync(entity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(QueueEntry entity, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entity);

        entity.ModifiedAt = DateTime.UtcNow;

        _dbContext.QueueEntries.Update(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        await _dbContext.QueueEntries
            .Where(qe => qe.Id == id)
            .ExecuteDeleteAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task ReorderAsync(IEnumerable<QueueEntry> entries, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entries);

        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            foreach (var entry in entries)
            {
                entry.ModifiedAt = DateTime.UtcNow;
                _dbContext.QueueEntries.Update(entry);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
