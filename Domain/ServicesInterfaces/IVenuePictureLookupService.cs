using MyVocaList.Contracts.DTOs.Venues;

namespace MyVocaList.Domain.ServicesInterfaces;

/// <summary>
/// Resolves the picture address of a looked-up place from its open-data picture source (REQ-VEN-38, 39, 40).
/// Returns null when there is no picture, when the device is offline, or when any lookup fails; never throws
/// for expected failures.
/// </summary>
public interface IVenuePictureLookupService
{
    /// <summary>Resolves one picture address for the source, or null when none can be obtained.</summary>
    Task<Uri?> ResolveAsync(VenuePictureSource source, CancellationToken ct = default);
}
