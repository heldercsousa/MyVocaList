using MyVocaList.Contracts.DTOs.Suggestions;

namespace MyVocaList.Services;

public interface ISongSuggestionService
{
    /// <summary>Local title search: FTS candidates ranked by title and title + artist; below 2 trimmed characters returns empty (REQ-SFT-03/04/30/32).</summary>
    Task<IReadOnlyList<SongSuggestionDto>> SearchTitleLocalAsync(string query, CancellationToken ct = default);

    /// <summary>Deezer title search with library dedup/promotion and an explicit status (REQ-SFT-05/06/08/09/53).</summary>
    /// <param name="query">Raw user query; below 3 trimmed characters returns empty with <c>Ok</c>.</param>
    /// <param name="shownLocalSongIds">Ids of the local options already shown; matches among them are dropped, not promoted.</param>
    /// <param name="ct">Caller cancellation; rethrown (stale query).</param>
    Task<SongRemoteSearchResult> SearchTitleRemoteAsync(
        string query, IReadOnlyCollection<int> shownLocalSongIds, CancellationToken ct = default);

    /// <summary>Local song-title suggestions for a term (registered songs; title + artist name), max 5.</summary>
    Task<IReadOnlyList<SongSuggestionDto>> GetLocalAsync(string term, CancellationToken ct = default);

    /// <summary>Remote suggestions — same provider order and dedup/failure semantics as
    /// IArtistSuggestionService.GetRemoteAsync (MusicBrainz first, Deezer fallback per AC-4.2).</summary>
    Task<IReadOnlyList<SongSuggestionDto>> GetRemoteAsync(
        string term, string? artistHint, IReadOnlyList<SongSuggestionDto> localResults, CancellationToken ct = default);
}
