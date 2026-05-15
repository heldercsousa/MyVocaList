using Microsoft.EntityFrameworkCore;
using MyVocaList.Contracts.DTOs.List;
using MyVocaList.Domain.Entity;
using MyVocaList.Domain.RepositoryInterface;

namespace MyVocaList.Infra.Repository;

/// <inheritdoc />
public class CatalogRepository : ICatalogRepository
{
    private readonly AppDbContext _db;

    public CatalogRepository(AppDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<(IEnumerable<SongListItemDto> items, int totalCount)> GetPagedByArtistAsync(
        int artistId, int pageNumber, int pageSize, string? normalizedQuery, CancellationToken ct)
    {
        var q = _db.Catalog
            .Where(c => c.ArtistId == artistId)
            .Select(c => c.Song);

        if (!string.IsNullOrEmpty(normalizedQuery))
        {
            var pattern = normalizedQuery + "%";
            q = q.Where(s => EF.Functions.Like(
                EF.Functions.Collate(s.TitleNormalized, "NOCASE"),
                EF.Functions.Collate(pattern, "NOCASE")));
        }

        var totalCount = await q.CountAsync(ct);

        var items = await q
            .OrderBy(s => s.TitleNormalized)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new SongListItemDto(
                s.Id,
                s.Title,
                s.ArtistId,
                s.OriginalArtist.Name,
                s.FeaturedArtists,
                s.ExternalProvider,
                s.HasManualEdits))
            .ToListAsync(ct);

        return (items, totalCount);
    }

    /// <inheritdoc />
    public async Task<int> CountByArtistAsync(int artistId, CancellationToken ct)
        => await _db.Catalog.CountAsync(c => c.ArtistId == artistId, ct);

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(int artistId, int songId, CancellationToken ct)
        => await _db.Catalog.AnyAsync(c => c.ArtistId == artistId && c.SongId == songId, ct);

    /// <inheritdoc />
    public Task AddAsync(Catalog entry, CancellationToken ct)
    {
        _db.Catalog.Add(entry);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task RemoveAsync(int artistId, int songId, CancellationToken ct)
    {
        await _db.Catalog
            .Where(c => c.ArtistId == artistId && c.SongId == songId)
            .ExecuteDeleteAsync(ct);
    }

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
