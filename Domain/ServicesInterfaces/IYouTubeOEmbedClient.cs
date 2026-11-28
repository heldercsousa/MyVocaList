using MyVocaList.Contracts.DTOs;

namespace MyVocaList.Domain.ServicesInterfaces;

/// <summary>REQ-MSRC-14: keyless YouTube oEmbed lookup of a video's title and thumbnail.</summary>
public interface IYouTubeOEmbedClient
{
    /// <summary>
    /// The title and thumbnail of the video, or null when the video is unknown, the device is offline or the request
    /// fails. Expected failures never throw (they are logged); only cancellation propagates.
    /// </summary>
    Task<YouTubeVideoInfo?> GetVideoInfoAsync(string videoId, CancellationToken ct = default);
}
