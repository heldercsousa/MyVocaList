using MyVocaList.Contracts.DTOs;
using MyVocaList.Domain.ServicesInterfaces;

namespace MyVocaList.Services;

/// <summary>
/// Temporary stub — always returns null. Replaced by WhatsNewService in MauiProgram.cs.
/// </summary>
public sealed class NullWhatsNewService : IWhatsNewService
{
    /// <inheritdoc />
    public Task<ReleaseEntry?> GetCurrentReleaseAsync(CancellationToken ct = default)
        => Task.FromResult<ReleaseEntry?>(null);

    /// <inheritdoc />
    public Task<ReleaseEntry?> GetPendingReleaseAsync(CancellationToken ct = default)
        => Task.FromResult<ReleaseEntry?>(null);

    /// <inheritdoc />
    public void MarkCurrentVersionSeen() { }
}
