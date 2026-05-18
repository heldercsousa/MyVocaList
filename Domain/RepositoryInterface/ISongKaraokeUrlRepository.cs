using MyVocaList.Domain.Entity;

namespace MyVocaList.Domain.RepositoryInterface;

public interface ISongKaraokeUrlRepository
{
    /// <summary>Returns all URLs for a song, ordered: PlayCount DESC, LastUsedAt DESC, AddedAt DESC.</summary>
    Task<List<SongKaraokeUrl>> GetBySongIdAsync(int songId, CancellationToken ct = default);

    /// <summary>Returns the top-ranked URL (play count → recency), or null if none exist.</summary>
    Task<SongKaraokeUrl?> GetSuggestedAsync(int songId, CancellationToken ct = default);

    /// <summary>Returns true if the video ID is already associated with the song.</summary>
    Task<bool> ExistsAsync(int songId, string videoId, CancellationToken ct = default);

    Task AddAsync(SongKaraokeUrl url, CancellationToken ct = default);
    Task RemoveAsync(int songId, string videoId, CancellationToken ct = default);

    /// <summary>Increments PlayCount by 1 and sets LastUsedAt = UtcNow for the given (songId, videoId).</summary>
    Task IncrementPlayCountAsync(int songId, string videoId, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
