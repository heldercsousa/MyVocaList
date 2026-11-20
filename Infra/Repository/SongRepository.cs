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
    private const string TitleSearchSelectSql =
        """
        SELECT s.Id AS SongId, s.Title AS Title, s.ArtistId AS ArtistId,
               a.Name AS ArtistName, s.ExternalProvider AS ExternalProvider, s.ExternalId AS ExternalId,
               s.ImageUrl AS ImageUrl, a.ImageUrl AS ArtistImageUrl
        FROM SongSearch
        JOIN Songs s ON s.Id = SongSearch.rowid
        JOIN Artists a ON a.Id = s.ArtistId

        """;

    private const string TitleSearchSql = TitleSearchSelectSql + """
        WHERE SongSearch MATCH @m
        ORDER BY bm25(SongSearch, 10.0, 1.0), length(s.Title)
        LIMIT @take
        """;

    // BUG-085 (REQ-SFT-23): same as TitleSearchSql, restricted to one artist.
    private const string TitleSearchForArtistSql = TitleSearchSelectSql + """
        WHERE SongSearch MATCH @m AND s.ArtistId = @artistId
        ORDER BY bm25(SongSearch, 10.0, 1.0), length(s.Title)
        LIMIT @take
        """;

    // REQ-SLD-01/02/03: Songs list search. Same FTS index and bm25 weights as TitleSearchSql; the id
    // tie-break keeps pages disjoint; the count joins the same tables so it cannot exceed the rows.
    private const string ListSearchSql =
        """
        SELECT s.Id AS Id, s.Title AS Title, s.ArtistId AS OriginalArtistId, a.Name AS OriginalArtistName,
               s.ExternalProvider AS ExternalProvider,
               COALESCE(s.ImageUrl, a.ImageUrl) AS ImageUrl
        FROM SongSearch
        JOIN Songs s ON s.Id = SongSearch.rowid
        JOIN Artists a ON a.Id = s.ArtistId
        WHERE SongSearch MATCH @m
        ORDER BY bm25(SongSearch, 10.0, 1.0), length(s.Title), s.Id
        LIMIT @take OFFSET @skip
        """;

    private const string ListSearchCountSql =
        """
        SELECT count(*) AS Value
        FROM SongSearch
        JOIN Songs s ON s.Id = SongSearch.rowid
        JOIN Artists a ON a.Id = s.ArtistId
        WHERE SongSearch MATCH @m
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
        if (string.IsNullOrEmpty(query))
        {
            // Blank query: the list of every song, unchanged.
            var all = _db.Songs.AsQueryable();
            var allCount = await all.CountAsync(ct);
            var allItems = await all
                .OrderBy(s => s.Title)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new SongListItemDto(
                    s.Id,
                    s.Title,
                    s.ArtistId,
                    s.OriginalArtist.Name,
                    s.ExternalProvider,
                    s.ImageUrl ?? s.OriginalArtist.ImageUrl))
                .ToListAsync(ct);
            return (allItems, allCount);
        }

        if (FtsQueryBuilder.BuildTrigramMatch(query) is null)
        {
            // REQ-SLD-01/02: no word of 3+ chars - title prefix only, ordered by title.
            return await GetPagedPrefixAsync(pageNumber, pageSize, query, ct);
        }

        try
        {
            return await SearchPagedAsync(pageNumber, pageSize, query, ct);
        }
        catch (SqliteException ex) when (ex.Message.Contains("no such table", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogError(ex, "SongSearch FTS index is missing; songs list falls back to the title prefix");
            return await GetPagedPrefixAsync(pageNumber, pageSize, query, ct);
        }
    }

    /// <summary>
    /// REQ-SLD-35 short text (no word of 3+ characters, or the index missing): the title or the artist name
    /// contains the text, collated (case and accents ignored, the custom LIKE). Ordered: title starts with the
    /// text first, then title, then id, so pages are disjoint (REQ-SLD-02/03). Only called with a non-blank query.
    /// </summary>
    private async Task<(IEnumerable<SongListItemDto> items, int totalCount)> GetPagedPrefixAsync(
        int pageNumber, int pageSize, string? query, CancellationToken ct)
    {
        var containsPattern = "%" + query + "%";
        var startsPattern = query + "%";

        var q = _db.Songs.Where(s =>
            EF.Functions.Like(
                EF.Functions.Collate(s.Title, CollationConstants.Default),
                EF.Functions.Collate(containsPattern, CollationConstants.Default))
            || EF.Functions.Like(
                EF.Functions.Collate(s.OriginalArtist.Name, CollationConstants.Default),
                EF.Functions.Collate(containsPattern, CollationConstants.Default)));

        var totalCount = await q.CountAsync(ct);

        var items = await q
            .OrderBy(s => EF.Functions.Like(
                EF.Functions.Collate(s.Title, CollationConstants.Default),
                EF.Functions.Collate(startsPattern, CollationConstants.Default)) ? 0 : 1)
            .ThenBy(s => s.Title)
            .ThenBy(s => s.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new SongListItemDto(
                s.Id,
                s.Title,
                s.ArtistId,
                s.OriginalArtist.Name,
                s.ExternalProvider,
                s.ImageUrl ?? s.OriginalArtist.ImageUrl))
            .ToListAsync(ct);

        return (items, totalCount);
    }

    /// <summary>
    /// REQ-SLD-01/02/03/04: full-text match over title and artist, ranked by bm25 (title 10, artist 1),
    /// then title length, then id. Matching and ranking stay in the database.
    /// </summary>
    private async Task<(IEnumerable<SongListItemDto> items, int totalCount)> SearchPagedAsync(
        int pageNumber, int pageSize, string query, CancellationToken ct)
    {
        var match = FtsQueryBuilder.BuildTrigramMatch(query)!;
        var skip = (pageNumber - 1) * pageSize;

        var totalCount = await _db.Database
            .SqlQueryRaw<int>(ListSearchCountSql, new SqliteParameter("@m", match))
            .SingleAsync(ct);

        var items = await _db.Database
            .SqlQueryRaw<SongListItemDto>(
                ListSearchSql,
                new SqliteParameter("@m", match),
                new SqliteParameter("@take", pageSize),
                new SqliteParameter("@skip", skip))
            .ToListAsync(ct);

        return (items, totalCount);
    }

    /// <inheritdoc />
    public Task<List<string>> GetLocalImageReferencesAsync(CancellationToken ct = default)
        // Prefix filter on a technical value, not search or de-duplication (design § 2.3).
        => _db.Songs.AsNoTracking()
            .Where(s => s.ImageUrl != null && s.ImageUrl.StartsWith("local:"))
            .Select(s => s.ImageUrl!)
            .ToListAsync(ct);

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
    public async Task<int?> FindIdByTitleForArtistAsync(
        int artistId, string title, int? excludeId, CancellationToken ct)
        => await _db.Songs
            .Where(s => (excludeId == null || s.Id != excludeId.Value) &&
                        s.ArtistId == artistId &&
                        EF.Functions.Collate(s.Title, CollationConstants.Default) == EF.Functions.Collate(title, CollationConstants.Default))
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
    public Task<IReadOnlyList<SongSearchHit>> SearchTitleCandidatesAsync(
        string query, int take, CancellationToken ct)
        => SearchTitleCoreAsync(query, take, artistId: null, ct);

    /// <inheritdoc />
    public async Task<IReadOnlyList<SongSearchHit>> SearchTitleCandidatesForArtistAsync(
        int artistId, string title, int take, CancellationToken ct = default)
    {
        // BUG-085: a blank typed title has no trigram windows; it must not fall back to a match-all prefix.
        if (string.IsNullOrWhiteSpace(title)) return [];

        return await SearchTitleCoreAsync(title, take, artistId, ct);
    }

    /// <summary>
    /// Shared title search (REQ-SFT-02/23/33): FTS5 trigram OR-match when the query has a word of &gt;= 3 chars,
    /// else the collated title prefix. <paramref name="artistId"/> null = global; otherwise scoped to that artist.
    /// </summary>
    private async Task<IReadOnlyList<SongSearchHit>> SearchTitleCoreAsync(
        string query, int take, int? artistId, CancellationToken ct)
    {
        var match = FtsQueryBuilder.BuildTrigramMatch(query);
        if (match is null)
        {
            // REQ-SFT-33: no word of >= 3 chars - collated title prefix, still DB-side.
            var pattern = (query ?? string.Empty).Trim() + "%";
            return await _db.Songs
                .AsNoTracking()
                .Where(s => (artistId == null || s.ArtistId == artistId.Value) &&
                            EF.Functions.Like(
                                EF.Functions.Collate(s.Title, CollationConstants.Default),
                                EF.Functions.Collate(pattern, CollationConstants.Default)))
                .OrderBy(s => s.Title.Length)
                .Take(take)
                .Select(s => new SongSearchHit(
                    s.Id, s.Title, s.ArtistId, s.OriginalArtist.Name, s.ExternalProvider, s.ExternalId,
                    s.ImageUrl, s.OriginalArtist.ImageUrl))
                .ToListAsync(ct);
        }

        try
        {
            return artistId is null
                ? await _db.Database
                    .SqlQueryRaw<SongSearchHit>(
                        TitleSearchSql,
                        new SqliteParameter("@m", match),
                        new SqliteParameter("@take", take))
                    .ToListAsync(ct)
                : await _db.Database
                    .SqlQueryRaw<SongSearchHit>(
                        TitleSearchForArtistSql,
                        new SqliteParameter("@m", match),
                        new SqliteParameter("@take", take),
                        new SqliteParameter("@artistId", artistId.Value))
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
                s.Id, s.Title, s.ArtistId, s.OriginalArtist.Name, s.ExternalProvider, s.ExternalId,
                s.ImageUrl, s.OriginalArtist.ImageUrl))
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SongSearchHit>> FindByTitleAndArtistNamesAsync(
        IReadOnlyCollection<(string Title, string ArtistName)> items, CancellationToken ct)
    {
        if (items is null || items.Count == 0) return [];

        // One bounded indexed query per pair (the batch is at most a result page); collation decides every match.
        var hits = new Dictionary<int, SongSearchHit>();
        foreach (var (title, artistName) in items)
        {
            var matches = await _db.Songs
                .AsNoTracking()
                .Where(s =>
                    EF.Functions.Collate(s.Title, CollationConstants.Default) == EF.Functions.Collate(title, CollationConstants.Default) &&
                    EF.Functions.Collate(s.OriginalArtist.Name, CollationConstants.Default) == EF.Functions.Collate(artistName, CollationConstants.Default))
                .Select(s => new SongSearchHit(
                    s.Id, s.Title, s.ArtistId, s.OriginalArtist.Name, s.ExternalProvider, s.ExternalId,
                    s.ImageUrl, s.OriginalArtist.ImageUrl))
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

    /// <inheritdoc />
    public async Task<int> CountByArtistAsync(int artistId, CancellationToken ct)
        => await _db.Songs.CountAsync(s => s.ArtistId == artistId, ct);
}
