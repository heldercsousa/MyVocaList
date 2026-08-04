using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Integration.UnitOfWork;

/// <summary>
/// Smoke test for <see cref="UnitOfWorkTestHost"/> — proves the harness reproduces the
/// production single-scope AppDbContext lifetime (Phase 0 Task 0.1).
/// </summary>
public class UnitOfWorkTestHostTests
{
    [Fact]
    public async Task LegacyHost_TwoDifferentServices_ShareOneAppDbContextInstance()
    {
        await using var host = UnitOfWorkTestHost.CreateLegacy();

        // Both services resolve AppDbContext through their repositories; under AddDbContext(Scoped)
        // in a single scope, that is one and the same instance — the precondition for BUG-068.
        var artists = host.Resolve<IArtistService>();
        var songs = host.Resolve<ISongService>();
        Assert.NotNull(artists);
        Assert.NotNull(songs);

        var (ok, _, artist) = await artists.CreateArtistAsync("Shared Context Probe");
        Assert.True(ok);
        // The entity created through ArtistService is still tracked by the context the host resolves —
        // i.e. one context spans both service calls.
        Assert.Contains(host.Db.ChangeTracker.Entries<Artist>(), e => e.Entity.Id == artist!.Id);
    }
}
