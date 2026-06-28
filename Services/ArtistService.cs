using MyVocaList.Domain.Entity;
using MyVocaList.Domain.ReadModels;
using MyVocaList.Domain.RepositoryInterface;
using MyVocaList.Domain.ServicesInterfaces;

namespace MyVocaList.Services;

public class ArtistService : IArtistService
{
    private readonly IArtistRepository _artistRepository;
    private readonly ISongRepository _songRepository;
    private readonly ICatalogRepository _catalogRepository;
    private readonly ILogger<ArtistService> _logger;

    public int MaxInputLength => 60;
    public int ShowCounterAt => 50;

    public ArtistService(
        IArtistRepository artistRepository,
        ISongRepository songRepository,
        ICatalogRepository catalogRepository,
        ILogger<ArtistService> logger)
    {
        _artistRepository = artistRepository;
        _songRepository = songRepository;
        _catalogRepository = catalogRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public (bool isValid, string message) ValidateNameInput(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return (false, "Artist name is required");

        name = name.Trim();

        if (name.Length > MaxInputLength)
            return (false, $"Name is too long. Maximum {MaxInputLength} characters.");

        return (true, string.Empty);
    }

    /// <inheritdoc />
    public async Task<(bool success, string message, Artist? artist)> CreateArtistAsync(
        string name, CancellationToken ct = default)
    {
        var (isValid, message) = ValidateNameInput(name);
        if (!isValid)
            return (false, message, null);

        name = name.Trim();

        if (await _artistRepository.ExistsByNameAsync(name, ct))
            return (false, "An artist with this name already exists", null);

        var artist = new Artist
        {
            Name = name,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _artistRepository.AddAsync(artist, ct);
        await _artistRepository.SaveChangesAsync(ct);
        return (true, $"Artist '{name}' created successfully", artist);
    }

    /// <inheritdoc />
    public async Task<(bool success, string message)> UpdateArtistAsync(
        int id, string name, CancellationToken ct = default)
    {
        var (isValid, message) = ValidateNameInput(name);
        if (!isValid)
            return (false, message);

        name = name.Trim();

        var artist = await _artistRepository.GetByIdAsync(id, ct);
        if (artist == null)
            return (false, "Artist not found");

        if (await _artistRepository.ExistsByNameAsync(name, id, ct))
            return (false, "An artist with this name already exists");

        artist.Name = name;
        artist.UpdatedAt = DateTime.UtcNow;
        artist.HasManualEdits = true;

        await _artistRepository.UpdateAsync(artist, ct);
        await _artistRepository.SaveChangesAsync(ct);
        return (true, $"Artist updated to '{name}'");
    }

    /// <inheritdoc />
    public async Task<(bool success, string message)> DeleteArtistsAsync(
        IEnumerable<int> ids, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(ids);

        var idList = ids.ToList();
        if (idList.Count == 0)
            return (false, "No artist selected for deletion");

        foreach (var id in idList)
        {
            var artist = await _artistRepository.GetByIdAsync(id, ct);
            if (artist == null)
                continue;

            var songCount = await _catalogRepository.CountByArtistAsync(id, ct);
            if (songCount > 0)
                return (false, $"'{artist.Name}' has {songCount} song(s) and cannot be deleted");
        }

        await _artistRepository.DeleteAsync(idList, ct);
        return (true, idList.Count == 1 ? "Artist deleted" : $"{idList.Count} artists deleted");
    }

    /// <inheritdoc />
    public async Task<(IEnumerable<ArtistListItem> items, int totalCount)> GetPagedArtistsForListAsync(
        int pageNumber, int pageSize, string query = null,
        ArtistRoleFilter roleFilter = ArtistRoleFilter.All, CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageNumber);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);

        return await _artistRepository.GetPagedAsync(pageNumber, pageSize, query?.Trim(), roleFilter, ct);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ArtistListItem>> SearchArtistsByNameAsync(
        string query, int maxResults = 5, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return [];

        return await _artistRepository.SearchByNameAsync(query.Trim(), maxResults, ct);
    }

    /// <inheritdoc />
    public async Task<string> GetDeleteConfirmationAsync(IEnumerable<int> ids, CancellationToken ct = default)
    {
        var idList = ids.ToList();
        if (idList.Count == 1)
        {
            var artist = await _artistRepository.GetByIdAsync(idList[0], ct);
            return artist != null
                ? $"Delete '{artist.Name}'?"
                : "Delete artist?";
        }
        return $"Delete {idList.Count} artists?";
    }

    /// <inheritdoc />
    public bool ShouldShowCharacterCounter(int currentLength) => currentLength > ShowCounterAt;

    /// <inheritdoc />
    public (string text, bool isWarning, bool isError) GetCharacterCounterInfo(int currentLength)
    {
        var text = $"{currentLength}/{MaxInputLength}";
        var isWarning = currentLength > 55;
        var isError = currentLength >= MaxInputLength;
        return (text, isWarning, isError);
    }
}
