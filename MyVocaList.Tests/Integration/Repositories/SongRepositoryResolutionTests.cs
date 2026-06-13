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

    // ── GetFuzzyCandidatePoolAsync ─────────────────────────────────────────

    // [AC] AC-5.6: GetFuzzyCandidatePoolAsync respects artist scope and take limit
    [Fact]
    public async Task GetFuzzyCandidatePoolAsync_RespectsArtistScopeAndTake()
    {
        var artist1 = await SeedArtistAsync("Queen");
        var artist2 = await SeedArtistAsync("Quentin Music");
        _db.Songs.AddRange(
            MakeSong(artist1.Id, "Bohemian Rhapsody", ""),
            MakeSong(artist1.Id, "Bohemian Anniversary", ""),
            MakeSong(artist1.Id, "Somebody to Love", ""),
            MakeSong(artist2.Id, "Bohemian Dreams", ""));
        await _db.SaveChangesAsync();

        var pool = await _repo.GetFuzzyCandidatePoolAsync(artist1.Id, "Bohe", 2, CancellationToken.None);

        Assert.True(pool.Count <= 2);
        Assert.All(pool, s => Assert.Equal(artist1.Id, s.ArtistId));
    }

    // [AC] AC-5.7: GetFuzzyCandidatePoolAsync returns only songs whose title starts with the prefix token
    [Fact]
    public async Task GetFuzzyCandidatePoolAsync_PrefixFilter_ReturnsOnlyMatchingPrefix()
    {
        var artist = await SeedArtistAsync("Led Zeppelin");
        _db.Songs.AddRange(
            MakeSong(artist.Id, "Stairway to Heaven", ""),
            MakeSong(artist.Id, "Black Dog", ""),
            MakeSong(artist.Id, "Stairway Alternate", "Live"));
        await _db.SaveChangesAsync();

        var pool = await _repo.GetFuzzyCandidatePoolAsync(artist.Id, "Stair", 50, CancellationToken.None);

        Assert.Equal(2, pool.Count);
        Assert.All(pool, s => Assert.StartsWith("Stairway", s.Title, StringComparison.OrdinalIgnoreCase));
    }

    // ── Helpers ───────────────────────────────────────────────────────────

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
