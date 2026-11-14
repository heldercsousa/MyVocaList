using Microsoft.Extensions.DependencyInjection;
using MyVocaList.Domain.RepositoryInterface;
using MyVocaList.Domain.ServicesInterfaces;
using MyVocaList.Domain.UnitOfWork;

namespace MyVocaList.Services;

/// <inheritdoc />
public class ImageMaintenanceService : IImageMaintenanceService
{
    /// <summary>Files younger than this are never swept (REQ-PIC-11).</summary>
    public static readonly TimeSpan MinimumOrphanAge = TimeSpan.FromHours(24);

    private readonly ISongRepository _songRepository;
    private readonly IArtistRepository _artistRepository;
    private readonly IImageStore _imageStore;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ImageMaintenanceService> _logger;

    public ImageMaintenanceService(
        ISongRepository songRepository,
        IArtistRepository artistRepository,
        IImageStore imageStore,
        IUnitOfWork uow,
        ILogger<ImageMaintenanceService> logger)
    {
        _songRepository = songRepository;
        _artistRepository = artistRepository;
        _imageStore = imageStore;
        _uow = uow;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<int> SweepOrphansAsync(CancellationToken ct = default)
    {
        var references = await _uow.ExecuteReadAsync(async sp =>
        {
            // Resolved from the lambda's own scope — never a constructor field.
            var songs = await sp.GetRequiredService<ISongRepository>().GetLocalImageReferencesAsync(ct);
            var artists = await sp.GetRequiredService<IArtistRepository>().GetLocalImageReferencesAsync(ct);
            return songs.Concat(artists).Distinct(StringComparer.Ordinal).ToList();
        }, ct);

        var deleted = await _imageStore.DeleteUnreferencedAsync(references, MinimumOrphanAge, ct);
        _logger.LogInformation("Orphan image sweep deleted {Count} file(s)", deleted);
        return deleted;
    }
}
