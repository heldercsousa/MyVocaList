using MyVocaList.Contracts.DTOs;
using MyVocaList.Contracts.DTOs.Suggestions;
using MyVocaList.Domain.Entity;
using MyVocaList.Domain.RepositoryInterface;
using MyVocaList.Domain.Resolution;
using MyVocaList.Domain.ServicesInterfaces;
using MyVocaList.Extensions.Strings;

namespace MyVocaList.Services;

/// <inheritdoc />
public class ArtistSuggestionService : IArtistSuggestionService
{
    private const int MaxResults = 5;
    private const string MusicBrainzProviderName = "MusicBrainz";
    private const string DeezerProviderName = "Deezer";

    private readonly IArtistRepository _artistRepository;
    private readonly IReadOnlyList<IMusicMetadataProvider> _providers;
    private readonly ISimilarityScorer _scorer;
    private readonly ILogger<ArtistSuggestionService> _logger;

    public ArtistSuggestionService(
        IArtistRepository artistRepository,
        IEnumerable<IMusicMetadataProvider> providers,
        ISimilarityScorer scorer,
        ILogger<ArtistSuggestionService> logger)
    {
        _artistRepository = artistRepository;
        _providers = providers as IReadOnlyList<IMusicMetadataProvider> ?? providers.ToList();
        _scorer = scorer;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ArtistSuggestionDto>> GetLocalAsync(string term, CancellationToken ct = default)
    {
        var trimmed = term.NormalizeSearchQuery();
        if (trimmed.Length < 2)
            return [];

        var matches = await _artistRepository.SearchByNameAsync(trimmed, MaxResults, ct);
        var exact = await _artistRepository.GetByNameAsync(trimmed, ct);

        return matches
            .Select(a => new ArtistSuggestionDto(
                LocalId: a.Id,
                Name: a.Name,
                ExternalId: null,
                ExternalProvider: null,
                IsRemote: false,
                IsExactMatch: exact is not null && exact.Id == a.Id))
            .ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ArtistSuggestionDto>> GetRemoteAsync(
        string term, IReadOnlyList<ArtistSuggestionDto> localResults, CancellationToken ct = default)
    {
        var normalizedTerm = term.NormalizeSearchQuery();
        var fetched = await FetchFromProvidersAsync(normalizedTerm, ct);
        if (fetched.Count == 0)
            return [];

        // Tier (a) — REQ-FORMUX-03: drop results whose (Provider, ExternalId) matches a local suggestion.
        var afterExternalId = fetched.Where(r => !HasExternalIdMatch(r, localResults)).ToList();
        if (afterExternalId.Count == 0)
            return [];

        // Tier (b) — REQ-FORMUX-03: drop results whose name is collation-equal to a local artist,
        // resolved via a single batch DB call.
        var candidateNames = afterExternalId
            .Select(r => r.ArtistName)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct()
            .ToList();

        IReadOnlyList<Artist> collatedMatches = candidateNames.Count > 0
            ? await _artistRepository.GetByNamesCollatedAsync(candidateNames, ct) ?? []
            : [];

        var collatedNames = collatedMatches
            .Select(a => a.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var afterCollation = afterExternalId
            .Where(r => !collatedNames.Contains(r.ArtistName))
            .ToList();
        if (afterCollation.Count == 0)
            return [];

        // Tier (c) — REQ-FORMUX-03: drop results scoring >= DefaultThreshold against any local suggestion.
        var afterSimilarity = afterCollation
            .Where(r => localResults.All(l => _scorer.Score(r.ArtistName, l.Name) < SimilarityConstants.DefaultThreshold))
            .ToList();

        return afterSimilarity
            .Take(MaxResults)
            .Select(r => new ArtistSuggestionDto(
                LocalId: null,
                Name: r.ArtistName,
                ExternalId: r.ExternalId,
                ExternalProvider: r.Provider,
                IsRemote: true,
                IsExactMatch: false))
            .ToList();
    }

    /// <inheritdoc />
    public IReadOnlyList<ArtistSuggestionDto> FilterSimilar(string typedName, IReadOnlyList<ArtistSuggestionDto> fetched)
    {
        if (string.IsNullOrWhiteSpace(typedName) || fetched is null || fetched.Count == 0)
            return [];

        return fetched
            .Where(f => !f.IsExactMatch)
            .Where(f => _scorer.Score(typedName, f.Name) >= SimilarityConstants.DefaultThreshold)
            .ToList();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<IReadOnlyList<MusicSearchResultDto>> FetchFromProvidersAsync(string term, CancellationToken ct)
    {
        var musicBrainz = _providers.FirstOrDefault(p => p.ProviderName == MusicBrainzProviderName);
        var results = await TrySearchAsync(musicBrainz, term, ct);
        if (results.Count > 0)
            return results;

        var deezer = _providers.FirstOrDefault(p => p.ProviderName == DeezerProviderName);
        return await TrySearchAsync(deezer, term, ct);
    }

    private async Task<IReadOnlyList<MusicSearchResultDto>> TrySearchAsync(
        IMusicMetadataProvider? provider, string term, CancellationToken ct)
    {
        if (provider is null)
            return [];

        try
        {
            var results = await provider.SearchArtistsAsync(term, ct);
            return results?.ToList() ?? [];
        }
        catch (OperationCanceledException)
        {
            // Cancellation is not a provider failure — propagate so stale lookups are discarded.
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Artist provider {Provider} failed for term '{Term}'", provider.ProviderName, term);
            return [];
        }
    }

    private static bool HasExternalIdMatch(MusicSearchResultDto remote, IReadOnlyList<ArtistSuggestionDto> localResults) =>
        !string.IsNullOrWhiteSpace(remote.ExternalId) &&
        localResults.Any(l =>
            string.Equals(l.ExternalProvider, remote.Provider, StringComparison.Ordinal) &&
            string.Equals(l.ExternalId, remote.ExternalId, StringComparison.Ordinal));
}
