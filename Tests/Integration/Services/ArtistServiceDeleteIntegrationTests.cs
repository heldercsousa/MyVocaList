using Microsoft.EntityFrameworkCore;
using MyVocaList.Infra;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Integration.Services;

/// <summary>
/// CATALOG-PROP (REQ-CATPROP-10): <c>ArtistService.DeleteArtistsAsync</c> refuses only when a selected artist
/// owns songs; catalog (cover) rows never block and go with the artist. Real SQLite temp file, real
/// repositories, real unit of work (<see cref="UnitOfWorkTestHost"/>); assertions re-read with a FRESH context.
/// </summary>
public class ArtistServiceDeleteIntegrationTests
{
    private static AppDbContext FreshContext(UnitOfWorkTestHost host)
        => host.Resolve<IDbContextFactory<AppDbContext>>().CreateDbContext();

    private static async Task<int> SeedArtistAsync(UnitOfWorkTestHost host, string name)
    {
        var (ok, message, artist) = await host.Resolve<IArtistService>().CreateArtistAsync(name);
        Assert.True(ok, message);
        return artist!.Id;
    }

    // [AC] REQ-CATPROP-10: an artist with only cover rows is deleted with those rows; the songs are kept.
    [Fact]
    public async Task Delete_CoverOnlyArtist_RowsGoneSongsKept()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var authorId = await SeedArtistAsync(host, "Author");
        var coverId = await SeedArtistAsync(host, "Cover Singer");
        var (ok, message, song) = await host.Resolve<ISongService>().CreateSongAsync(authorId, "Original");
        Assert.True(ok, message);
        await using (var seed = FreshContext(host))
        {
            seed.Catalog.Add(new Catalog { ArtistId = coverId, SongId = song!.Id });
            await seed.SaveChangesAsync();
        }

        var (success, _) = await host.Resolve<IArtistService>().DeleteArtistsAsync([coverId]);

        Assert.True(success);
        await using var db = FreshContext(host);
        Assert.False(await db.Artists.AnyAsync(a => a.Id == coverId));
        Assert.Empty(await db.Catalog.Where(c => c.ArtistId == coverId).ToListAsync());
        Assert.True(await db.Songs.AnyAsync(s => s.Id == song!.Id));
        Assert.True(await db.Catalog.AnyAsync(c => c.ArtistId == authorId && c.SongId == song!.Id));
    }
}
