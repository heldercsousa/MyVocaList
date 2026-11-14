using Microsoft.EntityFrameworkCore;
using MyVocaList.Infra;
using MyVocaList.Infra.Repository;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Integration.Repositories;

/// <summary>
/// REQ-PIC-11: the orphan sweep reads every stored local image reference. Real SQLite temp file.
/// </summary>
public class LocalImageReferencesRepositoryTests : IAsyncLifetime
{
    private const string SongLocal = "local:songs/0123456789abcdef0123456789abcdef";
    private const string ArtistLocal = "local:artists/fedcba9876543210fedcba9876543210";

    private AppDbContext _db;

    public async Task InitializeAsync()
    {
        _db = TestDbContextFactory.Create();
        await _db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _db.Database.EnsureDeletedAsync();
        await _db.DisposeAsync();
    }

    private static Artist MakeArtist(string name, string? imageUrl) => new()
    {
        Name = name,
        ImageUrl = imageUrl,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private static Song MakeSong(int artistId, string title, string? imageUrl) => new()
    {
        ArtistId = artistId,
        Title = title,
        Version = "",
        ImageUrl = imageUrl,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    [Fact]
    // [AC] REQ-PIC-11: only local references of songs are returned; https URLs and nulls are excluded.
    public async Task SongRepository_GetLocalImageReferencesAsync_ReturnsOnlyLocalReferences()
    {
        var artist = MakeArtist("ABBA", null);
        _db.Artists.Add(artist);
        await _db.SaveChangesAsync();
        _db.Songs.AddRange(
            MakeSong(artist.Id, "Local", SongLocal),
            MakeSong(artist.Id, "Remote", "https://e.test/c.jpg"),
            MakeSong(artist.Id, "None", null));
        await _db.SaveChangesAsync();

        var result = await new SongRepository(_db).GetLocalImageReferencesAsync();

        Assert.Equal([SongLocal], result);
    }

    [Fact]
    // [AC] REQ-PIC-11: the songs read does not return artist references.
    public async Task SongRepository_GetLocalImageReferencesAsync_IgnoresArtistReferences()
    {
        _db.Artists.Add(MakeArtist("Has Local Picture", ArtistLocal));
        await _db.SaveChangesAsync();

        Assert.Empty(await new SongRepository(_db).GetLocalImageReferencesAsync());
    }

    [Fact]
    // [AC] REQ-PIC-11: only local references of artists are returned; https URLs and nulls are excluded.
    public async Task ArtistRepository_GetLocalImageReferencesAsync_ReturnsOnlyLocalReferences()
    {
        _db.Artists.AddRange(
            MakeArtist("Local", ArtistLocal),
            MakeArtist("Remote", "https://e.test/a.jpg"),
            MakeArtist("None", null));
        await _db.SaveChangesAsync();

        var result = await new ArtistRepository(_db).GetLocalImageReferencesAsync();

        Assert.Equal([ArtistLocal], result);
    }

    [Fact]
    // [AC] REQ-PIC-11: with no local pictures the list is empty.
    public async Task ArtistRepository_GetLocalImageReferencesAsync_EmptyDatabase_ReturnsEmpty()
        => Assert.Empty(await new ArtistRepository(_db).GetLocalImageReferencesAsync());
}
