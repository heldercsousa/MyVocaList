using MyVocaList.Contracts.DTOs;

namespace MyVocaList.Services;

public interface IMusicMetadataService
{
    /// <summary>Searches for artists matching the given term using the configured provider chain.</summary>
    Task<IEnumerable<MusicSearchResultDto>> SearchArtistsAsync(string term, CancellationToken ct = default);

    /// <summary>Searches for songs matching the given term using the configured provider chain.</summary>
    Task<IEnumerable<MusicSearchResultDto>> SearchSongsAsync(string term, string? artistHint = null, CancellationToken ct = default);
}
