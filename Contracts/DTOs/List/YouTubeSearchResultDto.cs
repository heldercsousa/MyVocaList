namespace MyVocaList.Contracts.DTOs.List;

public record YouTubeSearchResultDto(
    string VideoId,
    string Title,
    string ChannelName,
    int? DurationSeconds,
    string ThumbnailUrl);
