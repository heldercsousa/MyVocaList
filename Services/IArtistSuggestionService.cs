using MyVocaList.Contracts.DTOs.Suggestions;

namespace MyVocaList.Services;

public interface IArtistSuggestionService
{
    /// <summary>Local artist suggestions for a term (DB collation match, max 5). Immediate path.</summary>
    Task<IReadOnlyList<ArtistSuggestionDto>> GetLocalAsync(string term, CancellationToken ct = default);

    /// <summary>Remote suggestions — MusicBrainz first, Deezer fallback on empty/error, per AC-4.2.
    /// Deduplicated against localResults per REQ-FORMUX-03
    /// (external-id → collation-equal name via batch DB lookup → similarity ≥ threshold). Max 5.
    /// Returns an empty list on provider failure (logged) — never throws for provider errors.</summary>
    Task<IReadOnlyList<ArtistSuggestionDto>> GetRemoteAsync(
        string term, IReadOnlyList<ArtistSuggestionDto> localResults, CancellationToken ct = default);

    /// <summary>Classifies cached suggestions against the typed term: similar = score ≥
    /// SimilarityConstants.DefaultThreshold AND not exact. Pure in-memory — no I/O, no refetch.</summary>
    IReadOnlyList<ArtistSuggestionDto> FilterSimilar(string typedName, IReadOnlyList<ArtistSuggestionDto> fetched);
}
