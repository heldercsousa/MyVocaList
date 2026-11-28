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

    private const string Mbid = "76df3287-6cda-33eb-8e9a-044b5e15ffdd";
    private const string MbidCover = "https://coverartarchive.org/release/76df3287-6cda-33eb-8e9a-044b5e15ffdd/front-250";

    // [AC] REQ-MSRC-16: a song with no picture but a release id shows the album cover before the artist picture (plain list)
    [Fact]
    public async Task GetPagedAsync_SongWithReleaseMbid_RowImageIsAlbumCoverBeforeArtist()
    {
        var artist = await SeedArtistAsync("Artist With Picture", ArtistPictureUrl);
        var song = await SeedSongAsync(artist.Id, "Song With Release", null);
        song.ReleaseMbid = Mbid;
        await _db.SaveChangesAsync();

        var (items, _) = await _songs.GetPagedAsync(1, 20, null, CancellationToken.None);

        Assert.Equal(MbidCover, items.Single().ImageUrl);
    }

    // [AC] REQ-MSRC-16: the song's own picture still wins over the album cover (plain list)
    [Fact]
    public async Task GetPagedAsync_SongWithImageAndReleaseMbid_RowImageIsSongImage()
    {
        var artist = await SeedArtistAsync("Artist With Picture", ArtistPictureUrl);
        var song = await SeedSongAsync(artist.Id, "Song With Both", CoverUrl);
        song.ReleaseMbid = Mbid;
        await _db.SaveChangesAsync();

        var (items, _) = await _songs.GetPagedAsync(1, 20, null, CancellationToken.None);

        Assert.Equal(CoverUrl, items.Single().ImageUrl);
    }

    // [AC] REQ-MSRC-16: the album cover applies on the search (full-text) list path too
    [Fact]
    public async Task GetPagedAsync_SearchPath_SongWithReleaseMbid_RowImageIsAlbumCover()
    {
        var artist = await SeedArtistAsync("Artist With Picture", ArtistPictureUrl);
        var song = await SeedSongAsync(artist.Id, "Wonderwall Release", null);
        song.ReleaseMbid = Mbid;
        await _db.SaveChangesAsync();

        var (items, _) = await _songs.GetPagedAsync(1, 20, "Wonderwall", CancellationToken.None);

        Assert.Equal(MbidCover, items.Single().ImageUrl);
    }

    // [AC] REQ-MSRC-16: the album cover applies on the short-query (prefix) list path too
    [Fact]
    public async Task GetPagedAsync_PrefixPath_SongWithReleaseMbid_RowImageIsAlbumCover()
    {
        var artist = await SeedArtistAsync("Artist With Picture", ArtistPictureUrl);
        var song = await SeedSongAsync(artist.Id, "Zz Release", null);
        song.ReleaseMbid = Mbid;
        await _db.SaveChangesAsync();

        var (items, _) = await _songs.GetPagedAsync(1, 20, "Zz", CancellationToken.None);

        Assert.Equal(MbidCover, items.Single().ImageUrl);
    }

    // [AC] REQ-MSRC-16: the album cover applies on the catalog list too
    [Fact]
    public async Task GetPagedByArtistAsync_SongWithReleaseMbid_RowImageIsAlbumCover()
    {
        var original = await SeedArtistAsync("Original Artist", ArtistPictureUrl);
        var performer = await SeedArtistAsync("Performer", null);
        var song = await SeedSongAsync(original.Id, "Catalog Release", null);
        song.ReleaseMbid = Mbid;
        _db.Catalog.Add(new Catalog { ArtistId = performer.Id, SongId = song.Id });
        await _db.SaveChangesAsync();

        var (items, _) = await _catalog.GetPagedByArtistAsync(
            performer.Id, 1, 20, null, CancellationToken.None);

        Assert.Equal(MbidCover, items.Single().ImageUrl);
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
