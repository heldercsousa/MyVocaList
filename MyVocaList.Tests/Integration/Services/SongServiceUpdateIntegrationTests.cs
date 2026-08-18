using MyVocaList.Domain.Entity;
using MyVocaList.Domain.RepositoryInterface;
using MyVocaList.Domain.ServicesInterfaces;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Integration.Services;

/// <summary>
/// BUG-068 (Critical) / BUG-067 (REQ-ACREATE-16): reproduces the edit-mode save flow — a read of
/// the Song for page hydration (SongFormViewModel.LoadSongForEditAsync) followed by
/// SongService.UpdateSongAsync, with the hydration read served by the long-lived window-scope
/// AppDbContext exactly as MAUI's DI resolves it (a single root scope for the whole app lifetime —
/// there is no per-page child scope).
///
/// Per testing.md § Project anti-patterns this runs against REAL SQLite — a mocked ISongRepository
/// (as SongServiceTests uses) cannot reach DbSet.Update's identity-map code and is why 535/535 was
/// green while every device save failed.
///
/// MIGRATED 2026-08-18 (merge of develop into feat/uow-pilot): the original file constructed
/// SongService by hand over a single AppDbContext and called ISongRepository.SaveChangesAsync.
/// Both are gone under the unit-of-work pattern (REQ-UOW-11 retired the repository save entry
/// points; SongService now takes IUnitOfWork). The harness is now UnitOfWorkTestHost — the real
/// production composition — so these two acceptance criteria are asserted against the shipping
/// write path. No assertion was weakened or removed.
/// </summary>
public class SongServiceUpdateIntegrationTests
{
    // [AC] REQ-ACREATE-16: a changed artist on a saved song is persisted, even after the form's
    // edit-mode hydration read (SongFormViewModel.LoadSongForEditAsync -> GetSongByIdAsync) has
    // already run once on the long-lived window scope, as it always does before Save is reachable.
    [Fact]
    public async Task UpdateSongAsync_AfterPriorHydrationRead_PersistsChangedArtistWithoutThrowing()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var songService = host.Resolve<ISongService>();
        var songRepo = host.Resolve<ISongRepository>();

        var originalArtistId = await SeedArtistAsync(host, "Original Artist");
        var newArtistId = await SeedArtistAsync(host, "New Artist");

        var (created, createMessage, song) = await songService.CreateSongAsync(
            originalArtistId, "Some Title", ct: CancellationToken.None);
        Assert.True(created, createMessage);
        var songId = song!.Id;

        // Simulate SongFormViewModel.LoadSongForEditAsync's hydration read — happens on every
        // edit-mode page open, on the window-scope AppDbContext that outlives the page.
        var hydrationRead = await songRepo.GetByIdAsync(songId, CancellationToken.None);
        Assert.NotNull(hydrationRead);

        // Simulate Save — SongFormViewModel.ExecuteEditSaveAsync -> SongService.UpdateSongAsync.
        var (success, message) = await songService.UpdateSongAsync(
            songId, "Some Title", featuredArtists: null, lyrics: null, hasManualEdits: true,
            artistId: newArtistId, ct: CancellationToken.None);

        Assert.True(success, message);

        var reloaded = await songRepo.GetByIdAsync(songId, CancellationToken.None);
        Assert.Equal(newArtistId, reloaded!.ArtistId);
    }

    // [AC] REQ-ACREATE-16: covers "face 2" from T10 re-run #5 — a second write against a Song row
    // already written once through the app's own write path (its creation) must not throw an EF
    // identity-map conflict, and must actually persist.
    [Fact]
    public async Task UpdateSongAsync_SongAlreadyWrittenOnceInSession_PersistsWithoutThrowing()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var songService = host.Resolve<ISongService>();
        var songRepo = host.Resolve<ISongRepository>();

        var originalArtistId = await SeedArtistAsync(host, "Original Artist");
        var newArtistId = await SeedArtistAsync(host, "New Artist");

        var (created, createMessage, song) = await songService.CreateSongAsync(
            originalArtistId, "Some Title 2", ct: CancellationToken.None);
        Assert.True(created, createMessage);
        var songId = song!.Id;

        var (success, message) = await songService.UpdateSongAsync(
            songId, "Some Title 2", featuredArtists: null, lyrics: null, hasManualEdits: true,
            artistId: newArtistId, ct: CancellationToken.None);

        Assert.True(success, message);

        var reloaded = await songRepo.GetByIdAsync(songId, CancellationToken.None);
        Assert.Equal(newArtistId, reloaded!.ArtistId);
    }

    private static async Task<int> SeedArtistAsync(UnitOfWorkTestHost host, string name)
    {
        var artistService = host.Resolve<IArtistService>();
        var (success, message, artist) = await artistService.CreateArtistAsync(name);
        Assert.True(success, message);
        return artist!.Id;
    }
}
