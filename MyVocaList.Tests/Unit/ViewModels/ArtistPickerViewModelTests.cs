using CommunityToolkit.Mvvm.Messaging;
using MyVocaList.Contracts.DTOs;
using MyVocaList.Contracts.Messages;
using MyVocaList.UI.Services;
using MyVocaList.UI.ViewModels;

namespace MyVocaList.Tests.Unit.ViewModels;

public class ArtistPickerViewModelTests
{
    private static MusicSearchResultDto MakeResult(string artistName = "Artist A") =>
        new("id1", "musicbrainz", artistName, null, null);

    private ArtistPickerViewModel CreateSut(
        Mock<IMusicMetadataService>? service = null,
        Mock<IMessenger>? messenger = null,
        Mock<INavigationService>? navigation = null)
    {
        return new ArtistPickerViewModel(
            (service ?? new Mock<IMusicMetadataService>()).Object,
            (messenger ?? new Mock<IMessenger>()).Object,
            (navigation ?? new Mock<INavigationService>()).Object,
            new Mock<ILogger<ArtistPickerViewModel>>().Object);
    }

    // [AC] AC-2.1 — empty/whitespace search does not call service
    [Fact]
    public async Task SearchCommand_EmptyText_DoesNotCallService()
    {
        var service = new Mock<IMusicMetadataService>();
        var sut = CreateSut(service: service);
        sut.SearchText = "   ";

        await sut.SearchCommand.ExecuteAsync(null);

        service.Verify(s => s.SearchArtistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // [AC] AC-2.2 — IsLoading set before first await
    [Fact]
    public async Task SearchCommand_SetsIsLoadingBeforeAwait()
    {
        var tcs = new TaskCompletionSource<IEnumerable<MusicSearchResultDto>>();
        var service = new Mock<IMusicMetadataService>();
        service.Setup(s => s.SearchArtistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
               .Returns(tcs.Task);

        var sut = CreateSut(service: service);
        sut.SearchText = "Art";

        var task = sut.SearchCommand.ExecuteAsync(null);

        Assert.True(sut.IsLoading);

        tcs.SetResult([]);
        await task;
    }

    // [AC] AC-2.3 — prior results are cleared before new results arrive
    [Fact]
    public async Task SearchCommand_ClearsPriorResults()
    {
        var tcs = new TaskCompletionSource<IEnumerable<MusicSearchResultDto>>();
        var service = new Mock<IMusicMetadataService>();
        service.Setup(s => s.SearchArtistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
               .Returns(tcs.Task);

        var sut = CreateSut(service: service);
        sut.Results.Add(MakeResult("Old Artist"));
        sut.SearchText = "Art";

        var task = sut.SearchCommand.ExecuteAsync(null);

        Assert.Empty(sut.Results);

        tcs.SetResult([]);
        await task;
    }

    // [AC] AC-2.4 — successful search populates results
    [Fact]
    public async Task SearchCommand_OnSuccess_PopulatesResults()
    {
        var results = new[] { MakeResult("Artist A"), MakeResult("Artist B") };
        var service = new Mock<IMusicMetadataService>();
        service.Setup(s => s.SearchArtistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(results);

        var sut = CreateSut(service: service);
        sut.SearchText = "Art";

        await sut.SearchCommand.ExecuteAsync(null);

        Assert.Equal(2, sut.Results.Count);
        Assert.True(sut.HasResults);
        Assert.True(sut.HasSearched);
        Assert.False(sut.IsLoading);
    }

    // [AC] AC-2.5 — empty result sets HasSearched without HasResults
    [Fact]
    public async Task SearchCommand_OnEmptyResult_SetsHasSearchedNoResults()
    {
        var service = new Mock<IMusicMetadataService>();
        service.Setup(s => s.SearchArtistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync([]);

        var sut = CreateSut(service: service);
        sut.SearchText = "XYZ";

        await sut.SearchCommand.ExecuteAsync(null);

        Assert.False(sut.HasResults);
        Assert.True(sut.HasSearched);
    }

    // [AC] AC-2.6 — service exception sets error state
    [Fact]
    public async Task SearchCommand_OnException_SetsHasSearchedNoResults()
    {
        var service = new Mock<IMusicMetadataService>();
        service.Setup(s => s.SearchArtistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
               .ThrowsAsync(new HttpRequestException("Network error"));

        var sut = CreateSut(service: service);
        sut.SearchText = "Art";

        await sut.SearchCommand.ExecuteAsync(null);

        Assert.False(sut.HasResults);
        Assert.True(sut.HasSearched);
        Assert.False(sut.IsLoading);
        Assert.Contains("failed", sut.EmptyStateMessage, StringComparison.OrdinalIgnoreCase);
    }

    // [AC] AC-2.7 — selecting a result sends ArtistPickedMessage
    [Fact]
    public async Task SelectResultCommand_SendsArtistPickedMessage()
    {
        var realMessenger = new WeakReferenceMessenger();
        ArtistPickedMessage? received = null;
        realMessenger.Register<ArtistPickedMessage>(this, (_, msg) => received = msg);

        var navigation = new Mock<INavigationService>();
        navigation.Setup(n => n.GoBackAsync()).Returns(Task.CompletedTask);

        var sut = new ArtistPickerViewModel(
            new Mock<IMusicMetadataService>().Object,
            realMessenger,
            navigation.Object,
            new Mock<ILogger<ArtistPickerViewModel>>().Object);
        var result = MakeResult("Artist A");

        await sut.SelectResultCommand.ExecuteAsync(result);

        Assert.NotNull(received);
        Assert.Equal(result, received.Result);
    }

    // [AC] AC-2.7 — SelectResultCommand calls navigation GoBack
    [Fact]
    public async Task SelectResultCommand_CallsNavigationGoBack()
    {
        var navigation = new Mock<INavigationService>();
        navigation.Setup(n => n.GoBackAsync()).Returns(Task.CompletedTask);
        var sut = CreateSut(navigation: navigation);
        var result = MakeResult();

        await sut.SelectResultCommand.ExecuteAsync(result);

        navigation.Verify(n => n.GoBackAsync(), Times.Once);
    }

    // [AC] AC-2.7 — BackCommand calls navigation GoBack
    [Fact]
    public async Task BackCommand_CallsNavigationGoBack()
    {
        var navigation = new Mock<INavigationService>();
        navigation.Setup(n => n.GoBackAsync()).Returns(Task.CompletedTask);
        var sut = CreateSut(navigation: navigation);

        await sut.BackCommand.ExecuteAsync(null);

        navigation.Verify(n => n.GoBackAsync(), Times.Once);
    }
}
