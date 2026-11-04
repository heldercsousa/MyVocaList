using MyVocaList.Contracts.DTOs;

namespace MyVocaList.Services;

public interface IMusicMetadataProvider
{
    /// <summary>Gets the name of this provider (e.g. "MusicBrainz", "Deezer").</summary>
    string ProviderName { get; }

    /// <summary>Searches for artists matching the given term.</summary>
    /// <param name="term">The search term.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Up to 5 matching results; empty when the provider has none.</returns>
    /// <exception cref="HttpRequestException">Transport failure or non-success HTTP status.</exception>
    /// <exception cref="System.Text.Json.JsonException">The response body is not valid JSON.</exception>
    /// <exception cref="MusicProviderException">The provider returned an error body.</exception>
    /// <exception cref="OperationCanceledException">The caller cancelled or the request timed out.</exception>
    Task<IEnumerable<MusicSearchResultDto>> SearchArtistsAsync(string term, CancellationToken ct = default);

    /// <summary>Searches for songs matching the given term. Failures throw, as for <see cref="SearchArtistsAsync"/>.</summary>
    /// <param name="term">The song title search term.</param>
    /// <param name="artistHint">Retained for compatibility; ignored (the query is plain, REQ-SFT-35).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Up to 5 matching results; empty when the provider has none.</returns>
    /// <exception cref="HttpRequestException">Transport failure or non-success HTTP status.</exception>
    /// <exception cref="System.Text.Json.JsonException">The response body is not valid JSON.</exception>
    /// <exception cref="MusicProviderException">The provider returned an error body.</exception>
    /// <exception cref="OperationCanceledException">The caller cancelled or the request timed out.</exception>
    Task<IEnumerable<MusicSearchResultDto>> SearchSongsAsync(string term, string? artistHint = null, CancellationToken ct = default);
}
