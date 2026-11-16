using MyVocaList.Contracts.DTOs;
using MyVocaList.Domain.Constants;
using MyVocaList.Domain.ServicesInterfaces;

namespace MyVocaList.Services;

/// <inheritdoc />
public sealed class SongLyricsService : ISongLyricsService
{
    private readonly ILyricsProvider _provider;
    private readonly INetworkStatus _networkStatus;
    private readonly ILogger<SongLyricsService> _logger;

    public SongLyricsService(ILyricsProvider provider, INetworkStatus networkStatus, ILogger<SongLyricsService> logger)
    {
        _provider = provider;
        _networkStatus = networkStatus;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string?> FindLyricsAsync(LyricsQuery query, CancellationToken ct = default)
        => (await LookupLyricsAsync(query, ct)).Lyrics;

    /// <inheritdoc />
    // [AC] REQ-LYR-02/03/05/06/07/08: blank and offline guards, one bounded lookup with at most one retry, trim and length limit.
    // REQ-SLD-22/23: a provider answer of no record, blank or over-limit text is NoMatch; every other end is NoAnswer.
    public async Task<LyricsLookupResult> LookupLyricsAsync(LyricsQuery query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query.Title) || string.IsNullOrWhiteSpace(query.ArtistName))
            return LyricsLookupResult.NoAnswer;

        // Read live on every call, never cached (REQ-LYR-07).
        if (!_networkStatus.HasInternetAccess)
        {
            _logger.LogInformation("Lyrics lookup skipped: device has no internet access");
            return LyricsLookupResult.NoAnswer;
        }

        // One linked token for both attempts, so the bound covers the whole lookup (REQ-LYR-06).
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct);
        linked.CancelAfter(SearchConstants.LyricsLookupTimeoutMilliseconds);
        try
        {
            var answer = await _provider.FetchLyricsAsync(query, linked.Token);
            if (answer is null && HasAlbumOrDuration(query))
                answer = await _provider.FetchLyricsAsync(query with { AlbumTitle = null, DurationSeconds = null }, linked.Token);

            return AcceptLyrics(answer);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning(
                "Lyrics lookup for '{Title}' by '{Artist}' did not finish within {Timeout} ms",
                query.Title, query.ArtistName, SearchConstants.LyricsLookupTimeoutMilliseconds);
            return LyricsLookupResult.NoAnswer;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lyrics lookup failed for '{Title}' by '{Artist}'", query.Title, query.ArtistName);
            return LyricsLookupResult.NoAnswer;
        }
    }

    private static bool HasAlbumOrDuration(LyricsQuery query) =>
        !string.IsNullOrWhiteSpace(query.AlbumTitle) || query.DurationSeconds is not null;

    /// <summary>
    /// Trims the answer. A null answer, blank text and text longer than the limit are a definite no match
    /// (REQ-LYR-03, 05; REQ-SLD-22). Otherwise the trimmed text is found lyrics.
    /// </summary>
    private LyricsLookupResult AcceptLyrics(string? answer)
    {
        var trimmed = answer?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return LyricsLookupResult.NoMatch;

        if (trimmed.Length > SearchConstants.LyricsMaxLength)
        {
            _logger.LogInformation("Lyrics match ignored: {Length} characters exceed the {Max} limit",
                trimmed.Length, SearchConstants.LyricsMaxLength);
            return LyricsLookupResult.NoMatch;
        }

        return LyricsLookupResult.Found(trimmed);
    }
}
