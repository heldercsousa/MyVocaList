using MyVocaList.Contracts.DTOs.List;
using MyVocaList.Domain.Entity;
using MyVocaList.Domain.RepositoryInterface;
using MyVocaList.Domain.ServicesInterfaces;

namespace MyVocaList.Services;

public class SongService : ISongService
{
    private readonly ISongRepository _songRepository;
    private readonly IArtistRepository _artistRepository;
    private readonly ILogger<SongService> _logger;

    public int MaxTitleLength => 100;
    public int ShowCounterAt => 80;

    public SongService(
        ISongRepository songRepository,
        IArtistRepository artistRepository,
        ILogger<SongService> logger)
    {
        _songRepository = songRepository;
        _artistRepository = artistRepository;
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
    public async Task<(bool success, string message, Song? song)> CreateSongAsync(
        int artistId, string title, string? featuredArtists = null, string? lyrics = null,
        string? externalId = null, string? externalProvider = null, CancellationToken ct = default)
    {
        var (isValid, message) = ValidateTitleInput(title);
        if (!isValid)
            return (false, message, null);

        title = title.Trim();
        var normalized = title.ToLowerInvariant();

        var artist = await _artistRepository.GetByIdAsync(artistId, ct);
        if (artist == null)
            return (false, "Artist not found", null);

        if (await _songRepository.ExistsByTitleForArtistAsync(artistId, normalized, ct))
            return (false, "A song with this title already exists for this artist", null);

        var song = new Song
        {
            ArtistId = artistId,
            Title = title,
            TitleNormalized = normalized,
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
        CancellationToken ct = default)
    {
        var (isValid, message) = ValidateTitleInput(title);
        if (!isValid)
            return (false, message);

        title = title.Trim();
        var normalized = title.ToLowerInvariant();

        var song = await _songRepository.GetByIdAsync(id, ct);
        if (song == null)
            return (false, "Song not found");

        if (await _songRepository.ExistsByTitleForArtistAsync(song.ArtistId, normalized, id, ct))
            return (false, "A song with this title already exists for this artist");

        song.Title = title;
        song.TitleNormalized = normalized;
        song.FeaturedArtists = featuredArtists?.Trim();
        song.Lyrics = lyrics;
        song.UpdatedAt = DateTime.UtcNow;
        song.HasManualEdits = hasManualEdits;

        await _songRepository.UpdateAsync(song, ct);
        await _songRepository.SaveChangesAsync(ct);
        return (true, $"Song updated to '{title}'");
    }

    /// <inheritdoc />
    public async Task<bool> ExistsByTitleForArtistAsync(
        string title, int artistId, int? excludeId = null, CancellationToken ct = default)
    {
        var normalized = title.Trim().ToLowerInvariant();
        return excludeId == null
            ? await _songRepository.ExistsByTitleForArtistAsync(artistId, normalized, ct)
            : await _songRepository.ExistsByTitleForArtistAsync(artistId, normalized, excludeId.Value, ct);
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

        var normalized = string.IsNullOrWhiteSpace(query) ? null : query.Trim().ToLowerInvariant();
        var (items, totalCount) = await _songRepository.GetPagedAsync(pageNumber, pageSize, normalized, ct);

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
