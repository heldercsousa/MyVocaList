namespace MyVocaList.Contracts.DTOs.Suggestions;

/// <summary>
/// Outcome of one remote artist-name search (REQ-SFT-47, REQ-SFT-53). Consumed by the Artist form suggestion pipeline (6.1).
/// </summary>
/// <param name="Remote">Remote artist rows; empty when the query was not sent or failed.</param>
/// <param name="Status">Whether Deezer answered, was offline, or failed (REQ-SFT-06).</param>
public sealed record ArtistRemoteSearchResult(IReadOnlyList<ArtistSuggestionDto> Remote, RemoteSearchStatus Status);
