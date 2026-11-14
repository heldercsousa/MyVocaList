using MyVocaList.Contracts.DTOs;

namespace MyVocaList.Services;

/// <summary>
/// Source of song lyrics (LRCLIB for the Deezer pick flow). No consumer until the lyrics service is wired.
/// </summary>
public interface ILyricsProvider
{
    /// <summary>Plain lyrics of the best record for the query, or null when the source has no usable record.</summary>
    /// <remarks>Throws on transport or protocol failure; rethrows <see cref="OperationCanceledException"/>.</remarks>
    Task<string?> FetchLyricsAsync(LyricsQuery query, CancellationToken ct = default);
}
