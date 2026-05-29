using System.Net.Http.Json;
using MyVocaList.Contracts.DTOs.List;
using MyVocaList.Domain.ServicesInterfaces;

namespace MyVocaList.Services;

public class YouTubeSearchService : IYouTubeSearchService
{
    private const string SearchEndpoint =
        "https://www.googleapis.com/youtube/v3/search?part=snippet&type=video&maxResults=5&q={0}&key={1}";
    private const string VideosEndpoint =
        "https://www.googleapis.com/youtube/v3/videos?part=contentDetails&id={0}&key={1}";

    private readonly IHttpClientFactory _httpFactory;
    private readonly ISecureStorageWrapper _secureStorage;
    private readonly ILogger<YouTubeSearchService> _logger;

    public YouTubeSearchService(
        IHttpClientFactory httpFactory,
        ISecureStorageWrapper secureStorage,
        ILogger<YouTubeSearchService> logger)
    {
        _httpFactory = httpFactory;
        _secureStorage = secureStorage;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<YouTubeSearchResultDto>> SearchAsync(string query, CancellationToken ct = default)
    {
        var apiKey = await _secureStorage.GetAsync("youtube_api_key");
        if (string.IsNullOrEmpty(apiKey))
            return [];

        try
        {
            var client = _httpFactory.CreateClient();
            var url = string.Format(SearchEndpoint, Uri.EscapeDataString(query), apiKey);
            var response = await client.GetFromJsonAsync<YouTubeSearchResponse>(url, ct);
            if (response?.Items is not { Length: > 0 })
                return [];

            var videoIds = string.Join(",", response.Items.Select(i => i.Id.VideoId));
            var durationsUrl = string.Format(VideosEndpoint, videoIds, apiKey);
            var durationsResponse = await client.GetFromJsonAsync<YouTubeVideosResponse>(durationsUrl, ct);

            var durations = durationsResponse?.Items?
                .ToDictionary(v => v.Id, v => ParseIso8601Duration(v.ContentDetails?.Duration))
                ?? new Dictionary<string, int?>();

            return response.Items.Select(i =>
            {
                var videoId = i.Id.VideoId;
                return new YouTubeSearchResultDto(
                    videoId,
                    i.Snippet.Title,
                    i.Snippet.ChannelTitle,
                    durations.GetValueOrDefault(videoId),
                    $"https://img.youtube.com/vi/{videoId}/mqdefault.jpg");
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "YouTube search failed for query: {Query}", query);
            return [];
        }
    }

    /// <inheritdoc />
    public async Task<bool> ValidateApiKeyAsync(string apiKey, CancellationToken ct = default)
    {
        try
        {
            var client = _httpFactory.CreateClient();
            var url = string.Format(SearchEndpoint, "test", apiKey);
            var response = await client.GetAsync(url, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "YouTube API key validation failed");
            return false;
        }
    }

    private static int? ParseIso8601Duration(string? iso)
    {
        if (string.IsNullOrEmpty(iso)) return null;
        var match = System.Text.RegularExpressions.Regex.Match(iso,
            @"PT(?:(\d+)H)?(?:(\d+)M)?(?:(\d+)S)?");
        if (!match.Success) return null;
        var h = int.TryParse(match.Groups[1].Value, out var hh) ? hh : 0;
        var m = int.TryParse(match.Groups[2].Value, out var mm) ? mm : 0;
        var s = int.TryParse(match.Groups[3].Value, out var ss) ? ss : 0;
        return h * 3600 + m * 60 + s;
    }

    private record YouTubeSearchResponse(YouTubeSearchItem[] Items);
    private record YouTubeSearchItem(YouTubeSearchItemId Id, YouTubeSnippet Snippet);
    private record YouTubeSearchItemId(string VideoId);
    private record YouTubeSnippet(string Title, string ChannelTitle);
    private record YouTubeVideosResponse(YouTubeVideoItem[]? Items);
    private record YouTubeVideoItem(string Id, YouTubeContentDetails? ContentDetails);
    private record YouTubeContentDetails(string? Duration);
}
