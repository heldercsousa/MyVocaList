using Microsoft.EntityFrameworkCore;
using MyVocaList.Infra;
using MyVocaList.Infra.Repository;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Integration.Repositories;

public class ArtistRepositoryTests : IAsyncLifetime
{
    private AppDbContext _db;
    private ArtistRepository _repo;

    public async Task InitializeAsync()
    {
        _db = TestDbContextFactory.Create();
        await _db.Database.EnsureCreatedAsync();
        _repo = new ArtistRepository(_db);
    }

    public async Task DisposeAsync()
    {
        await _db.Database.EnsureDeletedAsync();
        await _db.DisposeAsync();
    }

    // ── AddAsync / GetByIdAsync ───────────────────────────────────────────

    [Fact]
    public async Task AddAsync_ValidArtist_PersistedAndReturnedById()
    {
        var artist = MakeArtist("The Beatles");

        await _repo.AddAsync(artist, CancellationToken.None);
        await _db.SaveChangesAsync();

        var found = await _repo.GetByIdAsync(artist.Id, CancellationToken.None);

        Assert.NotNull(found);
        Assert.Equal("The Beatles", found.Name);
    }

    // ── GetPagedAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetPagedAsync_NoQuery_ReturnsAllSortedByName()
    {
        _db.Set<Artist>().AddRange(
            MakeArtist("The Rolling Stones"),
            MakeArtist("ABBA"),
            MakeArtist("Metallica"));
        await _db.SaveChangesAsync();

        var (items, totalCount) = await _repo.GetPagedAsync(1, 20, string.Empty, ct: CancellationToken.None);

        Assert.Equal(3, totalCount);
        var names = items.Select(x => x.Name).ToList();
        Assert.Equal("ABBA", names[0]);
        Assert.Equal("Metallica", names[1]);
        Assert.Equal("The Rolling Stones", names[2]);
    }

    [Fact]
    public async Task GetPagedAsync_WithQuery_FiltersResults()
    {
        _db.Set<Artist>().AddRange(
            MakeArtist("The Beatles"),
            MakeArtist("ABBA"));
        await _db.SaveChangesAsync();

        var (items, totalCount) = await _repo.GetPagedAsync(1, 20, "beatl", ct: CancellationToken.None);

        Assert.Equal(1, totalCount);
        Assert.Equal("The Beatles", items.Single().Name);
    }

    [Fact]
    public async Task GetPagedAsync_Page2_SkipsFirstPage()
    {
        _db.Set<Artist>().AddRange(
            MakeArtist("Artist A"),
            MakeArtist("Artist B"),
            MakeArtist("Artist C"));
        await _db.SaveChangesAsync();

        var (items, totalCount) = await _repo.GetPagedAsync(2, 2, string.Empty, ct: CancellationToken.None);

        Assert.Equal(3, totalCount);
        Assert.Single(items);
        Assert.Equal("Artist C", items.Single().Name);
    }

    [Fact]
    public async Task GetPagedAsync_ReturnsCatalogCount()
    {
        var artist = MakeArtist("Queen");
        _db.Set<Artist>().Add(artist);
        await _db.SaveChangesAsync();

        var song1 = MakeSong(artist.Id, "Bohemian Rhapsody");
        var song2 = MakeSong(artist.Id, "We Will Rock You");
        _db.Set<Song>().AddRange(song1, song2);
        await _db.SaveChangesAsync();

        _db.Set<Catalog>().AddRange(
            new Catalog { ArtistId = artist.Id, SongId = song1.Id },
            new Catalog { ArtistId = artist.Id, SongId = song2.Id });
        await _db.SaveChangesAsync();

        var (items, _) = await _repo.GetPagedAsync(1, 20, string.Empty, ct: CancellationToken.None);

        Assert.Equal(2, items.Single().CatalogCount);
    }

    // ── Case-insensitive search ───────────────────────────────────────────

    [Fact]
    public async Task GetPagedAsync_CaseInsensitive_FindsMatch()
    {
        _db.Set<Artist>().Add(MakeArtist("Queen"));
        await _db.SaveChangesAsync();

        var (items, totalCount) = await _repo.GetPagedAsync(1, 20, "QUEEN", ct: CancellationToken.None);

        Assert.Equal(1, totalCount);
        Assert.Equal("Queen", items.Single().Name);
    }

    // ── SearchByNameAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task SearchByNameAsync_PrefixMatch_ReturnsMatchingArtists()
    {
        _db.Set<Artist>().AddRange(
            MakeArtist("Radiohead"),
            MakeArtist("Rage Against the Machine"),
            MakeArtist("ABBA"));
        await _db.SaveChangesAsync();

        var results = await _repo.SearchByNameAsync("ra", 5, CancellationToken.None);

        var names = results.Select(a => a.Name).ToList();
        Assert.Equal(2, names.Count);
        Assert.Contains("Radiohead", names);
        Assert.Contains("Rage Against the Machine", names);
    }

    [Fact]
    public async Task SearchByNameAsync_RespectsMaxResults()
    {
        _db.Set<Artist>().AddRange(
            MakeArtist("Band A"),
            MakeArtist("Band B"),
            MakeArtist("Band C"));
        await _db.SaveChangesAsync();

        var results = await _repo.SearchByNameAsync("band", 2, CancellationToken.None);

        Assert.Equal(2, results.Count());
    }

    // ── ExistsByNameAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task ExistsByNameAsync_NewName_ReturnsFalse()
    {
        var result = await _repo.ExistsByNameAsync("NewArtist", CancellationToken.None);
        Assert.False(result);
    }

    [Fact]
    public async Task ExistsByNameAsync_ExistingName_ReturnsTrue()
    {
        _db.Set<Artist>().Add(MakeArtist("Nirvana"));
        await _db.SaveChangesAsync();

        var result = await _repo.ExistsByNameAsync("nirvana", CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task ExistsByNameAsync_ExcludeSelf_ReturnsFalse()
    {
        var artist = MakeArtist("Nirvana");
        _db.Set<Artist>().Add(artist);
        await _db.SaveChangesAsync();

        var result = await _repo.ExistsByNameAsync("nirvana", artist.Id, CancellationToken.None);

        Assert.False(result);
    }

    // ── GetByExternalIdAsync ──────────────────────────────────────────────

    [Fact]
    public async Task GetByExternalIdAsync_ExistingExternalId_ReturnsArtist()
    {
        var artist = MakeArtist("David Bowie");
        artist.ExternalId = "mb-12345";
        artist.ExternalProvider = "musicbrainz";
        _db.Set<Artist>().Add(artist);
        await _db.SaveChangesAsync();

        var found = await _repo.GetByExternalIdAsync("mb-12345", "musicbrainz", CancellationToken.None);

        Assert.NotNull(found);
        Assert.Equal("David Bowie", found.Name);
    }

    [Fact]
    public async Task GetByExternalIdAsync_UnknownId_ReturnsNull()
    {
        var found = await _repo.GetByExternalIdAsync("unknown-id", "musicbrainz", CancellationToken.None);
        Assert.Null(found);
    }

    // ── Unique name constraint ────────────────────────────────────────────

    [Fact]
    public async Task AddAsync_DuplicateName_ThrowsDbUpdateException()
    {
        _db.Set<Artist>().Add(MakeArtist("Pink Floyd"));
        await _db.SaveChangesAsync();

        _db.Set<Artist>().Add(MakeArtist("Pink Floyd"));

        await Assert.ThrowsAsync<DbUpdateException>(() => _db.SaveChangesAsync());
    }

    // ── UpdateAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_ChangeName_Persisted()
    {
        var artist = MakeArtist("Old Name");
        _db.Set<Artist>().Add(artist);
        await _db.SaveChangesAsync();

        artist.Name = "New Name";
        await _repo.UpdateAsync(artist, CancellationToken.None);
        await _db.SaveChangesAsync();

        var found = await _repo.GetByIdAsync(artist.Id, CancellationToken.None);
        Assert.Equal("New Name", found.Name);
    }

    [Fact]
    // [AC] BUG-018: UpdateAsync must work with detached instances from form submissions
    public async Task UpdateAsync_DetachedInstance_Updates()
    {
        // Arrange — persist the artist
        var artist = MakeArtist("Original Artist");
        _db.Set<Artist>().Add(artist);
        await _db.SaveChangesAsync();

        // Detach — simulates returning from a service layer where the context is not shared
        _db.ChangeTracker.Clear();

        // Create a detached instance with modified values (simulates data from form/service layer)
        var editedInstance = new Artist
        {
            Id = artist.Id,
            Name = "Updated Name",
            CreatedAt = artist.CreatedAt,
            UpdatedAt = DateTime.UtcNow
        };

        // Act — UpdateAsync with a detached instance should not throw
        var exception = await Record.ExceptionAsync(async () =>
        {
            await _repo.UpdateAsync(editedInstance, CancellationToken.None);
            await _db.SaveChangesAsync();
        });

        // Assert — no crash and values were persisted
        Assert.Null(exception);
        var found = await _db.Artists.AsNoTracking().FirstAsync(a => a.Id == artist.Id);
        Assert.Equal("Updated Name", found.Name);
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ExistingArtist_Removed()
    {
        var artist = MakeArtist("To Be Deleted");
        _db.Set<Artist>().Add(artist);
        await _db.SaveChangesAsync();

        await _repo.DeleteAsync([artist.Id], CancellationToken.None);
        await _db.SaveChangesAsync();

        var found = await _repo.GetByIdAsync(artist.Id, CancellationToken.None);
        Assert.Null(found);
    }

    [Fact]
    public async Task DeleteAsync_ArtistWithSongs_CascadeDeletesSongs()
    {
        var artist = MakeArtist("Artist With Songs");
        _db.Set<Artist>().Add(artist);
        await _db.SaveChangesAsync();

        _db.Set<Song>().Add(MakeSong(artist.Id, "Song One"));
        await _db.SaveChangesAsync();

        await _repo.DeleteAsync([artist.Id], CancellationToken.None);
        await _db.SaveChangesAsync();

        Assert.Equal(0, await _db.Set<Song>().CountAsync(s => s.ArtistId == artist.Id));
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static Artist MakeArtist(string name) => new()
    {
        Name = name,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private static Song MakeSong(int artistId, string title) => new()
    {
        ArtistId = artistId,
        Title = title,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    // ── Accent-insensitive search ─────────────────────────────────────────

    [Fact]
    public async Task GetPagedAsync_QueryWithoutAccents_FindsAccentedArtist()
    {
        _db.Set<Artist>().Add(MakeArtist("Björk"));
        _db.Set<Artist>().Add(MakeArtist("Adele")); // should not match "bjork"
        await _db.SaveChangesAsync();

        var (items, totalCount) = await _repo.GetPagedAsync(1, 20, "bjork", ct: CancellationToken.None);

        Assert.Equal(1, totalCount);
        Assert.Equal("Björk", items.Single().Name);
    }

    // ── BUG-018 Regression test ───────────────────────────────────────────

    [Fact]
    // [AC] BUG-018: GetPagedAsync must not add entities to the ChangeTracker
    public async Task GetPagedAsync_ExplicitNoTracking_DoesNotPollutTracker_AndUpdateSucceeds()
    {
        // Override global NoTracking — simulates a context where the global setting is absent
        _db.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.TrackAll;

        // Arrange — seed
        var artist = MakeArtist("Test Artist");
        _db.Artists.Add(artist);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        // Act — list query (must not track despite TrackAll context setting)
        await _repo.GetPagedAsync(1, 20, string.Empty);

        // Assert 1 — explicit AsNoTracking on GetPagedAsync overrides context default
        Assert.Empty(_db.ChangeTracker.Entries<Artist>());

        // Assert 2 — update on the same entity Id succeeds with no tracking conflict
        artist.Name = "Updated Name";
        await _repo.UpdateAsync(artist, CancellationToken.None);
        await _db.SaveChangesAsync();

        var saved = await _db.Artists.AsNoTracking().FirstAsync(a => a.Id == artist.Id);
        Assert.Equal("Updated Name", saved.Name);
    }

    // ── GetByNamesCollatedAsync ──────────────────────────────────────────

    [Fact]
    // [AC] REQ-FORMUX-03: remote dedup tier (b) — collation-equal name via batch DB lookup
    public async Task GetByNamesCollatedAsync_AccentAndCaseVariants_ResolvesInOneQuery()
    {
        _db.Artists.AddRange(new Artist { Name = "cafe" }, new Artist { Name = "Metallica" });
        await _db.SaveChangesAsync();

        var found = await _repo.GetByNamesCollatedAsync(["Café", "METALLICA", "Nobody"]);

        Assert.Equal(2, found.Count);
        Assert.Contains(found, a => a.Name == "cafe");
        Assert.Contains(found, a => a.Name == "Metallica");
    }

    [Fact]
    // [AC] REQ-FORMUX-03: batch lookup with no matches returns empty (no exception)
    public async Task GetByNamesCollatedAsync_NoMatches_ReturnsEmpty()
    {
        var found = await _repo.GetByNamesCollatedAsync(["Ghost"]);
        Assert.Empty(found);
    }
}
