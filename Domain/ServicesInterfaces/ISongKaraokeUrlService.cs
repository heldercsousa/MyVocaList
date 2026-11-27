using MyVocaList.Contracts.DTOs;
using MyVocaList.Contracts.DTOs.List;

namespace MyVocaList.Domain.ServicesInterfaces;

public interface ISongKaraokeUrlService
{
    /// <summary>Returns all URLs for the song, marked IsSuggested on the top-ranked one.</summary>
    Task<List<SongKaraokeUrlDto>> GetUrlsForSongAsync(int songId, CancellationToken ct = default);

    /// <summary>
    /// Normalises rawUrl to a video ID, validates it, checks for duplicates, persists.
    /// Returns (false, "This URL is already saved for this song.") on duplicate.
    /// Returns (false, "Not a valid YouTube URL.") on parse failure.
    /// </summary>
    Task<(bool success, string message, SongKaraokeUrlDto? url)> AddUrlAsync(
        int songId, string rawUrl, string? label = null, CancellationToken ct = default);

    Task<(bool success, string message)> RemoveUrlAsync(
        int songId, string videoId, CancellationToken ct = default);

    /// <summary>Increments PlayCount and sets LastUsedAt. Called on confirmed launch from queue.</summary>
    Task RecordPlayAsync(int songId, string videoId, CancellationToken ct = default);

    Task<SongKaraokeUrlDto?> GetSuggestedUrlAsync(int songId, CancellationToken ct = default);

    /// <summary>
    /// REQ-MSRC-14: fills the title and thumbnail of every stored row of the song that lacks a title (oEmbed, in place).
    /// Returns the rows that were filled; failures are logged and the row keeps falling back to the video id.
    /// </summary>
    Task<List<SongKaraokeUrlDto>> FillMissingVideoInfoAsync(int songId, CancellationToken ct = default);

    /// <summary>REQ-MSRC-14: the oEmbed title and thumbnail of a video (null on any failure), for rows not stored yet.</summary>
    Task<YouTubeVideoInfo?> GetVideoInfoAsync(string videoId, CancellationToken ct = default);

    /// <summary>
    /// Extracts the 11-char video ID from any YouTube URL format.
    /// Accepts: watch?v=, youtu.be/, /embed/, /shorts/
    /// Returns null if the input cannot be parsed.
    /// </summary>
    string? ExtractVideoId(string rawUrl);
}
