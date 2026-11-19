using Microsoft.Extensions.DependencyInjection;
using MyVocaList.Domain.Entity;
using MyVocaList.Domain.ServicesInterfaces;
using MyVocaList.Domain.UnitOfWork;
using MyVocaList.Infra;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Integration.UnitOfWork;

/// <summary>
/// Task 4.4 — <see cref="SongKaraokeUrlService.GetUrlsForSongAsync"/> and
/// <see cref="SongKaraokeUrlService.GetSuggestedUrlAsync"/> are scoped through
/// <see cref="IUnitOfWork.ExecuteReadAsync{TResult}"/> (REQ-UOW-40).
/// </summary>
public class SongKaraokeUrlServiceReadScopeTests
{
    // [AC] REQ-UOW-40: GetUrlsForSongAsync returns the same projected DTOs as before the change
    // for a seeded fixture, against a real SQLite temp-file database.
    [Fact]
    public async Task GetUrlsForSongAsync_SeededFixture_ReturnsExpectedDtos()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var songId = await SeedSongWithUrlsAsync(host);
        var sut = host.Resolve<ISongKaraokeUrlService>();

        var result = await sut.GetUrlsForSongAsync(songId);

        Assert.Equal(2, result.Count);
        Assert.True(result[0].IsSuggested);
        Assert.False(result[1].IsSuggested);
        Assert.Equal("AAAAAAAAAAA", result[0].VideoId);
        Assert.Equal("BBBBBBBBBBB", result[1].VideoId);
    }

    // [AC] REQ-UOW-40: GetSuggestedUrlAsync returns the top-ranked URL as before the change for a
    // seeded fixture, against a real SQLite temp-file database.
    [Fact]
    public async Task GetSuggestedUrlAsync_SeededFixture_ReturnsTopRankedUrl()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var songId = await SeedSongWithUrlsAsync(host);
        var sut = host.Resolve<ISongKaraokeUrlService>();

        var result = await sut.GetSuggestedUrlAsync(songId);

        Assert.NotNull(result);
        Assert.Equal("AAAAAAAAAAA", result!.VideoId);
        Assert.True(result.IsSuggested);
    }

    // [AC] REQ-UOW-40: no rows for the song — GetSuggestedUrlAsync returns null rather than
    // throwing, unchanged behavior from before the wrap.
    [Fact]
    public async Task GetSuggestedUrlAsync_NoUrls_ReturnsNull()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var songId = await SeedSongWithUrlsAsync(host, seedUrls: false);
        var sut = host.Resolve<ISongKaraokeUrlService>();

        var result = await sut.GetSuggestedUrlAsync(songId);

        Assert.Null(result);
    }

    // [AC] REQ-UOW-39/40 observation mechanism: two successive calls to a wrapped read method
    // obtain two distinct AppDbContext instances — captured from inside the lambda via a
    // recording decorator, proving the read is genuinely scoped rather than cosmetically wrapped.
    [Fact]
    public async Task GetUrlsForSongAsync_TwoSuccessiveCalls_UseDistinctAppDbContexts()
    {
        await using var host = UnitOfWorkTestHost.Create();
        var songId = await SeedSongWithUrlsAsync(host);
        var recordingUow = new RecordingUnitOfWork(host.Resolve<IUnitOfWork>());
        var repo = host.Resolve<MyVocaList.Domain.RepositoryInterface.ISongKaraokeUrlRepository>();
        var logger = host.Resolve<Microsoft.Extensions.Logging.ILogger<SongKaraokeUrlService>>();
        var sut = new SongKaraokeUrlService(repo, recordingUow, logger);

        await sut.GetUrlsForSongAsync(songId);
        await sut.GetUrlsForSongAsync(songId);

        Assert.Equal(2, recordingUow.CapturedContexts.Count);
        Assert.NotSame(recordingUow.CapturedContexts[0], recordingUow.CapturedContexts[1]);
    }

    private static async Task<int> SeedSongWithUrlsAsync(UnitOfWorkTestHost host, bool seedUrls = true)
    {
        var artist = new Artist { Name = "Seed Artist", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        host.Db.Artists.Add(artist);
        await host.Db.SaveChangesAsync();

        var song = new Song
        {
            ArtistId = artist.Id,
            Title = "Seed Song",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        host.Db.Songs.Add(song);
        await host.Db.SaveChangesAsync();

        if (seedUrls)
        {
            host.Db.SongKaraokeUrls.AddRange(
                new SongKaraokeUrl
                {
                    SongId = song.Id,
                    VideoId = "AAAAAAAAAAA",
                    PlayCount = 5,
                    AddedAt = DateTime.UtcNow
                },
                new SongKaraokeUrl
                {
                    SongId = song.Id,
                    VideoId = "BBBBBBBBBBB",
                    PlayCount = 2,
                    AddedAt = DateTime.UtcNow
                });
            await host.Db.SaveChangesAsync();
        }

        return song.Id;
    }

    /// <summary>Forwards to the real <see cref="IUnitOfWork"/> while recording the
    /// <see cref="AppDbContext"/> resolved from inside each <c>ExecuteReadAsync</c> body — the
    /// REQ-UOW-39 observation mechanism (no way to observe scope identity from outside the lambda).</summary>
    private sealed class RecordingUnitOfWork(IUnitOfWork inner) : IUnitOfWork
    {
        public List<AppDbContext> CapturedContexts { get; } = [];

        public Task<TResult> ExecuteAsync<TResult>(Func<IServiceProvider, Task<TResult>> body, CancellationToken ct = default)
            => inner.ExecuteAsync(body, ct);

        public Task ExecuteAsync(Func<IServiceProvider, Task> body, CancellationToken ct = default)
            => inner.ExecuteAsync(body, ct);

        public Task<TResult> ExecuteReadAsync<TResult>(Func<IServiceProvider, Task<TResult>> body, CancellationToken ct = default)
            => inner.ExecuteReadAsync(async sp =>
            {
                CapturedContexts.Add(sp.GetRequiredService<AppDbContext>());
                return await body(sp);
            }, ct);

        public Task FlushAsync(CancellationToken ct = default) => inner.FlushAsync(ct);
    }
}
