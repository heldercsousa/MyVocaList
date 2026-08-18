using Microsoft.Extensions.DependencyInjection;
using MyVocaList.Contracts.DTOs.List;
using MyVocaList.Domain.Entity;
using MyVocaList.Domain.RepositoryInterface;
using MyVocaList.Domain.ServicesInterfaces;
using MyVocaList.Domain.UnitOfWork;
using MyVocaList.Extensions.Strings;

namespace MyVocaList.Services;

public class SongService : ISongService
{
    private readonly ISongRepository _songRepository;
    private readonly IArtistRepository _artistRepository;
    private readonly ISongKaraokeUrlRepository _urlRepository;
    private readonly ISongKaraokeUrlService _urlService;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<SongService> _logger;

    public int MaxTitleLength => 100;
    public int ShowCounterAt => 80;
    public int MaxVersionLength => 60;

    public SongService(
        ISongRepository songRepository,
        IArtistRepository artistRepository,
        ISongKaraokeUrlRepository urlRepository,
        ISongKaraokeUrlService urlService,
        IUnitOfWork uow,
        ILogger<SongService> logger)
    {
        _songRepository = songRepository;
        _artistRepository = artistRepository;
        _urlRepository = urlRepository;
        _urlService = urlService;
        _uow = uow;
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
    public Task<(bool success, string message, Song? song)> CreateSongAsync(
        int artistId, string title, string? featuredArtists = null, string? lyrics = null,
        string? externalId = null, string? externalProvider = null, CancellationToken ct = default)
        => _uow.ExecuteAsync<(bool success, string message, Song? song)>(async sp =>
        {
            // REQ-UOW-28: both repositories resolved from the lambda's own scope — never the
            // constructor fields, which hold the window-scope AppDbContext that caused BUG-068.
            var songRepository = sp.GetRequiredService<ISongRepository>();
            var artistRepository = sp.GetRequiredService<IArtistRepository>();

            var (isValid, message) = ValidateTitleInput(title);
            if (!isValid)
                return (false, message, null);

            var artist = await artistRepository.GetByIdAsync(artistId, ct);
            if (artist == null)
                return (false, "Artist not found", null);

            // Song.Title trimming is enforced by the EF Core ValueConverter configured in
            // SongConfiguration (design.md § D3) — not here. EF applies the same converter to this
            // WHERE parameter, so an untrimmed check value still matches trimmed stored rows.
            if (await songRepository.ExistsByTitleForArtistAsync(artistId, title, ct))
                return (false, "A song with this title already exists for this artist", null);

            // Song.Title/FeaturedArtists trimming is enforced by the ValueConverter (design.md § D3).
            var song = new Song
            {
                ArtistId = artistId,
                Title = title,
                FeaturedArtists = featuredArtists,
                Lyrics = lyrics,
                ExternalId = externalId,
                ExternalProvider = externalProvider,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await songRepository.AddAsync(song, ct);
            // SaveChangesAsync deleted — the single save is owned by IUnitOfWork (REQ-UOW-10).
            return (true, $"Song '{title.Trim()}' created successfully", song);
        }, ct);

    /// <inheritdoc />
    public Task<(bool success, string message)> UpdateSongAsync(
        int id, string title, string? featuredArtists, string? lyrics, bool hasManualEdits,
        string? externalId = null, string? externalProvider = null, string? version = null,
        CancellationToken ct = default)
        => _uow.ExecuteAsync<(bool success, string message)>(async sp =>
        {
            // REQ-UOW-28: resolved from the lambda's own scope — never the constructor field.
            var songRepository = sp.GetRequiredService<ISongRepository>();

            var (isValid, message) = ValidateTitleInput(title);
            if (!isValid)
                return (false, message);

            if (version != null)
            {
                var (versionValid, versionMessage) = ValidateVersionInput(version);
                if (!versionValid)
                    return (false, versionMessage);
            }

            var song = await songRepository.GetByIdAsync(id, ct);
            if (song == null)
                return (false, "Song not found");

            // Song.Title trimming is enforced by the EF Core ValueConverter configured in
            // SongConfiguration (design.md § D3) — not here. EF applies the same converter to this
            // WHERE parameter, so an untrimmed check value still matches trimmed stored rows.
            if (await songRepository.ExistsByTitleForArtistAsync(song.ArtistId, title, id, ct))
                return (false, "A song with this title already exists for this artist");

            // Song.Title/FeaturedArtists/Version trimming is enforced by the ValueConverter (design.md § D3).
            song.Title = title;
            song.FeaturedArtists = featuredArtists;
            song.Lyrics = lyrics;
            song.UpdatedAt = DateTime.UtcNow;
            song.HasManualEdits = hasManualEdits;

            // BUG-024: persist the version label when provided; null = keep existing value
            // (mirrors the externalId/externalProvider null-keeps-existing semantics below).
            if (version != null)
                song.Version = version;

            // M2: persist external identity when provided; null = keep existing value
            if (externalId != null)
                song.ExternalId = externalId;
            if (externalProvider != null)
                song.ExternalProvider = externalProvider;

            await songRepository.UpdateAsync(song, ct);
            // SaveChangesAsync deleted — the single save is owned by IUnitOfWork (REQ-UOW-10).
            return (true, $"Song updated to '{title}'");
        }, ct);

    /// <inheritdoc />
    public Task<(bool success, string message, Song? song)> CreateSongWithUrlsAsync(
        int artistId, string title, string version, string? featuredArtists, string? lyrics,
        string? externalId, string? externalProvider,
        IEnumerable<string> urls,
        CancellationToken ct = default)
        => _uow.ExecuteAsync<(bool success, string message, Song? song)>(async sp =>
        {
            // REQ-UOW-28: all three repositories AND the nested URL service are resolved from the
            // lambda's own scope — never the constructor fields. All three repositories therefore
            // share ONE AppDbContext, which is what keeps REQ-UOW-07's single-save FK fixup valid.
            var songRepository = sp.GetRequiredService<ISongRepository>();
            var artistRepository = sp.GetRequiredService<IArtistRepository>();
            var urlRepository = sp.GetRequiredService<ISongKaraokeUrlRepository>();
            var urlService = sp.GetRequiredService<ISongKaraokeUrlService>();

            var (isValid, message) = ValidateTitleInput(title);
            if (!isValid)
                return (false, message, null);

            version ??= string.Empty;

            var artist = await artistRepository.GetByIdAsync(artistId, ct);
            if (artist == null)
                return (false, "Artist not found", null);

            // Song.Title/Version trimming is enforced by the EF Core ValueConverter configured in
            // SongConfiguration (design.md § D3) — not here. EF applies the same converter to this
            // WHERE parameter, so an untrimmed check value still matches trimmed stored rows.
            if (await songRepository.ExistsByTitleVersionForArtistAsync(artistId, title, version, ct))
                return (false, "A song with this title and version already exists for this artist", null);

            // Song.Title/Version/FeaturedArtists trimming is enforced by the ValueConverter (design.md § D3).
            var song = new Song
            {
                ArtistId = artistId,
                Title = title,
                Version = version,
                FeaturedArtists = featuredArtists,
                Lyrics = lyrics,
                ExternalId = externalId,
                ExternalProvider = externalProvider,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Stage the song entity (no SaveChangesAsync yet)
            await songRepository.AddAsync(song, ct);

            // Stage URL entities on the SAME context — N3: one SaveChangesAsync commits both atomically
            var urlList = urls?.ToList() ?? [];
            foreach (var rawUrl in urlList)
            {
                var videoId = urlService.ExtractVideoId(rawUrl);
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

                await urlRepository.AddAsync(urlEntity, ct);
            }

            // REQ-UOW-07: the single commit — song + all URLs — is now owned by IUnitOfWork, which
            // saves the one shared context inside its explicit transaction (AC-6.2 preserved).
            return (true, $"Song '{title}' created successfully", song);
        }, ct);

    /// <inheritdoc />
    public async Task<Song?> GetSongByIdAsync(int id, CancellationToken ct = default)
        => await _songRepository.GetByIdAsync(id, ct);

    /// <inheritdoc />
    public async Task<bool> ExistsByTitleForArtistAsync(
        string title, int artistId, int? excludeId = null, CancellationToken ct = default)
    {
        var trimmed = title.NormalizeSearchQuery();
        return excludeId == null
            ? await _songRepository.ExistsByTitleForArtistAsync(artistId, trimmed, ct)
            : await _songRepository.ExistsByTitleForArtistAsync(artistId, trimmed, excludeId.Value, ct);
    }

    /// <inheritdoc />
    public Task<(bool success, string message)> DeleteSongsAsync(
        IEnumerable<int> ids, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(ids);

        return _uow.ExecuteAsync<(bool success, string message)>(async sp =>
        {
            // REQ-UOW-28: the repository is resolved from the lambda's own scope — never the
            // constructor field, which holds the window-scope AppDbContext that caused BUG-068.
            // ISongRepository.DeleteAsync is ExecuteDeleteAsync-based (REQ-UOW-33), so the explicit
            // transaction opened by IUnitOfWork is what brings the bulk delete under the unit of work.
            var songRepository = sp.GetRequiredService<ISongRepository>();

            var idList = ids.ToList();
            if (idList.Count == 0)
                return (false, "No song selected for deletion");

            await songRepository.DeleteAsync(idList, ct);
            return (true, idList.Count == 1 ? "Song deleted" : $"{idList.Count} songs deleted");
        }, ct);
    }

    /// <inheritdoc />
    public async Task<(IEnumerable<SongListItemDto> items, int totalCount)> GetPagedSongsForListAsync(
        int pageNumber, int pageSize, string? query = null, CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageNumber);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);

        var normalizedQuery = string.IsNullOrWhiteSpace(query) ? null : query.NormalizeSearchQuery();
        var (items, totalCount) = await _songRepository.GetPagedAsync(pageNumber, pageSize, normalizedQuery, ct);

        return (items, totalCount);
    }

    /// <inheritdoc />
    public bool ShouldShowCharacterCounter(int currentLength) => currentLength > ShowCounterAt;

    /// <inheritdoc />
    public (string text, bool isWarning, bool isError) GetCharacterCounterInfo(int currentLength)
    {
        var text = $"{currentLength}/{MaxTitleLength}";
        var isWarning = currentLength > 90;
        // Error only when ValidateTitleInput would reject the same length (counter threshold
        // alignment, Form Validation Standard) — exactly MaxTitleLength is still valid.
        var isError = currentLength > MaxTitleLength;
        return (text, isWarning, isError);
    }
}
