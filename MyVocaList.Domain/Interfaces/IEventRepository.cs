namespace MyVocaList.Domain.Interfaces;

using MyVocaList.Domain.Entities;

public interface IEventRepository
{
    Task<Event?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<(IEnumerable<Event> items, int totalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? query = null,
        CancellationToken cancellationToken = default);

    Task AddAsync(Event entity, CancellationToken cancellationToken);

    Task UpdateAsync(Event entity, CancellationToken cancellationToken);

    Task DeleteAsync(int id, CancellationToken cancellationToken);

    Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken);
}
