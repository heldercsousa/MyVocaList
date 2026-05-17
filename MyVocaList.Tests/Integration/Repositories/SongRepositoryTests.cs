using Microsoft.EntityFrameworkCore;
using MyVocaList.Contracts.DTOs.List;
using MyVocaList.Infra;
using MyVocaList.Infra.Repository;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Integration.Repositories;

public class SongRepositoryTests : IAsyncLifetime
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

    // ── AddAsync / GetByIdAsync ───────────────────────────────────────────

    [Fact]
    public async Task AddAsync_ValidSong_PersistedAndReturnedById()
    {
        var artist = await SeedArtistAsync("Queen");
        var song = MakeSong(artist.Id, "Bohemian Rhapsody");

        await _repo.AddAsync(song, CancellationToken.None);
        await _db.SaveChangesAsync();

        var found = await _repo.GetByIdAsync(song.Id, CancellationToken.None);

        Assert.NotNull(found);
        Assert.Equal("Bohemian Rhapsody", found.Title);
        Assert.Equal(artist.Id, found.ArtistId);
    }

    // ── GetPagedAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetPagedAsync_NoQuery_ReturnsSongsSortedByTitle()
    {
        var artist = await SeedArtistAsync("The Beatles");
        _db.Set<Song>().AddRange(
            MakeSong(artist.Id, "Yesterday"),
            MakeSong(artist.Id, "Come Together"),
            MakeSong(artist.Id, "Hey Jude"));
        await _db.SaveChangesAsync();

        var (items, totalCount) = await _repo.GetPagedAsync(
            1, 20, null, CancellationToken.None);

        Assert.Equal(3, totalCount);
        var titles = items.Select(s => s.Title).ToList();
        Assert.Equal("Come Together", titles[0]);
        Assert.Equal("Hey Jude", titles[1]);
        Assert.Equal("Yesterday", titles[2]);
    }

    [Fact]
    public async Task GetPagedAsync_WithQuery_FiltersResults()
    {
        var artist = await SeedArtistAsync("Led Zeppelin");
        _db.Set<Song>().AddRange(
            MakeSong(artist.Id, "Stairway to Heaven"),
            MakeSong(artist.Id, "Black Dog"));
        await _db.SaveChangesAsync();

        var (items, totalCount) = await _repo.GetPagedAsync(
            1, 20, "stair", CancellationToken.None);

        Assert.Equal(1, totalCount);
        Assert.Equal("Stairway to Heaven", items.Single().Title);
    }

    [Fact]
    public async Task GetPagedAsync_Page2_SkipsFirstPage()
    {
        var artist = await SeedArtistAsync("Pink Floyd");
        _db.Set<Song>().AddRange(
            MakeSong(artist.Id, "Comfortably Numb"),
            MakeSong(artist.Id, "Money"),
            MakeSong(artist.Id, "Wish You Were Here"));
        await _db.SaveChangesAsync();

        var (items, totalCount) = await _repo.GetPagedAsync(
            2, 2, null, CancellationToken.None);

        Assert.Equal(3, totalCount);
        Assert.Single(items);
        Assert.Equal("Wish You Were Here", items.Single().Title);
    }

    [Fact]
    public async Task GetPagedAsync_CaseInsensitive_FindsMatch()
    {
        var artist = await SeedArtistAsync("Metallica");
        _db.Set<Song>().Add(MakeSong(artist.Id, "Enter Sandman"));
        await _db.SaveChangesAsync();

        var (items, totalCount) = await _repo.GetPagedAsync(
            1, 20, "enter", CancellationToken.None);

        Assert.Equal(1, totalCount);
        Assert.Equal("Enter Sandman", items.Single().Title);
    }

    // ── ExistsByTitleForArtistAsync ───────────────────────────────────────

    [Fact]
    public async Task ExistsByTitleForArtistAsync_NewTitle_ReturnsFalse()
    {
        var artist = await SeedArtistAsync("New Artist");

        var result = await _repo.ExistsByTitleForArtistAsync(artist.Id, "nonexistenttitle", CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task ExistsByTitleForArtistAsync_ExistingTitle_ReturnsTrue()
    {
        var artist = await SeedArtistAsync("Nirvana");
        _db.Set<Song>().Add(MakeSong(artist.Id, "Smells Like Teen Spirit"));
        await _db.SaveChangesAsync();

        var result = await _repo.ExistsByTitleForArtistAsync(
            artist.Id, "smells like teen spirit", CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task ExistsByTitleForArtistAsync_ExcludeSelf_ReturnsFalse()
    {
        var artist = await SeedArtistAsync("Radiohead");
        var song = MakeSong(artist.Id, "Creep");
        _db.Set<Song>().Add(song);
        await _db.SaveChangesAsync();

        var result = await _repo.ExistsByTitleForArtistAsync(
            artist.Id, "creep", song.Id, CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task ExistsByTitleForArtistAsync_SameTitleDifferentArtist_ReturnsFalse()
    {
        var artist1 = await SeedArtistAsync("Artist One");
        var artist2 = await SeedArtistAsync("Artist Two");
        _db.Set<Song>().Add(MakeSong(artist1.Id, "Shared Title"));
        await _db.SaveChangesAsync();

        var result = await _repo.ExistsByTitleForArtistAsync(
            artist2.Id, "shared title", CancellationToken.None);

        Assert.False(result);
    }

    // ── Composite unique constraint ───────────────────────────────────────

    [Fact]
    public async Task AddAsync_DuplicateTitleForSameArtist_ThrowsDbUpdateException()
    {
        var artist = await SeedArtistAsync("The Who");
        _db.Set<Song>().Add(MakeSong(artist.Id, "My Generation"));
        await _db.SaveChangesAsync();

        _db.Set<Song>().Add(MakeSong(artist.Id, "My Generation"));

        await Assert.ThrowsAsync<DbUpdateException>(() => _db.SaveChangesAsync());
    }

    [Fact]
    public async Task AddAsync_SameTitleDifferentArtist_Succeeds()
    {
        var artist1 = await SeedArtistAsync("Artist One");
        var artist2 = await SeedArtistAsync("Artist Two");
        _db.Set<Song>().Add(MakeSong(artist1.Id, "Shared Title"));
        await _db.SaveChangesAsync();

        _db.Set<Song>().Add(MakeSong(artist2.Id, "Shared Title"));
        var ex = await Record.ExceptionAsync(() => _db.SaveChangesAsync());

        Assert.Null(ex);
    }

    // ── GetByExternalIdAsync ──────────────────────────────────────────────

    [Fact]
    public async Task GetByExternalIdAsync_ExistingExternalId_ReturnsSong()
    {
        var artist = await SeedArtistAsync("The Doors");
        var song = MakeSong(artist.Id, "Light My Fire");
        song.ExternalId = "mb-song-99999";
        song.ExternalProvider = "musicbrainz";
        _db.Set<Song>().Add(song);
        await _db.SaveChangesAsync();

        var found = await _repo.GetByExternalIdAsync("mb-song-99999", "musicbrainz", CancellationToken.None);

        Assert.NotNull(found);
        Assert.Equal("Light My Fire", found.Title);
    }

    [Fact]
    public async Task GetByExternalIdAsync_UnknownId_ReturnsNull()
    {
        var found = await _repo.GetByExternalIdAsync("unknown-id", "musicbrainz", CancellationToken.None);
        Assert.Null(found);
    }

    // ── UpdateAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_ChangeTitle_Persisted()
    {
        var artist = await SeedArtistAsync("Artist");
        var song = MakeSong(artist.Id, "Old Title");
        _db.Set<Song>().Add(song);
        await _db.SaveChangesAsync();

        song.Title = "New Title";
        song.TitleNormalized = "new title";
        await _repo.UpdateAsync(song, CancellationToken.None);
        await _db.SaveChangesAsync();

        var found = await _repo.GetByIdAsync(song.Id, CancellationToken.None);
        Assert.Equal("New Title", found.Title);
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ExistingSong_Removed()
    {
        var artist = await SeedArtistAsync("Artist");
        var song = MakeSong(artist.Id, "To Be Deleted");
        _db.Set<Song>().Add(song);
        await _db.SaveChangesAsync();

        await _repo.DeleteAsync([song.Id], CancellationToken.None);
        await _db.SaveChangesAsync();

        var found = await _repo.GetByIdAsync(song.Id, CancellationToken.None);
        Assert.Null(found);
    }

    [Fact]
    public async Task DeleteAsync_MultipleSongs_AllRemoved()
    {
        var artist = await SeedArtistAsync("Artist");
        var song1 = MakeSong(artist.Id, "First Song");
        var song2 = MakeSong(artist.Id, "Second Song");
        _db.Set<Song>().AddRange(song1, song2);
        await _db.SaveChangesAsync();

        await _repo.DeleteAsync([song1.Id, song2.Id], CancellationToken.None);
        await _db.SaveChangesAsync();

        var remaining = await _db.Set<Song>().CountAsync(s => s.ArtistId == artist.Id);
        Assert.Equal(0, remaining);
    }

    // ── Lyrics field ─────────────────────────────────────────────────────

    [Fact]
    public async Task AddAsync_ValidSong_HasLyricsField()
    {
        var artist = await SeedArtistAsync("Bob Dylan");
        var song = MakeSong(artist.Id, "Blowin in the Wind");
        song.Lyrics = "Test lyrics";

        await _repo.AddAsync(song, CancellationToken.None);
        await _db.SaveChangesAsync();

        var found = await _repo.GetByIdAsync(song.Id, CancellationToken.None);

        Assert.NotNull(found);
        Assert.Equal("Test lyrics", found.Lyrics);
    }

    // ── Catalog join table ────────────────────────────────────────────────

    [Fact]
    public async Task AddAsync_CatalogEntry_LinksArtistAndSong()
    {
        var originalArtist = await SeedArtistAsync("Original Artist");
        var performer = await SeedArtistAsync("Performer Artist");
        var song = MakeSong(originalArtist.Id, "Cover Song");
        _db.Set<Song>().Add(song);
        await _db.SaveChangesAsync();

        _db.Catalog.Add(new Catalog { ArtistId = performer.Id, SongId = song.Id });
        await _db.SaveChangesAsync();

        var exists = await _db.Catalog.AnyAsync(c => c.ArtistId == performer.Id && c.SongId == song.Id);

        Assert.True(exists);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private async Task<Artist> SeedArtistAsync(string name)
    {
        var artist = new Artist
        {
            Name = name,
            NameNormalized = name.ToLowerInvariant(),
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
        TitleNormalized = title.ToLowerInvariant(),
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };
}
