using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MyVocaList.Contracts.DTOs;
using MyVocaList.Contracts.Enums;
using MyVocaList.Infra;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Integration.Services;

/// <summary>
/// Lookup on open on the real stack (REQ-SLD-20, 22, 24): real SQLite temp file, real unit of work, real repositories,
/// real <see cref="SongService"/> and <see cref="SongLyricsService"/>. Only the HTTP lyrics provider and the network
/// status are stubbed. Verifies what is stored, not the mocked calls of the unit tests.
/// </summary>
public class SongServiceEnsureLyricsRealStackTests
{
    private const string DeezerProvider = "Deezer";
    private const string DeezerTrackId = "3135556";
    private const string StoredTitle = "Bohemian Rhapsody";
    private const string StoredArtistName = "Queen";

    private sealed class CountingLyricsProvider : ILyricsProvider
    {
        public int Calls { get; private set; }
        public string? Answer { get; set; }

        public Task<string?> FetchLyricsAsync(LyricsQuery query, CancellationToken ct = default)
        {
            Calls++;
            return Task.FromResult(Answer);
        }
    }

    private sealed class OnlineNetwork : INetworkStatus
    {
        public bool HasInternetAccess => true;
    }

    private readonly CountingLyricsProvider _provider = new();

    private UnitOfWorkTestHost CreateHost() => UnitOfWorkTestHost.Create(services =>
    {
        services.RemoveAll<ILyricsProvider>();   // drop the production (real-network) LRCLIB registration
        services.AddScoped<ILyricsProvider>(_ => _provider);
        services.AddSingleton<INetworkStatus, OnlineNetwork>();
    });

    private static async Task<int> SeedDeezerSongAsync(UnitOfWorkTestHost host)
    {
        var created = DateTime.UtcNow;
        var artist = new Artist { Name = StoredArtistName, CreatedAt = created, UpdatedAt = created };
        host.Db.Artists.Add(artist);
        await host.Db.SaveChangesAsync();

        var song = new Song
        {
            ArtistId = artist.Id,
            Title = StoredTitle,
            ExternalProvider = DeezerProvider,
            ExternalId = DeezerTrackId,
            Lyrics = null,
            LyricsOrigin = null,
            CreatedAt = created,
            UpdatedAt = created
        };
        host.Db.Songs.Add(song);
        await host.Db.SaveChangesAsync();
        return song.Id;
    }

    private static async Task<Song> ReadFreshAsync(UnitOfWorkTestHost host, int songId)
    {
        var factory = host.Resolve<IDbContextFactory<AppDbContext>>();
        await using var db = await factory.CreateDbContextAsync();
        return await db.Songs.AsNoTracking().SingleAsync(s => s.Id == songId);
    }

    [Fact]
    // [AC] REQ-SLD-20: a match is stored on the song at once, with origin LyricsSource; nothing else on the song changes.
    public async Task EnsureLyricsAsync_Match_IsPersistedAndReadBack()
    {
        _provider.Answer = "A";
        await using var host = CreateHost();
        var songId = await SeedDeezerSongAsync(host);
        var before = await ReadFreshAsync(host, songId);
        var sut = host.Resolve<ISongService>();

        var returned = await sut.EnsureLyricsAsync(songId, CancellationToken.None);

        var after = await ReadFreshAsync(host, songId);
        Assert.Equal("A", returned);
        Assert.Equal("A", after.Lyrics);
        Assert.Equal(LyricsOrigin.LyricsSource, after.LyricsOrigin);
        Assert.Equal(before.Title, after.Title);
        Assert.Equal(before.ArtistId, after.ArtistId);
        Assert.Equal(before.UpdatedAt, after.UpdatedAt);
    }

    [Fact]
    // [AC] REQ-SLD-24: once lyrics are stored, a second opening starts no lookup and returns null.
    public async Task EnsureLyricsAsync_SecondCall_ReturnsStoredLyricsWithoutARequest()
    {
        _provider.Answer = "A";
        await using var host = CreateHost();
        var songId = await SeedDeezerSongAsync(host);
        var sut = host.Resolve<ISongService>();

        await sut.EnsureLyricsAsync(songId, CancellationToken.None);
        var second = await sut.EnsureLyricsAsync(songId, CancellationToken.None);

        Assert.Equal(1, _provider.Calls);
        Assert.Null(second);
    }

    [Fact]
    // [AC] REQ-SLD-22: a definite no-match is recorded as origin NoneFound, and no lookup starts again for that song.
    public async Task EnsureLyricsAsync_NoMatch_PersistsNoneFoundAndNeverAsksAgain()
    {
        _provider.Answer = null;
        await using var host = CreateHost();
        var songId = await SeedDeezerSongAsync(host);
        var sut = host.Resolve<ISongService>();

        await sut.EnsureLyricsAsync(songId, CancellationToken.None);
        var stored = await ReadFreshAsync(host, songId);
        await sut.EnsureLyricsAsync(songId, CancellationToken.None);

        Assert.Equal(LyricsOrigin.NoneFound, stored.LyricsOrigin);
        Assert.True(string.IsNullOrWhiteSpace(stored.Lyrics));
        Assert.Equal(1, _provider.Calls);
    }
}
