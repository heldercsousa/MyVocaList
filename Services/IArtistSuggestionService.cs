using MyVocaList.Contracts.DTOs.Suggestions;

namespace MyVocaList.Services;

public interface IArtistSuggestionService
{
    /// <summary>Local artist-name search: FTS candidates ranked by <c>RankScore</c>, at most 5; below 2 trimmed
    /// characters returns empty (REQ-SFT-03/47).</summary>
    Task<IReadOnlyList<ArtistSuggestionDto>> SearchNameLocalAsync(string query, CancellationToken ct = default);

    /// <summary>Deezer artist-name search with an explicit status (REQ-SFT-06/08/09/48/53). Names the database
    /// collation matches to a stored artist are dropped. Below 3 trimmed characters returns empty with <c>Ok</c>.
    /// Caller cancellation is rethrown (stale query); provider failure never throws.</summary>
    Task<ArtistRemoteSearchResult> SearchNameRemoteAsync(string query, CancellationToken ct = default);
}
