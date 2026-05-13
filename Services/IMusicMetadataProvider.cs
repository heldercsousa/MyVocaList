using MyVocaList.Contracts.DTOs;

namespace MyVocaList.Services;

public interface IMusicMetadataProvider
{
    /// <summary>Gets the name of this provider (e.g. "MusicBrainz", "Deezer").</summary>
    string ProviderName { get; }

    /// <summary>Searches for artists matching the given term.</summary>
    /// <param name="term">The search term.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Up to 5 matching results, or empty if the provider fails.</returns>
    Task<IEnumerable<MusicSearchResultDto>> SearchArtistsAsync(string term, CancellationToken ct = default);

    /// <summary>Searches for songs matching the given term, optionally filtered by artist.</summary>
    /// <param name="term">The song title search term.</param>
    /// <param name="artistHint">Optional artist name to narrow the search.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Up to 5 matching results, or empty if the provider fails.</returns>
    Task<IEnumerable<MusicSearchResultDto>> SearchSongsAsync(string term, string? artistHint = null, CancellationToken ct = default);
}
