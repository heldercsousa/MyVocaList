using MyVocaList.Contracts.DTOs.List;
using MyVocaList.Domain.Entity;

namespace MyVocaList.Domain.RepositoryInterface;

public interface ICatalogRepository
{
    /// <summary>Songs in a specific artist's Catalog (their performance repertoire).</summary>
    Task<(IEnumerable<SongListItemDto> items, int totalCount)> GetPagedByArtistAsync(
        int artistId, int pageNumber, int pageSize, string? normalizedQuery, CancellationToken ct);

    Task<int> CountByArtistAsync(int artistId, CancellationToken ct);

    Task<bool> ExistsAsync(int artistId, int songId, CancellationToken ct);

    Task AddAsync(Catalog entry, CancellationToken ct);

    Task RemoveAsync(int artistId, int songId, CancellationToken ct);

    Task SaveChangesAsync(CancellationToken ct = default);
}
