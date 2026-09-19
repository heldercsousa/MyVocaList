using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyVocaList.Domain.RepositoryInterface;
using MyVocaList.Domain.ServicesInterfaces;
using MyVocaList.Domain.UnitOfWork;
using MyVocaList.Infra;
using MyVocaList.Services;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Integration.UnitOfWork;

/// <summary>
/// Task 6.1 (READ-SCOPE change, Wave 6) — REQ-UOW-42's authoring limb: two paged-list loads
/// across two DIFFERENT services (<see cref="ArtistService.GetPagedArtistsForListAsync"/> and
/// <see cref="SongService.GetPagedSongsForListAsync"/>), forced to genuinely overlap, resolve
/// two distinct <see cref="AppDbContext"/> instances and never throw the captive-context
/// symptom exception. Originally written and run in Wave 6 with the now-removed `DbLoadGate`
/// still present; this test never referenced the gate, so it is agnostic to whether the gate
/// exists. Re-run in Wave 8 after the gate's removal to complete REQ-UOW-42's mandated condition.
/// </summary>
public class PagedListConcurrencyTests
{
    // [AC] REQ-UOW-42: ArtistService.GetPagedArtistsForListAsync and
    // SongService.GetPagedSongsForListAsync, forced to genuinely overlap via a
    // TaskCompletionSource awaited inside each lambda body (never Task.WhenAll), resolve two
    // distinct AppDbContext instances and neither throws an InvalidOperationException mentioning
    // "A second operation was started on this context".
    [Fact]
    public async Task TwoPagedListLoadsAcrossDifferentServices_OverlapWithoutSharedContext()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var realUow = host.Resolve<IUnitOfWork>();

        var artistRepository = host.Resolve<IArtistRepository>();
        var songRepository = host.Resolve<ISongRepository>();
        var catalogRepository = host.Resolve<ICatalogRepository>();
        var urlRepository = host.Resolve<ISongKaraokeUrlRepository>();
        var urlService = host.Resolve<ISongKaraokeUrlService>();
        var artistLogger = host.Resolve<ILogger<ArtistService>>();
        var songLogger = host.Resolve<ILogger<SongService>>();

        // Seed data through the real (unwrapped) services — this is setup, not part of the probe.
        var seedingArtists = host.Resolve<IArtistService>();
        var seedingSongs = host.Resolve<ISongService>();
        var (artistOk, artistMsg, seededArtist) = await seedingArtists.CreateArtistAsync("Overlap Artist");
        Assert.True(artistOk, artistMsg);
        var (songOk, songMsg, _) = await seedingSongs.CreateSongAsync(seededArtist!.Id, "Overlap Song");
        Assert.True(songOk, songMsg);

        // RunContinuationsAsynchronously — see UnitOfWorkLifetimeTests.
        // ExecuteReadAsync_TwoOverlappingUnitsOfWork_SeeDistinctContexts for why a default
        // (synchronous) TCS would let the continuation run reentrantly on the same call stack as
        // SetResult(), before the first ExecuteReadAsync call has returned control to the caller.
        var artistEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseArtist = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        AppDbContext? artistContext = null;
        AppDbContext? songContext = null;

        // Probe wraps the real IUnitOfWork's ExecuteReadAsync so the TaskCompletionSource is
        // awaited INSIDE the same lambda body that ArtistService's read passes to
        // ExecuteReadAsync — never Task.WhenAll. The context is captured from the SAME
        // IServiceProvider the real body goes on to query with, so the capture is not cosmetic.
        var artistUow = new ConcurrencyProbeUnitOfWork(realUow, async ctx =>
        {
            artistContext = ctx;
            artistEntered.SetResult();
            await releaseArtist.Task;
        });
        var artistService = new ArtistService(
            artistRepository, songRepository, catalogRepository, artistUow, artistLogger);

        var songUow = new ConcurrencyProbeUnitOfWork(realUow, ctx =>
        {
            songContext = ctx;
            return Task.CompletedTask;
        });
        var songService = new SongService(
            songRepository, artistRepository, urlRepository, urlService, songUow, songLogger);

        Exception? capturedException = null;
        try
        {
            // 1) Start the artist read — it blocks INSIDE its ExecuteReadAsync lambda body,
            // holding its AppDbContext scope open, until releaseArtist is signalled.
            var artistTask = artistService.GetPagedArtistsForListAsync(1, 10);

            // 2) Wait until the artist read has genuinely entered its lambda body (its context is
            // resolved and it is now parked awaiting release) — this is the proof of overlap: the
            // song read below runs to completion WHILE the artist's scope is still open.
            await artistEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));

            // 3) Run the song read to completion while the artist read is still in flight. If the
            // two shared a captive AppDbContext, this is exactly where EF Core would throw
            // "A second operation was started on this context before a previous operation
            // completed" — the two contexts being genuinely distinct is what prevents that.
            await songService.GetPagedSongsForListAsync(1, 10).WaitAsync(TimeSpan.FromSeconds(5));

            // 4) Release the artist read and let it complete.
            releaseArtist.SetResult();
            var artistTask2 = artistTask.WaitAsync(TimeSpan.FromSeconds(5));
            _ = await artistTask2;
        }
        catch (Exception ex)
        {
            capturedException = ex;
        }

        // Assertion (ii): no InvalidOperationException mentioning the captive-context symptom.
        Assert.True(
            capturedException is not InvalidOperationException ioe
                || !ioe.Message.Contains("A second operation was started on this context"),
            $"Unexpected captive-context exception: {capturedException}");
        Assert.Null(capturedException);

        // Assertion (i): two distinct AppDbContext instances were actually used.
        Assert.NotNull(artistContext);
        Assert.NotNull(songContext);
        Assert.NotSame(artistContext, songContext);
    }

    /// <summary>Wraps a real <see cref="IUnitOfWork"/> so a test can observe (and, for reads,
    /// delay) the exact <see cref="AppDbContext"/> a body is about to run against — without
    /// touching production code. <paramref name="onReadEntered"/> runs INSIDE the real
    /// <c>ExecuteReadAsync</c> lambda, after the scope's <see cref="AppDbContext"/> is resolved
    /// and before the caller's own body runs, so blocking there genuinely holds the scope open.</summary>
    private sealed class ConcurrencyProbeUnitOfWork(IUnitOfWork inner, Func<AppDbContext, Task> onReadEntered)
        : IUnitOfWork
    {
        public Task<TResult> ExecuteAsync<TResult>(
            Func<IServiceProvider, Task<TResult>> body, CancellationToken ct = default)
            => inner.ExecuteAsync(body, ct);

        public Task ExecuteAsync(Func<IServiceProvider, Task> body, CancellationToken ct = default)
            => inner.ExecuteAsync(body, ct);

        public Task<TResult> ExecuteReadAsync<TResult>(
            Func<IServiceProvider, Task<TResult>> body, CancellationToken ct = default)
            => inner.ExecuteReadAsync(async sp =>
            {
                var context = sp.GetRequiredService<AppDbContext>();
                await onReadEntered(context);
                return await body(sp);
            }, ct);

        public Task FlushAsync(CancellationToken ct = default) => inner.FlushAsync(ct);
    }
}
