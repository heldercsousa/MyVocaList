using MyVocaList.Contracts.DTOs;

namespace MyVocaList.Services;

public sealed class MusicMetadataService : IMusicMetadataService
{
    private readonly IEnumerable<IMusicMetadataProvider> _providers;
    private readonly ILogger<MusicMetadataService> _logger;

    public MusicMetadataService(IEnumerable<IMusicMetadataProvider> providers, ILogger<MusicMetadataService> logger)
    {
        _providers = providers;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<MusicSearchResultDto>> SearchArtistsAsync(string term, CancellationToken ct = default)
    {
        foreach (var provider in _providers)
        {
            try
            {
                var results = await provider.SearchArtistsAsync(term, ct);
                var list = results.ToList();
                if (list.Count > 0)
                    return list;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Provider '{Provider}' failed during artist search", provider.ProviderName);
            }
        }
        return Enumerable.Empty<MusicSearchResultDto>();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<MusicSearchResultDto>> SearchSongsAsync(string term, string? artistHint = null, CancellationToken ct = default)
    {
        foreach (var provider in _providers)
        {
            try
            {
                var results = await provider.SearchSongsAsync(term, artistHint, ct);
                var list = results.ToList();
                if (list.Count > 0)
                    return list;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Provider '{Provider}' failed during song search", provider.ProviderName);
            }
        }
        return Enumerable.Empty<MusicSearchResultDto>();
    }
}
