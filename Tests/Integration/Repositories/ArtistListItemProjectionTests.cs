using Microsoft.EntityFrameworkCore;
using MyVocaList.Domain.Entity;
using MyVocaList.Infra;
using MyVocaList.Infra.Repository;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Integration.Repositories;

/// <summary>
/// T2: ArtistRepository projections carry the artist image and the real catalog count
/// (SearchByNameAsync used to project a constant 0). Real SQLite temp file, MigrateAsync.
/// </summary>
public class ArtistListItemProjectionTests : IAsyncLifetime
{
    private const string ArtistPictureUrl = "https://e.test/ramones.jpg";

    private AppDbContext _db;
    private ArtistRepository _repo;

    public async Task InitializeAsync()
    {
        _db = TestDbContextFactory.Create();
        await _db.Database.MigrateAsync();
        _repo = new ArtistRepository(_db);
    }

    public async Task DisposeAsync()
    {
        await _db.Database.EnsureDeletedAsync();
        await _db.DisposeAsync();
    }

    // [AC] REQ-ART-10: artist with 3 catalog entries returns CatalogCount 3 and its image
    [Fact]
    public async Task SearchByNameAsync_ArtistWithThreeCatalogEntries_ReturnsRealCountAndImage()
    {
        var ramones = await SeedArtistAsync("Ramones", ArtistPictureUrl);
        var original = await SeedArtistAsync("Original Writer", null);
        for (var i = 1; i <= 3; i++)
        {
            var song = await SeedSongAsync(original.Id, $"Song {i}");
            _db.Catalog.Add(new Catalog { ArtistId = ramones.Id, SongId = song.Id });
        }
        await _db.SaveChangesAsync();

        var results = await _repo.SearchByNameAsync("Ramo", 10, CancellationToken.None);

        var item = results.Single();
        Assert.Equal(3, item.CatalogCount);
        Assert.Equal(ArtistPictureUrl, item.ImageUrl);
    }

    // [AC] REQ-ART-10: artist without catalog entries and without image returns count 0 and null image
    [Fact]
    public async Task SearchByNameAsync_NoCatalogNoImage_ReturnsZeroAndNull()
    {
        await SeedArtistAsync("Ramones", null);

        var results = await _repo.SearchByNameAsync("Ramo", 10, CancellationToken.None);

        var item = results.Single();
        Assert.Equal(0, item.CatalogCount);
        Assert.Null(item.ImageUrl);
    }

    // [AC] REQ-ART-10: the artist list page carries the artist image (same read model)
    [Fact]
    public async Task GetPagedAsync_CarriesArtistImage()
    {
        await SeedArtistAsync("Ramones", ArtistPictureUrl);

        var (items, _) = await _repo.GetPagedAsync(1, 20, "", ArtistRoleFilter.All, CancellationToken.None);

        Assert.Equal(ArtistPictureUrl, items.Single().ImageUrl);
    }

    private async Task<Artist> SeedArtistAsync(string name, string? imageUrl)
    {
        var artist = new Artist
        {
            Name = name,
            ImageUrl = imageUrl,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Artists.Add(artist);
        await _db.SaveChangesAsync();
        return artist;
    }

    private async Task<Song> SeedSongAsync(int artistId, string title)
    {
        var song = new Song
        {
            ArtistId = artistId,
            Title = title,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Songs.Add(song);
        await _db.SaveChangesAsync();
        return song;
    }
}
