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

    // ── ExistsByTitleVersionForArtistAsync — accent + case insensitive ────

    // [AC] AC-5.1: ExistsByTitleVersionForArtistAsync matches under NOCASE_NOACCENT collation
    [Fact]
    public async Task ExistsByTitleVersionForArtistAsync_AccentAndCaseInsensitive_Matches()
    {
        var artist = await SeedArtistAsync("Artista");
        _db.Songs.Add(MakeSong(artist.Id, "Café", "Live"));
        await _db.SaveChangesAsync();

        var result = await _repo.ExistsByTitleVersionForArtistAsync(
            artist.Id, "cafe", "live", CancellationToken.None);

        Assert.True(result);
    }

    // [AC] AC-5.2: ExistsByTitleVersionForArtistAsync excludeId overload excludes target song
    [Fact]
    public async Task ExistsByTitleVersionForArtistAsync_ExcludeId_ExcludesSelf()
    {
        var artist = await SeedArtistAsync("Band");
        var song = MakeSong(artist.Id, "Song Title", "Acoustic");
        _db.Songs.Add(song);
        await _db.SaveChangesAsync();

        var result = await _repo.ExistsByTitleVersionForArtistAsync(
            artist.Id, "song title", "acoustic", song.Id, CancellationToken.None);

        Assert.False(result);
    }

    // [AC] AC-5.3: ExistsByTitleVersionForArtistAsync returns false when only title matches but version differs
    [Fact]
    public async Task ExistsByTitleVersionForArtistAsync_SameTitleDifferentVersion_ReturnsFalse()
    {
        var artist = await SeedArtistAsync("Artist");
        _db.Songs.Add(MakeSong(artist.Id, "Hit Song", "Live"));
        await _db.SaveChangesAsync();

        var result = await _repo.ExistsByTitleVersionForArtistAsync(
            artist.Id, "hit song", "acoustic", CancellationToken.None);

        Assert.False(result);
    }

    // [AC] AC-5.4: ExistsByTitleVersionForArtistAsync returns false for different artist
    [Fact]
    public async Task ExistsByTitleVersionForArtistAsync_DifferentArtist_ReturnsFalse()
    {
        var artist1 = await SeedArtistAsync("Artist One");
        var artist2 = await SeedArtistAsync("Artist Two");
        _db.Songs.Add(MakeSong(artist1.Id, "Shared Title", ""));
        await _db.SaveChangesAsync();

        var result = await _repo.ExistsByTitleVersionForArtistAsync(
            artist2.Id, "shared title", "", CancellationToken.None);

        Assert.False(result);
    }

    // [AC] AC-5.5: 3-col unique index rejects duplicate (ArtistId, Title, Version) incl. accent/case variants
    [Fact]
    public async Task DuplicateTitleVersion_ThrowsDbUpdateException()
    {
        var artist = await SeedArtistAsync("The Who");
        _db.Songs.Add(MakeSong(artist.Id, "My Generation", ""));
        await _db.SaveChangesAsync();

        _db.Songs.Add(MakeSong(artist.Id, "my generation", ""));

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

    private static Song MakeSong(int artistId, string title, string version) => new()
    {
        ArtistId = artistId,
        Title = title,
        Version = version,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };
}
