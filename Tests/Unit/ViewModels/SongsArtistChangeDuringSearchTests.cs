using MyVocaList.Contracts.DTOs.List;
using MyVocaList.Navigation;
using MyVocaList.UI.Services;

namespace MyVocaList.Tests.Unit.ViewModels;

/// <summary>
/// BUG-086 guard kept under REQ-DLH-05: the shimmer flag is now also true during a search, so the artist-change
/// reload must not depend on it. After the first load, an artist change reloads even while a search is running.
/// </summary>
public class SongsArtistChangeDuringSearchTests
{
    private static readonly TimeSpan WaitLimit = TimeSpan.FromSeconds(5);

    private readonly Mock<ICatalogService> _catalogMock = new();
    private readonly Mock<ISongService> _songMock = new();
    private readonly Mock<ISnackbarComponent> _snackMock = new();
    private readonly Mock<INavigationService> _navigationMock = new();
    private readonly Mock<ILogger<SongsViewModel>> _loggerMock = new();

    private SongsViewModel CreateSut() =>
        new(_catalogMock.Object, _songMock.Object, _snackMock.Object, _navigationMock.Object, _loggerMock.Object);

    // [AC] REQ-DLH-05 (BUG-086 guard): an artist change while a search is in flight still reloads the catalog for the new artist.
    [Fact]
    public async Task ArtistIdChangedWhileSearchIsRunning_StillReloadsCatalogForNewArtist()
    {
        var searchGate = new TaskCompletionSource<(IEnumerable<SongListItemDto> items, int totalCount)>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var searchStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var catalogCalled = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

        _songMock.Setup(s => s.GetPagedSongsForListAsync(It.IsAny<int>(), It.IsAny<int>(),
                        It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .Returns<int, int, string, CancellationToken>((_, _, query, ct) =>
                 {
                     if (query != "abc")
                         return Task.FromResult<(IEnumerable<SongListItemDto>, int)>((Enumerable.Empty<SongListItemDto>(), 0));
                     searchStarted.TrySetResult();
                     return searchGate.Task.WaitAsync(ct);
                 });
        _catalogMock.Setup(c => c.GetPagedCatalogForArtistAsync(It.IsAny<int>(), It.IsAny<int>(),
                        It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .Callback<int, int, int, string, CancellationToken>((id, _, _, _, _) => catalogCalled.TrySetResult(id))
                    .ReturnsAsync((Enumerable.Empty<SongListItemDto>(), 0));

        var sut = CreateSut();
        try
        {
            await sut.InitializeAsync(); // first load completes

            sut.SearchText = "abc";
            await searchStarted.Task.WaitAsync(WaitLimit); // the search fetch is now in flight

            sut.ArtistIdRaw = "3";

            var loadedArtistId = await catalogCalled.Task.WaitAsync(WaitLimit);
            Assert.Equal(3, loadedArtistId);
        }
        finally
        {
            searchGate.TrySetResult((Enumerable.Empty<SongListItemDto>(), 0));
        }
    }
}
