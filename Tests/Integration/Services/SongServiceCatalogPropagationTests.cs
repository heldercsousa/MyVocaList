using Microsoft.EntityFrameworkCore;
using MyVocaList.Contracts.DTOs.List;
using MyVocaList.Domain.Resolution;
using MyVocaList.Infra;
using MyVocaList.Infra.Repository;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Integration.Services;

/// <summary>
/// CATALOG-PROP (REQ-CATPROP-01 to 04): SongService create and update keep the author's derived catalog row
/// (<c>Catalog(Song.ArtistId, Song.Id)</c>) in the song's own unit-of-work transaction. Real SQLite temp
/// file, real repositories, real unit of work (<see cref="UnitOfWorkTestHost"/>); every assertion re-reads
/// with a FRESH context.
/// </summary>
public class SongServiceCatalogPropagationTests
{
    private static AppDbContext FreshContext(UnitOfWorkTestHost host)
        => host.Resolve<IDbContextFactory<AppDbContext>>().CreateDbContext();

    private static async Task<int> SeedArtistAsync(UnitOfWorkTestHost host, string name)
    {
        var (ok, message, artist) = await host.Resolve<IArtistService>().CreateArtistAsync(name);
        Assert.True(ok, message);
        return artist!.Id;
    }

    private static async Task<int> CreateSongAsync(UnitOfWorkTestHost host, int artistId, string title)
    {
        var (ok, message, song) = await host.Resolve<ISongService>().CreateSongAsync(artistId, title);
        Assert.True(ok, message);
        return song!.Id;
    }

    private static async Task AddCoverAsync(UnitOfWorkTestHost host, int artistId, int songId)
    {
        await using var db = FreshContext(host);
        db.Catalog.Add(new Catalog { ArtistId = artistId, SongId = songId });
        await db.SaveChangesAsync();
    }

    private static async Task<List<(int ArtistId, int SongId)>> RowsAsync(UnitOfWorkTestHost host)
    {
        await using var db = FreshContext(host);
        var rows = await db.Catalog.AsNoTracking().Select(c => new { c.ArtistId, c.SongId }).ToListAsync();
        return rows.Select(r => (r.ArtistId, r.SongId)).OrderBy(r => r).ToList();
    }

    private static async Task<int> StoredArtistIdAsync(UnitOfWorkTestHost host, int songId)
    {
        await using var db = FreshContext(host);
        return await db.Songs.AsNoTracking().Where(s => s.Id == songId).Select(s => s.ArtistId).SingleAsync();
    }

    private static SongFormSaveRequest AddRequest(int? artistId, string? artistText, string title)
        => new(null, title, Lyrics: null, artistId, artistText, ExternalProvider: null, ExternalId: null,
            PendingUrls: [], new ArtistDecision.None(), SongDecision.None);

    // [AC] REQ-CATPROP-01: CreateSongAsync stores exactly one catalog row (Song.ArtistId, Song.Id).
    [Fact]
    public async Task CreateSongAsync_WritesDerivedRow()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var artistId = await SeedArtistAsync(host, "Author");

        var songId = await CreateSongAsync(host, artistId, "Song One");

        Assert.Equal([(artistId, songId)], await RowsAsync(host));
    }

    // [AC] REQ-CATPROP-01: SaveFromFormAsync in add mode (existing artist) stores the derived row.
    [Fact]
    public async Task SaveFromForm_AddMode_WritesDerivedRow()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var artistId = await SeedArtistAsync(host, "Author");

        var outcome = await host.Resolve<ISongService>().SaveFromFormAsync(AddRequest(artistId, null, "Form Song"));

        Assert.Equal(SongFormSaveKind.Saved, outcome.Kind);
        Assert.Equal([(artistId, outcome.SavedSongId!.Value)], await RowsAsync(host));
    }

    // [AC] REQ-CATPROP-01: when the author is created in the same save, the row carries the real artist id.
    [Fact]
    public async Task SaveFromForm_AddMode_NewInlineArtist_WritesDerivedRowWithRealId()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var request = AddRequest(null, "Brand New Artist", "Inline Song") with { ArtistDecision = new ArtistDecision.CreateTyped() };

        var outcome = await host.Resolve<ISongService>().SaveFromFormAsync(request);

        Assert.Equal(SongFormSaveKind.Saved, outcome.Kind);
        var songId = outcome.SavedSongId!.Value;
        var newArtistId = await StoredArtistIdAsync(host, songId);
        Assert.NotEqual(0, newArtistId);
        Assert.Equal([(newArtistId, songId)], await RowsAsync(host));
    }

    // [AC] REQ-CATPROP-01: CreateSongWithUrlsAsync stores the derived row.
    [Fact]
    public async Task CreateSongWithUrls_WritesDerivedRow()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var artistId = await SeedArtistAsync(host, "Author");

        var (ok, message, song) = await host.Resolve<ISongService>().CreateSongWithUrlsAsync(
            artistId, null, "Url Song", null, null, null, []);

        Assert.True(ok, message);
        Assert.Equal([(artistId, song!.Id)], await RowsAsync(host));
    }

    // [AC] REQ-CATPROP-02: an update that keeps the author leaves the song's catalog rows unchanged.
    [Fact]
    public async Task Update_SameAuthor_CatalogUnchanged()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var a = await SeedArtistAsync(host, "A");
        var cover = await SeedArtistAsync(host, "Cover Artist");
        var songId = await CreateSongAsync(host, a, "Song");
        await AddCoverAsync(host, cover, songId);
        var before = await RowsAsync(host);

        var (ok, message) = await host.Resolve<ISongService>().UpdateSongAsync(
            songId, "Song Renamed", lyrics: "la la", artistId: a);

        Assert.True(ok, message);
        Assert.Equal(before, await RowsAsync(host));
        Assert.Contains((cover, songId), await RowsAsync(host));
    }

    // [AC] REQ-CATPROP-03: author A to B removes (A,S), writes (B,S) once, leaves A's other derived row alone.
    [Fact]
    public async Task Update_AuthorAtoB_MovesRow()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var a = await SeedArtistAsync(host, "A");
        var b = await SeedArtistAsync(host, "B");
        var s = await CreateSongAsync(host, a, "Moving Song");
        var other = await CreateSongAsync(host, a, "A Other Song");

        var (ok, message) = await host.Resolve<ISongService>().UpdateSongAsync(s, "Moving Song", null, artistId: b);

        Assert.True(ok, message);
        var rows = await RowsAsync(host);
        Assert.DoesNotContain((a, s), rows);
        Assert.Single(rows, r => r == (b, s));
        Assert.Contains((a, other), rows);
        Assert.Equal(2, rows.Count);
    }

    // [AC] REQ-CATPROP-03: B already covers the song, so that row becomes the derived row; no duplicate.
    [Fact]
    public async Task Update_AuthorAtoB_BAlreadyCovers_NoDuplicate()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var a = await SeedArtistAsync(host, "A");
        var b = await SeedArtistAsync(host, "B");
        var s = await CreateSongAsync(host, a, "Covered Song");
        await AddCoverAsync(host, b, s);

        var (ok, message) = await host.Resolve<ISongService>().UpdateSongAsync(s, "Covered Song", null, artistId: b);

        Assert.True(ok, message);
        Assert.Equal([(b, s)], await RowsAsync(host));
    }

    // [AC] REQ-CATPROP-03: the new author is created in the same save; the moved row uses the real id.
    [Fact]
    public async Task Update_AuthorToNewInlineArtist_MovesRowWithRealId()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var a = await SeedArtistAsync(host, "A");
        var s = await CreateSongAsync(host, a, "Song To Move");

        var (ok, message) = await host.Resolve<ISongService>().UpdateSongAsync(
            s, "Song To Move", null, newArtistName: "Fresh Inline Artist");

        Assert.True(ok, message);
        var newArtistId = await StoredArtistIdAsync(host, s);
        Assert.NotEqual(a, newArtistId);
        Assert.NotEqual(0, newArtistId);
        Assert.Equal([(newArtistId, s)], await RowsAsync(host));
    }

    // [AC] REQ-CATPROP-03: drift (old derived row missing) neither throws nor blocks writing the new row.
    [Fact]
    public async Task Update_AuthorChange_OldDerivedRowMissing_StillWritesNew()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var a = await SeedArtistAsync(host, "A");
        var b = await SeedArtistAsync(host, "B");
        var s = await CreateSongAsync(host, a, "Drifted Song");
        await using (var db = FreshContext(host))
            await db.Catalog.Where(c => c.ArtistId == a && c.SongId == s).ExecuteDeleteAsync();

        var (ok, message) = await host.Resolve<ISongService>().UpdateSongAsync(s, "Drifted Song", null, artistId: b);

        Assert.True(ok, message);
        Assert.Equal([(b, s)], await RowsAsync(host));
    }

    // [AC] REQ-CATPROP-04: a refused create (validation) leaves no catalog row.
    [Fact]
    public async Task Create_ValidationFails_NoCatalogRow()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var artistId = await SeedArtistAsync(host, "Author");

        var (ok, _, _) = await host.Resolve<ISongService>().CreateSongAsync(artistId, new string('x', 251));

        Assert.False(ok);
        Assert.Empty(await RowsAsync(host));
    }

    // [AC] REQ-CATPROP-04: ArtistChoiceNeeded writes nothing, so the catalog is unchanged.
    [Fact]
    public async Task Update_ArtistChoiceNeeded_CatalogUnchanged()
    {
        // The test host builds its schema with EnsureCreated, so the ArtistSearch trigram index does not exist
        // and the real resolver finds no pool; a stub resolver reports the near-duplicate instead.
        await using var host = UnitOfWorkTestHost.Create(services =>
            services.AddScoped<IArtistResolutionService, NearDuplicateResolver>());
        var a = await SeedArtistAsync(host, "Metallica");
        var s = await CreateSongAsync(host, a, "Enter Sandman");
        var before = await RowsAsync(host);
        var request = new SongFormSaveRequest(s, "Enter Sandman", null, null, "Metalica", null, null,
            [], new ArtistDecision.None(), SongDecision.None);

        var outcome = await host.Resolve<ISongService>().SaveFromFormAsync(request);

        Assert.Equal(SongFormSaveKind.ArtistChoiceNeeded, outcome.Kind);
        Assert.Equal(before, await RowsAsync(host));
        Assert.Equal(a, await StoredArtistIdAsync(host, s));
    }

    // [AC] REQ-CATPROP-04: an exception while staging the new row rolls the whole update back.
    [Fact]
    public async Task Update_AuthorAtoB_SaveThrows_CatalogUnchanged()
    {
        await using var host = UnitOfWorkTestHost.Create(services =>
            services.AddScoped<ICatalogRepository>(sp =>
                new ThrowOnAddCatalogRepository(ActivatorUtilities.CreateInstance<CatalogRepository>(sp))));
        var a = await SeedArtistAsync(host, "A");
        var b = await SeedArtistAsync(host, "B");
        // The throwing decorator also blocks the create path, so seed the song and its derived row directly.
        int s;
        await using (var seed = FreshContext(host))
        {
            var song = new Song { ArtistId = a, Title = "Throwing Song", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            seed.Songs.Add(song);
            await seed.SaveChangesAsync();
            seed.Catalog.Add(new Catalog { ArtistId = a, SongId = song.Id });
            await seed.SaveChangesAsync();
            s = song.Id;
        }

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            host.Resolve<ISongService>().UpdateSongAsync(s, "Throwing Song", null, artistId: b));

        Assert.Equal([(a, s)], await RowsAsync(host));
        Assert.Equal(a, await StoredArtistIdAsync(host, s));
    }

    private sealed class NearDuplicateResolver : IArtistResolutionService
    {
        public Task<ArtistResolution> ResolveAsync(ArtistCandidate candidate, CancellationToken ct = default)
            => Task.FromResult(new ArtistResolution(ResolutionKind.FuzzyCandidates, null, [(1, "Metallica", 0.9)]));
    }

    /// <summary>Decorator over the real <see cref="CatalogRepository"/> that throws from <see cref="AddAsync"/>.</summary>
    private sealed class ThrowOnAddCatalogRepository : ICatalogRepository
    {
        private readonly ICatalogRepository _inner;

        public ThrowOnAddCatalogRepository(ICatalogRepository inner) => _inner = inner;

        public Task<(IEnumerable<SongListItemDto> items, int totalCount)> GetPagedByArtistAsync(
            int artistId, int pageNumber, int pageSize, string? normalizedQuery, CancellationToken ct)
            => _inner.GetPagedByArtistAsync(artistId, pageNumber, pageSize, normalizedQuery, ct);

        public Task<int> CountByArtistAsync(int artistId, CancellationToken ct)
            => _inner.CountByArtistAsync(artistId, ct);

        public Task<bool> ExistsAsync(int artistId, int songId, CancellationToken ct)
            => _inner.ExistsAsync(artistId, songId, ct);

        public Task AddAsync(Catalog entry, CancellationToken ct)
            => throw new InvalidOperationException("injected");

        public Task RemoveAsync(int artistId, int songId, CancellationToken ct)
            => _inner.RemoveAsync(artistId, songId, ct);

        public Task<bool> IsDerivedAsync(int artistId, int songId, CancellationToken ct)
            => _inner.IsDerivedAsync(artistId, songId, ct);

        public Task<(int removed, int kept)> RemoveCoversAsync(int artistId, IReadOnlyCollection<int> songIds, CancellationToken ct)
            => _inner.RemoveCoversAsync(artistId, songIds, ct);
    }
}
