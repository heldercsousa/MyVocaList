using Microsoft.Extensions.DependencyInjection;
using MyVocaList.Contracts.DTOs.List;
using MyVocaList.Domain.Entity;
using MyVocaList.Domain.RepositoryInterface;
using MyVocaList.Domain.ServicesInterfaces;
using MyVocaList.Domain.UnitOfWork;
using System.Text.RegularExpressions;

namespace MyVocaList.Services;

public class SongKaraokeUrlService : ISongKaraokeUrlService
{
    private readonly ISongKaraokeUrlRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<SongKaraokeUrlService> _logger;

    // Matches the 11-char video ID segment in all supported YouTube URL formats
    private static readonly Regex VideoIdRegex = new(
        @"(?:youtube\.com/(?:watch\?v=|embed/|shorts/)|youtu\.be/)([A-Za-z0-9_-]{11})",
        RegexOptions.Compiled);

    public SongKaraokeUrlService(ISongKaraokeUrlRepository repo, IUnitOfWork uow, ILogger<SongKaraokeUrlService> logger)
    {
        _repo = repo;
        _uow = uow;
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
    public Task<(bool success, string message, SongKaraokeUrlDto? url)> AddUrlAsync(
        int songId, string rawUrl, string? label = null, CancellationToken ct = default)
    {
        var videoId = ExtractVideoId(rawUrl);
        if (videoId is null)
            return Task.FromResult<(bool success, string message, SongKaraokeUrlDto? url)>(
                (false, "Not a valid YouTube URL.", null));

        return _uow.ExecuteAsync<(bool success, string message, SongKaraokeUrlDto? url)>(async sp =>
        {
            // REQ-UOW-28: resolved from the lambda's own scope — never the constructor field.
            var repo = sp.GetRequiredService<ISongKaraokeUrlRepository>();

            if (await repo.ExistsAsync(songId, videoId, ct))
                return (false, "This URL is already saved for this song.", null);

            var entity = new SongKaraokeUrl
            {
                SongId = songId,
                VideoId = videoId,
                Label = label?.Trim() is { Length: > 0 } l ? l : null,
                AddedAt = DateTime.UtcNow
            };

            await repo.AddAsync(entity, ct);
            // SaveChangesAsync deleted — the single save is owned by IUnitOfWork (REQ-UOW-10).
            return (true, string.Empty, ToDto(entity, isSuggested: false));
        }, ct);
    }

    /// <inheritdoc />
    public Task<(bool success, string message)> RemoveUrlAsync(
        int songId, string videoId, CancellationToken ct = default)
        => _uow.ExecuteAsync<(bool success, string message)>(async sp =>
        {
            // REQ-UOW-28: resolved from the lambda's own scope — never the constructor field.
            // REQ-UOW-33: RemoveAsync is ExecuteDeleteAsync-based; the explicit transaction opened
            // by IUnitOfWork.ExecuteAsync brings this bulk delete under the unit of work.
            var repo = sp.GetRequiredService<ISongKaraokeUrlRepository>();

            if (!await repo.ExistsAsync(songId, videoId, ct))
                return (false, "URL not found.");

            await repo.RemoveAsync(songId, videoId, ct);
            return (true, string.Empty);
        }, ct);

    /// <inheritdoc />
    public Task RecordPlayAsync(int songId, string videoId, CancellationToken ct = default)
        => _uow.ExecuteAsync(async sp =>
        {
            // REQ-UOW-28: resolved from the lambda's own scope — never the constructor field.
            // REQ-UOW-26: no-signal overload — always saves; RecordPlayAsync has no failure mode.
            // REQ-UOW-33: IncrementPlayCountAsync is ExecuteUpdateAsync-based; the explicit
            // transaction opened by IUnitOfWork.ExecuteAsync brings this bulk update under the unit of work.
            var repo = sp.GetRequiredService<ISongKaraokeUrlRepository>();
            await repo.IncrementPlayCountAsync(songId, videoId, ct);
        }, ct);

    /// <inheritdoc />
    public async Task<SongKaraokeUrlDto?> GetSuggestedUrlAsync(int songId, CancellationToken ct = default)
    {
        var entity = await _repo.GetSuggestedAsync(songId, ct);
        return entity is null ? null : ToDto(entity, isSuggested: true);
    }

    private static SongKaraokeUrlDto ToDto(SongKaraokeUrl u, bool isSuggested)
        => new(u.VideoId, u.SongId, u.PlayCount, u.DurationSeconds, u.LastUsedAt, u.AddedAt, u.Label, isSuggested);
}
