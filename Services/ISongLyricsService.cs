using MyVocaList.Contracts.DTOs;

namespace MyVocaList.Services;

/// <summary>
/// Lyrics lookup for the Song form's Deezer pick flow: offline skip, time bound, one retry, length limit and trim
/// (REQ-LYR-02, 03, 05, 06, 07, 08).
/// </summary>
public interface ISongLyricsService
{
    /// <summary>Lyrics for the query, or null (offline, no match, over the limit, bound reached, source failure).</summary>
    /// <remarks>Never throws except <see cref="OperationCanceledException"/> when the caller's token is cancelled.</remarks>
    Task<string?> FindLyricsAsync(LyricsQuery query, CancellationToken ct = default);
}
