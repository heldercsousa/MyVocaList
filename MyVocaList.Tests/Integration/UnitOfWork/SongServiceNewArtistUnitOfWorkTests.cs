using MyVocaList.Domain.ReadModels;
using MyVocaList.Infra;
using MyVocaList.Infra.Repository;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Integration.UnitOfWork;

/// <summary>
/// Task 3.1 (SFT) - atomicity of <c>ISongService.CreateSongWithUrlsAsync(artistId, newArtistName, ...)</c>:
/// a typed new artist is created inside the song's unit of work (2 levels: SongService ->
/// ArtistService.CreateArtistAsync, joined to the ambient context). Ports the surviving intent of
/// <c>NestedUnitOfWorkTests</c> (:22, :66, :105, :146); that file is deleted in Task 3.3.
/// </summary>
public class SongServiceNewArtistUnitOfWorkTests
{
    private static Task<(bool success, string message, Song? song)> CreateWithNewArtist(
        UnitOfWorkTestHost host, string artistName, string title,
        string? externalId = null, string? externalProvider = null)
        => host.Resolve<ISongService>().CreateSongWithUrlsAsync(
            null, artistName, title, string.Empty, null, null, externalId, externalProvider, []);

    // [AC] REQ-SFT-18: a typed new artist and the song are persisted together - exactly one Artist
    // row and one Song row, and the song points at the new artist's id (ports NestedUnitOfWorkTests:22).
    [Fact]
    public async Task CreateSongWithUrlsAsync_NewArtistName_PersistsOneArtistAndOneSong()
    {
        await using var host = UnitOfWorkTestHost.Create();

        var (success, message, song) = await CreateWithNewArtist(host, "Nested Happy Path Artist", "Nested Happy Path Song");

        Assert.True(success, message);
        Assert.NotNull(song);
        var artists = host.Db.Artists.Where(a => a.Name == "Nested Happy Path Artist").ToList();
        var songs = host.Db.Songs.Where(s => s.Title == "Nested Happy Path Song").ToList();
        Assert.Single(artists);
        Assert.Single(songs);
        Assert.Equal(artists[0].Id, songs[0].ArtistId);
    }

    // [AC] REQ-SFT-18: a fault while staging the song, after the artist was staged (and flushed),
    // rolls back BOTH. Rewritten from NestedUnitOfWorkTests:66 (REQ-UOW-24 -> REQ-SFT-18): the
    // original asserted the 3-level CommitAsync path leaked an Artist row; this asserts 0/0 rows on
    // the 2-level path. Same failure injection (ISongRepository.AddAsync throws).
    [Fact]
    public async Task CreateSongWithUrlsAsync_ThrowAfterArtistStaged_RollsBackBoth()
    {
        await using var host = UnitOfWorkTestHost.Create(services =>
            services.AddScoped<ISongRepository>(sp =>
                new ThrowOnAddSongRepository(
                    ActivatorUtilities.CreateInstance<SongRepository>(sp))));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateWithNewArtist(host, "Fault Injection Artist", "Fault Injection Song"));

        Assert.Equal(0, host.Db.Artists.Count(a => a.Name == "Fault Injection Artist"));
        Assert.Equal(0, host.Db.Songs.Count(s => s.Title == "Fault Injection Song"));
    }

    // [AC] REQ-SFT-18: a failure TUPLE after the artist was staged writes nothing (0/0 rows).
    // Rewritten from NestedUnitOfWorkTests:105 (REQ-UOW-24 -> REQ-SFT-18), assertion inverted as
    // above. Same injection as the original: a 251-char title (SongService.MaxTitleLength == 250).
    [Fact]
    public async Task CreateSongWithUrlsAsync_FailureTupleAfterArtistStaged_WritesNothing()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var tooLongTitle = new string('x', 251);

        var (success, message, song) = await CreateWithNewArtist(host, "Failure Tuple Artist", tooLongTitle);

        Assert.False(success, message);
        Assert.Null(song);
        Assert.Equal(0, host.Db.Artists.Count(a => a.Name == "Failure Tuple Artist"));
        Assert.Equal(0, host.Db.Songs.Count());
    }

    // [AC] REQ-SFT-18: the new-artist path uses ONE AppDbContext across both levels. 2 levels; no
    // 3-level path remains (ports NestedUnitOfWorkTests:146, REQ-UOW-22 re-homed). AppDbContext is
    // scoped, so each repository factory below records the context of the scope it is resolved from;
    // a level that opened its own scope would record a DIFFERENT instance.
    [Fact]
    public async Task CreateSongWithUrlsAsync_NewArtist_UsesOneAppDbContextAcrossTwoLevels()
    {
        var observed = new List<AppDbContext>();

        await using var host = UnitOfWorkTestHost.Create(services =>
        {
            services.AddScoped<ISongRepository>(sp =>
            {
                observed.Add(sp.GetRequiredService<AppDbContext>());
                return ActivatorUtilities.CreateInstance<SongRepository>(sp);
            });
            services.AddScoped<IArtistRepository>(sp =>
            {
                observed.Add(sp.GetRequiredService<AppDbContext>());
                return ActivatorUtilities.CreateInstance<ArtistRepository>(sp);
            });
        });

        // Resolve the service BEFORE clearing (as NestedUnitOfWorkTests:164/177 do): resolving
        // SongService records the host's AppDbContext, which must not count as in-unit-of-work.
        var service = host.Resolve<ISongService>();
        observed.Clear(); // only contexts seen inside the unit of work under test

        var (success, message, _) = await service.CreateSongWithUrlsAsync(
            null, "Two Level Join Artist", "Two Level Join Song", string.Empty, null, null, null, null, []);

        Assert.True(success, message);
        Assert.True(observed.Count >= 2, $"Expected repositories from >= 2 levels, saw {observed.Count}.");
        Assert.Single(observed.Distinct(ReferenceEqualityComparer.Instance));
    }

    // [AC] REQ-SFT-52: a typed name that collides (NOCASE_NOACCENT) with a stored artist is refused
    // with the IArtistService duplicate message; no artist, no song is written.
    [Fact]
    public async Task CreateSongWithUrlsAsync_NewArtistNameCollatedDuplicate_FailsAndWritesNothing()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var (seeded, seedMessage, _) = await host.Resolve<IArtistService>().CreateArtistAsync("Metallica");
        Assert.True(seeded, seedMessage);

        var (success, message, song) = await CreateWithNewArtist(host, "m\u00e9tallica", "Enter Sandman");

        Assert.False(success);
        Assert.Equal("An artist with this name already exists", message);
        Assert.Null(song);
        Assert.Equal(1, host.Db.Artists.Count());
        Assert.Equal(0, host.Db.Songs.Count());
    }

    // [AC] REQ-SFT-26: when the external identity is already held by another song, the call fails
    // BEFORE the artist is created - the artist count is unchanged.
    [Fact]
    public async Task CreateSongWithUrlsAsync_NewArtistAndIdentityHeld_CreatesNoArtist()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var (artistOk, _, holder) = await host.Resolve<IArtistService>().CreateArtistAsync("Holder Artist");
        Assert.True(artistOk);
        var (holderOk, holderMessage, _) = await host.Resolve<ISongService>().CreateSongWithUrlsAsync(
            holder!.Id, null, "Holder Song", string.Empty, null, null, "dz-777", "deezer", []);
        Assert.True(holderOk, holderMessage);

        var (success, message, song) = await CreateWithNewArtist(
            host, "Would Be New Artist", "Another Title", "dz-777", "deezer");

        Assert.False(success);
        Assert.Equal("This music-database song is already in your library", message);
        Assert.Null(song);
        Assert.Equal(1, host.Db.Artists.Count());
        Assert.Equal(0, host.Db.Artists.Count(a => a.Name == "Would Be New Artist"));
    }

    /// <summary>
    /// Decorator over the real <see cref="SongRepository"/> that throws from <see cref="AddAsync"/>,
    /// forwarding every other member to the inner instance.
    /// </summary>
    private sealed class ThrowOnAddSongRepository : ISongRepository
    {
        private readonly ISongRepository _inner;

        public ThrowOnAddSongRepository(ISongRepository inner) => _inner = inner;

        public Task<(IEnumerable<SongListItemDto> items, int totalCount)> GetPagedAsync(
            int pageNumber, int pageSize, string? query, CancellationToken ct)
            => _inner.GetPagedAsync(pageNumber, pageSize, query, ct);

        public Task<Song> GetByIdAsync(int id, CancellationToken ct)
            => _inner.GetByIdAsync(id, ct);

        public Task<Song?> GetByExternalIdAsync(string externalId, string provider, CancellationToken ct)
            => _inner.GetByExternalIdAsync(externalId, provider, ct);

        public Task<bool> ExistsByTitleForArtistAsync(int artistId, string title, CancellationToken ct)
            => _inner.ExistsByTitleForArtistAsync(artistId, title, ct);

        public Task<bool> ExistsByTitleForArtistAsync(int artistId, string title, int excludeId, CancellationToken ct)
            => _inner.ExistsByTitleForArtistAsync(artistId, title, excludeId, ct);

        public Task<bool> ExistsByTitleVersionForArtistAsync(int artistId, string title, string version, CancellationToken ct = default)
            => _inner.ExistsByTitleVersionForArtistAsync(artistId, title, version, ct);

        public Task<bool> ExistsByTitleVersionForArtistAsync(int artistId, string title, string version, int excludeId, CancellationToken ct = default)
            => _inner.ExistsByTitleVersionForArtistAsync(artistId, title, version, excludeId, ct);

        public Task<IReadOnlyList<SongSearchHit>> SearchTitleCandidatesForArtistAsync(int artistId, string title, int take, CancellationToken ct = default)
            => _inner.SearchTitleCandidatesForArtistAsync(artistId, title, take, ct);

        public Task<IReadOnlyList<Song>> GetByTitlesCollatedAsync(IEnumerable<string> titles, CancellationToken ct = default)
            => _inner.GetByTitlesCollatedAsync(titles, ct);

        public Task<int?> FindIdByTitleVersionForArtistAsync(int artistId, string title, string version, int? excludeId, CancellationToken ct)
            => _inner.FindIdByTitleVersionForArtistAsync(artistId, title, version, excludeId, ct);

        public Task<bool> ExistsByExternalIdentityAsync(string provider, string externalId, int? excludeId, CancellationToken ct)
            => _inner.ExistsByExternalIdentityAsync(provider, externalId, excludeId, ct);

        public Task<List<string>> GetLocalImageReferencesAsync(CancellationToken ct = default)
            => _inner.GetLocalImageReferencesAsync(ct);

        public Task<IReadOnlyList<SongSearchHit>> SearchTitleCandidatesAsync(string query, int take, CancellationToken ct)
            => _inner.SearchTitleCandidatesAsync(query, take, ct);

        public Task<IReadOnlyList<SongSearchHit>> FindByExternalIdsAsync(string provider, IReadOnlyCollection<string> externalIds, CancellationToken ct)
            => _inner.FindByExternalIdsAsync(provider, externalIds, ct);

        public Task<IReadOnlyList<SongSearchHit>> FindByTitleAndArtistNamesAsync(IReadOnlyCollection<(string Title, string ArtistName, string Version)> items, CancellationToken ct)
            => _inner.FindByTitleAndArtistNamesAsync(items, ct);

        public Task AddAsync(Song song, CancellationToken ct)
            => throw new InvalidOperationException("injected");

        public Task UpdateAsync(Song song, CancellationToken ct)
            => _inner.UpdateAsync(song, ct);

        public Task DeleteAsync(IEnumerable<int> ids, CancellationToken ct)
            => _inner.DeleteAsync(ids, ct);
    }
}
