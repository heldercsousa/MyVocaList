using MyVocaList.Domain.Entity;
using MyVocaList.Domain.RepositoryInterface;
using MyVocaList.Domain.ServicesInterfaces;
using MyVocaList.Domain.UnitOfWork;
using MyVocaList.Infra;
using MyVocaList.Services;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Integration.UnitOfWork;

/// <summary>
/// Task 4.2 (READ-SCOPE change, Wave 4): <see cref="SongService.GetSongByIdAsync"/>,
/// <see cref="SongService.ExistsByTitleForArtistAsync"/> and
/// <see cref="SongService.GetPagedSongsForListAsync"/> are wrapped in
/// <see cref="IUnitOfWork.ExecuteReadAsync{TResult}"/>. These tests exercise the real DI
/// composition over a temp SQLite file (never the EF in-memory provider, never a mocked
/// <c>AppDbContext</c> — `testing.md § Project anti-patterns`).
/// </summary>
public class SongServiceReadScopeTests
{
    // ── Wrapped-method integration coverage (real SQLite, unchanged return values) ─────────

    [Fact]
    // [AC] REQ-UOW-40: GetSongByIdAsync, scoped through IUnitOfWork.ExecuteReadAsync, returns the
    // same song entity a caller would have seen before the wrap.
    public async Task GetSongByIdAsync_ExistingSong_ReturnsSong()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var artists = host.Resolve<IArtistService>();
        var songs = host.Resolve<ISongService>();

        var (artistOk, artistMsg, artist) = await artists.CreateArtistAsync("Queen");
        Assert.True(artistOk, artistMsg);
        var (createOk, createMsg, created) = await songs.CreateSongAsync(artist!.Id, "Bohemian Rhapsody");
        Assert.True(createOk, createMsg);

        var found = await songs.GetSongByIdAsync(created!.Id);

        Assert.NotNull(found);
        Assert.Equal("Bohemian Rhapsody", found!.Title);
    }

    [Fact]
    // [AC] REQ-UOW-40: GetSongByIdAsync returns null for an id that does not exist, unchanged by
    // the wrap.
    public async Task GetSongByIdAsync_UnknownId_ReturnsNull()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var songs = host.Resolve<ISongService>();

        var found = await songs.GetSongByIdAsync(int.MaxValue);

        Assert.Null(found);
    }

    [Fact]
    // [AC] REQ-UOW-40: ExistsByTitleForArtistAsync, scoped through IUnitOfWork.ExecuteReadAsync,
    // still reports true for an existing title/artist pair.
    public async Task ExistsByTitleForArtistAsync_ExistingTitle_ReturnsTrue()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var artists = host.Resolve<IArtistService>();
        var songs = host.Resolve<ISongService>();

        var (artistOk, artistMsg, artist) = await artists.CreateArtistAsync("Queen");
        Assert.True(artistOk, artistMsg);
        var (createOk, createMsg, _) = await songs.CreateSongAsync(artist!.Id, "Bohemian Rhapsody");
        Assert.True(createOk, createMsg);

        var exists = await songs.ExistsByTitleForArtistAsync("Bohemian Rhapsody", artist.Id);

        Assert.True(exists);
    }

    [Fact]
    // [AC] REQ-UOW-40: ExistsByTitleForArtistAsync still reports false for a title that does not
    // exist for the given artist, unchanged by the wrap.
    public async Task ExistsByTitleForArtistAsync_UnknownTitle_ReturnsFalse()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var artists = host.Resolve<IArtistService>();
        var songs = host.Resolve<ISongService>();

        var (artistOk, artistMsg, artist) = await artists.CreateArtistAsync("Queen");
        Assert.True(artistOk, artistMsg);

        var exists = await songs.ExistsByTitleForArtistAsync("Nonexistent Title", artist!.Id);

        Assert.False(exists);
    }

    [Fact]
    // [AC] REQ-UOW-40: GetPagedSongsForListAsync, scoped through IUnitOfWork.ExecuteReadAsync,
    // still returns the correct items/totalCount for the seeded fixture.
    public async Task GetPagedSongsForListAsync_SeededFixture_ReturnsItemsAndTotalCount()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var artists = host.Resolve<IArtistService>();
        var songs = host.Resolve<ISongService>();

        var (artistOk, artistMsg, artist) = await artists.CreateArtistAsync("Queen");
        Assert.True(artistOk, artistMsg);
        var (song1Ok, song1Msg, _) = await songs.CreateSongAsync(artist!.Id, "Bohemian Rhapsody");
        Assert.True(song1Ok, song1Msg);
        var (song2Ok, song2Msg, _) = await songs.CreateSongAsync(artist.Id, "Don't Stop Me Now");
        Assert.True(song2Ok, song2Msg);

        var (items, totalCount) = await songs.GetPagedSongsForListAsync(1, 10);

        Assert.Equal(2, totalCount);
        Assert.Equal(2, items.Count());
    }

    // ── Two-distinct-AppDbContext assertion (REQ-UOW-39) ────────────────────────────────────

    [Fact]
    // [AC] REQ-UOW-39: two successive ExecuteReadAsync calls resolve the AppDbContext from
    // distinct DI scopes — proving the read is genuinely scoped, not cosmetically wrapped.
    public async Task ExecuteReadAsync_TwoSuccessiveCalls_ResolveDistinctAppDbContext()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var uow = host.Resolve<IUnitOfWork>();

        var first = await uow.ExecuteReadAsync(sp => Task.FromResult(sp.GetRequiredService<AppDbContext>()));
        var second = await uow.ExecuteReadAsync(sp => Task.FromResult(sp.GetRequiredService<AppDbContext>()));

        Assert.NotSame(first, second);
    }

    // ── Guard / short-circuit path stays outside the lambda (REQ-UOW-41) ────────────────────

    [Fact]
    // [AC] REQ-UOW-41: GetPagedSongsForListAsync's argument-validation guard throws before any
    // ExecuteReadAsync invocation — zero calls into the unit of work for an invalid pageNumber.
    public async Task GetPagedSongsForListAsync_InvalidPageNumber_ThrowsWithoutInvokingExecuteReadAsync()
    {
        var songRepoMock = new Mock<ISongRepository>();
        var artistRepoMock = new Mock<IArtistRepository>();
        var urlRepoMock = new Mock<ISongKaraokeUrlRepository>();
        var urlServiceMock = new Mock<ISongKaraokeUrlService>();
        var loggerMock = new Mock<ILogger<SongService>>();
        var countingUow = new CountingUnitOfWork(
            PassthroughUnitOfWork.Over(songRepoMock, artistRepoMock, urlRepoMock, urlServiceMock));
        var sut = new SongService(
            songRepoMock.Object, artistRepoMock.Object, urlRepoMock.Object, urlServiceMock.Object,
            countingUow, loggerMock.Object);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => sut.GetPagedSongsForListAsync(0, 10));

        Assert.Equal(0, countingUow.ReadInvocations);
    }

    [Fact]
    // [AC] REQ-UOW-41: GetPagedSongsForListAsync's argument-validation guard throws before any
    // ExecuteReadAsync invocation — zero calls into the unit of work for an invalid pageSize.
    public async Task GetPagedSongsForListAsync_InvalidPageSize_ThrowsWithoutInvokingExecuteReadAsync()
    {
        var songRepoMock = new Mock<ISongRepository>();
        var artistRepoMock = new Mock<IArtistRepository>();
        var urlRepoMock = new Mock<ISongKaraokeUrlRepository>();
        var urlServiceMock = new Mock<ISongKaraokeUrlService>();
        var loggerMock = new Mock<ILogger<SongService>>();
        var countingUow = new CountingUnitOfWork(
            PassthroughUnitOfWork.Over(songRepoMock, artistRepoMock, urlRepoMock, urlServiceMock));
        var sut = new SongService(
            songRepoMock.Object, artistRepoMock.Object, urlRepoMock.Object, urlServiceMock.Object,
            countingUow, loggerMock.Object);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => sut.GetPagedSongsForListAsync(1, 0));

        Assert.Equal(0, countingUow.ReadInvocations);
    }

    /// <summary>Test-only <see cref="IUnitOfWork"/> decorator counting
    /// <see cref="ExecuteReadAsync{TResult}"/> invocations so a guard test can assert a
    /// short-circuit path made zero calls into the unit of work (REQ-UOW-41). Delegates every
    /// call to an inner <see cref="IUnitOfWork"/> (typically a <see cref="PassthroughUnitOfWork"/>).</summary>
    private sealed class CountingUnitOfWork(IUnitOfWork inner) : IUnitOfWork
    {
        public int ReadInvocations { get; private set; }

        public Task<TResult> ExecuteAsync<TResult>(
            Func<IServiceProvider, Task<TResult>> body, CancellationToken ct = default)
            => inner.ExecuteAsync(body, ct);

        public Task ExecuteAsync(Func<IServiceProvider, Task> body, CancellationToken ct = default)
            => inner.ExecuteAsync(body, ct);

        public Task<TResult> ExecuteReadAsync<TResult>(
            Func<IServiceProvider, Task<TResult>> body, CancellationToken ct = default)
        {
            ReadInvocations++;
            return inner.ExecuteReadAsync(body, ct);
        }

        public Task FlushAsync(CancellationToken ct = default) => inner.FlushAsync(ct);
    }
}
