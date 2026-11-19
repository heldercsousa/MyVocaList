using Microsoft.EntityFrameworkCore;
using MyVocaList.Domain.Resolution;
using MyVocaList.Infra;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Integration.Services;

/// <summary>
/// The picture rules of <c>SongService.SaveFromFormAsync</c> (REQ-PIC-21, 22, 32, 40) and the two picture
/// members of <c>ArtistService</c> (REQ-PIC-36) proven on the production composition
/// (<see cref="UnitOfWorkTestHost"/>: real SQLite temp file, real repositories, real unit of work, real
/// artist resolution). Every assertion re-reads with a FRESH context, so a lost (untracked) EF change fails.
/// </summary>
public class SongServiceSaveFromFormPictureRealStackTests
{
    private const string LocalSongImage = "local:songs/0123456789abcdef0123456789abcdef";
    private const string LocalArtistImage = "local:artists/0123456789abcdef0123456789abcdef";
    private const string StoredSongImage = "https://cdn-images.dzcdn.net/images/cover/zzz/250x250-000000-80-0-0.jpg";
    private const string ArtistImage = "https://cdn-images.dzcdn.net/images/artist/def/250x250-000000-80-0-0.jpg";
    private const string OtherArtistImage = "https://cdn-images.dzcdn.net/images/artist/yyy/250x250-000000-80-0-0.jpg";

    private static SongFormSaveRequest EditRequest(
        int songId, string title, int? artistId, string? artistText,
        string? imageUrl = null, string? artistImageUrl = null, bool imageChanged = false)
        => new(songId, title, string.Empty, FeaturedArtists: null, Lyrics: null,
            artistId, artistText, ExternalProvider: null, ExternalId: null,
            PendingUrls: [], new ArtistDecision.None(), SongDecision.None,
            imageUrl, artistImageUrl, imageChanged);

    private static AppDbContext FreshContext(UnitOfWorkTestHost host)
        => host.Resolve<IDbContextFactory<AppDbContext>>().CreateDbContext();

    private static async Task<Artist> SeedArtistAsync(UnitOfWorkTestHost host, string name, string? imageUrl = null)
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

    private static async Task<int> SeedSongAsync(UnitOfWorkTestHost host, int artistId, string title, string? imageUrl = null)
    {
        await using var seed = FreshContext(host);
        var song = new Song
        {
            ArtistId = artistId, Title = title, Version = string.Empty, ImageUrl = imageUrl,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        seed.Songs.Add(song);
        await seed.SaveChangesAsync();
        return song.Id;
    }

    private static async Task<Song> ReadSongAsync(UnitOfWorkTestHost host, int songId)
    {
        await using var db = FreshContext(host);
        return await db.Songs.SingleAsync(s => s.Id == songId);
    }

    private static async Task<string?> ReadArtistImageAsync(UnitOfWorkTestHost host, int artistId)
    {
        await using var db = FreshContext(host);
        return (await db.Artists.SingleAsync(a => a.Id == artistId)).ImageUrl;
    }

    // ── REQ-PIC-21: edit-mode picture ─────────────────────────────────────────

    [Fact]
    public async Task SaveFromFormAsync_EditImageChanged_PersistsTheLocalReference()
    {
        // [AC] REQ-PIC-21: in edit mode an uploaded picture replaces the stored reference
        await using var host = UnitOfWorkTestHost.Create();
        var artist = await SeedArtistAsync(host, "Edit Picture Artist");
        var songId = await SeedSongAsync(host, artist.Id, "Edit Picture Song", StoredSongImage);

        var outcome = await host.Resolve<ISongService>().SaveFromFormAsync(
            EditRequest(songId, "Edit Picture Song", artist.Id, null, LocalSongImage, imageChanged: true));

        Assert.Equal(SongFormSaveKind.Saved, outcome.Kind);
        Assert.Equal(LocalSongImage, (await ReadSongAsync(host, songId)).ImageUrl);
    }

    [Fact]
    public async Task SaveFromFormAsync_EditImageRemoved_PersistsNull()
    {
        // [AC] REQ-PIC-21: in edit mode a removed picture leaves the stored reference null
        await using var host = UnitOfWorkTestHost.Create();
        var artist = await SeedArtistAsync(host, "Remove Picture Artist");
        var songId = await SeedSongAsync(host, artist.Id, "Remove Picture Song", StoredSongImage);

        var outcome = await host.Resolve<ISongService>().SaveFromFormAsync(
            EditRequest(songId, "Remove Picture Song", artist.Id, null, imageUrl: null, imageChanged: true));

        Assert.Equal(SongFormSaveKind.Saved, outcome.Kind);
        Assert.Null((await ReadSongAsync(host, songId)).ImageUrl);
    }

    [Fact]
    public async Task SaveFromFormAsync_EditImageNotChanged_KeepsTheStoredReference()
    {
        // [AC] REQ-PIC-21: in edit mode without an upload or a removal the stored reference is unchanged
        await using var host = UnitOfWorkTestHost.Create();
        var artist = await SeedArtistAsync(host, "Keep Picture Artist");
        var songId = await SeedSongAsync(host, artist.Id, "Keep Picture Song", LocalSongImage);

        var outcome = await host.Resolve<ISongService>().SaveFromFormAsync(
            EditRequest(songId, "Keep Picture Song (edited)", artist.Id, null, imageUrl: null, imageChanged: false));

        Assert.Equal(SongFormSaveKind.Saved, outcome.Kind);
        var stored = await ReadSongAsync(host, songId);
        Assert.Equal("Keep Picture Song (edited)", stored.Title);
        Assert.Equal(LocalSongImage, stored.ImageUrl);
    }

    // ── REQ-PIC-32 / REQ-PIC-22: no duplicate by changing the artist ──────────

    [Fact]
    public async Task SaveFromFormAsync_EditArtistChangedByIdIntoDuplicate_FailsAndChangesNothing()
    {
        // [AC] REQ-PIC-32: moving a song onto an artist that already has the same title and version is refused
        await using var host = UnitOfWorkTestHost.Create();
        var target = await SeedArtistAsync(host, "Collision Target Artist");
        var source = await SeedArtistAsync(host, "Collision Source Artist");
        await SeedSongAsync(host, target.Id, "Collision Song");
        var songId = await SeedSongAsync(host, source.Id, "Collision Song", StoredSongImage);

        var outcome = await host.Resolve<ISongService>().SaveFromFormAsync(
            EditRequest(songId, "Collision Song", target.Id, null, LocalSongImage, ArtistImage, imageChanged: true));

        Assert.Equal(SongFormSaveKind.Failed, outcome.Kind);
        Assert.Equal(SaveField.Artist, outcome.FailedField);
        var stored = await ReadSongAsync(host, songId);
        Assert.Equal(source.Id, stored.ArtistId);
        Assert.Equal(StoredSongImage, stored.ImageUrl);
        Assert.Null(await ReadArtistImageAsync(host, target.Id));
    }

    [Fact]
    public async Task SaveFromFormAsync_EditArtistChangedByTypedNameIntoCollatedDuplicate_FailsAndChangesNothing()
    {
        // [AC] REQ-PIC-32: a typed artist name resolved (by the DB collation) to an artist that has the song is refused
        await using var host = UnitOfWorkTestHost.Create();
        var target = await SeedArtistAsync(host, "Beyoncé Typed Target");
        var source = await SeedArtistAsync(host, "Typed Source Artist");
        await SeedSongAsync(host, target.Id, "Halo");
        var songId = await SeedSongAsync(host, source.Id, "HALO");

        var outcome = await host.Resolve<ISongService>().SaveFromFormAsync(
            EditRequest(songId, "HALO", artistId: null, artistText: "beyonce typed target"));

        Assert.Equal(SongFormSaveKind.Failed, outcome.Kind);
        Assert.Equal(SaveField.Artist, outcome.FailedField);
        Assert.Equal(source.Id, (await ReadSongAsync(host, songId)).ArtistId);
        await using var db = FreshContext(host);
        Assert.Equal(2, await db.Artists.CountAsync());
    }

    [Fact]
    public async Task SaveFromFormAsync_EditArtistChangedWithoutDuplicate_RelinksTheSongInPlace()
    {
        // [AC] REQ-PIC-32: an artist change that gives no duplicate is saved on the same song row
        await using var host = UnitOfWorkTestHost.Create();
        var target = await SeedArtistAsync(host, "Relink Target Artist");
        var source = await SeedArtistAsync(host, "Relink Source Artist");
        var songId = await SeedSongAsync(host, source.Id, "Relink Song");

        var outcome = await host.Resolve<ISongService>().SaveFromFormAsync(
            EditRequest(songId, "Relink Song", target.Id, null));

        Assert.Equal(SongFormSaveKind.Saved, outcome.Kind);
        Assert.Equal(songId, outcome.SavedSongId);
        Assert.Equal(target.Id, (await ReadSongAsync(host, songId)).ArtistId);
    }

    // ── REQ-PIC-40 (REQ-ART-16 in edit mode): the artist picture ──────────────

    [Fact]
    public async Task SaveFromFormAsync_EditCreatesArtistFromTypedName_PersistsItsPicture()
    {
        // [AC] REQ-PIC-40: an artist created by an edit-mode Save stores the pending artist picture (REQ-ART-16)
        await using var host = UnitOfWorkTestHost.Create();
        var source = await SeedArtistAsync(host, "Create Source Artist");
        var songId = await SeedSongAsync(host, source.Id, "Create Artist Song");

        var outcome = await host.Resolve<ISongService>().SaveFromFormAsync(
            EditRequest(songId, "Create Artist Song", null, "Brand New Edit Artist", artistImageUrl: ArtistImage));

        Assert.Equal(SongFormSaveKind.Saved, outcome.Kind);
        await using var db = FreshContext(host);
        var created = await db.Artists.SingleAsync(a => a.Name == "Brand New Edit Artist");
        Assert.Equal(ArtistImage, created.ImageUrl);
        Assert.Equal(created.Id, (await db.Songs.SingleAsync(s => s.Id == songId)).ArtistId);
    }

    [Fact]
    public async Task SaveFromFormAsync_EditChangesToExistingArtistWithoutPicture_PersistsTheFill()
    {
        // [AC] REQ-PIC-40: an existing artist with no picture reached by an edit-mode artist change is filled (REQ-ART-16)
        await using var host = UnitOfWorkTestHost.Create();
        var target = await SeedArtistAsync(host, "Fill Target Artist");
        var source = await SeedArtistAsync(host, "Fill Source Artist");
        var songId = await SeedSongAsync(host, source.Id, "Fill Edit Song");

        var outcome = await host.Resolve<ISongService>().SaveFromFormAsync(
            EditRequest(songId, "Fill Edit Song", target.Id, null, artistImageUrl: ArtistImage));

        Assert.Equal(SongFormSaveKind.Saved, outcome.Kind);
        Assert.Equal(ArtistImage, await ReadArtistImageAsync(host, target.Id));
        Assert.Equal(target.Id, (await ReadSongAsync(host, songId)).ArtistId);
    }

    [Fact]
    public async Task SaveFromFormAsync_EditChangesToExistingArtistWithPicture_NeverOverwrites()
    {
        // [AC] REQ-PIC-40: an existing artist that already has a picture keeps it in edit mode (REQ-ART-16)
        await using var host = UnitOfWorkTestHost.Create();
        var target = await SeedArtistAsync(host, "Keep Target Artist", OtherArtistImage);
        var source = await SeedArtistAsync(host, "Keep Source Artist");
        var songId = await SeedSongAsync(host, source.Id, "Keep Edit Song");

        var outcome = await host.Resolve<ISongService>().SaveFromFormAsync(
            EditRequest(songId, "Keep Edit Song", target.Id, null, artistImageUrl: ArtistImage));

        Assert.Equal(SongFormSaveKind.Saved, outcome.Kind);
        Assert.Equal(OtherArtistImage, await ReadArtistImageAsync(host, target.Id));
    }

    // ── REQ-PIC-36: ArtistService picture members ─────────────────────────────

    [Theory]
    [InlineData(LocalArtistImage, LocalArtistImage)]
    [InlineData(ArtistImage, ArtistImage)]
    [InlineData("local:artists/../../x", null)]
    [InlineData(null, null)]
    public async Task SetArtistImageAsync_PersistsTheNormalisedReference(string? reference, string? expected)
    {
        // [AC] REQ-PIC-36: Save stores the artist picture reference (normalised; null removes it) with a tracked write
        await using var host = UnitOfWorkTestHost.Create();
        var artist = await SeedArtistAsync(host, "Set Picture Artist", OtherArtistImage);

        var (success, message) = await host.Resolve<IArtistService>().SetArtistImageAsync(artist.Id, reference);

        Assert.True(success, message);
        Assert.Equal(expected, await ReadArtistImageAsync(host, artist.Id));
    }

    [Fact]
    public async Task SetArtistImageAsync_UnknownId_FailsAndWritesNothing()
    {
        // [AC] REQ-PIC-36: a failed Save stores nothing
        await using var host = UnitOfWorkTestHost.Create();
        var artist = await SeedArtistAsync(host, "Untouched Picture Artist", OtherArtistImage);

        var (success, _) = await host.Resolve<IArtistService>().SetArtistImageAsync(artist.Id + 100, LocalArtistImage);

        Assert.False(success);
        Assert.Equal(OtherArtistImage, await ReadArtistImageAsync(host, artist.Id));
    }

    [Fact]
    public async Task GetArtistByIdAsync_ReadsTheStoredPictureOrNull()
    {
        // [AC] REQ-PIC-36: edit mode reads the stored artist picture through the service; an unknown id is null
        await using var host = UnitOfWorkTestHost.Create();
        var artist = await SeedArtistAsync(host, "Read Picture Artist", LocalArtistImage);
        var service = host.Resolve<IArtistService>();

        var read = await service.GetArtistByIdAsync(artist.Id);

        Assert.NotNull(read);
        Assert.Equal(LocalArtistImage, read.ImageUrl);
        Assert.Null(await service.GetArtistByIdAsync(artist.Id + 100));
    }
}
