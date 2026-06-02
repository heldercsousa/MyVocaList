using MyVocaList.Domain.Entity;

namespace MyVocaList.Domain.RepositoryInterface;

public enum ArtistRoleFilter { All, AuthorsOnly, PerformersOnly }

public interface IArtistRepository
{
    /// <summary>Returns a paged list of artists with their catalog counts.</summary>
    Task<(IEnumerable<(Artist artist, int catalogCount)> items, int totalCount)> GetPagedAsync(
        int pageNumber, int pageSize, string query,
        ArtistRoleFilter roleFilter = ArtistRoleFilter.All, CancellationToken ct = default);

    /// <summary>Returns artists whose name starts with the query (max results).</summary>
    Task<IEnumerable<Artist>> SearchByNameAsync(string query, int maxResults, CancellationToken ct);

    Task<Artist> GetByIdAsync(int id, CancellationToken ct);
    Task<Artist> GetByExternalIdAsync(string externalId, string provider, CancellationToken ct);
    Task<bool> ExistsByNameAsync(string name, CancellationToken ct);
    Task<bool> ExistsByNameAsync(string name, int excludeId, CancellationToken ct);
    Task AddAsync(Artist artist, CancellationToken ct);
    Task UpdateAsync(Artist artist, CancellationToken ct);
    Task DeleteAsync(IEnumerable<int> ids, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct = default);
}
