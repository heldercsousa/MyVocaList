using MyVocaList.Contracts.DTOs;

namespace MyVocaList.Domain.ServicesInterfaces;

public interface IVersionCheckService
{
    /// <summary>Fetches the version manifest and determines if the current app version requires action. Never throws — returns UpToDate on any error.</summary>
    Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken ct = default);
}
