namespace MyVocaList.Contracts.DTOs.Suggestions;

/// <summary>
/// Outcome of one remote title search (design § 3.3, REQ-SFT-05/06/53).
/// </summary>
/// <param name="Promoted">Remote hits that already exist in the library, returned as local rows (REQ-SFT-05).</param>
/// <param name="Remote">At most <c>MaxSearchResults</c> remote rows not in the library.</param>
/// <param name="Status">Whether the provider answered, was skipped (offline) or failed.</param>
public sealed record SongRemoteSearchResult(
    IReadOnlyList<SongSuggestionDto> Promoted,
    IReadOnlyList<SongSuggestionDto> Remote,
    RemoteSearchStatus Status);
