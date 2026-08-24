using Microsoft.Extensions.DependencyInjection;
using MyVocaList.Domain.Entity;
using MyVocaList.Domain.RepositoryInterface;
using MyVocaList.Infra.Repository;
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
        await using var host = UnitOfWorkTestHost.Create();
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

    // [AC] REQ-UOW-04: create -> read -> update through the normal write path must not throw
    // "already being tracked", and the update must persist.
    // Characterization, not regression: this family never reproduced BUG-068 — ArtistRepository.GetByIdAsync
    // (ArtistRepository.cs:79-80) explicitly calls .AsTracking(), so EF identity resolution returns the
    // already-tracked instance instead of a fresh detached one. Locks the behavior in through the
    // unit-of-work refactor.
    [Fact]
    public async Task Artist_CreateThenReadThenUpdate_DoesNotThrowTrackingConflict()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var artists = host.Resolve<IArtistService>();

        var (createOk, _, artist) = await artists.CreateArtistAsync("Tracking Artist");
        Assert.True(createOk);

        var (updateOk, message) = await artists.UpdateArtistAsync(artist!.Id, "Updated Artist Name");

        Assert.True(updateOk, message);
        var (items, _) = await artists.GetPagedArtistsForListAsync(1, 20, "Updated Artist Name");
        Assert.Contains(items, i => i.Id == artist.Id && i.Name == "Updated Artist Name");
    }

    // [AC] REQ-UOW-04: create -> read -> update through the normal write path must not throw
    // "already being tracked", and the update must persist.
    // Characterization, not regression: this family never reproduced BUG-068 — PersonRepository inherits
    // GetByIdAsync from BaseRepository<T> (BaseRepository.cs:24-29), which uses _dbSet.FindAsync(id) — also
    // identity-resolving, so EF returns the already-tracked instance instead of a fresh detached one.
    // Locks the behavior in through the unit-of-work refactor.
    [Fact]
    public async Task Person_CreateThenReadThenUpdate_DoesNotThrowTrackingConflict()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var persons = host.Resolve<IPersonService>();

        var (createOk, _, person) = await persons.CreatePersonAsync("Tracking Person");
        Assert.True(createOk);

        var (updateOk, message) = await persons.UpdatePersonAsync(person!.Id, "Updated Person Name");

        Assert.True(updateOk, message);
        var reread = await persons.GetPersonByIdAsync(person.Id);
        Assert.Equal("Updated Person Name", reread!.FullName);
    }

    // [AC] REQ-UOW-04: create -> read -> update through the normal write path must not throw
    // "already being tracked", and the update must persist.
    // Characterization, not regression: this family never reproduced BUG-068 — VenueRepository inherits
    // GetByIdAsync from BaseRepository<T> (BaseRepository.cs:24-29), which uses _dbSet.FindAsync(id) — also
    // identity-resolving, so EF returns the already-tracked instance instead of a fresh detached one.
    // Locks the behavior in through the unit-of-work refactor.
    [Fact]
    public async Task Venue_CreateThenReadThenUpdate_DoesNotThrowTrackingConflict()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var venues = host.Resolve<IVenueService>();

        var (createOk, _) = await venues.CreateVenueAsync("Tracking Venue");
        Assert.True(createOk);

        var (items, _) = await venues.GetPagedVenuesForListAsync(1, 20, "Tracking Venue");
        var created = Assert.Single(items);

        var (updateOk, message) = await venues.UpdateVenueAsync(created.Id, "Updated Venue Name");

        Assert.True(updateOk, message);
        var (rereadItems, _) = await venues.GetPagedVenuesForListAsync(1, 20, "Updated Venue Name");
        Assert.Contains(rereadItems, i => i.Id == created.Id && i.Name == "Updated Venue Name");
    }

    // [AC] REQ-UOW-07: CreateSongWithUrlsAsync persists the Song and its SongKaraokeUrl rows in
    // ONE save. A forced failure on the URL rows must therefore leave NO Song row behind — the
    // song write and the URL writes are the same unit of work, not two sequential commits.
    // Fault-injection technique mirrors Task 0.4's ThrowOnAddSongRepository decorator
    // (NestedUnitOfWorkTests): decorate the real repository, throw from the one member under test,
    // forward every other member to the inner instance.
    [Fact]
    public async Task CreateSongWithUrls_UrlAddFaults_PersistsNoSongRow()
    {
        await using var host = UnitOfWorkTestHost.Create(services =>
            services.AddScoped<ISongKaraokeUrlRepository>(sp =>
                new ThrowOnAddUrlRepository(
                    ActivatorUtilities.CreateInstance<SongKaraokeUrlRepository>(sp))));

        var artists = host.Resolve<IArtistService>();
        var songs = host.Resolve<ISongService>();

        var (artistOk, artistMessage, artist) = await artists.CreateArtistAsync("Atomic URL Artist");
        Assert.True(artistOk, artistMessage);

        await Assert.ThrowsAsync<InvalidOperationException>(() => songs.CreateSongWithUrlsAsync(
            artist!.Id, "Atomic URL Song", string.Empty, null, null, null, null,
            ["https://www.youtube.com/watch?v=dQw4w9WgXcQ"]));

        Assert.Equal(0, host.Db.Songs.Count(s => s.Title == "Atomic URL Song"));
        Assert.Equal(0, host.Db.SongKaraokeUrls.Count());
    }

    /// <summary>Decorator over the real <see cref="SongKaraokeUrlRepository"/> that throws from
    /// <see cref="AddAsync"/> — the fault point for REQ-UOW-07 — and forwards every other member
    /// to the inner instance.</summary>
    private sealed class ThrowOnAddUrlRepository(ISongKaraokeUrlRepository inner) : ISongKaraokeUrlRepository
    {
        public Task<List<SongKaraokeUrl>> GetBySongIdAsync(int songId, CancellationToken ct = default)
            => inner.GetBySongIdAsync(songId, ct);

        public Task<SongKaraokeUrl?> GetSuggestedAsync(int songId, CancellationToken ct = default)
            => inner.GetSuggestedAsync(songId, ct);

        public Task<bool> ExistsAsync(int songId, string videoId, CancellationToken ct = default)
            => inner.ExistsAsync(songId, videoId, ct);

        public Task AddAsync(SongKaraokeUrl url, CancellationToken ct = default)
            => throw new InvalidOperationException("injected");

        public Task RemoveAsync(int songId, string videoId, CancellationToken ct = default)
            => inner.RemoveAsync(songId, videoId, ct);

        public Task IncrementPlayCountAsync(int songId, string videoId, CancellationToken ct = default)
            => inner.IncrementPlayCountAsync(songId, videoId, ct);
    }
}
