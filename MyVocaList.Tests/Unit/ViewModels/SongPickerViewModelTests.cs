using CommunityToolkit.Mvvm.Messaging;
using MyVocaList.Contracts.DTOs;
using MyVocaList.Contracts.Messages;
using MyVocaList.UI.Services;
using MyVocaList.UI.ViewModels;

namespace MyVocaList.Tests.Unit.ViewModels;

public class SongPickerViewModelTests
{
    private static MusicSearchResultDto MakeResult(string title = "Song A", string? songTitle = "Song A") =>
        new("id1", "musicbrainz", "Artist A", songTitle, null);

    private SongPickerViewModel CreateSut(
        Mock<IMusicMetadataService>? service = null,
        Mock<IMessenger>? messenger = null,
        Mock<INavigationService>? navigation = null)
    {
        return new SongPickerViewModel(
            (service ?? new Mock<IMusicMetadataService>()).Object,
            (messenger ?? new Mock<IMessenger>()).Object,
            (navigation ?? new Mock<INavigationService>()).Object,
            new Mock<ILogger<SongPickerViewModel>>().Object);
    }

    // [AC] AC-2.1 — empty/whitespace search does not call service
    [Fact]
    public async Task SearchCommand_EmptyText_DoesNotCallService()
    {
        var service = new Mock<IMusicMetadataService>();
        var sut = CreateSut(service: service);
        sut.SearchText = "";

        await sut.SearchCommand.ExecuteAsync(null);

        service.Verify(s => s.SearchSongsAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // [AC] AC-2.2 — IsLoading set before first await
    [Fact]
    public async Task SearchCommand_SetsIsLoadingBeforeAwait()
    {
        var tcs = new TaskCompletionSource<IEnumerable<MusicSearchResultDto>>();
        var service = new Mock<IMusicMetadataService>();
        service.Setup(s => s.SearchSongsAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
               .Returns(tcs.Task);

        var sut = CreateSut(service: service);
        sut.SearchText = "Song";

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
        service.Setup(s => s.SearchSongsAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
               .Returns(tcs.Task);

        var sut = CreateSut(service: service);
        sut.Results.Add(MakeResult("Old Song"));
        sut.SearchText = "Song";

        var task = sut.SearchCommand.ExecuteAsync(null);

        Assert.Empty(sut.Results);

        tcs.SetResult([]);
        await task;
    }

    // [AC] AC-2.4 — successful search populates results
    [Fact]
    public async Task SearchCommand_OnSuccess_PopulatesResults()
    {
        var results = new[] { MakeResult("Song A"), MakeResult("Song B") };
        var service = new Mock<IMusicMetadataService>();
        service.Setup(s => s.SearchSongsAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(results);

        var sut = CreateSut(service: service);
        sut.SearchText = "Song";

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
        service.Setup(s => s.SearchSongsAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
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
        service.Setup(s => s.SearchSongsAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
               .ThrowsAsync(new HttpRequestException("Network error"));

        var sut = CreateSut(service: service);
        sut.SearchText = "Song";

        await sut.SearchCommand.ExecuteAsync(null);

        Assert.False(sut.HasResults);
        Assert.True(sut.HasSearched);
        Assert.False(sut.IsLoading);
        Assert.Contains("failed", sut.EmptyStateMessage, StringComparison.OrdinalIgnoreCase);
    }

    // [AC] AC-2.7 — selecting a result sends SongPickedMessage
    [Fact]
    public async Task SelectResultCommand_SendsSongPickedMessage()
    {
        var realMessenger = new WeakReferenceMessenger();
        SongPickedMessage? received = null;
        realMessenger.Register<SongPickedMessage>(this, (_, msg) => received = msg);

        var navigation = new Mock<INavigationService>();
        navigation.Setup(n => n.GoBackAsync()).Returns(Task.CompletedTask);

        var sut = new SongPickerViewModel(
            new Mock<IMusicMetadataService>().Object,
            realMessenger,
            navigation.Object,
            new Mock<ILogger<SongPickerViewModel>>().Object);
        var result = MakeResult("Song A");

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

    // [AC] AC-2.8 — null SongTitle in result does not throw
    [Fact]
    public async Task SearchCommand_SongTitleNull_DoesNotThrow()
    {
        var results = new[] { MakeResult(songTitle: null) };
        var service = new Mock<IMusicMetadataService>();
        service.Setup(s => s.SearchSongsAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(results);

        var sut = CreateSut(service: service);
        sut.SearchText = "Song";

        var ex = await Record.ExceptionAsync(() => sut.SearchCommand.ExecuteAsync(null));

        Assert.Null(ex);
        Assert.Single(sut.Results);
    }
}
