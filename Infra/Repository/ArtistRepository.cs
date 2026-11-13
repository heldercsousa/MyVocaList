using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MyVocaList.Domain.Entity;
using MyVocaList.Domain.ReadModels;
using MyVocaList.Domain.RepositoryInterface;
using MyVocaList.Infra.Collation;

namespace MyVocaList.Infra.Repository;

/// <inheritdoc />
public class ArtistRepository : IArtistRepository
{
    private const string NameSearchSql =
        """
        SELECT rowid AS Value
        FROM ArtistSearch
        WHERE ArtistSearch MATCH @m
        ORDER BY bm25(ArtistSearch), length(name)
        LIMIT @take
        """;

    private readonly AppDbContext _db;
    private readonly ILogger<ArtistRepository> _logger;

    public ArtistRepository(AppDbContext db, ILogger<ArtistRepository>? logger = null)
    {
        _db = db;
        _logger = logger ?? NullLogger<ArtistRepository>.Instance;
    }

    /// <inheritdoc />
    public async Task<(IEnumerable<ArtistListItem> items, int totalCount)> GetPagedAsync(
        int pageNumber, int pageSize, string query,
        ArtistRoleFilter roleFilter = ArtistRoleFilter.All, CancellationToken ct = default)
    {
        var q = _db.Artists.AsNoTracking().AsQueryable(); // Explicit + global default = doubly safe

        if (!string.IsNullOrEmpty(query))
        {
            var pattern = "%" + query + "%";
            q = q.Where(a => EF.Functions.Like(
                EF.Functions.Collate(a.Name, CollationConstants.Default),
                EF.Functions.Collate(pattern, CollationConstants.Default)));
        }

        q = roleFilter switch
        {
            ArtistRoleFilter.AuthorsOnly => q.Where(a => a.OriginalSongs.Any()),
            ArtistRoleFilter.PerformersOnly => q.Where(a => a.CatalogEntries.Any()),
            _ => q
        };

        var totalCount = await q.CountAsync(ct);

        var rawItems = await q
            .OrderBy(a => a.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new ArtistListItem(           // SQL-level column projection
                a.Id,
                a.Name,
                a.ExternalProvider,
                a.CatalogEntries.Count(),              // Scalar subquery in one SQL statement
                a.ImageUrl))
            .ToListAsync(ct);

        return (rawItems, totalCount);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ArtistListItem>> SearchByNameAsync(string query, int maxResults, CancellationToken ct)
    {
        var q = _db.Artists.AsNoTracking().AsQueryable();

        if (!string.IsNullOrEmpty(query))
        {
            var pattern = query + "%";
            q = q.Where(a => EF.Functions.Like(
                EF.Functions.Collate(a.Name, CollationConstants.Default),
                EF.Functions.Collate(pattern, CollationConstants.Default)));
        }

        return await q
            .OrderBy(a => a.Name)
            .Take(maxResults)
            .Select(a => new ArtistListItem(a.Id, a.Name, a.ExternalProvider, a.CatalogEntries.Count(), a.ImageUrl))
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<Artist> GetByIdAsync(int id, CancellationToken ct)
        => await _db.Artists.FirstOrDefaultAsync(a => a.Id == id, ct);

    /// <inheritdoc />
    public async Task<Artist> GetByExternalIdAsync(string externalId, string provider, CancellationToken ct)
        => await _db.Artists.AsNoTracking().FirstOrDefaultAsync(
            a => a.ExternalId == externalId && a.ExternalProvider == provider, ct);

    /// <inheritdoc />
    public async Task<Artist?> GetByNameAsync(string name, CancellationToken ct = default)
        => await _db.Artists
            .AsNoTracking()
            .FirstOrDefaultAsync(
                a => EF.Functions.Collate(a.Name, CollationConstants.Default) ==
                     EF.Functions.Collate(name, CollationConstants.Default), ct);

    /// <inheritdoc />
    public async Task<bool> ExistsByNameAsync(string name, CancellationToken ct)
        => await _db.Artists.AnyAsync(
            a => EF.Functions.Collate(a.Name, CollationConstants.Default) == EF.Functions.Collate(name, CollationConstants.Default), ct);

    /// <inheritdoc />
    public async Task<bool> ExistsByNameAsync(string name, int excludeId, CancellationToken ct)
        => await _db.Artists.AnyAsync(
            a => a.Id != excludeId &&
                 EF.Functions.Collate(a.Name, CollationConstants.Default) == EF.Functions.Collate(name, CollationConstants.Default), ct);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Artist>> SearchNameCandidatesAsync(
        string name, int take, CancellationToken ct)
    {
        var match = FtsQueryBuilder.BuildTrigramMatch(name);
        if (match is null)
        {
            // REQ-SFT-19: no word of >= 3 chars - collated name prefix, still DB-side.
            var pattern = (name ?? string.Empty).Trim() + "%";
            return await _db.Artists
                .AsNoTracking()
                .Where(a => EF.Functions.Like(
                    EF.Functions.Collate(a.Name, CollationConstants.Default),
                    EF.Functions.Collate(pattern, CollationConstants.Default)))
                .OrderBy(a => a.Name.Length)
                .Take(take)
                .ToListAsync(ct);
        }

        List<int> ids;
        try
        {
            ids = await _db.Database
                .SqlQueryRaw<int>(
                    NameSearchSql,
                    new SqliteParameter("@m", match),
                    new SqliteParameter("@take", take))
                .ToListAsync(ct);
        }
        catch (SqliteException ex) when (
            ex.Message.Contains("no such table", StringComparison.OrdinalIgnoreCase) &&
            ex.Message.Contains("ArtistSearch", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogError(ex, "ArtistSearch FTS index is missing; artist name search returns no candidates");
            return [];
        }

        if (ids.Count == 0) return [];

        var artists = await _db.Artists
            .AsNoTracking()
            .Where(a => ids.Contains(a.Id))
            .ToListAsync(ct);

        // Keep the bm25 order of the index (positional, not a string comparison).
        var position = ids.Select((id, i) => (id, i)).ToDictionary(x => x.id, x => x.i);
        return artists.OrderBy(a => position[a.Id]).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Artist>> GetByNamesCollatedAsync(
        IEnumerable<string> names, CancellationToken ct = default)
    {
        var list = names?.Where(n => !string.IsNullOrWhiteSpace(n)).Distinct().ToList() ?? [];
        if (list.Count == 0) return [];

        return await _db.Artists
            .AsNoTracking()
            .Where(a => list.Contains(EF.Functions.Collate(a.Name, CollationConstants.Default)))
            .ToListAsync(ct);
    }

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

}
