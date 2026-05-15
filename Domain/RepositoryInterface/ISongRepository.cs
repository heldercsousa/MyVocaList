using MyVocaList.Contracts.DTOs.List;
using MyVocaList.Domain.Entity;

namespace MyVocaList.Domain.RepositoryInterface;

public interface ISongRepository
{
    /// <summary>Returns a paged list of songs for a given artist.</summary>
    Task<(IEnumerable<SongListItemDto> items, int totalCount)> GetPagedByArtistAsync(
        int artistId, int pageNumber, int pageSize, string? normalizedQuery, CancellationToken ct);

    /// <summary>Returns songs whose normalized title starts with the query, scoped to an artist (max results).</summary>
    Task<IEnumerable<Song>> SearchByTitleAsync(int artistId, string normalizedQuery, int maxResults, CancellationToken ct);

    Task<Song> GetByIdAsync(int id, CancellationToken ct);
    Task<Song> GetByExternalIdAsync(string externalId, string provider, CancellationToken ct);
    Task<bool> ExistsByTitleForArtistAsync(int artistId, string normalizedTitle, CancellationToken ct);
    Task<bool> ExistsByTitleForArtistAsync(int artistId, string normalizedTitle, int excludeId, CancellationToken ct);
    Task<int> CountByArtistAsync(int artistId, CancellationToken ct);
    Task<int> CountByArtistsAsync(IEnumerable<int> artistIds, CancellationToken ct);
    Task AddAsync(Song song, CancellationToken ct);
    Task UpdateAsync(Song song, CancellationToken ct);
    Task DeleteAsync(IEnumerable<int> ids, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct = default);
}
