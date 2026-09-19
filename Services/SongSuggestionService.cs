using Microsoft.Extensions.DependencyInjection;
using MyVocaList.Contracts.DTOs;
using MyVocaList.Contracts.DTOs.List;
using MyVocaList.Contracts.DTOs.Suggestions;
using MyVocaList.Domain.Constants;
using MyVocaList.Domain.Entity;
using MyVocaList.Domain.RepositoryInterface;
using MyVocaList.Domain.Resolution;
using MyVocaList.Domain.ServicesInterfaces;
using MyVocaList.Domain.UnitOfWork;
using MyVocaList.Extensions.Strings;

namespace MyVocaList.Services;

/// <inheritdoc />
public class SongSuggestionService : ISongSuggestionService
{
    private const int MaxSuggestions = 5;
    private const string MusicBrainzProviderName = "MusicBrainz";
    private const string DeezerProviderName = "Deezer";

    private readonly ISongRepository _songRepository;
    private readonly IArtistRepository _artistRepository;
    private readonly IUnitOfWork _uow;
    private readonly IReadOnlyList<IMusicMetadataProvider> _providers;
    private readonly ISimilarityScorer _scorer;
    private readonly ILogger<SongSuggestionService> _logger;

    public SongSuggestionService(
        ISongRepository songRepository,
        IArtistRepository artistRepository,
        IUnitOfWork uow,
        IEnumerable<IMusicMetadataProvider> providers,
        ISimilarityScorer scorer,
        ILogger<SongSuggestionService> logger)
    {
        _songRepository = songRepository;
        _artistRepository = artistRepository;
        _uow = uow;
        _providers = providers.ToList();
        _scorer = scorer;
        _logger = logger;
    }

    /// <inheritdoc />
    // [AC] REQ-UOW-38/51: read scoped through IUnitOfWork; guard stays outside the lambda and is
    // measured on the trimmed term (Length < MinimumLocalQueryLength), not IsNullOrWhiteSpace.
    public async Task<IReadOnlyList<SongSuggestionDto>> GetLocalAsync(string term, CancellationToken ct = default)
    {
        var normalizedTerm = term.NormalizeSearchQuery();
        if (normalizedTerm.Length < SearchConstants.MinimumLocalQueryLength)
            return [];

        var (items, _) = await _uow.ExecuteReadAsync(async sp =>
        {
            // REQ-UOW-37: resolved from the lambda's own scope — never the constructor field.
            var songRepository = sp.GetRequiredService<ISongRepository>();
            return await songRepository.GetPagedAsync(1, MaxSuggestions, normalizedTerm, ct);
        }, ct);
        return items.Select(MapLocal).ToList();
    }

    /// <inheritdoc />
    // [AC] REQ-UOW-52: below the remote threshold, no provider fetch is issued.
    public async Task<IReadOnlyList<SongSuggestionDto>> GetRemoteAsync(
        string term, string? artistHint, IReadOnlyList<SongSuggestionDto> localResults, CancellationToken ct = default)
    {
        var normalizedTerm = term.NormalizeSearchQuery();
        if (normalizedTerm.Length < SearchConstants.MinimumRemoteQueryLength)
            return [];

        var raw = await FetchFromProvidersAsync(normalizedTerm, artistHint, ct);
        if (raw.Count == 0)
            return [];

        var deduped = await DedupAsync(raw, localResults, ct);
        if (deduped.Count == 0)
            return [];

        var withArtistIds = await ResolveLocalArtistIdsAsync(deduped, ct);
        return withArtistIds.Take(MaxSuggestions).ToList();
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static SongSuggestionDto MapLocal(SongListItemDto dto) => new(
        dto.Id,
        dto.Title,
        dto.OriginalArtistName ?? string.Empty,
        dto.OriginalArtistId,
        null,
        dto.ExternalProvider,
        false);

    private async Task<IReadOnlyList<MusicSearchResultDto>> FetchFromProvidersAsync(
        string term, string? artistHint, CancellationToken ct)
    {
        var musicBrainz = _providers.FirstOrDefault(p => p.ProviderName == MusicBrainzProviderName);
        var results = await TrySearchAsync(musicBrainz, term, artistHint, ct);
        if (results.Count > 0)
            return results;

        var deezer = _providers.FirstOrDefault(p => p.ProviderName == DeezerProviderName);
        return await TrySearchAsync(deezer, term, artistHint, ct);
    }

    private async Task<IReadOnlyList<MusicSearchResultDto>> TrySearchAsync(
        IMusicMetadataProvider? provider, string term, string? artistHint, CancellationToken ct)
    {
        if (provider is null)
            return [];

        try
        {
            var results = await provider.SearchSongsAsync(term, artistHint, ct);
            return results?.ToList() ?? [];
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Song suggestion provider {Provider} failed for term '{Term}'", provider.ProviderName, term);
            return [];
        }
    }

    private async Task<List<SongSuggestionDto>> DedupAsync(
        IReadOnlyList<MusicSearchResultDto> raw, IReadOnlyList<SongSuggestionDto> localResults, CancellationToken ct)
    {
        // Tier (a): external id equal to a local suggestion's (ExternalProvider, ExternalId).
        var afterExternalId = raw
            .Where(r => !localResults.Any(l => l.ExternalProvider == r.Provider && l.ExternalId == r.ExternalId))
            .ToList();

        if (afterExternalId.Count == 0)
            return [];

        // Tier (b): title collation-equal to a local record — one batch call (REQ-FORMUX-03).
        var titles = afterExternalId
            .Select(r => r.SongTitle)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t!)
            .Distinct()
            .ToList();

        // [AC] REQ-UOW-38/43: its own short-lived read scope — the network fetch above has already
        // completed, so no scope is ever pinned across HTTP I/O (D3/§ 4a).
        var collated = titles.Count > 0
            ? await _uow.ExecuteReadAsync(async sp =>
            {
                // REQ-UOW-37: resolved from the lambda's own scope — never the constructor field.
                var songRepository = sp.GetRequiredService<ISongRepository>();
                return await songRepository.GetByTitlesCollatedAsync(titles, ct);
            }, ct)
            : (IReadOnlyList<Song>)[];

        var afterCollation = afterExternalId
            .Where(r => string.IsNullOrWhiteSpace(r.SongTitle) ||
                        !collated.Any(s => string.Equals(s.Title, r.SongTitle, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        // Tier (c): similarity score >= threshold against any local suggestion's title.
        var afterSimilarity = afterCollation
            .Where(r => !localResults.Any(l =>
                _scorer.Score(r.SongTitle ?? string.Empty, l.Title) >= SimilarityConstants.DefaultThreshold))
            .ToList();

        return afterSimilarity
            .Select(r => new SongSuggestionDto(
                null, r.SongTitle ?? string.Empty, r.ArtistName, null, r.ExternalId, r.Provider, true))
            .ToList();
    }

    private async Task<List<SongSuggestionDto>> ResolveLocalArtistIdsAsync(
        List<SongSuggestionDto> remoteSuggestions, CancellationToken ct)
    {
        var artistNames = remoteSuggestions
            .Select(r => r.ArtistName)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct()
            .ToList();

        if (artistNames.Count == 0)
            return remoteSuggestions;

        // [AC] REQ-UOW-38/43: its own short-lived read scope — a second, separate scope from
        // DedupAsync's (D3/§ 4a); never enclosing the provider fetch that already completed.
        var matchedArtists = await _uow.ExecuteReadAsync(async sp =>
        {
            // REQ-UOW-37: resolved from the lambda's own scope — never the constructor field.
            var artistRepository = sp.GetRequiredService<IArtistRepository>();
            return await artistRepository.GetByNamesCollatedAsync(artistNames, ct);
        }, ct);
        if (matchedArtists.Count == 0)
            return remoteSuggestions;

        return remoteSuggestions
            .Select(r =>
            {
                var match = matchedArtists.FirstOrDefault(
                    a => string.Equals(a.Name, r.ArtistName, StringComparison.OrdinalIgnoreCase));
                return match is null ? r : r with { LocalArtistId = match.Id };
            })
            .ToList();
    }
}
