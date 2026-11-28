using System.Net.Http.Json;
using System.Text.Json.Serialization;
using MyVocaList.Contracts.DTOs;
using MyVocaList.Domain.ServicesInterfaces;

namespace MyVocaList.Services;

/// <summary>
/// REQ-MSRC-14: <c>https://www.youtube.com/oembed?url=...&amp;format=json</c> (keyless). The relative URI resolves against the
/// typed client's <c>BaseAddress</c>; the User-Agent is set at registration.
/// </summary>
public sealed class YouTubeOEmbedClient : IYouTubeOEmbedClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<YouTubeOEmbedClient> _logger;

    public YouTubeOEmbedClient(HttpClient httpClient, ILogger<YouTubeOEmbedClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<YouTubeVideoInfo?> GetVideoInfoAsync(string videoId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(videoId)) return null;

        var watchUrl = $"https://www.youtube.com/watch?v={videoId}";
        var url = $"oembed?url={Uri.EscapeDataString(watchUrl)}&format=json";
        try
        {
            using var response = await _httpClient.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("YouTube oEmbed answered {Status} for video {VideoId}", (int)response.StatusCode, videoId);
                return null;
            }

            var body = await response.Content.ReadFromJsonAsync<OEmbedResponse>(ct);
            if (body is null || string.IsNullOrWhiteSpace(body.Title)) return null;

            return new YouTubeVideoInfo(body.Title.Trim(),
                string.IsNullOrWhiteSpace(body.ThumbnailUrl) ? null : body.ThumbnailUrl,
                string.IsNullOrWhiteSpace(body.AuthorName) ? null : body.AuthorName.Trim());
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "YouTube oEmbed request failed for video {VideoId}", videoId);
            return null;
        }
    }

    private record OEmbedResponse(
        [property: JsonPropertyName("title")] string? Title,
        [property: JsonPropertyName("thumbnail_url")] string? ThumbnailUrl,
        [property: JsonPropertyName("author_name")] string? AuthorName = null);
}
