using MyVocaList.Contracts.DTOs.List;

namespace MyVocaList.Contracts.Messages;

public sealed record YouTubeVideoPickedMessage(YouTubeSearchResultDto Result);
