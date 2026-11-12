using MyVocaList.Contracts.DTOs.List;

namespace MyVocaList.Tests.Unit.ViewModels;

/// <summary>
/// Regression tests for BUG-086: a Songs page already on screen (Shell-cached) that receives a new
/// artist context via the query must reload its list instead of keeping the previous artist's rows.
/// </summary>
public class SongsViewModelBug086Tests
{
    private readonly Mock<ICatalogService> _catalogMock = new();
    private readonly Mock<ISongService> _songMock = new();
    private readonly Mock<ISnackbarComponent> _snackMock = new();
    private readonly Mock<ILogger<SongsViewModel>> _loggerMock = new();

    private SongsViewModel CreateSut() =>
        new(_catalogMock.Object, _songMock.Object, _snackMock.Object, _loggerMock.Object);

    private void SetupEmptyGlobalAndCatalog()
    {
        _songMock.Setup(s => s.GetPagedSongsForListAsync(It.IsAny<int>(), It.IsAny<int>(),
                        It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Enumerable.Empty<SongListItemDto>(), 0));
    }

    // BUG-086 regression: after the first load, a changed artistId query must reload the catalog for that artist.
    // (Bug record: no AC covers the reload itself; AC-9.1 is covered by ArtistsViewModelBug086Tests.)
    [Fact]
    public async Task ArtistIdChangedAfterFirstLoad_ReloadsCatalogForNewArtist()
    {
        SetupEmptyGlobalAndCatalog();
        var catalogCalled = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        _catalogMock.Setup(c => c.GetPagedCatalogForArtistAsync(It.IsAny<int>(), It.IsAny<int>(),
                        It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .Callback<int, int, int, string, CancellationToken>((id, _, _, _, _) => catalogCalled.TrySetResult(id))
                    .ReturnsAsync((Enumerable.Empty<SongListItemDto>(), 0));

        var sut = CreateSut();
        await sut.InitializeAsync(); // first (global) load completes; IsInitialLoading becomes false

        sut.ArtistIdRaw = "3";

        var loadedArtistId = await catalogCalled.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(3, loadedArtistId);
        Assert.True(sut.IsCatalogMode);
    }

    // BUG-086 guard: an artistId arriving before the first load must not trigger a second, competing load;
    // InitializeAsync (OnAppearing) performs that first load with the context already applied.
    [Fact]
    public async Task ArtistIdSetBeforeFirstLoad_DoesNotPreLoad_InitializeLoadsCatalog()
    {
        SetupEmptyGlobalAndCatalog();
        _catalogMock.Setup(c => c.GetPagedCatalogForArtistAsync(It.IsAny<int>(), It.IsAny<int>(),
                        It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Enumerable.Empty<SongListItemDto>(), 0));

        var sut = CreateSut();
        sut.ArtistIdRaw = "5";
        await sut.InitializeAsync();

        _catalogMock.Verify(c => c.GetPagedCatalogForArtistAsync(5, 1, It.IsAny<int>(),
                        It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _songMock.Verify(s => s.GetPagedSongsForListAsync(It.IsAny<int>(), It.IsAny<int>(),
                        It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // BUG-086 regression (round 2, reproduced on emulator): the Shell applies query values in URL order, so
    // artistId lands before artistName. The title must be re-notified when ArtistName arrives; otherwise the
    // bound title keeps the PREVIOUS artist's name (tapping Bravo's catalog showed "Alpha Artist").
    [Fact]
    public async Task ArtistNameArrivingAfterArtistId_RaisesAppBarTitleWithNewName()
    {
        SetupEmptyGlobalAndCatalog();
        _catalogMock.Setup(c => c.GetPagedCatalogForArtistAsync(It.IsAny<int>(), It.IsAny<int>(),
                        It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Enumerable.Empty<SongListItemDto>(), 0));

        var sut = CreateSut();
        await sut.InitializeAsync();
        sut.ArtistIdRaw = "1";
        sut.ArtistName = "Alpha Artist";

        var notifiedTitles = new List<string>();
        sut.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SongsViewModel.AppBarTitle))
                notifiedTitles.Add(sut.AppBarTitle);
        };

        sut.ArtistIdRaw = "2";
        sut.ArtistName = "Bravo Band";

        Assert.Equal("Bravo Band", sut.AppBarTitle);
        Assert.Equal("Bravo Band", notifiedTitles[^1]);
    }
}
