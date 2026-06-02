using Microsoft.EntityFrameworkCore;
using MyVocaList.Contracts.DTOs.List;
using MyVocaList.Domain.Entity;
using MyVocaList.Domain.RepositoryInterface;

namespace MyVocaList.Infra.Repository;

/// <inheritdoc />
public class SongRepository : ISongRepository
{
    private readonly AppDbContext _db;

    public SongRepository(AppDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<(IEnumerable<SongListItemDto> items, int totalCount)> GetPagedAsync(
        int pageNumber, int pageSize, string? query, CancellationToken ct)
    {
        var q = _db.Songs.AsQueryable();

        if (!string.IsNullOrEmpty(query))
        {
            var pattern = query + "%";
            q = q.Where(s => EF.Functions.Like(
                EF.Functions.Collate(s.Title, "NOCASE_NOACCENT"),
                EF.Functions.Collate(pattern, "NOCASE_NOACCENT")));
        }

        var totalCount = await q.CountAsync(ct);

        var items = await q
            .OrderBy(s => s.Title)
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
    public async Task<Song> GetByIdAsync(int id, CancellationToken ct)
        => await _db.Songs.FirstOrDefaultAsync(s => s.Id == id, ct);

    /// <inheritdoc />
    public async Task<Song?> GetByExternalIdAsync(string externalId, string provider, CancellationToken ct)
        => await _db.Songs.FirstOrDefaultAsync(
            s => s.ExternalId == externalId && s.ExternalProvider == provider, ct);

    /// <inheritdoc />
    public async Task<bool> ExistsByTitleForArtistAsync(int artistId, string title, CancellationToken ct)
        => await _db.Songs.AnyAsync(
            s => s.ArtistId == artistId &&
                 EF.Functions.Collate(s.Title, "NOCASE_NOACCENT") == EF.Functions.Collate(title, "NOCASE_NOACCENT"), ct);

    /// <inheritdoc />
    public async Task<bool> ExistsByTitleForArtistAsync(
        int artistId, string title, int excludeId, CancellationToken ct)
        => await _db.Songs.AnyAsync(
            s => s.Id != excludeId && s.ArtistId == artistId &&
                 EF.Functions.Collate(s.Title, "NOCASE_NOACCENT") == EF.Functions.Collate(title, "NOCASE_NOACCENT"), ct);

    /// <inheritdoc />
    public Task AddAsync(Song song, CancellationToken ct)
    {
        _db.Songs.Add(song);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task UpdateAsync(Song song, CancellationToken ct)
    {
        _db.Songs.Update(song);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(IEnumerable<int> ids, CancellationToken ct)
    {
        var idList = ids.ToList();
        await _db.Songs
            .Where(s => idList.Contains(s.Id))
            .ExecuteDeleteAsync(ct);
    }

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
