using MyVocaList.Domain.Resolution;
using MyVocaList.Domain.ReadModels;
using MyVocaList.Infra.Repository;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Integration.UnitOfWork;

/// <summary>
/// Task 0.4 / 0.4b — atomicity of the 3-level nested chain
/// (SongResolutionService.CommitAsync → SongService.* / ArtistResolutionService.CommitAsync →
/// ArtistService.CreateArtistAsync) and the REQ-UOW-09 characterization test for
/// ArtistResolutionService.CommitAsync's CreateNew branch.
/// </summary>
public class NestedUnitOfWorkTests
{
    // [AC] REQ-UOW-22: happy path — novel artist + novel song via CommitAsync produces exactly
    // one Artist row and one Song row, no tracking exception.
    // EXPECTED TO PASS on current HEAD — each nested call saves eagerly today; this assertion
    // alone is NOT RED evidence (see Step 2 for the RED test).
    [Fact]
    public async Task CommitAsync_NovelArtistAndSong_CreatesExactlyOneArtistAndOneSongRow()
    {
        await using var host = UnitOfWorkTestHost.CreateLegacy();
        var songResolution = host.Resolve<ISongResolutionService>();

        var candidate = new SongCandidate(
            "Nested Happy Path Song",
            string.Empty,
            null,
            null,
            new ArtistCandidate("Nested Happy Path Artist", null, null),
            null,
            null);

        var (success, message, song) = await songResolution.CommitAsync(
            candidate, ResolutionChoice.CreateNew, null, null);

        Assert.True(success, message);
        Assert.NotNull(song);

        var artistCount = host.Db.Artists.Count(a => a.Name == "Nested Happy Path Artist");
        var songCount = host.Db.Songs.Count(s => s.Title == "Nested Happy Path Song");
        Assert.Equal(1, artistCount);
        Assert.Equal(1, songCount);
    }

    // [AC] REQ-UOW-22: fault injection.
    //
    // DEVIATION FROM THE PLAN TEXT (recorded here, and in the task-log): the plan describes
    // injecting the throw into IArtistRepository.AddAsync as "the innermost point of the chain,
    // after the outer SongService write has already run". That call-order assumption does not
    // hold for the CreateNew branch actually on HEAD: SongResolutionService.CommitAsync resolves
    // the artist FIRST via ResolveOrCreateArtistIdAsync (:162) — which persists the Artist row
    // through ArtistResolutionService.CommitAsync -> ArtistService.CreateArtistAsync, already
    // committed — and only THEN calls _songService.CreateSongAsync (:166). So an
    // IArtistRepository.AddAsync throw fires BEFORE any Song write is even attempted; nothing has
    // been persisted yet, and the test is trivially green today (verified: 0/0 rows, not RED —
    // see task-log for the run that proved this before this file was corrected).
    //
    // To reproduce the real defect (partial state survives a mid-chain fault) the fault must sit
    // at the point that actually runs AFTER the Artist row has already been committed: the SONG
    // write (ISongRepository.AddAsync). That is the true "outer write already ran, then inner
    // write throws" ordering for this chain. THIS IS THE RED TEST.
    [Fact]
    public async Task CommitAsync_SongAddThrowsAfterArtistAlreadyCommitted_LeavesPartialArtistRow()
    {
        await using var host = UnitOfWorkTestHost.CreateLegacy(services =>
            services.AddScoped<ISongRepository>(sp =>
                new ThrowOnAddSongRepository(
                    ActivatorUtilities.CreateInstance<SongRepository>(sp))));

        var songResolution = host.Resolve<ISongResolutionService>();

        var candidate = new SongCandidate(
            "Fault Injection Song",
            string.Empty,
            null,
            null,
            new ArtistCandidate("Fault Injection Artist", null, null),
            null,
            null);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            songResolution.CommitAsync(candidate, ResolutionChoice.CreateNew, null, null));

        var artistCount = host.Db.Artists.Count(a => a.Name == "Fault Injection Artist");
        var songCount = host.Db.Songs.Count(s => s.Title == "Fault Injection Song");
        // All-or-nothing (REQ-UOW-22): both should be 0. Observed on HEAD: artistCount == 1
        // (the Artist row survives the later Song-write fault) — this is the RED evidence.
        Assert.Equal(0, artistCount);
        Assert.Equal(0, songCount);
    }

    // [AC] REQ-UOW-24: nested-call precedence — the SAME partial-commit defect as above, but
    // reached via a validation FAILURE TUPLE instead of a throw. SongService.CreateSongAsync
    // validates the title length before touching the repository at all (ValidateTitleInput,
    // SongService.cs) and returns (false, message, null) without persisting anything — this is
    // the realistic "inner call returns a failure tuple after an outer write has already run"
    // path: the Artist row is already committed (SongResolutionService.CommitAsync resolves the
    // artist before calling CreateSongAsync), then CreateSongAsync's own validation fails with a
    // tuple (not a throw), and the failure must reach SongResolutionService.CommitAsync's
    // returned tuple AND — per all-or-nothing — leave nothing persisted.
    [Fact]
    public async Task CommitAsync_SongValidationReturnsFailureTupleAfterArtistAlreadyCommitted_LeavesPartialArtistRow()
    {
        await using var host = UnitOfWorkTestHost.CreateLegacy();
        var songResolution = host.Resolve<ISongResolutionService>();

        var tooLongTitle = new string('x', 101); // SongService.MaxTitleLength == 100

        var candidate = new SongCandidate(
            tooLongTitle,
            string.Empty,
            null,
            null,
            new ArtistCandidate("Failure Tuple Artist", null, null),
            null,
            null);

        var (success, message, song) = await songResolution.CommitAsync(
            candidate, ResolutionChoice.CreateNew, null, null);

        Assert.False(success, message);
        Assert.Null(song);

        var artistCount = host.Db.Artists.Count(a => a.Name == "Failure Tuple Artist");
        // All-or-nothing (REQ-UOW-24 nested precedence): should be 0. Observed on HEAD:
        // artistCount == 1 (the Artist row survives despite the overall failure tuple) — RED.
        Assert.Equal(0, artistCount);
    }

    // [AC] REQ-UOW-09: ArtistResolutionService.CommitAsync CreateNew branch — an ArtistCandidate
    // with an external provider and id, choice = CreateNew, produces exactly one Artist row with
    // that name, ExternalProvider and ExternalId set, and the returned artistId matches that row.
    // EXPECTED TO PASS on HEAD — characterization test pinning an existing guarantee before
    // Task 2.3's re-shape, not a defect reproduction.
    [Fact]
    public async Task CommitAsync_CreateNewWithExternalIdentity_CreatesOneArtistWithExternalFieldsSet()
    {
        await using var host = UnitOfWorkTestHost.CreateLegacy();
        var artistResolution = host.Resolve<IArtistResolutionService>();

        var candidate = new ArtistCandidate("REQ-UOW-09 Probe Artist", "spotify", "ext-uow-09");

        var (success, message, artistId) = await artistResolution.CommitAsync(
            candidate, ResolutionChoice.CreateNew, null);

        Assert.True(success, message);
        Assert.True(artistId > 0);

        var rows = host.Db.Artists.Where(a => a.Name == "REQ-UOW-09 Probe Artist").ToList();
        Assert.Single(rows);
        var row = rows[0];
        Assert.Equal(artistId, row.Id);
        Assert.Equal("spotify", row.ExternalProvider);
        Assert.Equal("ext-uow-09", row.ExternalId);
    }

    /// <summary>
    /// Decorator over the real <see cref="SongRepository"/> that throws from
    /// <see cref="AddAsync"/> to simulate a fault at the point in the chain that runs AFTER the
    /// Artist row has already been committed (see the deviation note on
    /// <see cref="CommitAsync_SongAddThrowsAfterArtistAlreadyCommitted_LeavesPartialArtistRow"/>),
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

        public Task<IReadOnlyList<Song>> GetFuzzyCandidatePoolAsync(int artistId, string titlePrefixToken, int take, CancellationToken ct = default)
            => _inner.GetFuzzyCandidatePoolAsync(artistId, titlePrefixToken, take, ct);

        public Task<IReadOnlyList<Song>> GetByTitlesCollatedAsync(IEnumerable<string> titles, CancellationToken ct = default)
            => _inner.GetByTitlesCollatedAsync(titles, ct);

        public Task AddAsync(Song song, CancellationToken ct)
            => throw new InvalidOperationException("injected");

        public Task UpdateAsync(Song song, CancellationToken ct)
            => _inner.UpdateAsync(song, ct);

        public Task DeleteAsync(IEnumerable<int> ids, CancellationToken ct)
            => _inner.DeleteAsync(ids, ct);

        public Task SaveChangesAsync(CancellationToken ct = default)
            => _inner.SaveChangesAsync(ct);
    }
}
