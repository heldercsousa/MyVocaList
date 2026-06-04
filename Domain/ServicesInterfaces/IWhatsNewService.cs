using MyVocaList.Contracts.DTOs;

namespace MyVocaList.Domain.ServicesInterfaces;

public interface IWhatsNewService
{
    /// <summary>Returns the current version's release entry for display (e.g. About page). Never checks seen status.</summary>
    Task<ReleaseEntry?> GetCurrentReleaseAsync(CancellationToken ct = default);

    /// <summary>Returns the current version's release entry only if the user has not seen it yet. Returns null on fresh install, same version, or no matching entry.</summary>
    Task<ReleaseEntry?> GetPendingReleaseAsync(CancellationToken ct = default);

    /// <summary>Persists the current version as seen so GetPendingReleaseAsync returns null on subsequent launches.</summary>
    void MarkCurrentVersionSeen();
}
