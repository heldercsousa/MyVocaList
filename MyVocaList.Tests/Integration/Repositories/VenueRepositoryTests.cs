using MyVocaList.Infra;
using MyVocaList.Infra.Repository;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Integration.Repositories;

public class VenueRepositoryTests : IAsyncLifetime
{
    private AppDbContext _db = null!;
    private VenueRepository _repo = null!;

    public async Task InitializeAsync()
    {
        _db = TestDbContextFactory.Create();
        await _db.Database.EnsureCreatedAsync();
        _repo = new VenueRepository(_db);
    }

    public async Task DisposeAsync()
    {
        await _db.Database.EnsureDeletedAsync();
        await _db.DisposeAsync();
    }

    // ── AddAsync ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddAsync_ValidVenue_PersistedAndReturnedById()
    {
        var venue = new Venue { Name = "Jazz Club" };
        await _repo.AddAsync(venue);
        await _repo.SaveChangesAsync();

        var found = await _repo.GetByIdAsync(venue.Id);

        Assert.NotNull(found);
        Assert.Equal("Jazz Club", found.Name);
    }

    // ── GetPagedWithEventInfoAsync ────────────────────────────────────────────

    [Fact]
    public async Task GetPagedWithEventInfoAsync_NoQuery_ReturnsAllOrderedByName()
    {
        await _repo.AddAsync(new Venue { Name = "Rock Arena" });
        await _repo.AddAsync(new Venue { Name = "Jazz Club" });
        await _repo.SaveChangesAsync();

        var (items, total) = await _repo.GetPagedWithEventInfoAsync(1, 20);

        Assert.Equal(2, total);
        var names = items.Select(x => x.venue.Name).ToList();
        Assert.Equal("Jazz Club", names[0]);    // alphabetical order
        Assert.Equal("Rock Arena", names[1]);
    }

    [Fact]
    public async Task GetPagedWithEventInfoAsync_SearchQuery_ReturnsOnlyMatching()
    {
        await _repo.AddAsync(new Venue { Name = "Rock Arena" });
        await _repo.AddAsync(new Venue { Name = "Jazz Club" });
        await _repo.SaveChangesAsync();

        var (items, total) = await _repo.GetPagedWithEventInfoAsync(1, 20, "rock");

        Assert.Equal(1, total);
        Assert.Equal("Rock Arena", items.First().venue.Name);
    }

    [Fact]
    public async Task GetPagedWithEventInfoAsync_SearchQuery_CaseInsensitive()
    {
        await _repo.AddAsync(new Venue { Name = "Rock Arena" });
        await _repo.AddAsync(new Venue { Name = "Jazz Club" });
        await _repo.SaveChangesAsync();

        var (items, total) = await _repo.GetPagedWithEventInfoAsync(1, 20, "JAZZ");

        Assert.Equal(1, total);
        Assert.Equal("Jazz Club", items.First().venue.Name);
    }

    [Fact]
    public async Task GetPagedWithEventInfoAsync_SearchQuery_SubstringMatch()
    {
        await _repo.AddAsync(new Venue { Name = "Rock Arena" });
        await _repo.AddAsync(new Venue { Name = "Jazz Club" });
        await _repo.SaveChangesAsync();

        var (items, total) = await _repo.GetPagedWithEventInfoAsync(1, 20, "arena");

        Assert.Equal(1, total);
        Assert.Equal("Rock Arena", items.First().venue.Name);
    }

    [Fact]
    public async Task GetPagedWithEventInfoAsync_Page2_SkipsFirstPage()
    {
        for (var i = 1; i <= 5; i++)
        {
            await _repo.AddAsync(new Venue { Name = $"Venue {i:D2}" });
        }
        await _repo.SaveChangesAsync();

        var (items, total) = await _repo.GetPagedWithEventInfoAsync(pageNumber: 2, pageSize: 2);

        Assert.Equal(5, total);
        Assert.Equal(2, items.Count());
        Assert.Equal("Venue 03", items.First().venue.Name); // page 2, ordered by name
    }

    [Fact]
    public async Task GetPagedWithEventInfoAsync_EmptyDb_ReturnsZeroTotal()
    {
        var (items, total) = await _repo.GetPagedWithEventInfoAsync(1, 20);

        Assert.Equal(0, total);
        Assert.Empty(items);
    }

    // ── GetByNameAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByNameAsync_ExistingName_ReturnsVenue()
    {
        await _repo.AddAsync(new Venue { Name = "Jazz Club" });
        await _repo.SaveChangesAsync();

        var found = await _repo.GetByNameAsync("Jazz Club");

        Assert.NotNull(found);
        Assert.Equal("Jazz Club", found.Name);
    }

    [Fact]
    public async Task GetByNameAsync_UnknownName_ReturnsNull()
    {
        var found = await _repo.GetByNameAsync("Does Not Exist");

        Assert.Null(found);
    }

    // ── Accent + case collation (NOCASE_NOACCENT) ─────────────────────────────

    [Fact]
    public async Task GetPagedWithEventInfoAsync_AccentInsensitive_FindsAccentedVenue()
    {
        await _repo.AddAsync(new Venue { Name = "Café do Brasil" });
        await _repo.SaveChangesAsync();
        var (items, total) = await _repo.GetPagedWithEventInfoAsync(1, 20, "cafe");
        Assert.Equal(1, total);
        Assert.Equal("Café do Brasil", items.First().venue.Name);
    }

    [Fact]
    public async Task GetPagedWithEventInfoAsync_AccentAndCaseInsensitive_Combined()
    {
        await _repo.AddAsync(new Venue { Name = "João's Bar" });
        await _repo.SaveChangesAsync();
        var (items, total) = await _repo.GetPagedWithEventInfoAsync(1, 20, "JOAO");
        Assert.Equal(1, total);
        Assert.Equal("João's Bar", items.First().venue.Name);
    }

    [Fact]
    public async Task SearchByNameStartsWithAsync_AccentInsensitive_FindsMatch()
    {
        await _repo.AddAsync(new Venue { Name = "Müller's Hall" });
        await _repo.SaveChangesAsync();
        var results = await _repo.SearchByNameStartsWithAsync("muller", 10);
        Assert.Single(results);
    }
}
