using Microsoft.EntityFrameworkCore;
using MyVocaList.Domain.Entity;
using MyVocaList.Infra;
using MyVocaList.Infra.Repository;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Integration.Repositories;

/// <summary>
/// Task 3.9b: FTS5 trigram artist-name candidate search.
/// Real SQLite temp file, schema built with MigrateAsync so the ArtistSearch index and triggers exist.
/// </summary>
public class ArtistRepositorySearchTests : IAsyncLifetime
{
    private AppDbContext _db;
    private ArtistRepository _repo;

    public async Task InitializeAsync()
    {
        _db = TestDbContextFactory.Create();
        await _db.Database.MigrateAsync();
        _repo = new ArtistRepository(_db);

        foreach (var name in new[] { "Metallica", "The Beatles", "Beyoncé", "U2", "Oasis" })
            _db.Artists.Add(MakeArtist(name));
        await _db.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await _db.Database.EnsureDeletedAsync();
        await _db.DisposeAsync();
    }

    private static Artist MakeArtist(string name)
        => new() { Name = name, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

    // [AC] REQ-SFT-19: a one-letter typo still retrieves the artist via shared trigrams
    [Fact]
    public async Task SearchNameCandidatesAsync_Metalica_RetrievesMetallica()
    {
        var hits = await _repo.SearchNameCandidatesAsync("Metalica", 50, CancellationToken.None);
        Assert.Contains(hits, a => a.Name == "Metallica");
    }

    // [AC] REQ-SFT-19: case-insensitive, leading article does not block retrieval
    [Fact]
    public async Task SearchNameCandidatesAsync_Beatles_RetrievesTheBeatles()
    {
        var hits = await _repo.SearchNameCandidatesAsync("beatles", 50, CancellationToken.None);
        Assert.Contains(hits, a => a.Name == "The Beatles");
    }

    // [AC] REQ-SFT-19: diacritics are folded by the index
    [Fact]
    public async Task SearchNameCandidatesAsync_Unaccented_RetrievesAccentedName()
    {
        var hits = await _repo.SearchNameCandidatesAsync("beyonce", 50, CancellationToken.None);
        Assert.Contains(hits, a => a.Name == "Beyoncé");
    }

    // [AC] REQ-SFT-19: names under 3 chars use the collated prefix query
    [Fact]
    public async Task SearchNameCandidatesAsync_ShortName_UsesCollatedPrefix()
    {
        var hits = await _repo.SearchNameCandidatesAsync("u2", 50, CancellationToken.None);
        Assert.Contains(hits, a => a.Name == "U2");
    }

    // [AC] REQ-SFT-19: FTS syntax characters in the query never throw
    [Theory]
    [InlineData("\"")]
    [InlineData("a\"b")]
    [InlineData("metal*")]
    [InlineData("(meta")]
    [InlineData("NEAR metal")]
    [InlineData("metal AND")]
    [InlineData("-metal")]
    public async Task SearchNameCandidatesAsync_FtsSyntaxCharacters_DoNotThrow(string query)
    {
        var hits = await _repo.SearchNameCandidatesAsync(query, 50, CancellationToken.None);
        Assert.NotNull(hits);
    }

    // [AC] REQ-SFT-19: result is bounded by take
    [Fact]
    public async Task SearchNameCandidatesAsync_RespectsTake()
    {
        var hits = await _repo.SearchNameCandidatesAsync("met", 1, CancellationToken.None);
        Assert.True(hits.Count <= 1);
    }

    // [AC] REQ-SFT-19: missing index returns [] and the prefix path still works
    [Fact]
    public async Task SearchNameCandidatesAsync_IndexMissing_ReturnsEmptyAndPrefixStillWorks()
    {
        await using var plain = TestDbContextFactory.Create();
        await plain.Database.EnsureCreatedAsync();
        try
        {
            plain.Artists.Add(MakeArtist("ABBA"));
            await plain.SaveChangesAsync();
            var repo = new ArtistRepository(plain);

            var fts = await repo.SearchNameCandidatesAsync("abba", 50, CancellationToken.None);
            var prefix = await repo.SearchNameCandidatesAsync("ab", 50, CancellationToken.None);

            Assert.Empty(fts);
            Assert.Contains(prefix, a => a.Name == "ABBA");
        }
        finally
        {
            await plain.Database.EnsureDeletedAsync();
        }
    }

    // [AC] REQ-SFT-34: the index follows a rename
    [Fact]
    public async Task SearchNameCandidatesAsync_AfterRename_ReflectsNewName()
    {
        var oasis = await _db.Artists.SingleAsync(a => a.Name == "Oasis");
        oasis.Name = "Radiohead";
        await _db.SaveChangesAsync();

        var renamed = await _repo.SearchNameCandidatesAsync("Radiohead", 50, CancellationToken.None);
        var old = await _repo.SearchNameCandidatesAsync("Oasis", 50, CancellationToken.None);

        Assert.Contains(renamed, a => a.Name == "Radiohead");
        Assert.DoesNotContain(old, a => a.Name == "Oasis");
    }
}
