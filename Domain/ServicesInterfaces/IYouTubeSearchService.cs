using MyVocaList.Contracts.DTOs.List;

namespace MyVocaList.Domain.ServicesInterfaces;

public interface IYouTubeSearchService
{
    /// <summary>Returns up to 5 results. Returns empty list when no API key is configured.</summary>
    Task<IEnumerable<YouTubeSearchResultDto>> SearchAsync(string query, CancellationToken ct = default);

    /// <summary>Makes a minimal API call to verify the key is valid.</summary>
    Task<bool> ValidateApiKeyAsync(string apiKey, CancellationToken ct = default);
}
