using MyVocaList.Contracts.DTOs.List;
using MyVocaList.Domain.Entity;

namespace MyVocaList.Domain.RepositoryInterface;

public interface ISongRepository
{
    /// <summary>All songs — global, not scoped to any artist.</summary>
    Task<(IEnumerable<SongListItemDto> items, int totalCount)> GetPagedAsync(
        int pageNumber, int pageSize, string? normalizedQuery, CancellationToken ct);

    Task<Song> GetByIdAsync(int id, CancellationToken ct);
    Task<Song?> GetByExternalIdAsync(string externalId, string provider, CancellationToken ct);
    Task<bool> ExistsByTitleForArtistAsync(int artistId, string normalizedTitle, CancellationToken ct);
    Task<bool> ExistsByTitleForArtistAsync(int artistId, string normalizedTitle, int excludeId, CancellationToken ct);
    Task AddAsync(Song song, CancellationToken ct);
    Task UpdateAsync(Song song, CancellationToken ct);
    Task DeleteAsync(IEnumerable<int> ids, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct = default);
}
