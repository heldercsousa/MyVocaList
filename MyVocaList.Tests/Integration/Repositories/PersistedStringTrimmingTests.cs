using Microsoft.EntityFrameworkCore;
using MyVocaList.Domain.Entity;
using MyVocaList.Infra;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Integration.Repositories;

/// <summary>
/// Real-SQLite round-trip tests proving the EF Core <c>ValueConverter</c>s configured per
/// <c>design.md § D3</c> trim/collapse whitespace on write and compose correctly with the
/// existing <c>UseCollation(CollationConstants.Default)</c> comparison behaviour.
/// Uses a real temp-file SQLite database (never the EF in-memory provider) per
/// <c>testing.md</c> anti-pattern rule — collation quirks (<c>EF.Functions.Collate</c>) do not
/// replicate under the in-memory provider.
/// </summary>
public class PersistedStringTrimmingTests : IAsyncLifetime
{
    private AppDbContext _db;

    public async Task InitializeAsync()
    {
        _db = TestDbContextFactory.Create();
        await _db.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _db.Database.EnsureDeletedAsync();
        await _db.DisposeAsync();
    }

    // [AC] REQ-TRIM-05/06: Person.FullName — edge-trim + internal whitespace collapse on save.
    [Fact]
    public async Task PersonFullName_LeadingTrailingAndInternalWhitespace_PersistedTrimmedAndCollapsed()
    {
        var person = new Person(" John  Doe ");
        _db.Set<Person>().Add(person);
        await _db.SaveChangesAsync();

        var reloaded = await _db.Set<Person>().AsNoTracking().FirstAsync(p => p.Id == person.Id);

        Assert.Equal("John Doe", reloaded.FullName);
    }

    // [AC] REQ-TRIM-07: Person.Email — whitespace-only optional field persists as null.
    [Fact]
    public async Task PersonEmail_WhitespaceOnly_PersistedAsNull()
    {
        var person = new Person("Jane Doe") { Email = "   " };
        _db.Set<Person>().Add(person);
        await _db.SaveChangesAsync();

        var reloaded = await _db.Set<Person>().AsNoTracking().FirstAsync(p => p.Id == person.Id);

        Assert.Null(reloaded.Email);
    }

    // [AC] REQ-TRIM-05/06: Artist.Name — edge-trim + internal whitespace collapse on save.
    [Fact]
    public async Task ArtistName_LeadingTrailingAndInternalWhitespace_PersistedTrimmedAndCollapsed()
    {
        var artist = new Artist { Name = "  Guns  N' Roses  ", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _db.Set<Artist>().Add(artist);
        await _db.SaveChangesAsync();

        var reloaded = await _db.Set<Artist>().AsNoTracking().FirstAsync(a => a.Id == artist.Id);

        Assert.Equal("Guns N' Roses", reloaded.Name);
    }

    // Collation + converter composition: converter trims on write, collation compares case-
    // insensitively on read — both mechanisms must coexist on Artist.Name without conflict.
    [Fact]
    public async Task ArtistName_TrimmedOnWrite_StillMatchesCaseInsensitiveQuery()
    {
        var artist = new Artist { Name = "  Queen  ", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _db.Set<Artist>().Add(artist);
        await _db.SaveChangesAsync();

        var found = await _db.Set<Artist>().AsNoTracking().FirstOrDefaultAsync(a => a.Name == "QUEEN");

        Assert.NotNull(found);
        Assert.Equal("Queen", found.Name);
    }

    // [AC] REQ-TRIM-05/06: Song.Title — edge-trim + internal whitespace collapse on save.
    [Fact]
    public async Task SongTitle_LeadingTrailingAndInternalWhitespace_PersistedTrimmedAndCollapsed()
    {
        var artist = new Artist { Name = "Queen", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _db.Set<Artist>().Add(artist);
        await _db.SaveChangesAsync();

        var song = new Song
        {
            ArtistId = artist.Id,
            Title = "  Bohemian  Rhapsody ",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Set<Song>().Add(song);
        await _db.SaveChangesAsync();

        var reloaded = await _db.Set<Song>().AsNoTracking().FirstAsync(s => s.Id == song.Id);

        Assert.Equal("Bohemian Rhapsody", reloaded.Title);
    }

    // [AC] REQ-TRIM-07: Song.FeaturedArtists — whitespace-only optional field persists as null.
    [Fact]
    public async Task SongFeaturedArtists_WhitespaceOnly_PersistedAsNull()
    {
        var artist = new Artist { Name = "Queen", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _db.Set<Artist>().Add(artist);
        await _db.SaveChangesAsync();

        var song = new Song
        {
            ArtistId = artist.Id,
            Title = "Under Pressure",
            FeaturedArtists = "   ",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Set<Song>().Add(song);
        await _db.SaveChangesAsync();

        var reloaded = await _db.Set<Song>().AsNoTracking().FirstAsync(s => s.Id == song.Id);

        Assert.Null(reloaded.FeaturedArtists);
    }
}
