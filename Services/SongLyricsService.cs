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
    // [AC] REQ-LYR-02/03/05/06/07/08: blank and offline guards, one bounded lookup with at most one retry, trim and length limit.
    public async Task<string?> FindLyricsAsync(LyricsQuery query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query.Title) || string.IsNullOrWhiteSpace(query.ArtistName))
            return null;

        // Read live on every call, never cached (REQ-LYR-07).
        if (!_networkStatus.HasInternetAccess)
        {
            _logger.LogInformation("Lyrics lookup skipped: device has no internet access");
            return null;
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
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lyrics lookup failed for '{Title}' by '{Artist}'", query.Title, query.ArtistName);
            return null;
        }
    }

    private static bool HasAlbumOrDuration(LyricsQuery query) =>
        !string.IsNullOrWhiteSpace(query.AlbumTitle) || query.DurationSeconds is not null;

    /// <summary>Trims the answer; blank text and text longer than the limit are no match (REQ-LYR-03, 05).</summary>
    private string? AcceptLyrics(string? answer)
    {
        var trimmed = answer?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return null;

        if (trimmed.Length > SearchConstants.LyricsMaxLength)
        {
            _logger.LogInformation("Lyrics match ignored: {Length} characters exceed the {Max} limit",
                trimmed.Length, SearchConstants.LyricsMaxLength);
            return null;
        }

        return trimmed;
    }
}
