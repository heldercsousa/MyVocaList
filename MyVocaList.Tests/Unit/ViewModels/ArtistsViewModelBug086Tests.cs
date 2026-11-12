using MyVocaList.Domain.ReadModels;
using MyVocaList.Navigation;
using MyVocaList.UI.Services;

namespace MyVocaList.Tests.Unit.ViewModels;

/// <summary>
/// Regression tests for BUG-086: the trailing catalog button on an artist row did nothing.
/// The old handler fire-and-forgot a relative Shell GoToAsync to the hidden top-level "songs"
/// flyout route (its exception was discarded). The fix awaits navigation through INavigationService
/// with an ABSOLUTE route carrying the artist context.
/// </summary>
public class ArtistsViewModelBug086Tests
{
    private readonly Mock<IArtistService> _artistServiceMock = new();
    private readonly Mock<ISnackbarComponent> _snackMock = new();
    private readonly Mock<INavigationService> _navigationMock = new();
    private readonly Mock<ILogger<ArtistsViewModel>> _loggerMock = new();

    private ArtistsViewModel CreateSut() =>
        new(_artistServiceMock.Object, _snackMock.Object, _navigationMock.Object, _loggerMock.Object);

    // [AC] artists-songs AC-9.1: When the user taps the catalog-navigation icon button on an artist row,
    // the app shall navigate to the Songs page filtered to that artist's Catalog. (BUG-086 regression:
    // the route must be absolute and awaited through INavigationService.)
    [Fact]
    public async Task ViewCatalog_NavigatesToAbsoluteSongsRouteWithArtistContext()
    {
        var sut = CreateSut();
        var artist = new ArtistListItem(7, "Helder & Co", "Local", 3);
        _navigationMock.Setup(n => n.GoToAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

        await sut.ViewCatalogCommand.ExecuteAsync(artist);

        var expected = $"//{Routes.Songs}?artistId=7&artistName={Uri.EscapeDataString("Helder & Co")}";
        _navigationMock.Verify(n => n.GoToAsync(expected), Times.Once);
    }

    // BUG-086 regression: a failing navigation must surface to the caller (awaited), never be discarded.
    [Fact]
    public async Task ViewCatalog_NavigationFailure_PropagatesToCaller()
    {
        var sut = CreateSut();
        var artist = new ArtistListItem(7, "Helder", "Local", 0);
        _navigationMock.Setup(n => n.GoToAsync(It.IsAny<string>()))
                       .ThrowsAsync(new InvalidOperationException("route not found"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ViewCatalogCommand.ExecuteAsync(artist));
    }
}
