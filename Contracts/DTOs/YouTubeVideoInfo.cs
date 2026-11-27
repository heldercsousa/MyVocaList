namespace MyVocaList.Contracts.DTOs;

/// <summary>REQ-MSRC-14: the title and thumbnail address YouTube oEmbed returns for one video.</summary>
public record YouTubeVideoInfo(string Title, string? ThumbnailUrl);
