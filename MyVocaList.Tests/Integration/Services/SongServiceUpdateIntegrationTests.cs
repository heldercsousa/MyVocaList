using Microsoft.Extensions.Logging.Abstractions;
using MyVocaList.Domain.Entity;
using MyVocaList.Domain.RepositoryInterface;
using MyVocaList.Infra;
using MyVocaList.Infra.Repository;
using MyVocaList.Services;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Integration.Services;

/// <summary>
/// BUG-068 (Critical): reproduces the edit-mode save flow — a read of the Song for page
/// hydration (SongFormViewModel.LoadSongForEditAsync) followed by SongService.UpdateSongAsync
/// on the SAME AppDbContext instance, exactly as MAUI's DI resolves a "Scoped" AppDbContext
/// (a single root scope for the whole app lifetime — there is no per-page child scope).
/// Per testing.md § Project anti-patterns, this runs against REAL SQLite via
/// TestDbContextFactory — a mocked ISongRepository (as SongServiceTests uses) cannot reach
/// DbSet.Update's identity-map code and is why 535/535 was green while every device save failed.
/// </summary>
public class SongServiceUpdateIntegrationTests : IAsyncLifetime
{
    private AppDbContext _db;
    private SongRepository _songRepo;
    private ArtistRepository _artistRepo;
    private SongService _sut;

    public async Task InitializeAsync()
    {
        _db = TestDbContextFactory.Create();
        await _db.Database.EnsureCreatedAsync();
        _songRepo = new SongRepository(_db);
        _artistRepo = new ArtistRepository(_db);
        _sut = new SongService(
            _songRepo,
            _artistRepo,
            urlRepository: null!,
            urlService: null!,
            NullLogger<SongService>.Instance);
    }

    public async Task DisposeAsync()
    {
        await _db.Database.EnsureDeletedAsync();
        await _db.DisposeAsync();
    }

    [Fact]
    // [AC] REQ-ACREATE-16: a changed artist on a saved song is persisted, even after the form's
    // edit-mode hydration read (SongFormViewModel.LoadSongForEditAsync -> GetSongByIdAsync) has
    // already run once on the same scoped DbContext, as it always does before Save is reachable.
    public async Task UpdateSongAsync_AfterPriorHydrationRead_PersistsChangedArtistWithoutThrowing()
    {
        var originalArtist = await SeedArtistAsync("Original Artist");
        var newArtist = await SeedArtistAsync("New Artist");
        var song = MakeSong(originalArtist.Id, "Some Title");
        _db.Set<Song>().Add(song);
        await _db.SaveChangesAsync();
        var songId = song.Id;

        // Simulate SongFormViewModel.LoadSongForEditAsync's hydration read — happens on every
        // edit-mode page open, on the SAME AppDbContext instance the later Save reuses.
        var hydrationRead = await _songRepo.GetByIdAsync(songId, CancellationToken.None);
        Assert.NotNull(hydrationRead);

        // Simulate Save — SongFormViewModel.ExecuteEditSaveAsync -> SongService.UpdateSongAsync.
        var (success, message) = await _sut.UpdateSongAsync(
            songId, "Some Title", featuredArtists: null, lyrics: null, hasManualEdits: true,
            artistId: newArtist.Id, ct: CancellationToken.None);

        Assert.True(success, message);

        var reloaded = await _songRepo.GetByIdAsync(songId, CancellationToken.None);
        Assert.Equal(newArtist.Id, reloaded!.ArtistId);
    }

    [Fact]
    // [AC] REQ-ACREATE-16: covers "face 2" from T10 re-run #5 — a second write against a Song
    // row already tracked in this long-lived DbContext (from its own creation, or a prior
    // save) must not throw an EF identity-map conflict, and must actually persist.
    public async Task UpdateSongAsync_SongAlreadyTrackedInContext_PersistsWithoutThrowing()
    {
        var originalArtist = await SeedArtistAsync("Original Artist");
        var newArtist = await SeedArtistAsync("New Artist");
        var song = MakeSong(originalArtist.Id, "Some Title 2");

        // Mirrors production: the song's own creation (SongRepository.AddAsync + SaveChangesAsync)
        // leaves it tracked (Unchanged) in this AppDbContext for the rest of the app session —
        // QueryTrackingBehavior.NoTracking does not detach explicitly-saved entities.
        await _songRepo.AddAsync(song, CancellationToken.None);
        await _songRepo.SaveChangesAsync(CancellationToken.None);
        var songId = song.Id;

        var (success, message) = await _sut.UpdateSongAsync(
            songId, "Some Title 2", featuredArtists: null, lyrics: null, hasManualEdits: true,
            artistId: newArtist.Id, ct: CancellationToken.None);

        Assert.True(success, message);

        var reloaded = await _songRepo.GetByIdAsync(songId, CancellationToken.None);
        Assert.Equal(newArtist.Id, reloaded!.ArtistId);
    }

    private async Task<Artist> SeedArtistAsync(string name)
    {
        var artist = new Artist
        {
            Name = name,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Set<Artist>().Add(artist);
        await _db.SaveChangesAsync();
        return artist;
    }

    private static Song MakeSong(int artistId, string title) => new()
    {
        ArtistId = artistId,
        Title = title,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };
}
