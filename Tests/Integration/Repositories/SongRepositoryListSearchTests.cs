using Microsoft.EntityFrameworkCore;
using MyVocaList.Domain.Entity;
using MyVocaList.Infra;
using MyVocaList.Infra.Repository;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Integration.Repositories;

/// <summary>
/// REQ-SLD-01 to 04: the Songs list search (SongRepository.GetPagedAsync) over title and artist.
/// Real SQLite temp file, schema built with MigrateAsync so the SongSearch index and triggers exist.
/// </summary>
public class SongRepositoryListSearchTests : IAsyncLifetime
{
    private AppDbContext _db;
    private SongRepository _repo;

    public async Task InitializeAsync()
    {
        _db = TestDbContextFactory.Create();
        await _db.Database.MigrateAsync();
        _repo = new SongRepository(_db);
    }

    public async Task DisposeAsync()
    {
        await _db.Database.EnsureDeletedAsync();
        await _db.DisposeAsync();
    }

    // [AC] REQ-SLD-01: a query with a word of 3+ characters finds songs through their artist name
    [Fact]
    public async Task GetPagedAsync_QueryMatchesArtistName_ReturnsThatArtistsSongs()
    {
        var guns = await SeedArtistAsync(_db, "Guns N' Roses");
        var cyndi = await SeedArtistAsync(_db, "Cyndi Lauper");
        await SeedSongAsync(_db, guns.Id, "Don't Cry");
        await SeedSongAsync(_db, guns.Id, "Sweet Child O' Mine");
        await SeedSongAsync(_db, cyndi.Id, "Time After Time");

        var (items, _) = await _repo.GetPagedAsync(1, 20, "guns", CancellationToken.None);

        var titles = items.Select(i => i.Title).ToList();
        Assert.Contains("Don't Cry", titles);
        Assert.Contains("Sweet Child O' Mine", titles);
        Assert.DoesNotContain("Time After Time", titles);
    }

    // [AC] REQ-SLD-35: a short query matches the artist name anywhere (replaces the REQ-SLD-01 prefix-only test)
    [Fact]
    public async Task GetPagedAsync_ShortQueryMatchesArtistNameAnywhere_ReturnsThatArtistsSongs()
    {
        var abba = await SeedArtistAsync(_db, "ABBA");
        var beatles = await SeedArtistAsync(_db, "Beatles");
        await SeedSongAsync(_db, abba.Id, "Abba Song");
        await SeedSongAsync(_db, abba.Id, "Dancing Queen");
        await SeedSongAsync(_db, beatles.Id, "Yesterday");

        var (items, _) = await _repo.GetPagedAsync(1, 20, "ab", CancellationToken.None);

        var titles = items.Select(i => i.Title).ToList();
        Assert.Contains("Abba Song", titles);
        Assert.Contains("Dancing Queen", titles);
        Assert.DoesNotContain("Yesterday", titles);
    }

    // [AC] REQ-SLD-35: a short query finds a title that contains it, not only one that starts with it
    [Fact]
    public async Task GetPagedAsync_ShortQueryInsideTitle_IsFound()
    {
        // The artist name has no "es", so only the titles match.
        var quartet = await SeedArtistAsync(_db, "Quartet");
        await SeedSongAsync(_db, quartet.Id, "Yesterday");
        await SeedSongAsync(_db, quartet.Id, "Let It Be");

        var (items, _) = await _repo.GetPagedAsync(1, 20, "es", CancellationToken.None);

        Assert.Equal(new[] { "Yesterday" }, items.Select(i => i.Title).ToArray());
    }

    // [AC] REQ-SLD-35: a short query finds the songs of an artist whose name contains it
    [Fact]
    public async Task GetPagedAsync_ShortQueryInsideArtistName_FindsThatArtistsSongs()
    {
        var u2 = await SeedArtistAsync(_db, "U2");
        var beatles = await SeedArtistAsync(_db, "Beatles");
        await SeedSongAsync(_db, u2.Id, "One");
        await SeedSongAsync(_db, beatles.Id, "Yesterday");

        var (items, _) = await _repo.GetPagedAsync(1, 20, "u2", CancellationToken.None);

        Assert.Equal(new[] { "One" }, items.Select(i => i.Title).ToArray());
    }

    // [AC] REQ-SLD-35: songs whose title starts with the short text come first, then by title, then by id
    [Fact]
    public async Task GetPagedAsync_ShortQueryPrefixMatchesFirst_ThenTitle()
    {
        // The artist name has no "le", so only the titles match.
        var quartet = await SeedArtistAsync(_db, "Quartet");
        await SeedSongAsync(_db, quartet.Id, "Bleed");
        await SeedSongAsync(_db, quartet.Id, "Let It Be");
        await SeedSongAsync(_db, quartet.Id, "Zebra Crossing");

        var (items, _) = await _repo.GetPagedAsync(1, 20, "le", CancellationToken.None);

        Assert.Equal(new[] { "Let It Be", "Bleed" }, items.Select(i => i.Title).ToArray());
    }

    // [AC] REQ-SLD-35: a short query ignores letter case and accents
    [Fact]
    public async Task GetPagedAsync_ShortQueryCaseAndAccentInsensitive_IsFound()
    {
        var quartet = await SeedArtistAsync(_db, "Quartet");
        await SeedSongAsync(_db, quartet.Id, "Été");

        // "TÉ" is inside "Été", not at its start: the match needs the contains rule and folded case and accents.
        var (items, _) = await _repo.GetPagedAsync(1, 20, "TÉ", CancellationToken.None);

        Assert.Equal(new[] { "Été" }, items.Select(i => i.Title).ToArray());
    }

    // [AC] REQ-SLD-35: pages of a short-text search are disjoint and the count covers every match
    [Fact]
    public async Task GetPagedAsync_ShortQueryPages_AreDisjointAndCountEveryMatch()
    {
        var quartet = await SeedArtistAsync(_db, "Quartet");
        for (var i = 1; i <= 45; i++)
            await SeedSongAsync(_db, quartet.Id, $"Cab {i:D2}");

        var ids = new List<int>();
        for (var page = 1; page <= 3; page++)
        {
            var (items, totalCount) = await _repo.GetPagedAsync(page, 20, "ab", CancellationToken.None);
            Assert.Equal(45, totalCount);
            ids.AddRange(items.Select(i => i.Id));
        }

        Assert.Equal(45, ids.Count);
        Assert.Equal(45, ids.Distinct().Count());
    }

    // [AC] REQ-SLD-01: without the search index the list falls back to the title prefix and does not throw
    [Fact]
    public async Task GetPagedAsync_IndexTableMissing_FallsBackToTitlePrefix()
    {
        await using var plain = TestDbContextFactory.Create();
        await plain.Database.EnsureCreatedAsync();
        try
        {
            var abba = await SeedArtistAsync(plain, "ABBA");
            await SeedSongAsync(plain, abba.Id, "Abba Song");
            var repo = new SongRepository(plain);

            var (items, totalCount) = await repo.GetPagedAsync(1, 20, "abba", CancellationToken.None);

            Assert.Contains(items, i => i.Title == "Abba Song");
            Assert.Equal(1, totalCount);
        }
        finally
        {
            await plain.Database.EnsureDeletedAsync();
        }
    }

    // [AC] REQ-SLD-02: a title hit ranks before artist-only hits, and equal rank and length fall back to id order
    [Fact]
    public async Task GetPagedAsync_FullTextQuery_OrdersByRankThenTitleLengthThenId()
    {
        var artistA = await SeedArtistAsync(_db, "Artist A");
        var artistB = await SeedArtistAsync(_db, "Artist B");
        var crystal = await SeedArtistAsync(_db, "Crystal Band");
        var cryA = await SeedSongAsync(_db, artistA.Id, "Cry");
        var cryB = await SeedSongAsync(_db, artistB.Id, "Cry");
        var artistOnly = await SeedSongAsync(_db, crystal.Id, "Sweet Song");

        var (items, _) = await _repo.GetPagedAsync(1, 20, "cry", CancellationToken.None);

        Assert.Equal(new[] { cryA.Id, cryB.Id, artistOnly.Id }, items.Select(i => i.Id).ToArray());
    }

    // [AC] REQ-SLD-02: a short query orders its title-prefix results by title
    [Fact]
    public async Task GetPagedAsync_ShortQuery_OrdersByTitle()
    {
        var artist = await SeedArtistAsync(_db, "ABBA");
        await SeedSongAsync(_db, artist.Id, "Abba Song");
        await SeedSongAsync(_db, artist.Id, "Abacus");

        var (items, _) = await _repo.GetPagedAsync(1, 20, "ab", CancellationToken.None);

        Assert.Equal(new[] { "Abacus", "Abba Song" }, items.Select(i => i.Title).ToArray());
    }

    // [AC] REQ-SLD-03: pages of a full-text search are disjoint and together reach every matching song
    [Fact]
    public async Task GetPagedAsync_FullTextQuery_PagesAreDisjointAndCoverEveryMatch()
    {
        var artist = await SeedArtistAsync(_db, "Lullaby Choir");
        for (var i = 1; i <= 45; i++)
            await SeedSongAsync(_db, artist.Id, $"Lullaby {i:D2} Cry");

        var ids = new List<int>();
        for (var page = 1; page <= 3; page++)
        {
            var (items, totalCount) = await _repo.GetPagedAsync(page, 20, "cry", CancellationToken.None);
            Assert.Equal(45, totalCount);
            ids.AddRange(items.Select(i => i.Id));
        }

        Assert.Equal(45, ids.Count);
        Assert.Equal(45, ids.Distinct().Count());
    }

    // [AC] REQ-SLD-04: a non-blank query with no match returns no rows and a zero count
    [Fact]
    public async Task GetPagedAsync_QueryMatchesNothing_ReturnsEmptyAndZeroCount()
    {
        var artist = await SeedArtistAsync(_db, "Oasis");
        await SeedSongAsync(_db, artist.Id, "Dont Look Back in Anger");

        var (items, totalCount) = await _repo.GetPagedAsync(1, 20, "xyzzy plugh", CancellationToken.None);

        Assert.Empty(items);
        Assert.Equal(0, totalCount);
    }

    // [AC] REQ-SLD-01: a search row shows the artist image when the song has none
    [Fact]
    public async Task GetPagedAsync_SearchRow_UsesArtistImageWhenSongHasNone()
    {
        var guns = await SeedArtistAsync(_db, "Guns N' Roses", "https://img.example/guns.jpg");
        await SeedSongAsync(_db, guns.Id, "Sweet Child O' Mine");

        var (items, _) = await _repo.GetPagedAsync(1, 20, "guns", CancellationToken.None);

        var row = Assert.Single(items);
        Assert.Equal("https://img.example/guns.jpg", row.ImageUrl);
    }

    private static async Task<Artist> SeedArtistAsync(AppDbContext db, string name, string? imageUrl = null)
    {
        var artist = new Artist
        {
            Name = name,
            ImageUrl = imageUrl,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Artists.Add(artist);
        await db.SaveChangesAsync();
        return artist;
    }

    private static async Task<Song> SeedSongAsync(AppDbContext db, int artistId, string title)
    {
        var song = new Song
        {
            ArtistId = artistId,
            Title = title,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Songs.Add(song);
        await db.SaveChangesAsync();
        return song;
    }
}
