using MyVocaList.Contracts.DTOs;
using MyVocaList.Domain.ServicesInterfaces;

namespace MyVocaList.Services;

/// <summary>
/// Temporary stub — always returns null so the What's New section stays hidden.
/// Replace this registration in MauiProgram.cs when the real WhatsNewService is implemented.
/// </summary>
public sealed class NullWhatsNewService : IWhatsNewService
{
    /// <inheritdoc />
    public Task<ReleaseEntry?> GetCurrentReleaseAsync(CancellationToken ct = default)
        => Task.FromResult<ReleaseEntry?>(null);
}
