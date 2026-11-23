using Microsoft.EntityFrameworkCore;
using MyVocaList.Infra;
using MyVocaList.Infra.Repository;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Integration.Repositories;

/// <summary>
/// Integration tests for Song resolution query methods (Wave 2).
/// Uses real SQLite + CollationInterceptor to validate collation behaviour.
/// </summary>
public class SongRepositoryResolutionTests : IAsyncLifetime
{
    private AppDbContext _db;
    private SongRepository _repo;

    public async Task InitializeAsync()
    {
        _db = TestDbContextFactory.Create();
        await _db.Database.EnsureCreatedAsync();
        _repo = new SongRepository(_db);
    }

    public async Task DisposeAsync()
    {
        await _db.Database.EnsureDeletedAsync();
        await _db.DisposeAsync();
    }

    // ── ExistsByTitleForArtistAsync — identity (artist, title), accent + case insensitive ────

    // [AC] REQ-RMV-06: identity-only duplicate lookup matches under NOCASE_NOACCENT collation
    [Fact]
    public async Task ExistsByTitleForArtistAsync_AccentAndCaseInsensitive_Matches()
    {
        var artist = await SeedArtistAsync("Artista");
        _db.Songs.Add(MakeSong(artist.Id, "Café"));
        await _db.SaveChangesAsync();

        var result = await _repo.ExistsByTitleForArtistAsync(
            artist.Id, "cafe", CancellationToken.None);

        Assert.True(result);
    }

    // [AC] REQ-RMV-06: the excludeId overload excludes the target song (edit mode)
    [Fact]
    public async Task ExistsByTitleForArtistAsync_ExcludeId_ExcludesSelf()
    {
        var artist = await SeedArtistAsync("Band");
        var song = MakeSong(artist.Id, "Song Title");
        _db.Songs.Add(song);
        await _db.SaveChangesAsync();

        var result = await _repo.ExistsByTitleForArtistAsync(
            artist.Id, "song title", song.Id, CancellationToken.None);

        Assert.False(result);
    }

    // [AC] REQ-RMV-06: the same title under a different artist is not an exact match
    [Fact]
    public async Task ExistsByTitleForArtistAsync_DifferentArtist_ReturnsFalse()
    {
        var artist1 = await SeedArtistAsync("Artist One");
        var artist2 = await SeedArtistAsync("Artist Two");
        _db.Songs.Add(MakeSong(artist1.Id, "Shared Title"));
        await _db.SaveChangesAsync();

        var result = await _repo.ExistsByTitleForArtistAsync(
            artist2.Id, "shared title", CancellationToken.None);

        Assert.False(result);
    }

    // [AC] INV-RMV-1 / REQ-RMV-02: the 2-column unique index (ArtistId, Title) rejects a duplicate identity incl. accent/case variants
    [Fact]
    public async Task DuplicateTitleForArtist_ThrowsDbUpdateException()
    {
        var artist = await SeedArtistAsync("The Who");
        _db.Songs.Add(MakeSong(artist.Id, "My Generation"));
        await _db.SaveChangesAsync();

        _db.Songs.Add(MakeSong(artist.Id, "my generation"));

        await Assert.ThrowsAsync<DbUpdateException>(() => _db.SaveChangesAsync());
    }

    // ── Helpers ───────────────────────────────────────────────────────────
    // BUG-085: the prefix-pool candidate tests that lived here were retired and
    // rewritten as SongRepositoryArtistTrigramTests (SearchTitleCandidatesForArtistAsync, REQ-SFT-23).
    // They moved there because that class uses MigrateAsync (SongSearch FTS index present).

    private async Task<Artist> SeedArtistAsync(string name)
    {
        var artist = new Artist
        {
            Name = name,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Artists.Add(artist);
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
