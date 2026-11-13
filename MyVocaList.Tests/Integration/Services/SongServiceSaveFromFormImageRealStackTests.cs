using Microsoft.EntityFrameworkCore;
using MyVocaList.Domain.Resolution;
using MyVocaList.Infra;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Integration.Services;

/// <summary>
/// T10a: the image rules of <c>SongService.SaveFromFormAsync</c> proven on the production composition
/// (<see cref="UnitOfWorkTestHost"/>: real SQLite temp file, real repositories, real unit of work,
/// real artist resolution). Every assertion re-reads from the database with a FRESH context, never
/// from the tracked entities, so a lost EF change (e.g. the fill of an existing artist's image) fails.
/// </summary>
public class SongServiceSaveFromFormImageRealStackTests
{
    private const string SongImage = "https://cdn-images.dzcdn.net/images/cover/abc/250x250-000000-80-0-0.jpg";
    private const string ArtistImage = "https://cdn-images.dzcdn.net/images/artist/def/250x250-000000-80-0-0.jpg";
    private const string OtherSongImage = "https://cdn-images.dzcdn.net/images/cover/zzz/250x250-000000-80-0-0.jpg";
    private const string OtherArtistImage = "https://cdn-images.dzcdn.net/images/artist/yyy/250x250-000000-80-0-0.jpg";

    private static SongFormSaveRequest Request(
        int? songId, string title, string version, int? artistId, string? artistText,
        string? imageUrl, string? artistImageUrl)
        => new(songId, title, version, FeaturedArtists: null, Lyrics: null,
            artistId, artistText, ExternalProvider: null, ExternalId: null,
            PendingUrls: [], new ArtistDecision.None(), SongDecision.None,
            imageUrl, artistImageUrl);

    private static AppDbContext FreshContext(UnitOfWorkTestHost host)
        => host.Resolve<IDbContextFactory<AppDbContext>>().CreateDbContext();

    private static async Task<Artist> SeedArtistAsync(UnitOfWorkTestHost host, string name, string? imageUrl)
    {
        var (ok, message, artist) = await host.Resolve<IArtistService>().CreateArtistAsync(name);
        Assert.True(ok, message);
        if (imageUrl is not null)
        {
            await using var db = FreshContext(host);
            await db.Artists.Where(a => a.Id == artist!.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.ImageUrl, imageUrl));
        }
        return artist!;
    }

    // [AC] REQ-ART-15: a new song saved with a pending image stores it on the song row.
    // [AC] REQ-ART-16 (new artist half) is asserted in the same save: the created artist carries the artist image.
    [Fact]
    public async Task SaveFromFormAsync_AddModeNewArtist_PersistsSongAndArtistImages()
    {
        // [AC] REQ-ART-15: the song row stores the normalised pending image.
        await using var host = UnitOfWorkTestHost.Create();

        var outcome = await host.Resolve<ISongService>().SaveFromFormAsync(
            Request(null, "Bohemian Rhapsody", "", null, "Queen Image Test", SongImage, ArtistImage));

        Assert.Equal(SongFormSaveKind.Saved, outcome.Kind);
        await using var db = FreshContext(host);
        var song = await db.Songs.SingleAsync(s => s.Title == "Bohemian Rhapsody");
        var artist = await db.Artists.SingleAsync(a => a.Name == "Queen Image Test");
        Assert.Equal(SongImage, song.ImageUrl);
        Assert.Equal(artist.Id, song.ArtistId);
        Assert.Equal(ArtistImage, artist.ImageUrl);
    }

    // [AC] REQ-ART-16: an existing artist with no image, picked by id, is filled with the pending artist image.
    [Fact]
    public async Task SaveFromFormAsync_ExistingArtistByIdWithoutImage_FillsArtistImage()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var artist = await SeedArtistAsync(host, "Fill By Id Artist", imageUrl: null);

        var outcome = await host.Resolve<ISongService>().SaveFromFormAsync(
            Request(null, "Fill By Id Song", "", artist.Id, "Fill By Id Artist", SongImage, ArtistImage));

        Assert.Equal(SongFormSaveKind.Saved, outcome.Kind);
        await using var db = FreshContext(host);
        Assert.Equal(ArtistImage, (await db.Artists.SingleAsync(a => a.Id == artist.Id)).ImageUrl);
        Assert.Equal(SongImage, (await db.Songs.SingleAsync(s => s.Title == "Fill By Id Song")).ImageUrl);
    }

    // [AC] REQ-ART-16: an existing artist with no image, matched by exact typed name, is filled too.
    [Fact]
    public async Task SaveFromFormAsync_ExistingArtistByExactNameWithoutImage_FillsArtistImage()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var artist = await SeedArtistAsync(host, "Fill By Name Artist", imageUrl: null);

        var outcome = await host.Resolve<ISongService>().SaveFromFormAsync(
            Request(null, "Fill By Name Song", "", null, "Fill By Name Artist", SongImage, ArtistImage));

        Assert.Equal(SongFormSaveKind.Saved, outcome.Kind);
        await using var db = FreshContext(host);
        Assert.Equal(1, await db.Artists.CountAsync());
        Assert.Equal(ArtistImage, (await db.Artists.SingleAsync(a => a.Id == artist.Id)).ImageUrl);
        Assert.Equal(artist.Id, (await db.Songs.SingleAsync(s => s.Title == "Fill By Name Song")).ArtistId);
    }

    // [AC] REQ-ART-16: an existing artist that already has an image is never overwritten.
    [Fact]
    public async Task SaveFromFormAsync_ExistingArtistWithImage_KeepsStoredArtistImage()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var artist = await SeedArtistAsync(host, "Keep Image Artist", imageUrl: OtherArtistImage);

        var outcome = await host.Resolve<ISongService>().SaveFromFormAsync(
            Request(null, "Keep Image Song", "", artist.Id, "Keep Image Artist", SongImage, ArtistImage));

        Assert.Equal(SongFormSaveKind.Saved, outcome.Kind);
        await using var db = FreshContext(host);
        Assert.Equal(OtherArtistImage, (await db.Artists.SingleAsync(a => a.Id == artist.Id)).ImageUrl);
    }

    // [AC] REQ-ART-18: a refused Save (exact duplicate song, no decision) changes no image on the artist.
    [Fact]
    public async Task SaveFromFormAsync_RefusedDuplicateSave_LeavesArtistImageNull()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var artist = await SeedArtistAsync(host, "Refused Save Artist", imageUrl: null);
        await using (var seed = FreshContext(host))
        {
            seed.Songs.Add(new Song
            {
                ArtistId = artist.Id, Title = "Refused Save Song", Version = "",
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
            });
            await seed.SaveChangesAsync();
        }

        var outcome = await host.Resolve<ISongService>().SaveFromFormAsync(
            Request(null, "Refused Save Song", "", artist.Id, "Refused Save Artist", SongImage, ArtistImage));

        Assert.NotEqual(SongFormSaveKind.Saved, outcome.Kind);
        await using var db = FreshContext(host);
        Assert.Null((await db.Artists.SingleAsync(a => a.Id == artist.Id)).ImageUrl);
        Assert.Equal(1, await db.Songs.CountAsync());
    }

    // [AC] REQ-ART-17: edit mode never touches the stored song or artist image, yet the edit is persisted.
    [Fact]
    public async Task SaveFromFormAsync_EditMode_KeepsStoredImagesAndPersistsEdit()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var artist = await SeedArtistAsync(host, "Edit Mode Artist", imageUrl: OtherArtistImage);
        int songId;
        await using (var seed = FreshContext(host))
        {
            var song = new Song
            {
                ArtistId = artist.Id, Title = "Edit Mode Song", Version = "",
                ImageUrl = OtherSongImage, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
            };
            seed.Songs.Add(song);
            await seed.SaveChangesAsync();
            songId = song.Id;
        }

        var outcome = await host.Resolve<ISongService>().SaveFromFormAsync(
            Request(songId, "Edit Mode Song", "Live", artist.Id, "Edit Mode Artist", SongImage, ArtistImage));

        Assert.Equal(SongFormSaveKind.Saved, outcome.Kind);
        await using var db = FreshContext(host);
        var stored = await db.Songs.SingleAsync(s => s.Id == songId);
        Assert.Equal("Live", stored.Version);
        Assert.Equal(OtherSongImage, stored.ImageUrl);
        Assert.Equal(OtherArtistImage, (await db.Artists.SingleAsync(a => a.Id == artist.Id)).ImageUrl);
    }
}
