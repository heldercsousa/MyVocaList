using MyVocaList.Contracts.DTOs;
using MyVocaList.Contracts.DTOs.Suggestions;
using MyVocaList.Domain.Constants;
using MyVocaList.Domain.Rules;
using Microsoft.Extensions.DependencyInjection;
using MyVocaList.Domain.RepositoryInterface;
using MyVocaList.Domain.ServicesInterfaces;
using MyVocaList.Domain.UnitOfWork;

namespace MyVocaList.Services;

/// <inheritdoc />
public sealed class SharedVideoService : ISharedVideoService
{
    private const string Separator = " - ";

    private readonly IYouTubeOEmbedClient _oEmbed;
    private readonly IReadOnlyList<IMusicMetadataProvider> _providers;
    // Governed dependency kept for the REQ-UOW-36/37 convention; never dereferenced - the repository comes from the scope's `sp`.
    private readonly ISongKaraokeUrlRepository _urlRepository;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<SharedVideoService> _logger;

    public SharedVideoService(
        IYouTubeOEmbedClient oEmbed, IEnumerable<IMusicMetadataProvider> providers,
        ISongKaraokeUrlRepository urlRepository, IUnitOfWork uow, ILogger<SharedVideoService> logger)
    {
        _oEmbed = oEmbed;
        _providers = providers.ToList();
        _urlRepository = urlRepository;
        _uow = uow;
        _logger = logger;
    }

    /// <inheritdoc />
    // [AC] REQ-MSRC-11
    public async Task<SharedVideoTarget?> ResolveTargetAsync(string sharedText, CancellationToken ct = default)
    {
        if (!YouTubeLinkRules.TryExtractVideoId(sharedText, out var videoId))
            return null;

        try
        {
            // REQ-UOW-37: the repository comes from the lambda's own scope.
            var songId = await _uow.ExecuteReadAsync(
                sp => sp.GetRequiredService<ISongKaraokeUrlRepository>().FindSongIdByVideoIdAsync(videoId, ct), ct);
            return new SharedVideoTarget(videoId, songId);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // The lookup failing must not lose the link: the form still opens, in add mode.
            _logger.LogWarning(ex, "Looking up the song of shared video {VideoId} failed", videoId);
            return new SharedVideoTarget(videoId, null);
        }
    }

    /// <inheritdoc />
    // [AC] REQ-MSRC-12
    public async Task<VideoPrefill?> PrefillAsync(string videoId, CancellationToken ct = default)
    {
        var info = await _oEmbed.GetVideoInfoAsync(videoId, ct);
        if (info is null)
            return null;

        var cleaned = KaraokeTitleRules.Clean(info.Title, info.AuthorName);
        if (cleaned.Length == 0)
            return new VideoPrefill(string.Empty, null);

        try
        {
            return new VideoPrefill(cleaned, await FindBestRecordingAsync(cleaned, ct));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MusicBrainz lookup for the title of video {VideoId} failed", videoId);
            return new VideoPrefill(cleaned, null);
        }
    }

    /// <summary>
    /// MusicBrainz is queried as <c>recording:"title" AND artist:"hint"</c>, so "Artist - Title" is split at its separators and
    /// each half is tried as the title with the other as the artist hint; the first query with a result wins, and MusicBrainz
    /// has already matched the artist. A text with no separator is searched whole. Among the results the first whose title
    /// equals the searched one (ignoring case and accents) wins, else the first.
    /// </summary>
    private async Task<SongSuggestionDto?> FindBestRecordingAsync(string cleaned, CancellationToken ct)
    {
        var provider = _providers.FirstOrDefault(p => p.ProviderName == ProviderNames.MusicBrainz);
        if (provider is null)
            return null;

        var parts = cleaned.Split(Separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var attempts = parts.Length <= 1 ? [(cleaned, (string?)null)] : SplitCandidates(parts);

        foreach (var (title, artistHint) in attempts)
        {
            var results = (await provider.SearchSongsAsync(title, artistHint, ct))
                .Where(r => !string.IsNullOrWhiteSpace(r.SongTitle) && !string.IsNullOrWhiteSpace(r.ArtistName))
                .ToList();
            var best = results.FirstOrDefault(r => string.Equals(r.SongTitle, title, StringComparison.CurrentCultureIgnoreCase))
                ?? results.FirstOrDefault();
            if (best is not null)
                return ToSuggestion(best);
        }

        return null;
    }

    private static IEnumerable<(string title, string? artist)> SplitCandidates(string[] parts)
    {
        var seen = new HashSet<(string, string)>();
        var head = parts[0];
        var tail = string.Join(Separator, parts.Skip(1));
        var last = parts[^1];
        var init = string.Join(Separator, parts.Take(parts.Length - 1));

        // YouTube convention first (Artist - Title), then the reverse, for the first and the last separator.
        foreach (var pair in new[] { (tail, head), (head, tail), (last, init), (init, last) })
            if (seen.Add(pair))
                yield return (pair.Item1, pair.Item2);
    }

    private static SongSuggestionDto ToSuggestion(MusicSearchResultDto r) => new(
        LocalId: null, Title: r.SongTitle!, ArtistName: r.ArtistName, LocalArtistId: null,
        ExternalId: r.ExternalId, ExternalProvider: r.Provider, IsRemote: true,
        ImageUrl: r.ImageUrl, ArtistImageUrl: r.ArtistImageUrl, AlbumTitle: r.AlbumTitle,
        DurationSeconds: r.DurationSeconds, ArtistExternalId: r.ExternalArtistId);
}
