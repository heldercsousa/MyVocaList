using Microsoft.EntityFrameworkCore;
using MyVocaList.Contracts.DTOs.List;
using MyVocaList.Domain.Entity;
using MyVocaList.Domain.RepositoryInterface;
using MyVocaList.Infra.Collation;

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
                EF.Functions.Collate(s.Title, CollationConstants.Default),
                EF.Functions.Collate(pattern, CollationConstants.Default)));
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
        // BUG-055: eager-load OriginalArtist so edit-mode hydration can show the stored artist name.
        // Tracked query (callers mutate + save); the extra join is harmless for read-only callers.
        => await _db.Songs
            .Include(s => s.OriginalArtist)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

    /// <inheritdoc />
    public async Task<Song?> GetByExternalIdAsync(string externalId, string provider, CancellationToken ct)
        => await _db.Songs.FirstOrDefaultAsync(
            s => s.ExternalId == externalId && s.ExternalProvider == provider, ct);

    /// <inheritdoc />
    public async Task<bool> ExistsByTitleForArtistAsync(int artistId, string title, CancellationToken ct)
        => await _db.Songs.AnyAsync(
            s => s.ArtistId == artistId &&
                 EF.Functions.Collate(s.Title, CollationConstants.Default) == EF.Functions.Collate(title, CollationConstants.Default), ct);

    /// <inheritdoc />
    public async Task<bool> ExistsByTitleForArtistAsync(
        int artistId, string title, int excludeId, CancellationToken ct)
        => await _db.Songs.AnyAsync(
            s => s.Id != excludeId && s.ArtistId == artistId &&
                 EF.Functions.Collate(s.Title, CollationConstants.Default) == EF.Functions.Collate(title, CollationConstants.Default), ct);

    /// <inheritdoc />
    public async Task<bool> ExistsByTitleVersionForArtistAsync(
        int artistId, string title, string version, CancellationToken ct = default)
        => await _db.Songs.AnyAsync(
            s => s.ArtistId == artistId &&
                 EF.Functions.Collate(s.Title, CollationConstants.Default) == EF.Functions.Collate(title, CollationConstants.Default) &&
                 EF.Functions.Collate(s.Version, CollationConstants.Default) == EF.Functions.Collate(version, CollationConstants.Default),
            ct);

    /// <inheritdoc />
    public async Task<bool> ExistsByTitleVersionForArtistAsync(
        int artistId, string title, string version, int excludeId, CancellationToken ct = default)
        => await _db.Songs.AnyAsync(
            s => s.Id != excludeId &&
                 s.ArtistId == artistId &&
                 EF.Functions.Collate(s.Title, CollationConstants.Default) == EF.Functions.Collate(title, CollationConstants.Default) &&
                 EF.Functions.Collate(s.Version, CollationConstants.Default) == EF.Functions.Collate(version, CollationConstants.Default),
            ct);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Song>> GetFuzzyCandidatePoolAsync(
        int artistId, string titlePrefixToken, int take, CancellationToken ct = default)
    {
        var pattern = titlePrefixToken + "%";
        return await _db.Songs
            .Where(s => s.ArtistId == artistId &&
                        EF.Functions.Like(
                            EF.Functions.Collate(s.Title, CollationConstants.Default),
                            pattern))
            .Take(take)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Song>> GetByTitlesCollatedAsync(
        IEnumerable<string> titles, CancellationToken ct = default)
    {
        var list = titles?.Where(t => !string.IsNullOrWhiteSpace(t)).Distinct().ToList() ?? [];
        if (list.Count == 0) return [];

        return await _db.Songs
            .AsNoTracking()
            .Where(s => list.Contains(EF.Functions.Collate(s.Title, CollationConstants.Default)))
            .ToListAsync(ct);
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
        // BUG-068: the app's AppDbContext is registered Scoped, but MAUI has no per-page DI
        // scope — the root provider resolves one instance that lives for the app's entire
        // session. QueryTrackingBehavior.NoTracking only suppresses tracking for query
        // results; it does NOT detach entities that were explicitly Add()'ed/Update()'d and
        // saved earlier in that same long-lived context (e.g. this song's own creation, or a
        // prior edit-save). GetByIdAsync's fresh (untracked) read of this Song can therefore
        // collide with a still-tracked instance of the same row left over from an earlier
        // write, and DbSet.Update(song) throws an EF identity-map conflict on attach.
        // If a tracked instance for this Id already exists, update ITS values instead of
        // attaching a second instance — CurrentValues.SetValues copies scalar/FK properties
        // only (no navigation graph), so this cannot re-attach song.OriginalArtist either.
        var tracked = _db.ChangeTracker.Entries<Song>()
            .FirstOrDefault(e => e.Entity.Id == song.Id);

        if (tracked != null && !ReferenceEquals(tracked.Entity, song))
        {
            tracked.CurrentValues.SetValues(song);
        }
        else
        {
            _db.Songs.Update(song);
        }

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
