using MyVocaList.Infra;
using MyVocaList.Infra.Repository;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Integration.Repositories;

/// <summary>
/// Integration tests for Artist fuzzy pool query method (Wave 2).
/// Uses real SQLite + CollationInterceptor to validate collation behaviour.
/// </summary>
public class ArtistRepositoryResolutionTests : IAsyncLifetime
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

    // [AC] AC-5.8: GetFuzzyCandidatePoolAsync respects the take limit
    [Fact]
    public async Task GetFuzzyCandidatePoolAsync_RespectsTake()
    {
        _db.Artists.AddRange(
            MakeArtist("Queen Classic"),
            MakeArtist("Queen Tribute"),
            MakeArtist("Queen Revolution"),
            MakeArtist("Queen Forever"));
        await _db.SaveChangesAsync();

        var pool = await _repo.GetFuzzyCandidatePoolAsync("Queen", 2, CancellationToken.None);

        Assert.True(pool.Count <= 2);
    }

    // [AC] AC-5.9: GetFuzzyCandidatePoolAsync returns only artists whose name starts with the prefix
    [Fact]
    public async Task GetFuzzyCandidatePoolAsync_PrefixFilter_ReturnsOnlyMatchingPrefix()
    {
        _db.Artists.AddRange(
            MakeArtist("Beatles"),
            MakeArtist("Beach Boys"),
            MakeArtist("Metallica"));
        await _db.SaveChangesAsync();

        var pool = await _repo.GetFuzzyCandidatePoolAsync("Beat", 50, CancellationToken.None);

        Assert.Single(pool);
        Assert.Equal("Beatles", pool[0].Name);
    }

    // [AC] AC-5.10: GetFuzzyCandidatePoolAsync is case-insensitive (NOCASE_NOACCENT collation)
    [Fact]
    public async Task GetFuzzyCandidatePoolAsync_CaseInsensitivePrefix_FindsMatch()
    {
        _db.Artists.Add(MakeArtist("Nirvana"));
        await _db.SaveChangesAsync();

        var pool = await _repo.GetFuzzyCandidatePoolAsync("nirv", 50, CancellationToken.None);

        Assert.Single(pool);
        Assert.Equal("Nirvana", pool[0].Name);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static Artist MakeArtist(string name) => new()
    {
        Name = name,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };
}
