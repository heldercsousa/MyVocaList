using MyVocaList.Contracts.DTOs.List;
using MyVocaList.UI.Services;

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
    private readonly Mock<INavigationService> _navigationMock = new();
    private readonly Mock<ILogger<SongsViewModel>> _loggerMock = new();

    private SongsViewModel CreateSut() =>
        new(_catalogMock.Object, _songMock.Object, _snackMock.Object, _navigationMock.Object, _loggerMock.Object);

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

    // BUG-086 round 3 (emulator): the catalog is pushed with its artist context APPLIED BEFORE the page appears
    // (IQueryAttributable), so the first load already targets that artist and the title is the artist name.
    // [AC] artists-songs AC-9.1: the catalog shows that artist's songs under the artist name.
    [Fact]
    public async Task ApplyQueryAttributes_BeforeFirstLoad_InitialLoadTargetsThatArtistWithTitle()
    {
        SetupEmptyGlobalAndCatalog();
        _catalogMock.Setup(c => c.GetPagedCatalogForArtistAsync(It.IsAny<int>(), It.IsAny<int>(),
                        It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Enumerable.Empty<SongListItemDto>(), 0));

        var sut = CreateSut();
        sut.ApplyQueryAttributes(new Dictionary<string, object>
        {
            ["artistId"] = "5",
            ["artistName"] = "Echo Choir",
        });
        await sut.InitializeAsync();

        _catalogMock.Verify(c => c.GetPagedCatalogForArtistAsync(5, 1, It.IsAny<int>(),
                        It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _songMock.Verify(s => s.GetPagedSongsForListAsync(It.IsAny<int>(), It.IsAny<int>(),
                        It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.True(sut.IsCatalogMode);
        Assert.Equal("Echo Choir", sut.AppBarTitle);
    }

    // BUG-086 round 3 (emulator): a catalog opened from an artist with zero catalog rows keeps the artist name in the
    // title (the empty list is the empty state, not a missing title).
    [Fact]
    public async Task ApplyQueryAttributes_ArtistWithZeroCatalogRows_KeepsNameAsTitle()
    {
        SetupEmptyGlobalAndCatalog();
        _catalogMock.Setup(c => c.GetPagedCatalogForArtistAsync(It.IsAny<int>(), It.IsAny<int>(),
                        It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Enumerable.Empty<SongListItemDto>(), 0));

        var sut = CreateSut();
        sut.ApplyQueryAttributes(new Dictionary<string, object> { ["artistId"] = "9", ["artistName"] = "Zed Quartet" });
        await sut.InitializeAsync();

        Assert.Equal("Zed Quartet", sut.AppBarTitle);
        Assert.True(sut.IsEmptyNoSongs);
    }

    // BUG-086 round 3 (emulator): in catalog mode the page shows a BACK arrow (not the hamburger), and the arrow pops
    // the pushed page back to the Artists list.
    [Fact]
    public void ApplyAppBarNavigation_CatalogMode_ShowsBackArrowThatPopsPage()
    {
        var sut = CreateSut();
        sut.ApplyQueryAttributes(new Dictionary<string, object> { ["artistId"] = "2", ["artistName"] = "Bravo Band" });
        _navigationMock.Setup(n => n.PopAsync()).Returns(Task.CompletedTask);

        sut.ApplyAppBarNavigation();
        sut.AppBarNavigationCommand.Execute(null);

        Assert.Equal("arrow_back_outlined", sut.AppBarNavigationIcon);
        _navigationMock.Verify(n => n.PopAsync(), Times.Once);
    }
}
