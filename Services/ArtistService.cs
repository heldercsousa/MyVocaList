using Microsoft.Extensions.DependencyInjection;
using MyVocaList.Domain.Entity;
using MyVocaList.Domain.ReadModels;
using MyVocaList.Domain.RepositoryInterface;
using MyVocaList.Domain.ServicesInterfaces;
using MyVocaList.Domain.UnitOfWork;
using MyVocaList.Extensions.Strings;

namespace MyVocaList.Services;

public class ArtistService : IArtistService
{
    private readonly IArtistRepository _artistRepository;
    private readonly ISongRepository _songRepository;
    private readonly ICatalogRepository _catalogRepository;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ArtistService> _logger;

    public int MaxInputLength => 60;
    public int ShowCounterAt => 50;

    public ArtistService(
        IArtistRepository artistRepository,
        ISongRepository songRepository,
        ICatalogRepository catalogRepository,
        IUnitOfWork uow,
        ILogger<ArtistService> logger)
    {
        _artistRepository = artistRepository;
        _songRepository = songRepository;
        _catalogRepository = catalogRepository;
        _uow = uow;
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
    public Task<(bool success, string message, Artist? artist)> CreateArtistAsync(
        string name, string? externalId = null, string? externalProvider = null, CancellationToken ct = default)
        => _uow.ExecuteAsync<(bool success, string message, Artist? artist)>(async sp =>
        {
            // REQ-UOW-28: resolved from the lambda's own scope — never the constructor field.
            var artistRepository = sp.GetRequiredService<IArtistRepository>();

            var (isValid, message) = ValidateNameInput(name);
            if (!isValid)
                return (false, message, null);

            // Artist.Name/ExternalId trimming is enforced by the EF Core ValueConverter configured
            // in ArtistConfiguration (design.md § D3) — not here. EF applies the same converter to
            // this WHERE parameter, so an untrimmed check value still matches trimmed stored rows.
            if (await artistRepository.ExistsByNameAsync(name, ct))
                return (false, "An artist with this name already exists", null);

            var artist = new Artist
            {
                Name = name,
                ExternalId = string.IsNullOrWhiteSpace(externalId) ? null : externalId,
                ExternalProvider = string.IsNullOrWhiteSpace(externalProvider) ? null : externalProvider,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await artistRepository.AddAsync(artist, ct);
            // SaveChangesAsync deleted — the single save is owned by IUnitOfWork (REQ-UOW-10).
            return (true, $"Artist '{name.Trim()}' created successfully", artist);
        }, ct);

    /// <inheritdoc />
    public Task<(bool success, string message)> UpdateArtistAsync(
        int id, string name, CancellationToken ct = default)
        => _uow.ExecuteAsync<(bool success, string message)>(async sp =>
        {
            // REQ-UOW-28: resolved from the lambda's own scope — never the constructor field.
            var artistRepository = sp.GetRequiredService<IArtistRepository>();

            var (isValid, message) = ValidateNameInput(name);
            if (!isValid)
                return (false, message);

            var artist = await artistRepository.GetByIdAsync(id, ct);
            if (artist == null)
                return (false, "Artist not found");

            // Artist.Name trimming is enforced by the EF Core ValueConverter configured in
            // ArtistConfiguration (design.md § D3) — not here. EF applies the same converter to
            // this WHERE parameter, so an untrimmed check value still matches trimmed stored rows.
            if (await artistRepository.ExistsByNameAsync(name, id, ct))
                return (false, "An artist with this name already exists");

            artist.Name = name;
            artist.UpdatedAt = DateTime.UtcNow;
            artist.HasManualEdits = true;

            await artistRepository.UpdateAsync(artist, ct);
            // SaveChangesAsync deleted — the single save is owned by IUnitOfWork (REQ-UOW-10).
            return (true, $"Artist updated to '{name.Trim()}'");
        }, ct);

    /// <inheritdoc />
    public Task<(bool success, string message)> DeleteArtistsAsync(
        IEnumerable<int> ids, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(ids);

        return _uow.ExecuteAsync<(bool success, string message)>(async sp =>
        {
            // REQ-UOW-28: both repositories resolved from the lambda's own scope — never the
            // constructor fields. IArtistRepository.DeleteAsync is ExecuteDeleteAsync-based
            // (REQ-UOW-33), so the explicit transaction opened by IUnitOfWork is what makes the
            // bulk delete atomic with the per-id validation above it.
            var artistRepository = sp.GetRequiredService<IArtistRepository>();
            var catalogRepository = sp.GetRequiredService<ICatalogRepository>();

            var idList = ids.ToList();
            if (idList.Count == 0)
                return (false, "No artist selected for deletion");

            foreach (var id in idList)
            {
                var artist = await artistRepository.GetByIdAsync(id, ct);
                if (artist == null)
                    continue;

                var songCount = await catalogRepository.CountByArtistAsync(id, ct);
                if (songCount > 0)
                    return (false, $"'{artist.Name}' has {songCount} song(s) and cannot be deleted");
            }

            await artistRepository.DeleteAsync(idList, ct);
            return (true, idList.Count == 1 ? "Artist deleted" : $"{idList.Count} artists deleted");
        }, ct);
    }

    /// <inheritdoc />
    public async Task<(IEnumerable<ArtistListItem> items, int totalCount)> GetPagedArtistsForListAsync(
        int pageNumber, int pageSize, string query = null,
        ArtistRoleFilter roleFilter = ArtistRoleFilter.All, CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageNumber);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);

        return await _artistRepository.GetPagedAsync(
            pageNumber, pageSize, query.NormalizeSearchQuery(), roleFilter, ct);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ArtistListItem>> SearchArtistsByNameAsync(
        string query, int maxResults = 5, CancellationToken ct = default)
    {
        var normalized = query.NormalizeSearchQuery();
        if (string.IsNullOrWhiteSpace(normalized))
            return [];

        return await _artistRepository.SearchByNameAsync(normalized, maxResults, ct);
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
        // Error only when ValidateNameInput would reject the same length (counter threshold
        // alignment, Form Validation Standard) — exactly MaxInputLength is still valid.
        var isError = currentLength > MaxInputLength;
        return (text, isWarning, isError);
    }
}
