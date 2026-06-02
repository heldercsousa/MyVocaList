using System.Text.RegularExpressions;
using MyVocaList.Contracts.DTOs.List;
using MyVocaList.Domain.Entity;
using MyVocaList.Domain.RepositoryInterface;
using MyVocaList.Domain.ServicesInterfaces;

namespace MyVocaList.Services;

public class SongKaraokeUrlService : ISongKaraokeUrlService
{
    private readonly ISongKaraokeUrlRepository _repo;
    private readonly ILogger<SongKaraokeUrlService> _logger;

    // Matches the 11-char video ID segment in all supported YouTube URL formats
    private static readonly Regex VideoIdRegex = new(
        @"(?:youtube\.com/(?:watch\?v=|embed/|shorts/)|youtu\.be/)([A-Za-z0-9_-]{11})",
        RegexOptions.Compiled);

    public SongKaraokeUrlService(ISongKaraokeUrlRepository repo, ILogger<SongKaraokeUrlService> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    /// <inheritdoc />
    public string? ExtractVideoId(string rawUrl)
    {
        if (string.IsNullOrWhiteSpace(rawUrl)) return null;
        var match = VideoIdRegex.Match(rawUrl);
        return match.Success ? match.Groups[1].Value : null;
    }

    /// <inheritdoc />
    public async Task<List<SongKaraokeUrlDto>> GetUrlsForSongAsync(int songId, CancellationToken ct = default)
    {
        var urls = await _repo.GetBySongIdAsync(songId, ct);
        var suggested = urls.FirstOrDefault();
        return urls.Select((u, i) => ToDto(u, isSuggested: i == 0 && suggested != null)).ToList();
    }

    /// <inheritdoc />
    public async Task<(bool success, string message, SongKaraokeUrlDto? url)> AddUrlAsync(
        int songId, string rawUrl, string? label = null, CancellationToken ct = default)
    {
        var videoId = ExtractVideoId(rawUrl);
        if (videoId is null)
            return (false, "Not a valid YouTube URL.", null);

        if (await _repo.ExistsAsync(songId, videoId, ct))
            return (false, "This URL is already saved for this song.", null);

        var entity = new SongKaraokeUrl
        {
            SongId = songId,
            VideoId = videoId,
            Label = label?.Trim() is { Length: > 0 } l ? l : null,
            AddedAt = DateTime.UtcNow
        };

        await _repo.AddAsync(entity, ct);
        await _repo.SaveChangesAsync(ct);

        return (true, string.Empty, ToDto(entity, isSuggested: false));
    }

    /// <inheritdoc />
    public async Task<(bool success, string message)> RemoveUrlAsync(
        int songId, string videoId, CancellationToken ct = default)
    {
        if (!await _repo.ExistsAsync(songId, videoId, ct))
            return (false, "URL not found.");

        await _repo.RemoveAsync(songId, videoId, ct);
        await _repo.SaveChangesAsync(ct);
        return (true, string.Empty);
    }

    /// <inheritdoc />
    public async Task RecordPlayAsync(int songId, string videoId, CancellationToken ct = default)
    {
        await _repo.IncrementPlayCountAsync(songId, videoId, ct);
        await _repo.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task<SongKaraokeUrlDto?> GetSuggestedUrlAsync(int songId, CancellationToken ct = default)
    {
        var entity = await _repo.GetSuggestedAsync(songId, ct);
        return entity is null ? null : ToDto(entity, isSuggested: true);
    }

    private static SongKaraokeUrlDto ToDto(SongKaraokeUrl u, bool isSuggested)
        => new(u.VideoId, u.SongId, u.PlayCount, u.DurationSeconds, u.LastUsedAt, u.AddedAt, u.Label, isSuggested);
}
