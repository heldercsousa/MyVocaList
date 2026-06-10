namespace MyVocaList.Domain.Interfaces;

using MyVocaList.Domain.Entities;

public interface IQueueRepository
{
    Task<QueueEntry?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<IEnumerable<QueueEntry>> GetByEventIdAsync(int eventId, CancellationToken cancellationToken);

    Task AddAsync(QueueEntry entity, CancellationToken cancellationToken);

    Task UpdateAsync(QueueEntry entity, CancellationToken cancellationToken);

    Task DeleteAsync(int id, CancellationToken cancellationToken);

    Task ReorderAsync(IEnumerable<QueueEntry> entries, CancellationToken cancellationToken);
}
