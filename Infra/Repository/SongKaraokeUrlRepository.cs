using Microsoft.EntityFrameworkCore;
using MyVocaList.Domain.Entity;
using MyVocaList.Domain.RepositoryInterface;

namespace MyVocaList.Infra.Repository;

/// <inheritdoc />
public class SongKaraokeUrlRepository : ISongKaraokeUrlRepository
{
    private readonly AppDbContext _db;

    public SongKaraokeUrlRepository(AppDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public Task<List<SongKaraokeUrl>> GetBySongIdAsync(int songId, CancellationToken ct = default)
        => _db.SongKaraokeUrls
              .Where(u => u.SongId == songId)
              .OrderByDescending(u => u.PlayCount)
              .ThenByDescending(u => u.LastUsedAt)
              .ThenByDescending(u => u.AddedAt)
              .ToListAsync(ct);

    /// <inheritdoc />
    public Task<SongKaraokeUrl?> GetSuggestedAsync(int songId, CancellationToken ct = default)
        => _db.SongKaraokeUrls
              .Where(u => u.SongId == songId)
              .OrderByDescending(u => u.PlayCount)
              .ThenByDescending(u => u.LastUsedAt)
              .ThenByDescending(u => u.AddedAt)
              .FirstOrDefaultAsync(ct);

    /// <inheritdoc />
    public Task<bool> ExistsAsync(int songId, string videoId, CancellationToken ct = default)
        => _db.SongKaraokeUrls
              .AnyAsync(u => u.SongId == songId && u.VideoId == videoId, ct);

    /// <inheritdoc />
    public Task AddAsync(SongKaraokeUrl url, CancellationToken ct = default)
    {
        _db.SongKaraokeUrls.Add(url);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task RemoveAsync(int songId, string videoId, CancellationToken ct = default)
    {
        await _db.SongKaraokeUrls
                 .Where(u => u.SongId == songId && u.VideoId == videoId)
                 .ExecuteDeleteAsync(ct);
    }

    /// <inheritdoc />
    public async Task IncrementPlayCountAsync(int songId, string videoId, CancellationToken ct = default)
    {
        await _db.SongKaraokeUrls
                 .Where(u => u.SongId == songId && u.VideoId == videoId)
                 .ExecuteUpdateAsync(s => s
                     .SetProperty(u => u.PlayCount, u => u.PlayCount + 1)
                     .SetProperty(u => u.LastUsedAt, _ => DateTime.UtcNow),
                     ct);
    }

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
