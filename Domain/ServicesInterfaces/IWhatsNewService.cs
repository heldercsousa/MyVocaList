using MyVocaList.Contracts.DTOs;

namespace MyVocaList.Domain.ServicesInterfaces;

public interface IWhatsNewService
{
    /// <summary>Returns the release entry for the current app version, or null if no entry exists.</summary>
    Task<ReleaseEntry?> GetCurrentReleaseAsync(CancellationToken ct = default);
}
