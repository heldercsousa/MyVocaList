using MyVocaList.Contracts.DTOs.Suggestions;

namespace MyVocaList.Contracts.DTOs;

/// <summary>REQ-MSRC-11: where a shared YouTube link goes. <paramref name="SongId"/> is the stored song that holds the video (edit mode), or null (add mode).</summary>
public sealed record SharedVideoTarget(string VideoId, int? SongId);

/// <summary>
/// REQ-MSRC-12: what the form takes from a shared video. <paramref name="CleanedTitle"/> is the cleaned video title (may be empty);
/// <paramref name="Match"/> is the best MusicBrainz recording, or null when nothing matched.
/// </summary>
public sealed record VideoPrefill(string CleanedTitle, SongSuggestionDto? Match);
