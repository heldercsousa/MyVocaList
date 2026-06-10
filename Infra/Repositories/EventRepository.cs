using Microsoft.EntityFrameworkCore;
using MyVocaList.Contracts;
using MyVocaList.Domain.Entities;
using MyVocaList.Domain.Interfaces;
using Event = MyVocaList.Domain.Entities.Event;

namespace MyVocaList.Infra.Repositories;

/// <summary>
/// Repository implementation for Queue Management Event entity
/// </summary>
public sealed class EventRepository : IEventRepository
{
    private readonly AppDbContext _dbContext;

    public EventRepository(AppDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<Event?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await _dbContext.QueueManagementEvents
            .Include(e => e.QueueEntries)
            .Include(e => e.Venue)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<(IEnumerable<Event> items, int totalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? query = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageNumber);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);

        var queryable = _dbContext.QueueManagementEvents.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            queryable = queryable.Where(e => EF.Functions.Like(e.Name, $"%{query}%"));
        }

        var totalCount = await queryable.CountAsync(cancellationToken);

        var items = await queryable
            .OrderByDescending(e => e.ScheduledStartTime)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    /// <inheritdoc />
    public async Task AddAsync(Event entity, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entity);

        entity.CreatedAt = DateTime.UtcNow;
        entity.ModifiedAt = DateTime.UtcNow;

        await _dbContext.QueueManagementEvents.AddAsync(entity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Event entity, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entity);

        entity.ModifiedAt = DateTime.UtcNow;

        _dbContext.QueueManagementEvents.Update(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        await _dbContext.QueueManagementEvents
            .Where(e => e.Id == id)
            .ExecuteDeleteAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return await _dbContext.QueueManagementEvents
            .AnyAsync(e => EF.Functions.Like(e.Name, name), cancellationToken);
    }
}
