using MyVocaList.Contracts.DTOs.List;
using MyVocaList.Domain.Entity;
using MyVocaList.Domain.RepositoryInterface;
using MyVocaList.Domain.ServicesInterfaces;

namespace MyVocaList.Services;

public class SongService : ISongService
{
    private readonly ISongRepository _songRepository;
    private readonly IArtistRepository _artistRepository;
    private readonly ISongKaraokeUrlRepository _urlRepository;
    private readonly ISongKaraokeUrlService _urlService;
    private readonly ILogger<SongService> _logger;

    public int MaxTitleLength => 100;
    public int ShowCounterAt => 80;
    public int MaxVersionLength => 60;

    public SongService(
        ISongRepository songRepository,
        IArtistRepository artistRepository,
        ISongKaraokeUrlRepository urlRepository,
        ISongKaraokeUrlService urlService,
        ILogger<SongService> logger)
    {
        _songRepository = songRepository;
        _artistRepository = artistRepository;
        _urlRepository = urlRepository;
        _urlService = urlService;
        _logger = logger;
    }

    /// <inheritdoc />
    public (bool isValid, string message) ValidateTitleInput(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return (false, "Song title is required");

        title = title.Trim();

        if (title.Length > MaxTitleLength)
            return (false, $"Title is too long. Maximum {MaxTitleLength} characters.");

        return (true, string.Empty);
    }

    /// <inheritdoc />
    public (bool isValid, string message) ValidateVersionInput(string? version, bool isRequired = false)
    {
        var trimmed = (version ?? string.Empty).Trim();

        if (isRequired && trimmed.Length == 0)
            return (false, "A version label is required (e.g. Live, Acoustic, Remix)");

        if (trimmed.Length > MaxVersionLength)
            return (false, $"Version is too long. Maximum {MaxVersionLength} characters.");

        return (true, string.Empty);
    }

    /// <inheritdoc />
    public async Task<(bool success, string message, Song? song)> CreateSongAsync(
        int artistId, string title, string? featuredArtists = null, string? lyrics = null,
        string? externalId = null, string? externalProvider = null, CancellationToken ct = default)
    {
        var (isValid, message) = ValidateTitleInput(title);
        if (!isValid)
            return (false, message, null);

        title = title.Trim();

        var artist = await _artistRepository.GetByIdAsync(artistId, ct);
        if (artist == null)
            return (false, "Artist not found", null);

        if (await _songRepository.ExistsByTitleForArtistAsync(artistId, title, ct))
            return (false, "A song with this title already exists for this artist", null);

        var song = new Song
        {
            ArtistId = artistId,
            Title = title,
            FeaturedArtists = featuredArtists?.Trim(),
            Lyrics = lyrics,
            ExternalId = externalId,
            ExternalProvider = externalProvider,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _songRepository.AddAsync(song, ct);
        await _songRepository.SaveChangesAsync(ct);
        return (true, $"Song '{title}' created successfully", song);
    }

    /// <inheritdoc />
    public async Task<(bool success, string message)> UpdateSongAsync(
        int id, string title, string? featuredArtists, string? lyrics, bool hasManualEdits,
        string? externalId = null, string? externalProvider = null,
        CancellationToken ct = default)
    {
        var (isValid, message) = ValidateTitleInput(title);
        if (!isValid)
            return (false, message);

        title = title.Trim();

        var song = await _songRepository.GetByIdAsync(id, ct);
        if (song == null)
            return (false, "Song not found");

        if (await _songRepository.ExistsByTitleForArtistAsync(song.ArtistId, title, id, ct))
            return (false, "A song with this title already exists for this artist");

        song.Title = title;
        song.FeaturedArtists = featuredArtists?.Trim();
        song.Lyrics = lyrics;
        song.UpdatedAt = DateTime.UtcNow;
        song.HasManualEdits = hasManualEdits;

        // M2: persist external identity when provided; null = keep existing value
        if (externalId != null)
            song.ExternalId = externalId;
        if (externalProvider != null)
            song.ExternalProvider = externalProvider;

        await _songRepository.UpdateAsync(song, ct);
        await _songRepository.SaveChangesAsync(ct);
        return (true, $"Song updated to '{title}'");
    }

    /// <inheritdoc />
    public async Task<(bool success, string message, Song? song)> CreateSongWithUrlsAsync(
        int artistId, string title, string version, string? featuredArtists, string? lyrics,
        string? externalId, string? externalProvider,
        IEnumerable<string> urls,
        CancellationToken ct = default)
    {
        var (isValid, message) = ValidateTitleInput(title);
        if (!isValid)
            return (false, message, null);

        title = title.Trim();
        version = (version ?? string.Empty).Trim();

        var artist = await _artistRepository.GetByIdAsync(artistId, ct);
        if (artist == null)
            return (false, "Artist not found", null);

        // Use the 3-column unique check (artistId, title, version) — AC-5.5 / N3
        if (await _songRepository.ExistsByTitleVersionForArtistAsync(artistId, title, version, ct))
            return (false, "A song with this title and version already exists for this artist", null);

        var song = new Song
        {
            ArtistId = artistId,
            Title = title,
            Version = version,
            FeaturedArtists = featuredArtists?.Trim(),
            Lyrics = lyrics,
            ExternalId = externalId,
            ExternalProvider = externalProvider,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Stage the song entity (no SaveChangesAsync yet)
        await _songRepository.AddAsync(song, ct);

        // Stage URL entities on the SAME context — N3: one SaveChangesAsync commits both atomically
        var urlList = urls?.ToList() ?? [];
        foreach (var rawUrl in urlList)
        {
            var videoId = _urlService.ExtractVideoId(rawUrl);
            if (videoId is null)
            {
                _logger.LogWarning("Skipping invalid YouTube URL during atomic song create: {Url}", rawUrl);
                continue;
            }

            var urlEntity = new SongKaraokeUrl
            {
                // SongId is set by EF Core via the navigation after SaveChangesAsync resolves
                // the song's generated PK. We set the reference via Song navigation property.
                Song = song,
                VideoId = videoId,
                AddedAt = DateTime.UtcNow
            };

            await _urlRepository.AddAsync(urlEntity, ct);
        }

        // Single commit — song + all URLs in one transaction (AC-6.2)
        await _songRepository.SaveChangesAsync(ct);
        return (true, $"Song '{title}' created successfully", song);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsByTitleForArtistAsync(
        string title, int artistId, int? excludeId = null, CancellationToken ct = default)
    {
        var trimmed = title.Trim();
        return excludeId == null
            ? await _songRepository.ExistsByTitleForArtistAsync(artistId, trimmed, ct)
            : await _songRepository.ExistsByTitleForArtistAsync(artistId, trimmed, excludeId.Value, ct);
    }

    /// <inheritdoc />
    public async Task<(bool success, string message)> DeleteSongsAsync(
        IEnumerable<int> ids, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(ids);

        var idList = ids.ToList();
        if (idList.Count == 0)
            return (false, "No song selected for deletion");

        await _songRepository.DeleteAsync(idList, ct);
        return (true, idList.Count == 1 ? "Song deleted" : $"{idList.Count} songs deleted");
    }

    /// <inheritdoc />
    public async Task<(IEnumerable<SongListItemDto> items, int totalCount)> GetPagedSongsForListAsync(
        int pageNumber, int pageSize, string? query = null, CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageNumber);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);

        var (items, totalCount) = await _songRepository.GetPagedAsync(pageNumber, pageSize, query?.Trim(), ct);

        return (items, totalCount);
    }

    /// <inheritdoc />
    public bool ShouldShowCharacterCounter(int currentLength) => currentLength > ShowCounterAt;

    /// <inheritdoc />
    public (string text, bool isWarning, bool isError) GetCharacterCounterInfo(int currentLength)
    {
        var text = $"{currentLength}/{MaxTitleLength}";
        var isWarning = currentLength > 90;
        var isError = currentLength >= MaxTitleLength;
        return (text, isWarning, isError);
    }
}
