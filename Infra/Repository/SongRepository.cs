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
    public async Task<(IEnumerable<SongListItemDto> items, int totalCount)> GetPagedByArtistAsync(
        int artistId, int pageNumber, int pageSize, string? normalizedQuery, CancellationToken ct)
    {
        var q = _db.Songs.Where(s => s.ArtistId == artistId);

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
                s.ArtistId,
                s.Title,
                s.Artist.Name,
                s.FeaturedArtists,
                s.ExternalProvider,
                s.HasManualEdits))
            .ToListAsync(ct);

        return (items, totalCount);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Song>> SearchByTitleAsync(
        int artistId, string normalizedQuery, int maxResults, CancellationToken ct)
    {
        var q = _db.Songs.Where(s => s.ArtistId == artistId);

        if (!string.IsNullOrEmpty(normalizedQuery))
        {
            var pattern = normalizedQuery + "%";
            q = q.Where(s => EF.Functions.Like(
                EF.Functions.Collate(s.TitleNormalized, "NOCASE"),
                EF.Functions.Collate(pattern, "NOCASE")));
        }

        return await q
            .OrderBy(s => s.TitleNormalized)
            .Take(maxResults)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<Song> GetByIdAsync(int id, CancellationToken ct)
        => await _db.Songs.FirstOrDefaultAsync(s => s.Id == id, ct);

    /// <inheritdoc />
    public async Task<Song> GetByExternalIdAsync(string externalId, string provider, CancellationToken ct)
        => await _db.Songs.FirstOrDefaultAsync(
            s => s.ExternalId == externalId && s.ExternalProvider == provider, ct);

    /// <inheritdoc />
    public async Task<bool> ExistsByTitleForArtistAsync(int artistId, string normalizedTitle, CancellationToken ct)
        => await _db.Songs.AnyAsync(
            s => s.ArtistId == artistId && EF.Functions.Like(
                EF.Functions.Collate(s.TitleNormalized, "NOCASE"),
                EF.Functions.Collate(normalizedTitle, "NOCASE")), ct);

    /// <inheritdoc />
    public async Task<bool> ExistsByTitleForArtistAsync(
        int artistId, string normalizedTitle, int excludeId, CancellationToken ct)
        => await _db.Songs.AnyAsync(
            s => s.Id != excludeId && s.ArtistId == artistId && EF.Functions.Like(
                EF.Functions.Collate(s.TitleNormalized, "NOCASE"),
                EF.Functions.Collate(normalizedTitle, "NOCASE")), ct);

    /// <inheritdoc />
    public async Task<int> CountByArtistAsync(int artistId, CancellationToken ct)
        => await _db.Songs.CountAsync(s => s.ArtistId == artistId, ct);

    /// <inheritdoc />
    public async Task<int> CountByArtistsAsync(IEnumerable<int> artistIds, CancellationToken ct)
    {
        var idList = artistIds.ToList();
        return await _db.Songs.CountAsync(s => idList.Contains(s.ArtistId), ct);
    }

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
