using Microsoft.EntityFrameworkCore;
using MyVocaList.Infra;
using MyVocaList.Infra.Repository;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Integration.Repositories;

public class CatalogRepositoryTests : IAsyncLifetime
{
    private AppDbContext _db;
    private CatalogRepository _repo;

    public async Task InitializeAsync()
    {
        _db = TestDbContextFactory.Create();
        await _db.Database.EnsureCreatedAsync();
        _repo = new CatalogRepository(_db);
    }

    public async Task DisposeAsync()
    {
        await _db.Database.EnsureDeletedAsync();
        await _db.DisposeAsync();
    }

    // ── ExistsAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task ExistsAsync_NonExistent_ReturnsFalse()
    {
        var artist = await SeedArtistAsync("New Artist");
        var song = await SeedSongAsync(artist.Id, "Unlinked Song");

        var result = await _repo.ExistsAsync(artist.Id, song.Id, CancellationToken.None);

        Assert.False(result);
    }

    // ── AddAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task AddAsync_NewEntry_ExistsReturnsTrue()
    {
        var originalArtist = await SeedArtistAsync("Original Artist");
        var performer = await SeedArtistAsync("Performer");
        var song = await SeedSongAsync(originalArtist.Id, "A Great Song");

        await _repo.AddAsync(new Catalog { ArtistId = performer.Id, SongId = song.Id }, CancellationToken.None);
        await _db.SaveChangesAsync();

        var exists = await _repo.ExistsAsync(performer.Id, song.Id, CancellationToken.None);

        Assert.True(exists);
    }

    [Fact]
    public async Task AddAsync_DuplicateEntry_ThrowsDbUpdateException()
    {
        var originalArtist = await SeedArtistAsync("Original Artist");
        var performer = await SeedArtistAsync("Performer");
        var song = await SeedSongAsync(originalArtist.Id, "Unique Song");

        await _repo.AddAsync(new Catalog { ArtistId = performer.Id, SongId = song.Id }, CancellationToken.None);
        await _db.SaveChangesAsync();

        // Detach tracked entities so EF doesn't raise identity conflict before hitting the DB constraint
        _db.ChangeTracker.Clear();

        await _repo.AddAsync(new Catalog { ArtistId = performer.Id, SongId = song.Id }, CancellationToken.None);

        await Assert.ThrowsAsync<DbUpdateException>(() => _db.SaveChangesAsync());
    }

    // ── RemoveAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task RemoveAsync_ExistingEntry_ExistsReturnsFalse()
    {
        var originalArtist = await SeedArtistAsync("Original Artist");
        var performer = await SeedArtistAsync("Performer");
        var song = await SeedSongAsync(originalArtist.Id, "Removable Song");

        await _repo.AddAsync(new Catalog { ArtistId = performer.Id, SongId = song.Id }, CancellationToken.None);
        await _db.SaveChangesAsync();

        await _repo.RemoveAsync(performer.Id, song.Id, CancellationToken.None);

        var exists = await _repo.ExistsAsync(performer.Id, song.Id, CancellationToken.None);

        Assert.False(exists);
    }

    // ── CountByArtistAsync ────────────────────────────────────────────────

    [Fact]
    public async Task CountByArtistAsync_TwoEntries_ReturnsTwo()
    {
        var originalArtist = await SeedArtistAsync("Original Artist");
        var performer = await SeedArtistAsync("Performer");
        var song1 = await SeedSongAsync(originalArtist.Id, "First Song");
        var song2 = await SeedSongAsync(originalArtist.Id, "Second Song");

        await _repo.AddAsync(new Catalog { ArtistId = performer.Id, SongId = song1.Id }, CancellationToken.None);
        await _repo.AddAsync(new Catalog { ArtistId = performer.Id, SongId = song2.Id }, CancellationToken.None);
        await _db.SaveChangesAsync();

        var count = await _repo.CountByArtistAsync(performer.Id, CancellationToken.None);

        Assert.Equal(2, count);
    }

    // ── GetPagedByArtistAsync ─────────────────────────────────────────────

    [Fact]
    public async Task GetPagedByArtistAsync_FiltersToArtist()
    {
        var originalArtist = await SeedArtistAsync("Original Artist");
        var artist1 = await SeedArtistAsync("Artist One");
        var artist2 = await SeedArtistAsync("Artist Two");
        var song1 = await SeedSongAsync(originalArtist.Id, "Song For A1");
        var song2 = await SeedSongAsync(originalArtist.Id, "Song For A2");

        await _repo.AddAsync(new Catalog { ArtistId = artist1.Id, SongId = song1.Id }, CancellationToken.None);
        await _repo.AddAsync(new Catalog { ArtistId = artist2.Id, SongId = song2.Id }, CancellationToken.None);
        await _db.SaveChangesAsync();

        var (items, totalCount) = await _repo.GetPagedByArtistAsync(
            artist1.Id, 1, 20, null, CancellationToken.None);

        Assert.Equal(1, totalCount);
        Assert.Equal("Song For A1", items.Single().Title);
    }

    [Fact]
    public async Task GetPagedByArtistAsync_WithQuery_FiltersByTitle()
    {
        var originalArtist = await SeedArtistAsync("Original Artist");
        var performer = await SeedArtistAsync("Metallica");
        var song1 = await SeedSongAsync(originalArtist.Id, "Enter Sandman");
        var song2 = await SeedSongAsync(originalArtist.Id, "Nothing Else Matters");

        await _repo.AddAsync(new Catalog { ArtistId = performer.Id, SongId = song1.Id }, CancellationToken.None);
        await _repo.AddAsync(new Catalog { ArtistId = performer.Id, SongId = song2.Id }, CancellationToken.None);
        await _db.SaveChangesAsync();

        var (items, totalCount) = await _repo.GetPagedByArtistAsync(
            performer.Id, 1, 20, "enter", CancellationToken.None);

        Assert.Equal(1, totalCount);
        Assert.Equal("Enter Sandman", items.Single().Title);
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
        _db.Set<Artist>().Add(artist);
        await _db.SaveChangesAsync();
        return artist;
    }

    private async Task<Song> SeedSongAsync(int artistId, string title)
    {
        var song = new Song
        {
            ArtistId = artistId,
            Title = title,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Set<Song>().Add(song);
        await _db.SaveChangesAsync();
        return song;
    }
}
