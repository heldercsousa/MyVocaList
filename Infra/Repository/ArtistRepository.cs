using Microsoft.EntityFrameworkCore;
using MyVocaList.Domain.Entity;
using MyVocaList.Domain.RepositoryInterface;

namespace MyVocaList.Infra.Repository;

/// <inheritdoc />
public class ArtistRepository : IArtistRepository
{
    private readonly AppDbContext _db;

    public ArtistRepository(AppDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<(IEnumerable<(Artist artist, int catalogCount)> items, int totalCount)> GetPagedAsync(
        int pageNumber, int pageSize, string query,
        ArtistRoleFilter roleFilter = ArtistRoleFilter.All, CancellationToken ct = default)
    {
        var q = _db.Artists.AsQueryable();

        if (!string.IsNullOrEmpty(query))
        {
            var pattern = "%" + query + "%";
            q = q.Where(a => EF.Functions.Like(
                EF.Functions.Collate(a.Name, "NOCASE_NOACCENT"),
                EF.Functions.Collate(pattern, "NOCASE_NOACCENT")));
        }

        q = roleFilter switch
        {
            ArtistRoleFilter.AuthorsOnly    => q.Where(a => a.OriginalSongs.Any()),
            ArtistRoleFilter.PerformersOnly => q.Where(a => a.CatalogEntries.Any()),
            _                               => q
        };

        var totalCount = await q.CountAsync(ct);

        var rawItems = await q
            .OrderBy(a => a.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new { Artist = a, CatalogCount = a.CatalogEntries.Count() })
            .ToListAsync(ct);

        var items = rawItems.Select(x => (x.Artist, x.CatalogCount)).ToList();
        return (items, totalCount);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Artist>> SearchByNameAsync(string query, int maxResults, CancellationToken ct)
    {
        var q = _db.Artists.AsQueryable();

        if (!string.IsNullOrEmpty(query))
        {
            var pattern = query + "%";
            q = q.Where(a => EF.Functions.Like(
                EF.Functions.Collate(a.Name, "NOCASE_NOACCENT"),
                EF.Functions.Collate(pattern, "NOCASE_NOACCENT")));
        }

        return await q
            .OrderBy(a => a.Name)
            .Take(maxResults)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<Artist> GetByIdAsync(int id, CancellationToken ct)
        => await _db.Artists.FirstOrDefaultAsync(a => a.Id == id, ct);

    /// <inheritdoc />
    public async Task<Artist> GetByExternalIdAsync(string externalId, string provider, CancellationToken ct)
        => await _db.Artists.FirstOrDefaultAsync(
            a => a.ExternalId == externalId && a.ExternalProvider == provider, ct);

    /// <inheritdoc />
    public async Task<bool> ExistsByNameAsync(string name, CancellationToken ct)
        => await _db.Artists.AnyAsync(
            a => EF.Functions.Collate(a.Name, "NOCASE_NOACCENT") == EF.Functions.Collate(name, "NOCASE_NOACCENT"), ct);

    /// <inheritdoc />
    public async Task<bool> ExistsByNameAsync(string name, int excludeId, CancellationToken ct)
        => await _db.Artists.AnyAsync(
            a => a.Id != excludeId &&
                 EF.Functions.Collate(a.Name, "NOCASE_NOACCENT") == EF.Functions.Collate(name, "NOCASE_NOACCENT"), ct);

    /// <inheritdoc />
    public Task AddAsync(Artist artist, CancellationToken ct)
    {
        _db.Artists.Add(artist);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task UpdateAsync(Artist artist, CancellationToken ct)
    {
        _db.Artists.Update(artist);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(IEnumerable<int> ids, CancellationToken ct)
    {
        var idList = ids.ToList();
        await _db.Artists
            .Where(a => idList.Contains(a.Id))
            .ExecuteDeleteAsync(ct);
    }

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
