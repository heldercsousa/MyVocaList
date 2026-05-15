using MyVocaList.Contracts.DTOs.List;

namespace MyVocaList.Domain.ServicesInterfaces;

public interface ICatalogService
{
    Task<(IEnumerable<SongListItemDto> items, int totalCount)> GetPagedCatalogForArtistAsync(
        int artistId, int pageNumber, int pageSize, string? query = null, CancellationToken ct = default);

    Task<(bool success, string message)> AddSongToCatalogAsync(
        int artistId, int songId, CancellationToken ct = default);

    Task<(bool success, string message)> RemoveSongFromCatalogAsync(
        int artistId, int songId, CancellationToken ct = default);
}
