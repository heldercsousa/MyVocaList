using Microsoft.Extensions.DependencyInjection;
using MyVocaList.Contracts.DTOs;
using MyVocaList.UI.Services;

namespace MyVocaList.Tests.Unit.Services;

/// <summary>REQ-MSRC-11: a YouTube link shared into the app opens the Song form (edit mode when the video is stored, else add mode).</summary>
public class SharedLinkInboxTests
{
    private const string Link = "https://youtu.be/dQw4w9WgXcQ";

    private readonly Mock<ISharedVideoService> _sharedVideo = new();
    private readonly Mock<INavigationService> _navigation = new();

    /// <summary>Test seam: no Shell, no main thread; readiness is scripted.</summary>
    private sealed class TestInbox(
        IServiceScopeFactory scopes, INavigationService navigation, ILogger<SharedLinkInbox> logger, Func<bool> ready)
        : SharedLinkInbox(scopes, navigation, logger)
    {
        protected override bool IsNavigationReady => ready();
        protected override TimeSpan ReadyPollInterval => TimeSpan.FromMilliseconds(1);
        protected override Task RunOnMainThreadAsync(Func<Task> action) => action();
    }

    private TestInbox CreateSut(Func<bool>? ready = null)
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => _sharedVideo.Object);
        var provider = services.BuildServiceProvider();
        return new TestInbox(
            provider.GetRequiredService<IServiceScopeFactory>(), _navigation.Object,
            new Mock<ILogger<SharedLinkInbox>>().Object, ready ?? (() => true));
    }

    private void Resolves(int? songId)
        => _sharedVideo.Setup(s => s.ResolveTargetAsync(Link, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SharedVideoTarget("dQw4w9WgXcQ", songId));

    // [AC] REQ-MSRC-11: a video no stored song holds opens the add form, and the link waits for the form to take it
    [Fact]
    public async Task Receive_UnknownVideo_OpensAddFormAndKeepsLinkPending()
    {
        Resolves(null);
        var sut = CreateSut();

        sut.Receive(Link);
        await sut.RoutingTask;

        _navigation.Verify(n => n.GoToAsync("song-form"), Times.Once);
        Assert.Equal(Link, sut.TakePending());
    }

    // [AC] REQ-MSRC-11: a video a stored song holds opens that song's edit form, and nothing stays pending
    [Fact]
    public async Task Receive_StoredVideo_OpensEditFormAndClearsPending()
    {
        Resolves(42);
        var sut = CreateSut();

        sut.Receive(Link);
        await sut.RoutingTask;

        _navigation.Verify(n => n.GoToAsync("song-form?songId=42"), Times.Once);
        Assert.Null(sut.TakePending());
    }

    // [AC] REQ-MSRC-11: shared text with no YouTube link opens nothing
    [Fact]
    public async Task Receive_NotAYouTubeLink_DoesNotNavigate()
    {
        _sharedVideo.Setup(s => s.ResolveTargetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SharedVideoTarget?)null);
        var sut = CreateSut();

        sut.Receive("hello");
        await sut.RoutingTask;

        _navigation.Verify(n => n.GoToAsync(It.IsAny<string>()), Times.Never);
        Assert.Null(sut.TakePending());
    }

    // [AC] REQ-MSRC-11: a form already open takes the link itself; the app does not navigate
    [Fact]
    public async Task Receive_FormListening_RaisesEventWithoutNavigating()
    {
        var sut = CreateSut();
        var raised = 0;
        sut.LinkReceived += (_, _) => raised++;

        sut.Receive(Link);
        await sut.RoutingTask;

        Assert.Equal(1, raised);
        _navigation.Verify(n => n.GoToAsync(It.IsAny<string>()), Times.Never);
        Assert.Equal(Link, sut.TakePending());
    }

    // [AC] REQ-MSRC-11: a share that starts the app waits for the Shell before it navigates
    [Fact]
    public async Task Receive_ShellNotReadyYet_WaitsThenNavigates()
    {
        Resolves(null);
        var polls = 0;
        var sut = CreateSut(ready: () => ++polls > 3);

        sut.Receive(Link);
        await sut.RoutingTask;

        Assert.True(polls > 3);
        _navigation.Verify(n => n.GoToAsync("song-form"), Times.Once);
    }

    // [AC] REQ-MSRC-11: blank text is ignored
    [Fact]
    public async Task Receive_BlankText_Ignored()
    {
        var sut = CreateSut();

        sut.Receive("  ");
        await sut.RoutingTask;

        _navigation.Verify(n => n.GoToAsync(It.IsAny<string>()), Times.Never);
        Assert.Null(sut.TakePending());
    }
}
