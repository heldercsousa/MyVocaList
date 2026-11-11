using Microsoft.EntityFrameworkCore;
using MyVocaList.Domain.Entity;
using MyVocaList.Infra;
using MyVocaList.Infra.Repository;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Integration.Repositories;

/// <summary>
/// Task 3.9: FTS5 trigram title/artist candidate search and dedup lookups.
/// Real SQLite temp file, schema built with MigrateAsync so the SongSearch index and triggers exist.
/// </summary>
public class SongRepositorySearchTests : IAsyncLifetime
{
    private AppDbContext _db;
    private SongRepository _repo;

    public async Task InitializeAsync()
    {
        _db = TestDbContextFactory.Create();
        await _db.Database.MigrateAsync();
        _repo = new SongRepository(_db);

        var guns = await SeedArtistAsync("Guns N' Roses");
        var cyndi = await SeedArtistAsync("Cyndi Lauper");
        var cure = await SeedArtistAsync("The Cure");
        var oasis = await SeedArtistAsync("Oasis");
        var ana = await SeedArtistAsync("Ana Carolina");
        var abba = await SeedArtistAsync("ABBA");

        _db.Songs.AddRange(
            MakeSong(guns.Id, "Don't Cry", "", "Deezer", "3135556"),
            MakeSong(guns.Id, "Don't Cry", "Live"),
            MakeSong(guns.Id, "Sweet Child O' Mine", ""),
            MakeSong(cyndi.Id, "Cry", ""),
            MakeSong(cure.Id, "Boys Don't Cry", ""),
            MakeSong(oasis.Id, "Dont Look Back in Anger", ""),
            MakeSong(ana.Id, "Não Sei", ""),
            MakeSong(abba.Id, "Abba Song", ""));
        await _db.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await _db.Database.EnsureDeletedAsync();
        await _db.DisposeAsync();
    }

    // [AC] REQ-SFT-30: query words become an OR of distinct quoted 3-char windows
    [Fact]
    public void BuildTrigramMatch_DontCry_ReturnsQuotedWindowsOred()
        => Assert.Equal("\"don\" OR \"ont\" OR \"cry\"", FtsQueryBuilder.BuildTrigramMatch("dont cry"));

    // [AC] REQ-SFT-33: no word of 3+ chars means no FTS query (prefix path)
    [Theory]
    [InlineData("ab")]
    [InlineData("a b")]
    [InlineData("  ")]
    public void BuildTrigramMatch_NoWordOfThreeChars_ReturnsNull(string query)
        => Assert.Null(FtsQueryBuilder.BuildTrigramMatch(query));

    // [AC] REQ-SFT-02: a double quote inside a word is doubled inside the quoted window
    [Fact]
    public void BuildTrigramMatch_QuoteInWord_IsDoubled()
        => Assert.Equal("\"don\" OR \"on\"\"\" OR \"n\"\"t\"", FtsQueryBuilder.BuildTrigramMatch("don\"t"));

    // [AC] REQ-SFT-30: "dont cry" finds "Don't Cry"
    [Fact]
    public async Task SearchTitleCandidatesAsync_DontCry_IncludesDontCry()
    {
        var hits = await _repo.SearchTitleCandidatesAsync("dont cry", 50, CancellationToken.None);
        Assert.Contains(hits, h => h.Title == "Don't Cry");
    }

    // [AC] REQ-SFT-30: "don cry" finds "Don't Cry"
    [Fact]
    public async Task SearchTitleCandidatesAsync_DonCry_IncludesDontCry()
    {
        var hits = await _repo.SearchTitleCandidatesAsync("don cry", 50, CancellationToken.None);
        Assert.Contains(hits, h => h.Title == "Don't Cry");
    }

    // [AC] REQ-SFT-32: the artist column is searched too
    [Fact]
    public async Task SearchTitleCandidatesAsync_GunsCry_IncludesDontCryViaArtist()
    {
        var hits = await _repo.SearchTitleCandidatesAsync("guns cry", 50, CancellationToken.None);
        Assert.Contains(hits, h => h.Title == "Don't Cry");
    }

    // [AC] REQ-SFT-31: case and diacritics are folded by the tokenizer
    [Theory]
    [InlineData("nao sei")]
    [InlineData("NAO SEI")]
    public async Task SearchTitleCandidatesAsync_NaoSeiAnyCase_MatchesAccentedTitle(string query)
    {
        var hits = await _repo.SearchTitleCandidatesAsync(query, 50, CancellationToken.None);
        Assert.Contains(hits, h => h.Title == "Não Sei");
    }

    // [AC] REQ-SFT-33: queries without a 3-char word use the collated title prefix
    [Theory]
    [InlineData("ab")]
    [InlineData("AB")]
    public async Task SearchTitleCandidatesAsync_ShortQuery_UsesCollatedTitlePrefix(string query)
    {
        var hits = await _repo.SearchTitleCandidatesAsync(query, 50, CancellationToken.None);
        Assert.Contains(hits, h => h.Title == "Abba Song");
    }

    // [AC] REQ-SFT-02: FTS syntax characters never throw
    [Theory]
    [InlineData("\"")]
    [InlineData("don\"t cry")]
    [InlineData("ac/dc")]
    [InlineData("rock*")]
    [InlineData("a:b cry")]
    [InlineData("(cry)")]
    [InlineData("NEAR cry")]
    [InlineData("cry AND")]
    [InlineData("-cry")]
    public async Task SearchTitleCandidatesAsync_FtsSyntaxCharacters_DoNotThrow(string query)
    {
        var hits = await _repo.SearchTitleCandidatesAsync(query, 50, CancellationToken.None);
        Assert.NotNull(hits);
    }

    // [AC] REQ-SFT-04: result is bounded by take
    [Fact]
    public async Task SearchTitleCandidatesAsync_RespectsTake()
    {
        var hits = await _repo.SearchTitleCandidatesAsync("cry", 2, CancellationToken.None);
        Assert.True(hits.Count is > 0 and <= 2);
    }

    // [AC] REQ-SFT-33: missing index returns [] and the prefix path still works
    [Fact]
    public async Task SearchTitleCandidatesAsync_IndexMissing_ReturnsEmptyAndPrefixStillWorks()
    {
        await using var plain = TestDbContextFactory.Create();
        await plain.Database.EnsureCreatedAsync();
        try
        {
            var artist = new Artist { Name = "ABBA", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            plain.Artists.Add(artist);
            await plain.SaveChangesAsync();
            plain.Songs.Add(MakeSong(artist.Id, "Abba Song", ""));
            await plain.SaveChangesAsync();
            var repo = new SongRepository(plain);

            var fts = await repo.SearchTitleCandidatesAsync("dont cry", 50, CancellationToken.None);
            var prefix = await repo.SearchTitleCandidatesAsync("ab", 50, CancellationToken.None);

            Assert.Empty(fts);
            Assert.Contains(prefix, h => h.Title == "Abba Song");
        }
        finally
        {
            await plain.Database.EnsureDeletedAsync();
        }
    }

    // [AC] REQ-SFT-05: external-id holders only
    [Fact]
    public async Task FindByExternalIdsAsync_ReturnsHoldersOnly()
    {
        var hits = await _repo.FindByExternalIdsAsync("Deezer", ["3135556", "x"], CancellationToken.None);
        var hit = Assert.Single(hits);
        Assert.Equal("Don't Cry", hit.Title);
        Assert.Equal("3135556", hit.ExternalId);
    }

    // [AC] REQ-SFT-05: collated (title, artist, "") triple matches
    [Fact]
    public async Task FindByTitleAndArtistNamesAsync_CollatedTripleEmptyVersion_Returns()
    {
        var hits = await _repo.FindByTitleAndArtistNamesAsync(
            [("DON'T CRY", "guns n' roses", "")], CancellationToken.None);
        var hit = Assert.Single(hits);
        Assert.Equal("Don't Cry", hit.Title);
        Assert.Equal("", hit.Version);
    }

    // [AC] REQ-SFT-05: the version is part of the match, per item
    [Fact]
    public async Task FindByTitleAndArtistNamesAsync_VersionMustMatch()
    {
        var empty = await _repo.FindByTitleAndArtistNamesAsync(
            [("don't cry", "guns n' roses", "")], CancellationToken.None);
        var live = await _repo.FindByTitleAndArtistNamesAsync(
            [("don't cry", "guns n' roses", "LIVE")], CancellationToken.None);

        Assert.Equal("", Assert.Single(empty).Version);
        Assert.Equal("Live", Assert.Single(live).Version);
    }

    // [AC] AC-STL-04 (REQ-STL-04): a 250-char title is found by exact (collated triple) and trigram search
    [Fact]
    public async Task LongTitle250Chars_FoundByExactAndTrigramSearch()
    {
        const string head = "Zanzibar Quixotic Remastered Medley (feat. Marmalade Orchestra) - Live at the Grand Hall ";
        var title = (head + new string('z', 250)).Substring(0, 250).TrimEnd();
        title = title.PadRight(250, 'q');
        Assert.Equal(250, title.Length);
        var artist = await SeedArtistAsync("Long Title Artist");
        _db.Songs.Add(MakeSong(artist.Id, title, ""));
        await _db.SaveChangesAsync();

        var exact = await _repo.FindByTitleAndArtistNamesAsync(
            [(title.ToUpperInvariant(), "long title artist", "")], CancellationToken.None);
        var trigram = await _repo.SearchTitleCandidatesAsync("zanzibar quixotic marmalade", 50, CancellationToken.None);

        Assert.Equal(title, Assert.Single(exact).Title);
        Assert.Contains(trigram, h => h.Title == title);
    }

    private async Task<Artist> SeedArtistAsync(string name)
    {
        var artist = new Artist { Name = name, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _db.Artists.Add(artist);
        await _db.SaveChangesAsync();
        return artist;
    }

    private static Song MakeSong(int artistId, string title, string version,
        string? provider = null, string? externalId = null) => new()
    {
        ArtistId = artistId,
        Title = title,
        Version = version,
        ExternalProvider = provider,
        ExternalId = externalId,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };
}
