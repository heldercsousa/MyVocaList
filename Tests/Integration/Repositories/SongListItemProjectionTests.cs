using Microsoft.EntityFrameworkCore;
using MyVocaList.Domain.Entity;
using MyVocaList.Infra;
using MyVocaList.Infra.Repository;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Integration.Repositories;

/// <summary>
/// T2: list projections of SongRepository.GetPagedAsync and CatalogRepository.GetPagedByArtistAsync
/// carry the row image (song image, else artist image). Real SQLite, MigrateAsync.
/// </summary>
public class SongListItemProjectionTests : IAsyncLifetime
{
    private const string CoverUrl = "https://e.test/cover.jpg";
    private const string ArtistPictureUrl = "https://e.test/artist.jpg";

    private AppDbContext _db;
    private SongRepository _songs;
    private CatalogRepository _catalog;

    public async Task InitializeAsync()
    {
        _db = TestDbContextFactory.Create();
        await _db.Database.MigrateAsync();
        _songs = new SongRepository(_db);
        _catalog = new CatalogRepository(_db);
    }

    public async Task DisposeAsync()
    {
        await _db.Database.EnsureDeletedAsync();
        await _db.DisposeAsync();
    }

    // [AC] REQ-ART-19: a song with its own image shows that image
    [Fact]
    public async Task GetPagedAsync_SongWithImage_RowImageIsSongImage()
    {
        var artist = await SeedArtistAsync("Artist With Picture", ArtistPictureUrl);
        await SeedSongAsync(artist.Id, "Song With Cover", CoverUrl);

        var (items, _) = await _songs.GetPagedAsync(1, 20, null, CancellationToken.None);

        Assert.Equal(CoverUrl, items.Single().ImageUrl);
    }

    // [AC] REQ-ART-19: a song without an image falls back to its artist's image
    [Fact]
    public async Task GetPagedAsync_SongWithoutImageArtistWithImage_RowImageIsArtistImage()
    {
        var artist = await SeedArtistAsync("Artist With Picture", ArtistPictureUrl);
        await SeedSongAsync(artist.Id, "Song Without Cover", null);

        var (items, _) = await _songs.GetPagedAsync(1, 20, null, CancellationToken.None);

        Assert.Equal(ArtistPictureUrl, items.Single().ImageUrl);
    }

    // [AC] REQ-ART-19: neither song nor artist has an image, the row image is null (placeholder)
    [Fact]
    public async Task GetPagedAsync_NeitherHasImage_RowImageIsNull()
    {
        var artist = await SeedArtistAsync("Artist Without Picture", null);
        await SeedSongAsync(artist.Id, "Song Without Cover", null);

        var (items, _) = await _songs.GetPagedAsync(1, 20, null, CancellationToken.None);

        Assert.Null(items.Single().ImageUrl);
    }

    // [AC] REQ-ART-19: catalog query, song with its own image shows that image
    [Fact]
    public async Task GetPagedByArtistAsync_SongWithImage_CarriesImage()
    {
        var original = await SeedArtistAsync("Original Artist", null);
        var performer = await SeedArtistAsync("Performer", ArtistPictureUrl);
        var song = await SeedSongAsync(original.Id, "Catalog Song", CoverUrl);
        _db.Catalog.Add(new Catalog { ArtistId = performer.Id, SongId = song.Id });
        await _db.SaveChangesAsync();

        var (items, _) = await _catalog.GetPagedByArtistAsync(
            performer.Id, 1, 20, null, CancellationToken.None);

        Assert.Equal(CoverUrl, items.Single().ImageUrl);
    }

    // [AC] REQ-ART-19: catalog query, song without image falls back to the artist image of the song's original artist
    [Fact]
    public async Task GetPagedByArtistAsync_SongWithoutImage_FallsBackToOriginalArtistImage()
    {
        var original = await SeedArtistAsync("Original Artist", ArtistPictureUrl);
        var performer = await SeedArtistAsync("Performer", null);
        var song = await SeedSongAsync(original.Id, "Catalog Song", null);
        _db.Catalog.Add(new Catalog { ArtistId = performer.Id, SongId = song.Id });
        await _db.SaveChangesAsync();

        var (items, _) = await _catalog.GetPagedByArtistAsync(
            performer.Id, 1, 20, null, CancellationToken.None);

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

    private async Task<Song> SeedSongAsync(int artistId, string title, string? imageUrl)
    {
        var song = new Song
        {
            ArtistId = artistId,
            Title = title,
            ImageUrl = imageUrl,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Songs.Add(song);
        await _db.SaveChangesAsync();
        return song;
    }
}
