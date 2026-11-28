namespace MyVocaList.Contracts.DTOs;

/// <summary>REQ-MSRC-14: the title and thumbnail address YouTube oEmbed returns for one video; REQ-MSRC-12: plus the channel name.</summary>
public record YouTubeVideoInfo(string Title, string? ThumbnailUrl, string? AuthorName = null);
