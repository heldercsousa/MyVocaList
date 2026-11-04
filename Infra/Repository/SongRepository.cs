using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MyVocaList.Contracts.DTOs.List;
using MyVocaList.Domain.Entity;
using MyVocaList.Domain.ReadModels;
using MyVocaList.Domain.RepositoryInterface;
using MyVocaList.Infra.Collation;

namespace MyVocaList.Infra.Repository;

/// <inheritdoc />
public class SongRepository : ISongRepository
{
    private const string TitleSearchSql =
        """
        SELECT s.Id AS SongId, s.Title AS Title, s.Version AS Version, s.ArtistId AS ArtistId,
               a.Name AS ArtistName, s.ExternalProvider AS ExternalProvider, s.ExternalId AS ExternalId
        FROM SongSearch
        JOIN Songs s ON s.Id = SongSearch.rowid
        JOIN Artists a ON a.Id = s.ArtistId
        WHERE SongSearch MATCH @m
        ORDER BY bm25(SongSearch, 10.0, 1.0), length(s.Title)
        LIMIT @take
        """;

    private readonly AppDbContext _db;
    private readonly ILogger<SongRepository> _logger;

    public SongRepository(AppDbContext db, ILogger<SongRepository>? logger = null)
    {
        _db = db;
        _logger = logger ?? NullLogger<SongRepository>.Instance;
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
                s.ExternalProvider))
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
    public async Task<int?> FindIdByTitleVersionForArtistAsync(
        int artistId, string title, string version, int? excludeId, CancellationToken ct)
        => await _db.Songs
            .Where(s => (excludeId == null || s.Id != excludeId.Value) &&
                        s.ArtistId == artistId &&
                        EF.Functions.Collate(s.Title, CollationConstants.Default) == EF.Functions.Collate(title, CollationConstants.Default) &&
                        EF.Functions.Collate(s.Version, CollationConstants.Default) == EF.Functions.Collate(version, CollationConstants.Default))
            .Select(s => (int?)s.Id)
            .FirstOrDefaultAsync(ct);

    /// <inheritdoc />
    public async Task<bool> ExistsByExternalIdentityAsync(
        string provider, string externalId, int? excludeId, CancellationToken ct)
        => await _db.Songs.AnyAsync(
            s => (excludeId == null || s.Id != excludeId.Value) &&
                 s.ExternalProvider == provider &&
                 s.ExternalId == externalId,
            ct);

    /// <inheritdoc />
    public async Task<IReadOnlyList<SongSearchHit>> SearchTitleCandidatesAsync(
        string query, int take, CancellationToken ct)
    {
        var match = FtsQueryBuilder.BuildTrigramMatch(query);
        if (match is null)
        {
            // REQ-SFT-33: no word of >= 3 chars - collated title prefix, still DB-side.
            var pattern = (query ?? string.Empty).Trim() + "%";
            return await _db.Songs
                .AsNoTracking()
                .Where(s => EF.Functions.Like(
                    EF.Functions.Collate(s.Title, CollationConstants.Default),
                    EF.Functions.Collate(pattern, CollationConstants.Default)))
                .OrderBy(s => s.Title.Length)
                .Take(take)
                .Select(s => new SongSearchHit(
                    s.Id, s.Title, s.Version, s.ArtistId, s.OriginalArtist.Name, s.ExternalProvider, s.ExternalId))
                .ToListAsync(ct);
        }

        try
        {
            return await _db.Database
                .SqlQueryRaw<SongSearchHit>(
                    TitleSearchSql,
                    new SqliteParameter("@m", match),
                    new SqliteParameter("@take", take))
                .ToListAsync(ct);
        }
        catch (SqliteException ex) when (ex.Message.Contains("no such table", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogError(ex, "SongSearch FTS index is missing; title search returns no candidates");
            return [];
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SongSearchHit>> FindByExternalIdsAsync(
        string provider, IReadOnlyCollection<string> externalIds, CancellationToken ct)
    {
        if (externalIds is null || externalIds.Count == 0) return [];
        var ids = externalIds.ToList();

        return await _db.Songs
            .AsNoTracking()
            .Where(s => s.ExternalProvider == provider && ids.Contains(s.ExternalId))
            .Select(s => new SongSearchHit(
                s.Id, s.Title, s.Version, s.ArtistId, s.OriginalArtist.Name, s.ExternalProvider, s.ExternalId))
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SongSearchHit>> FindByTitleAndArtistNamesAsync(
        IReadOnlyCollection<(string Title, string ArtistName, string Version)> items, CancellationToken ct)
    {
        if (items is null || items.Count == 0) return [];

        // One bounded indexed query per triple (the batch is at most a result page); collation decides every match.
        var hits = new Dictionary<int, SongSearchHit>();
        foreach (var (title, artistName, version) in items)
        {
            var matches = await _db.Songs
                .AsNoTracking()
                .Where(s =>
                    EF.Functions.Collate(s.Title, CollationConstants.Default) == EF.Functions.Collate(title, CollationConstants.Default) &&
                    EF.Functions.Collate(s.OriginalArtist.Name, CollationConstants.Default) == EF.Functions.Collate(artistName, CollationConstants.Default) &&
                    EF.Functions.Collate(s.Version, CollationConstants.Default) == EF.Functions.Collate(version ?? string.Empty, CollationConstants.Default))
                .Select(s => new SongSearchHit(
                    s.Id, s.Title, s.Version, s.ArtistId, s.OriginalArtist.Name, s.ExternalProvider, s.ExternalId))
                .ToListAsync(ct);
            foreach (var m in matches) hits[m.SongId] = m;
        }

        return hits.Values.ToList();
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
        // REQ-UOW-18: the BUG-068 ChangeTracker.Entries<Song>() detach guard (stopgap, commit
        // 1a114c1 on feat/inline-artist-create) is deleted here. It existed because the app's
        // AppDbContext was effectively app-lifetime, so a Song saved earlier in the session was
        // still tracked when a later edit tried to attach a second instance of the same row.
        // Under the unit-of-work pattern each write runs in its own freshly-scoped context that
        // has never seen this row, so DbSet.Update attaches cleanly and no guard is needed.
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
}
