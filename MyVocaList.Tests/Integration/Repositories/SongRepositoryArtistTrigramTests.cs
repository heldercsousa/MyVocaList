using Microsoft.EntityFrameworkCore;
using MyVocaList.Domain.Entity;
using MyVocaList.Infra;
using MyVocaList.Infra.Repository;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Integration.Repositories;

/// <summary>
/// BUG-085: same-artist trigram candidate query for the Possible duplicates step (REQ-SFT-23).
/// Real SQLite temp file, schema built with MigrateAsync so the SongSearch FTS5 index and its
/// triggers exist (same pattern as <see cref="SongRepositorySearchTests"/>).
/// </summary>
public class SongRepositoryArtistTrigramTests : IAsyncLifetime
{
    private AppDbContext _db;
    private SongRepository _repo;
    private Artist _queen;
    private Artist _metallica;

    public async Task InitializeAsync()
    {
        _db = TestDbContextFactory.Create();
        await _db.Database.MigrateAsync();
        _repo = new SongRepository(_db);
        _queen = await SeedArtistAsync("Queen");
        _metallica = await SeedArtistAsync("Metallica");
    }

    public async Task DisposeAsync()
    {
        await _db.Database.EnsureDeletedAsync();
        await _db.DisposeAsync();
    }

    // [AC] REQ-SFT-23: a typed title LONGER than the stored one ("Patiencee" vs stored "Patience")
    // finds the stored title for the same artist. This is the regression: the old forward prefix
    // pool could never return it.
    // [BUG] BUG-085
    [Fact]
    public async Task SearchTitleCandidatesForArtistAsync_TypedTitleLongerThanStored_FindsStoredTitle()
    {
        _db.Songs.Add(MakeSong(_queen.Id, "Patience", ""));
        await _db.SaveChangesAsync();

        var hits = await _repo.SearchTitleCandidatesForArtistAsync(
            _queen.Id, "Patiencee", 50, CancellationToken.None);

        var hit = Assert.Single(hits);
        Assert.Equal("Patience", hit.Title);
        Assert.Equal(_queen.Id, hit.ArtistId);
    }

    // [AC] REQ-SFT-23: candidates are scoped to the same artist; an identical title stored under
    // another artist is never returned.
    // [BUG] BUG-085
    [Fact]
    public async Task SearchTitleCandidatesForArtistAsync_OtherArtistSameTitle_IsExcluded()
    {
        var queenPatience = MakeSong(_queen.Id, "Patience", "");
        _db.Songs.AddRange(queenPatience, MakeSong(_metallica.Id, "Patience", ""));
        await _db.SaveChangesAsync();

        var hits = await _repo.SearchTitleCandidatesForArtistAsync(
            _queen.Id, "Patiencee", 50, CancellationToken.None);

        var hit = Assert.Single(hits);
        Assert.Equal(queenPatience.Id, hit.SongId);
        Assert.Equal(_queen.Id, hit.ArtistId);
    }

    // [AC] REQ-SFT-23: the result is bounded by take (the candidate pool size) and stays scoped to the
    // same artist. Rewritten from the retired GetFuzzyCandidatePoolAsync_RespectsArtistScopeAndTake
    // (old tag AC-5.6).
    // [BUG] BUG-085
    [Fact]
    public async Task SearchTitleCandidatesForArtistAsync_RespectsArtistScopeAndTake()
    {
        var quentin = await SeedArtistAsync("Quentin Music");
        _db.Songs.AddRange(
            MakeSong(_queen.Id, "Bohemian Rhapsody", ""),
            MakeSong(_queen.Id, "Bohemian Anniversary", ""),
            MakeSong(_queen.Id, "Somebody to Love", ""),
            MakeSong(quentin.Id, "Bohemian Dreams", ""));
        await _db.SaveChangesAsync();

        var hits = await _repo.SearchTitleCandidatesForArtistAsync(
            _queen.Id, "Bohemian", 2, CancellationToken.None);

        Assert.Equal(2, hits.Count);
        Assert.All(hits, h => Assert.Equal(_queen.Id, h.ArtistId));
    }

    // [AC] REQ-SFT-23: a typo inside the FIRST word still finds the stored title, and a title that
    // only shares a prefix-like start with another stored title is not returned by the trigram match.
    // Rewritten from the retired GetFuzzyCandidatePoolAsync_PrefixFilter_ReturnsOnlyMatchingPrefix
    // (old tag AC-5.7: the prefix-only behaviour is retired by the REQ-SFT-23 note of 2026-11-09).
    // [BUG] BUG-085
    [Fact]
    public async Task SearchTitleCandidatesForArtistAsync_TypoInFirstWord_FindsStoredTitleAndExcludesUnrelated()
    {
        var led = await SeedArtistAsync("Led Zeppelin");
        _db.Songs.AddRange(
            MakeSong(led.Id, "Stairway to Heaven", ""),
            MakeSong(led.Id, "Black Dog", ""));
        await _db.SaveChangesAsync();

        var hits = await _repo.SearchTitleCandidatesForArtistAsync(
            led.Id, "Stairwya to Heavn", 50, CancellationToken.None);

        Assert.Contains(hits, h => h.Title == "Stairway to Heaven");
        Assert.DoesNotContain(hits, h => h.Title == "Black Dog");
    }

    // [AC] REQ-SFT-33: a typed title with no word of 3+ characters has no trigram window; the query
    // falls back to the collated title prefix, scoped to the same artist.
    // [BUG] BUG-085
    [Fact]
    public async Task SearchTitleCandidatesForArtistAsync_ShortTitle_UsesCollatedPrefixFallback()
    {
        _db.Songs.AddRange(
            MakeSong(_queen.Id, "Abba Song", ""),
            MakeSong(_queen.Id, "Cab Driver", ""),
            MakeSong(_metallica.Id, "Abba Other", ""));
        await _db.SaveChangesAsync();

        var hits = await _repo.SearchTitleCandidatesForArtistAsync(
            _queen.Id, "ab", 50, CancellationToken.None);

        // "Cab Driver" contains "ab" but does not start with it; "Abba Other" belongs to another artist.
        Assert.Equal("Abba Song", Assert.Single(hits).Title);
    }

    // [AC] REQ-SFT-23: a typo in the middle of the typed title still retrieves the stored title
    // (shared trigram windows), for the same artist.
    // [BUG] BUG-085
    [Theory]
    [InlineData("Pateince")]
    [InlineData("Paitence")]
    public async Task SearchTitleCandidatesForArtistAsync_TypoInMiddle_StillRetrieves(string typed)
    {
        _db.Songs.Add(MakeSong(_queen.Id, "Patience", ""));
        await _db.SaveChangesAsync();

        var hits = await _repo.SearchTitleCandidatesForArtistAsync(
            _queen.Id, typed, 50, CancellationToken.None);

        Assert.Contains(hits, h => h.Title == "Patience");
    }

    // [AC] REQ-SFT-23: a title that shares no 3-character window with any stored title of the artist
    // returns no candidates.
    // [BUG] BUG-085
    [Fact]
    public async Task SearchTitleCandidatesForArtistAsync_UnrelatedTitle_ReturnsEmpty()
    {
        _db.Songs.Add(MakeSong(_queen.Id, "Patience", ""));
        await _db.SaveChangesAsync();

        var hits = await _repo.SearchTitleCandidatesForArtistAsync(
            _queen.Id, "Xyzzy", 50, CancellationToken.None);

        Assert.Empty(hits);
    }

    private async Task<Artist> SeedArtistAsync(string name)
    {
        var artist = new Artist { Name = name, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
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
