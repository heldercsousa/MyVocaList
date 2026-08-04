using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Integration.UnitOfWork;

/// <summary>
/// Phase 0 RED tests reproducing BUG-068 (REQ-UOW-03/04): a shared session-lifetime
/// <see cref="MyVocaList.Infra.AppDbContext"/> throws "another instance with the same key value"
/// on a create-then-read-then-update sequence through the service layer.
/// </summary>
public class Bug068RegressionTests
{
    // [AC] REQ-UOW-03: create -> read -> update through the normal write path must not throw
    // "already being tracked", and the update must persist.
    [Fact]
    public async Task Song_CreateThenReadThenUpdate_DoesNotThrowTrackingConflict()
    {
        await using var host = UnitOfWorkTestHost.CreateLegacy();
        var artists = host.Resolve<IArtistService>();
        var songs = host.Resolve<ISongService>();

        var (artistOk, _, artist) = await artists.CreateArtistAsync("Tracking Artist");
        Assert.True(artistOk);

        var (createOk, _, song) = await songs.CreateSongAsync(artist!.Id, "Original Title");
        Assert.True(createOk);

        var (updateOk, message) = await songs.UpdateSongAsync(
            song!.Id, "Updated Title", featuredArtists: null, lyrics: null, hasManualEdits: false);

        Assert.True(updateOk, message);
        var reread = await songs.GetSongByIdAsync(song.Id);
        Assert.Equal("Updated Title", reread!.Title);
    }
}
