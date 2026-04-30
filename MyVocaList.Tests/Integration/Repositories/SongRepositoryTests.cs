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

    // ── GetPagedByArtistAsync ─────────────────────────────────────────────

    [Fact]
    public async Task GetPagedByArtistAsync_NoQuery_ReturnsSongsForArtistSortedByTitle()
    {
        var artist = await SeedArtistAsync("The Beatles");
        _db.Set<Song>().AddRange(
            MakeSong(artist.Id, "Yesterday"),
            MakeSong(artist.Id, "Come Together"),
            MakeSong(artist.Id, "Hey Jude"));
        await _db.SaveChangesAsync();

        var (items, totalCount) = await _repo.GetPagedByArtistAsync(
            artist.Id, 1, 20, string.Empty, CancellationToken.None);

        Assert.Equal(3, totalCount);
        var titles = items.Select(s => s.Title).ToList();
        Assert.Equal("Come Together", titles[0]);
        Assert.Equal("Hey Jude", titles[1]);
        Assert.Equal("Yesterday", titles[2]);
    }

    [Fact]
    public async Task GetPagedByArtistAsync_WithQuery_FiltersResults()
    {
        var artist = await SeedArtistAsync("Led Zeppelin");
        _db.Set<Song>().AddRange(
            MakeSong(artist.Id, "Stairway to Heaven"),
            MakeSong(artist.Id, "Black Dog"));
        await _db.SaveChangesAsync();

        var (items, totalCount) = await _repo.GetPagedByArtistAsync(
            artist.Id, 1, 20, "stair", CancellationToken.None);

        Assert.Equal(1, totalCount);
        Assert.Equal("Stairway to Heaven", items.Single().Title);
    }

    [Fact]
    public async Task GetPagedByArtistAsync_OnlyReturnsSongsForRequestedArtist()
    {
        var artist1 = await SeedArtistAsync("Artist One");
        var artist2 = await SeedArtistAsync("Artist Two");
        _db.Set<Song>().AddRange(
            MakeSong(artist1.Id, "Song A"),
            MakeSong(artist2.Id, "Song B"));
        await _db.SaveChangesAsync();

        var (items, totalCount) = await _repo.GetPagedByArtistAsync(
            artist1.Id, 1, 20, string.Empty, CancellationToken.None);

        Assert.Equal(1, totalCount);
        Assert.Equal("Song A", items.Single().Title);
    }

    [Fact]
    public async Task GetPagedByArtistAsync_Page2_SkipsFirstPage()
    {
        var artist = await SeedArtistAsync("Pink Floyd");
        _db.Set<Song>().AddRange(
            MakeSong(artist.Id, "Comfortably Numb"),
            MakeSong(artist.Id, "Money"),
            MakeSong(artist.Id, "Wish You Were Here"));
        await _db.SaveChangesAsync();

        var (items, totalCount) = await _repo.GetPagedByArtistAsync(
            artist.Id, 2, 2, string.Empty, CancellationToken.None);

        Assert.Equal(3, totalCount);
        Assert.Single(items);
        Assert.Equal("Wish You Were Here", items.Single().Title);
    }

    [Fact]
    public async Task GetPagedByArtistAsync_CaseInsensitive_FindsMatch()
    {
        var artist = await SeedArtistAsync("Metallica");
        _db.Set<Song>().Add(MakeSong(artist.Id, "Enter Sandman"));
        await _db.SaveChangesAsync();

        var (items, totalCount) = await _repo.GetPagedByArtistAsync(
            artist.Id, 1, 20, "ENTER", CancellationToken.None);

        Assert.Equal(1, totalCount);
        Assert.Equal("Enter Sandman", items.Single().Title);
    }

    // ── SearchByTitleAsync ────────────────────────────────────────────────

    [Fact]
    public async Task SearchByTitleAsync_PrefixMatch_ReturnsMatchingSongs()
    {
        var artist = await SeedArtistAsync("David Bowie");
        _db.Set<Song>().AddRange(
            MakeSong(artist.Id, "Heroes"),
            MakeSong(artist.Id, "Space Oddity"),
            MakeSong(artist.Id, "Starman"));
        await _db.SaveChangesAsync();

        var results = await _repo.SearchByTitleAsync(artist.Id, "sta", 5, CancellationToken.None);

        var titles = results.Select(s => s.Title).ToList();
        Assert.Equal(2, titles.Count);
        Assert.Contains("Starman", titles);
        Assert.Contains("Space Oddity", titles);
    }

    [Fact]
    public async Task SearchByTitleAsync_RespectsMaxResults()
    {
        var artist = await SeedArtistAsync("ABBA");
        _db.Set<Song>().AddRange(
            MakeSong(artist.Id, "Dancing Queen"),
            MakeSong(artist.Id, "Mamma Mia"),
            MakeSong(artist.Id, "Waterloo"));
        await _db.SaveChangesAsync();

        var results = await _repo.SearchByTitleAsync(artist.Id, string.Empty, 2, CancellationToken.None);

        Assert.Equal(2, results.Count());
    }

    [Fact]
    public async Task SearchByTitleAsync_ScopedToArtist_DoesNotReturnOtherArtistSongs()
    {
        var artist1 = await SeedArtistAsync("Artist One");
        var artist2 = await SeedArtistAsync("Artist Two");
        _db.Set<Song>().AddRange(
            MakeSong(artist1.Id, "Common Title"),
            MakeSong(artist2.Id, "Common Title"));
        await _db.SaveChangesAsync();

        var results = await _repo.SearchByTitleAsync(artist1.Id, "common", 10, CancellationToken.None);

        Assert.Single(results);
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

    // ── CountByArtistAsync ────────────────────────────────────────────────

    [Fact]
    public async Task CountByArtistAsync_NoSongs_ReturnsZero()
    {
        var artist = await SeedArtistAsync("Empty Artist");

        var count = await _repo.CountByArtistAsync(artist.Id, CancellationToken.None);

        Assert.Equal(0, count);
    }

    [Fact]
    public async Task CountByArtistAsync_WithSongs_ReturnsCorrectCount()
    {
        var artist = await SeedArtistAsync("Counting Artist");
        _db.Set<Song>().AddRange(
            MakeSong(artist.Id, "Song One"),
            MakeSong(artist.Id, "Song Two"),
            MakeSong(artist.Id, "Song Three"));
        await _db.SaveChangesAsync();

        var count = await _repo.CountByArtistAsync(artist.Id, CancellationToken.None);

        Assert.Equal(3, count);
    }

    // ── CountByArtistsAsync ───────────────────────────────────────────────

    [Fact]
    public async Task CountByArtistsAsync_MultipleArtists_ReturnsCombinedCount()
    {
        var artist1 = await SeedArtistAsync("Multi Artist One");
        var artist2 = await SeedArtistAsync("Multi Artist Two");
        _db.Set<Song>().AddRange(
            MakeSong(artist1.Id, "A Song"),
            MakeSong(artist2.Id, "B Song"),
            MakeSong(artist2.Id, "C Song"));
        await _db.SaveChangesAsync();

        var count = await _repo.CountByArtistsAsync([artist1.Id, artist2.Id], CancellationToken.None);

        Assert.Equal(3, count);
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
