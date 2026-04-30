using MyVocaList.Domain.Entity;

namespace MyVocaList.Domain.RepositoryInterface;

public interface IArtistRepository
{
    /// <summary>Returns a paged list of artists with their song counts.</summary>
    Task<(IEnumerable<(Artist artist, int songCount)> items, int totalCount)> GetPagedAsync(
        int pageNumber, int pageSize, string normalizedQuery, CancellationToken ct);

    /// <summary>Returns artists whose normalized name starts with the normalized query (max results).</summary>
    Task<IEnumerable<Artist>> SearchByNameAsync(string normalizedQuery, int maxResults, CancellationToken ct);

    Task<Artist> GetByIdAsync(int id, CancellationToken ct);
    Task<Artist> GetByExternalIdAsync(string externalId, string provider, CancellationToken ct);
    Task<bool> ExistsByNameAsync(string normalizedName, CancellationToken ct);
    Task<bool> ExistsByNameAsync(string normalizedName, int excludeId, CancellationToken ct);
    Task AddAsync(Artist artist, CancellationToken ct);
    Task UpdateAsync(Artist artist, CancellationToken ct);
    Task DeleteAsync(IEnumerable<int> ids, CancellationToken ct);
}
