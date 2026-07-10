using MyVocaList.Contracts.DTOs.Suggestions;

namespace MyVocaList.Services;

public interface ISongSuggestionService
{
    /// <summary>Local song-title suggestions for a term (registered songs; title + artist name), max 5.</summary>
    Task<IReadOnlyList<SongSuggestionDto>> GetLocalAsync(string term, CancellationToken ct = default);

    /// <summary>Remote suggestions — same provider order and dedup/failure semantics as
    /// IArtistSuggestionService.GetRemoteAsync (MusicBrainz first, Deezer fallback per AC-4.2).</summary>
    Task<IReadOnlyList<SongSuggestionDto>> GetRemoteAsync(
        string term, string? artistHint, IReadOnlyList<SongSuggestionDto> localResults, CancellationToken ct = default);
}
