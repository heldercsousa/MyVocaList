using MyVocaList.Contracts.DTOs;
using MyVocaList.Contracts.Enums;
using MyVocaList.Domain.UnitOfWork;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Unit.Services;

/// <summary>
/// Lookup on open (REQ-SLD-19, 20, 22 to 24, 29): <c>EnsureLyricsAsync</c> asks the lyrics source for a Deezer song
/// with blank lyrics and no recorded origin, stores the answer, and never holds a unit of work open while waiting.
/// Moq repositories and lyrics service, <c>PassthroughUnitOfWork.Over</c> unless a test needs scope tracking; no database.
/// </summary>
public class SongServiceEnsureLyricsTests
{
    private readonly Mock<ISongRepository> _songRepoMock = new();
    private readonly Mock<ICatalogRepository> _catalogRepoMock = new();
    private readonly Mock<IArtistRepository> _artistRepoMock = new();
    private readonly Mock<ISongKaraokeUrlRepository> _urlRepoMock = new();
    private readonly Mock<ISongKaraokeUrlService> _urlServiceMock = new();
    private readonly Mock<ISongLyricsService> _lyricsMock = new();
    private readonly Mock<ILogger<SongService>> _loggerMock = new();

    private const int SongId = 50;
    private const int ArtistId = 7;
    private const string StoredTitle = "Stored Title";
    private const string StoredArtistName = "Queen";
    private const string DeezerProvider = "Deezer";
    private const string DeezerTrackId = "3135556";
    private const string FoundLyrics = "We will rock you";

    private SongService CreateSut(IUnitOfWork? uow = null) => new(
        _songRepoMock.Object,
        _artistRepoMock.Object,
        _urlRepoMock.Object,
        _urlServiceMock.Object,
        uow ?? PassthroughUnitOfWork.Over(
            _songRepoMock, _artistRepoMock, _urlRepoMock, _urlServiceMock, _catalogRepoMock.Object, _lyricsMock),
        _loggerMock.Object);

    private static Song MakeSong(
        string? provider = DeezerProvider,
        string? externalId = DeezerTrackId,
        string? lyrics = null,
        LyricsOrigin? origin = null,
        string? artistName = StoredArtistName)
        => new()
        {
            Id = SongId,
            ArtistId = ArtistId,
            Title = StoredTitle,
            Lyrics = lyrics,
            LyricsOrigin = origin,
            ExternalProvider = provider,
            ExternalId = externalId,
            OriginalArtist = artistName is null ? null : new Artist { Id = ArtistId, Name = artistName }
        };

    private void SetupRead(Song song)
        => _songRepoMock
            .Setup(r => r.GetByIdAsync(SongId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(song);

    private void SetupLookup(LyricsLookupResult answer)
        => _lyricsMock
            .Setup(l => l.LookupLyricsAsync(It.IsAny<LyricsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(answer);

    private void VerifyNothingStored()
        => _songRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Song>(), It.IsAny<CancellationToken>()), Times.Never);

    private void VerifyNoLookup()
        => _lyricsMock.Verify(
            l => l.LookupLyricsAsync(It.IsAny<LyricsQuery>(), It.IsAny<CancellationToken>()), Times.Never);

    [Fact]
    // [AC] REQ-SLD-19: a Deezer song with blank lyrics and no origin starts one lookup with the stored title and artist name.
    public async Task EnsureLyricsAsync_DeezerSongBlankLyricsNoOrigin_LooksUpWithStoredTitleAndArtist()
    {
        SetupRead(MakeSong());
        SetupLookup(LyricsLookupResult.NoAnswer);

        await CreateSut().EnsureLyricsAsync(SongId);

        _lyricsMock.Verify(
            l => l.LookupLyricsAsync(new LyricsQuery(StoredTitle, StoredArtistName, null, null), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    // [AC] REQ-SLD-20: a match is stored with origin lyrics source, and the stored lyrics are returned.
    public async Task EnsureLyricsAsync_Found_StoresLyricsWithOriginLyricsSource()
    {
        SetupRead(MakeSong());
        SetupLookup(LyricsLookupResult.Found(FoundLyrics));

        var result = await CreateSut().EnsureLyricsAsync(SongId);

        Assert.Equal(FoundLyrics, result);
        _songRepoMock.Verify(r => r.UpdateAsync(
            It.Is<Song>(s => s.Lyrics == FoundLyrics && s.LyricsOrigin == LyricsOrigin.LyricsSource),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    // [AC] REQ-SLD-20: a song changed while the lookup ran (lyrics typed or origin recorded) is not overwritten.
    [InlineData("Typed by the user", null)]
    [InlineData(null, LyricsOrigin.User)]
    public async Task EnsureLyricsAsync_SongChangedMeanwhile_StoresNothing(string? changedLyrics, LyricsOrigin? changedOrigin)
    {
        _songRepoMock
            .SetupSequence(r => r.GetByIdAsync(SongId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeSong())
            .ReturnsAsync(MakeSong(lyrics: changedLyrics, origin: changedOrigin));
        SetupLookup(LyricsLookupResult.Found(FoundLyrics));

        var result = await CreateSut().EnsureLyricsAsync(SongId);

        Assert.Equal(changedLyrics, result);
        VerifyNothingStored();
    }

    [Fact]
    // [AC] REQ-SLD-20: a match that arrives after the caller cancelled is still stored (the store step is not cancellable).
    public async Task EnsureLyricsAsync_CancelledAfterTheAnswer_StillStores()
    {
        using var cts = new CancellationTokenSource();
        SetupRead(MakeSong());
        _lyricsMock
            .Setup(l => l.LookupLyricsAsync(It.IsAny<LyricsQuery>(), It.IsAny<CancellationToken>()))
            .Returns((LyricsQuery _, CancellationToken __) =>
            {
                cts.Cancel();
                return Task.FromResult(LyricsLookupResult.Found(FoundLyrics));
            });

        var result = await CreateSut().EnsureLyricsAsync(SongId, cts.Token);

        Assert.Equal(FoundLyrics, result);
        _songRepoMock.Verify(r => r.UpdateAsync(
            It.Is<Song>(s => s.Lyrics == FoundLyrics && s.LyricsOrigin == LyricsOrigin.LyricsSource),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    // [AC] REQ-SLD-22: a definite no match stores origin none found, no lyrics, and returns null.
    public async Task EnsureLyricsAsync_NoMatch_StoresOriginNoneFoundAndReturnsNull()
    {
        SetupRead(MakeSong());
        SetupLookup(LyricsLookupResult.NoMatch);

        var result = await CreateSut().EnsureLyricsAsync(SongId);

        Assert.Null(result);
        _songRepoMock.Verify(r => r.UpdateAsync(
            It.Is<Song>(s => s.Lyrics == null && s.LyricsOrigin == LyricsOrigin.NoneFound),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    // [AC] REQ-SLD-23: a lookup with no answer stores nothing and returns null, so the next open tries again.
    public async Task EnsureLyricsAsync_NoAnswer_StoresNothing()
    {
        SetupRead(MakeSong());
        SetupLookup(LyricsLookupResult.NoAnswer);

        var result = await CreateSut().EnsureLyricsAsync(SongId);

        Assert.Null(result);
        VerifyNothingStored();
    }

    [Fact]
    // [AC] REQ-SLD-23: a lookup cancelled by the caller rethrows OperationCanceledException and stores nothing.
    public async Task EnsureLyricsAsync_CancelledDuringTheLookup_RethrowsAndStoresNothing()
    {
        using var cts = new CancellationTokenSource();
        SetupRead(MakeSong());
        _lyricsMock
            .Setup(l => l.LookupLyricsAsync(It.IsAny<LyricsQuery>(), It.IsAny<CancellationToken>()))
            .Returns(async (LyricsQuery _, CancellationToken t) =>
            {
                cts.Cancel();
                t.ThrowIfCancellationRequested();
                return LyricsLookupResult.NoAnswer;
            });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => CreateSut().EnsureLyricsAsync(SongId, cts.Token));

        VerifyNothingStored();
    }

    [Theory]
    // [AC] REQ-SLD-24: a local-only song, a song with lyrics, a song whose origin is user or none found, a song without
    // a stored artist name, or a song that does not exist starts no lookup.
    [InlineData("local only")]
    [InlineData("has lyrics")]
    [InlineData("origin user")]
    [InlineData("origin none found")]
    [InlineData("blank artist name")]
    [InlineData("song not found")]
    public async Task EnsureLyricsAsync_OutOfScopeSong_StartsNoLookup(string scenario)
    {
        switch (scenario)
        {
            case "local only":
                SetupRead(MakeSong(provider: null, externalId: null));
                break;
            case "has lyrics":
                SetupRead(MakeSong(lyrics: "Existing words"));
                break;
            case "origin user":
                SetupRead(MakeSong(origin: LyricsOrigin.User));
                break;
            case "origin none found":
                SetupRead(MakeSong(origin: LyricsOrigin.NoneFound));
                break;
            case "blank artist name":
                SetupRead(MakeSong(artistName: null));
                break;
            default:
                _songRepoMock
                    .Setup(r => r.GetByIdAsync(SongId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Song)null!);
                break;
        }

        var result = await CreateSut().EnsureLyricsAsync(SongId);

        Assert.Null(result);
        VerifyNoLookup();
        VerifyNothingStored();
    }

    [Fact]
    // [AC] REQ-SLD-20: the lookup runs while no unit-of-work scope is open, so no context or transaction waits on it.
    public async Task EnsureLyricsAsync_LookupRunsOutsideAnyUnitOfWork()
    {
        var passthrough = PassthroughUnitOfWork.Over(
            _songRepoMock, _artistRepoMock, _urlRepoMock, _urlServiceMock, _catalogRepoMock.Object, _lyricsMock);
        var uow = new ScopeTrackingUnitOfWork(passthrough);
        SetupRead(MakeSong());
        var scopesDuringLookup = -1;
        _lyricsMock
            .Setup(l => l.LookupLyricsAsync(It.IsAny<LyricsQuery>(), It.IsAny<CancellationToken>()))
            .Returns((LyricsQuery _, CancellationToken __) =>
            {
                scopesDuringLookup = uow.ActiveScopes;
                return Task.FromResult(LyricsLookupResult.Found(FoundLyrics));
            });

        await CreateSut(uow).EnsureLyricsAsync(SongId);

        _lyricsMock.Verify(
            l => l.LookupLyricsAsync(It.IsAny<LyricsQuery>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(0, scopesDuringLookup);
    }

    [Fact]
    // [AC] REQ-SLD-23: no lyrics service registered in the scope means no lookup, nothing stored, and null returned.
    public async Task EnsureLyricsAsync_NoLyricsServiceRegistered_ReturnsNullWithoutARequest()
    {
        SetupRead(MakeSong());
        // A provider that answers null for types it does not hold, as the non-throwing GetService does in production.
        var provider = new NullableServiceProvider(_songRepoMock.Object, _artistRepoMock.Object,
            _urlRepoMock.Object, _urlServiceMock.Object);
        var sut = CreateSut(new PassthroughUnitOfWork(provider));

        var result = await sut.EnsureLyricsAsync(SongId);

        Assert.Null(result);
        VerifyNothingStored();
    }

    /// <summary>Returns the given instances and null for any other service type (the non-throwing lookup).</summary>
    private sealed class NullableServiceProvider(params object[] instances) : IServiceProvider
    {
        public object? GetService(Type serviceType)
            => instances.FirstOrDefault(serviceType.IsInstanceOfType);
    }

    /// <summary>Wraps a unit of work and counts the scope bodies that are running at any moment.</summary>
    private sealed class ScopeTrackingUnitOfWork(IUnitOfWork inner) : IUnitOfWork
    {
        public int ActiveScopes { get; private set; }

        public async Task<TResult> ExecuteAsync<TResult>(
            Func<IServiceProvider, Task<TResult>> body, CancellationToken ct = default)
        {
            ActiveScopes++;
            try { return await inner.ExecuteAsync(body, ct); }
            finally { ActiveScopes--; }
        }

        public async Task ExecuteAsync(Func<IServiceProvider, Task> body, CancellationToken ct = default)
        {
            ActiveScopes++;
            try { await inner.ExecuteAsync(body, ct); }
            finally { ActiveScopes--; }
        }

        public async Task<TResult> ExecuteReadAsync<TResult>(
            Func<IServiceProvider, Task<TResult>> body, CancellationToken ct = default)
        {
            ActiveScopes++;
            try { return await inner.ExecuteReadAsync(body, ct); }
            finally { ActiveScopes--; }
        }

        public Task FlushAsync(CancellationToken ct = default) => inner.FlushAsync(ct);
    }
}
