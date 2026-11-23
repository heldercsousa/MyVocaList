using MyVocaList.Contracts.DTOs.List;
using MyVocaList.UI.Services;

namespace MyVocaList.Tests.Unit.ViewModels;

/// <summary>
/// CATALOG-PROP: in catalog mode, Delete removes catalog rows through the bulk remove and never deletes songs.
/// </summary>
public class SongsViewModelCatalogDeleteTests
{
    private readonly Mock<ICatalogService> _catalogMock = new();
    private readonly Mock<ISongService> _songMock = new();
    private readonly Mock<ISnackbarComponent> _snackMock = new();
    private readonly Mock<INavigationService> _navigationMock = new();
    private readonly Mock<ILogger<SongsViewModel>> _loggerMock = new();

    private static SongListItemDto Song(int id, string title) => new(id, title, 1, "Ramones", null);

    private SongsViewModel CreateSut(bool catalogMode)
    {
        _songMock.Setup(s => s.GetPagedSongsForListAsync(It.IsAny<int>(), It.IsAny<int>(),
                        It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Enumerable.Empty<SongListItemDto>(), 0));
        _catalogMock.Setup(c => c.GetPagedCatalogForArtistAsync(It.IsAny<int>(), It.IsAny<int>(),
                        It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Enumerable.Empty<SongListItemDto>(), 0));
        var sut = new SongsViewModel(_catalogMock.Object, _songMock.Object, _snackMock.Object,
            _navigationMock.Object, _loggerMock.Object);
        if (catalogMode)
        {
            sut.ArtistIdRaw = "7";
            sut.ArtistName = "Ramones";
        }
        return sut;
    }

    private static void Select(SongsViewModel sut, params SongListItemDto[] songs)
    {
        sut.ItemLongPressCommand.Execute(songs[0]);
        foreach (var s in songs.Skip(1))
            sut.ToggleItemSelectionCommand.Execute(s);
    }

    [Fact]
    // [AC] REQ-CATPROP-08: the catalog confirmation for one song reads "Remove '<title>' from <A>'s catalog?"
    public void CatalogMode_OneSong_ConfirmTextNamesSongAndCatalog()
    {
        var sut = CreateSut(catalogMode: true);
        Select(sut, Song(1, "Blitzkrieg Bop"));

        sut.DeleteSelectedCommand.Execute(null);

        Assert.Equal("Remove 'Blitzkrieg Bop' from Ramones's catalog?", sut.ConfirmMessage);
    }

    [Fact]
    // [AC] REQ-CATPROP-08: the catalog confirmation for several songs reads "Remove <n> songs from <A>'s catalog?"
    public void CatalogMode_ThreeSongs_ConfirmTextCountsAll()
    {
        var sut = CreateSut(catalogMode: true);
        Select(sut, Song(1, "A"), Song(2, "B"), Song(3, "C"));

        sut.DeleteSelectedCommand.Execute(null);

        Assert.Equal("Remove 3 songs from Ramones's catalog?", sut.ConfirmMessage);
    }

    [Fact]
    // [AC] REQ-CATPROP-08: catalog-mode Delete calls the bulk remove and never DeleteSongsAsync; success shows the service message as a success snackbar
    public async Task CatalogMode_Confirm_CallsBulkRemoveNeverDeleteSongs_AndShowsSuccess()
    {
        var sut = CreateSut(catalogMode: true);
        _catalogMock.Setup(c => c.RemoveSongsFromCatalogAsync(7, It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((true, "Removed 2.", 2, 0));
        Select(sut, Song(1, "A"), Song(2, "B"));

        sut.DeleteSelectedCommand.Execute(null);
        await sut.ConfirmActionCommand.ExecuteAsync(null);

        _catalogMock.Verify(c => c.RemoveSongsFromCatalogAsync(7,
            It.Is<IReadOnlyCollection<int>>(ids => ids.OrderBy(i => i).SequenceEqual(new[] { 1, 2 })),
            It.IsAny<CancellationToken>()), Times.Once);
        _songMock.Verify(s => s.DeleteSongsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()), Times.Never);
        _snackMock.Verify(s => s.ShowSuccessAsync("Removed 2."), Times.Once);
        _snackMock.Verify(s => s.ShowErrorAsync(It.IsAny<string>()), Times.Never);
        _catalogMock.Verify(c => c.GetPagedCatalogForArtistAsync(7, It.IsAny<int>(), It.IsAny<int>(),
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    // [AC] REQ-CATPROP-08: when the bulk remove reports failure, the VM shows an error snackbar, still reloads the list, and never deletes songs
    public async Task CatalogMode_RemoveFails_ShowsErrorStillReloads_NeverDeletesSongs()
    {
        var sut = CreateSut(catalogMode: true);
        _catalogMock.Setup(c => c.RemoveSongsFromCatalogAsync(7, It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((false, "Kept 1 own song.", 0, 1));
        Select(sut, Song(1, "A"));

        sut.DeleteSelectedCommand.Execute(null);
        await sut.ConfirmActionCommand.ExecuteAsync(null);

        _snackMock.Verify(s => s.ShowErrorAsync("Kept 1 own song."), Times.Once);
        _snackMock.Verify(s => s.ShowSuccessAsync(It.IsAny<string>()), Times.Never);
        _catalogMock.Verify(c => c.GetPagedCatalogForArtistAsync(7, It.IsAny<int>(), It.IsAny<int>(),
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        _songMock.Verify(s => s.DeleteSongsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    // [AC] REQ-CATPROP-08: outside catalog mode Delete deletes songs as today (same confirmation, DeleteSongsAsync)
    public async Task SongsMode_Confirm_StillDeletesSongs()
    {
        var sut = CreateSut(catalogMode: false);
        _songMock.Setup(s => s.DeleteSongsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((true, "Deleted."));
        Select(sut, Song(1, "Blitzkrieg Bop"));

        sut.DeleteSelectedCommand.Execute(null);
        Assert.Equal("Delete 'Blitzkrieg Bop'?", sut.ConfirmMessage);
        await sut.ConfirmActionCommand.ExecuteAsync(null);

        _songMock.Verify(s => s.DeleteSongsAsync(It.Is<IEnumerable<int>>(ids => ids.SequenceEqual(new[] { 1 })),
            It.IsAny<CancellationToken>()), Times.Once);
        _catalogMock.Verify(c => c.RemoveSongsFromCatalogAsync(It.IsAny<int>(), It.IsAny<IReadOnlyCollection<int>>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _snackMock.Verify(s => s.ShowSuccessAsync("Deleted."), Times.Once);
    }
}
