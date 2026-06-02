using MyVocaList.Contracts.DTOs.List;
using MyVocaList.Domain.Entity;
using MyVocaList.Domain.RepositoryInterface;
using MyVocaList.Domain.ServicesInterfaces;

namespace MyVocaList.Services;

public class CatalogService : ICatalogService
{
    private readonly ICatalogRepository _catalogRepository;
    private readonly ILogger<CatalogService> _logger;

    public CatalogService(ICatalogRepository catalogRepository, ILogger<CatalogService> logger)
    {
        _catalogRepository = catalogRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<(IEnumerable<SongListItemDto> items, int totalCount)> GetPagedCatalogForArtistAsync(
        int artistId, int pageNumber, int pageSize, string? query = null, CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageNumber);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);

        return await _catalogRepository.GetPagedByArtistAsync(artistId, pageNumber, pageSize, query?.Trim(), ct);
    }

    /// <inheritdoc />
    public async Task<(bool success, string message)> AddSongToCatalogAsync(
        int artistId, int songId, CancellationToken ct = default)
    {
        if (await _catalogRepository.ExistsAsync(artistId, songId, ct))
            return (false, "This song is already in the catalog.");

        var entry = new Catalog { ArtistId = artistId, SongId = songId };
        await _catalogRepository.AddAsync(entry, ct);
        await _catalogRepository.SaveChangesAsync(ct);
        return (true, "Song added to catalog.");
    }

    /// <inheritdoc />
    public async Task<(bool success, string message)> RemoveSongFromCatalogAsync(
        int artistId, int songId, CancellationToken ct = default)
    {
        await _catalogRepository.RemoveAsync(artistId, songId, ct);
        return (true, "Song removed from catalog.");
    }
}
