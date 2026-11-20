using MyVocaList.Domain.ReadModels;
using MyVocaList.UI.Services;

namespace MyVocaList.Tests.Unit.ViewModels;

public class ArtistsViewModelDeleteConfirmTests
{
    private readonly Mock<IArtistService> _artistServiceMock = new();
    private readonly Mock<ISnackbarComponent> _snackMock = new();
    private readonly Mock<INavigationService> _navigationMock = new();
    private readonly Mock<ILogger<ArtistsViewModel>> _loggerMock = new();

    private ArtistsViewModel CreateSut() =>
        new(_artistServiceMock.Object, _snackMock.Object, _navigationMock.Object, _loggerMock.Object);

    [Fact]
    // [AC] REQ-CATPROP-10: the confirmation for one artist reads "Delete '<Name>'? Their catalog entries will also be removed."
    public void OneArtist_ConfirmTextMentionsCatalogEntries()
    {
        var sut = CreateSut();
        sut.ItemLongPressCommand.Execute(new ArtistListItem(1, "Ramones", "Local", 0));

        sut.DeleteSelectedCommand.Execute(null);

        Assert.Equal("Delete 'Ramones'? Their catalog entries will also be removed.", sut.ConfirmMessage);
    }

    [Fact]
    // [AC] REQ-CATPROP-10: the confirmation for several artists reads "Delete <n> artists? Their catalog entries will also be removed."
    public void TwoArtists_ConfirmTextMentionsCatalogEntries()
    {
        var sut = CreateSut();
        sut.ItemLongPressCommand.Execute(new ArtistListItem(1, "Ramones", "Local", 0));
        sut.ToggleItemSelectionCommand.Execute(new ArtistListItem(2, "Blondie", "Local", 0));

        sut.DeleteSelectedCommand.Execute(null);

        Assert.Equal("Delete 2 artists? Their catalog entries will also be removed.", sut.ConfirmMessage);
    }
}
