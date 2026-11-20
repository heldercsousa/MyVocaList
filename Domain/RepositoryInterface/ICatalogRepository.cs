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

    /// <summary>
    /// REQ-CATPROP-06: true when the artist's catalog row for the song is derived,
    /// i.e. the artist authored the song (<c>Song.ArtistId == artistId</c>). False for a cover or a missing row.
    /// </summary>
    Task<bool> IsDerivedAsync(int artistId, int songId, CancellationToken ct);

    /// <summary>
    /// REQ-CATPROP-07: removes the artist's cover rows among <paramref name="songIds"/> and leaves derived rows.
    /// Duplicate ids count once; ids with no catalog row count in neither total.
    /// </summary>
    /// <returns><c>removed</c> = cover rows deleted, <c>kept</c> = derived rows skipped.</returns>
    Task<(int removed, int kept)> RemoveCoversAsync(int artistId, IReadOnlyCollection<int> songIds, CancellationToken ct);
}
