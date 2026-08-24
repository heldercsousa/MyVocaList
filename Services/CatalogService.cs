using Microsoft.Extensions.DependencyInjection;
using MyVocaList.Contracts.DTOs.List;
using MyVocaList.Domain.Entity;
using MyVocaList.Domain.RepositoryInterface;
using MyVocaList.Domain.ServicesInterfaces;
using MyVocaList.Domain.UnitOfWork;
using MyVocaList.Extensions.Strings;

namespace MyVocaList.Services;

public class CatalogService : ICatalogService
{
    private readonly ICatalogRepository _catalogRepository;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<CatalogService> _logger;

    public CatalogService(ICatalogRepository catalogRepository, IUnitOfWork uow, ILogger<CatalogService> logger)
    {
        _catalogRepository = catalogRepository;
        _uow = uow;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<(IEnumerable<SongListItemDto> items, int totalCount)> GetPagedCatalogForArtistAsync(
        int artistId, int pageNumber, int pageSize, string? query = null, CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageNumber);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);

        var normalizedQuery = string.IsNullOrWhiteSpace(query) ? null : query.NormalizeSearchQuery();
        return await _catalogRepository.GetPagedByArtistAsync(artistId, pageNumber, pageSize, normalizedQuery, ct);
    }

    /// <inheritdoc />
    public Task<(bool success, string message)> AddSongToCatalogAsync(
        int artistId, int songId, CancellationToken ct = default)
        => _uow.ExecuteAsync<(bool success, string message)>(async sp =>
        {
            // REQ-UOW-28: resolved from the lambda's own scope — never the constructor field.
            var catalogRepository = sp.GetRequiredService<ICatalogRepository>();

            if (await catalogRepository.ExistsAsync(artistId, songId, ct))
                return (false, "This song is already in the catalog.");

            var entry = new Catalog { ArtistId = artistId, SongId = songId };
            await catalogRepository.AddAsync(entry, ct);
            // SaveChangesAsync deleted — the single save is owned by IUnitOfWork (REQ-UOW-10).
            return (true, "Song added to catalog.");
        }, ct);

    /// <inheritdoc />
    public Task<(bool success, string message)> RemoveSongFromCatalogAsync(
        int artistId, int songId, CancellationToken ct = default)
        => _uow.ExecuteAsync<(bool success, string message)>(async sp =>
        {
            // REQ-UOW-28: resolved from the lambda's own scope — never the constructor field.
            // REQ-UOW-33: RemoveAsync is ExecuteDeleteAsync-based; the explicit transaction opened
            // by IUnitOfWork.ExecuteAsync brings this bulk delete under the unit of work.
            var catalogRepository = sp.GetRequiredService<ICatalogRepository>();

            await catalogRepository.RemoveAsync(artistId, songId, ct);
            return (true, "Song removed from catalog.");
        }, ct);
}
