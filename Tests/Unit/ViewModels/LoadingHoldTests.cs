using CommunityToolkit.Mvvm.Messaging;
using MyVocaList.Contracts.DTOs.Suggestions;
using MyVocaList.Domain.Resolution;
using MyVocaList.Navigation;
using MyVocaList.Tests.Infrastructure;
using MyVocaList.UI.Collections;
using MyVocaList.UI.Services;

namespace MyVocaList.Tests.Unit.ViewModels;

/// <summary>Shared collection: the hold is a static, so tests that set it never run beside another test.</summary>
[CollectionDefinition(LoadingHoldCollection.Name, DisableParallelization = true)]
public sealed class LoadingHoldCollection
{
    public const string Name = "LoadingHold";
}

/// <summary>
/// The debug-only shimmer hold (REQ-DLH-01..03): zero duration returns at once; with a duration the first load of a
/// CRUD list and the edit-mode load of the song form do not end before it. The duration is reset in <c>finally</c>.
/// </summary>
[Collection(LoadingHoldCollection.Name)]
public class LoadingHoldTests
{
    private static readonly TimeSpan HoldForTest = TimeSpan.FromMilliseconds(200);
    private static readonly TimeSpan EarlyProbe = TimeSpan.FromMilliseconds(50);
    private static readonly TimeSpan WaitLimit = TimeSpan.FromSeconds(5);

    // [AC] REQ-DLH-03: with the zero default (no start-up switch, as in every unit test) the hold returns at once.
    [Fact]
    public async Task WaitAsync_ZeroDuration_ReturnsCompletedTaskAtOnce()
    {
        var original = LoadingHold.Duration;
        try
        {
            LoadingHold.Duration = TimeSpan.Zero;

            var wait = LoadingHold.WaitAsync(CancellationToken.None);

            Assert.True(wait.IsCompletedSuccessfully);
            await wait;
        }
        finally
        {
            LoadingHold.Duration = original;
        }
    }

    // [AC] REQ-DLH-01: the first load of a CRUD list stays in its loading state for at least the hold time.
    [Fact]
    public async Task ListFirstLoad_WithHold_DoesNotEndBeforeTheHold()
    {
        var original = LoadingHold.Duration;
        try
        {
            LoadingHold.Duration = HoldForTest;
            var sut = new HoldTestListViewModel();

            var init = sut.InitializeAsync();
            await Task.Delay(EarlyProbe);

            Assert.True(sut.IsInitialLoading);
            Assert.False(init.IsCompleted);

            await init.WaitAsync(WaitLimit);
            Assert.False(sut.IsInitialLoading);
        }
        finally
        {
            LoadingHold.Duration = original;
        }
    }

    // [AC] REQ-DLH-01: the edit-mode load of the song form keeps IsSongLoading true for at least the hold time.
    [Fact]
    public async Task SongEditLoad_WithHold_IsSongLoadingStaysTrueUntilTheHoldEnds()
    {
        var original = LoadingHold.Duration;
        try
        {
            LoadingHold.Duration = HoldForTest;
            var sut = CreateSongSut(StoredSong());

            sut.SongIdRaw = StoredSong().Id.ToString();
            await Task.Delay(EarlyProbe);

            Assert.True(sut.IsSongLoading);

            await WaitUntilAsync(() => !sut.IsSongLoading);
            Assert.False(sut.IsSongLoading);
        }
        finally
        {
            LoadingHold.Duration = original;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static Song StoredSong() => new()
    {
        Id = 43,
        ArtistId = 8,
        OriginalArtist = new Artist { Id = 8, Name = "Ramones" },
        Title = "Blitzkrieg Bop",
    };

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow + WaitLimit;
        while (!condition() && DateTime.UtcNow < deadline)
            await Task.Delay(10);
    }

    private static SongFormViewModel CreateSongSut(Song song)
    {
        var songService = new Mock<ISongService>();
        songService.Setup(s => s.ValidateTitleInput(It.IsAny<string>())).Returns((true, string.Empty));
        songService.Setup(s => s.GetSongByIdAsync(song.Id, It.IsAny<CancellationToken>())).ReturnsAsync(song);

        var suggestions = new Mock<ISongSuggestionService>();
        suggestions
            .Setup(s => s.SearchTitleLocalAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        suggestions
            .Setup(s => s.SearchTitleRemoteAsync(It.IsAny<string>(), It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SongRemoteSearchResult([], [], RemoteSearchStatus.Ok));

        var urls = new Mock<ISongKaraokeUrlService>();
        urls.Setup(s => s.GetUrlsForSongAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var sut = new SongFormViewModel(
            new Mock<IArtistService>().Object,
            songService.Object,
            new Mock<ISnackbarComponent>().Object,
            new Mock<ILogger<SongFormViewModel>>().Object,
            urls.Object,
            new Mock<IMessenger>().Object,
            new Mock<INavigationService>().Object,
            suggestions.Object,
            TestImageStore.PassThrough().Object,
            new Mock<IPhotoPicker>().Object,
            new Mock<ISongLyricsService>().Object);
        sut.CompleteHydration();
        return sut;
    }

    /// <summary>Minimal list view model: the first-load fetch returns at once, so only the hold can delay it.</summary>
    private sealed class HoldTestListViewModel : CrudListViewModelBase<string>
    {
        public HoldTestListViewModel() : base(Mock.Of<ILogger>()) { }

        protected override ObservableRangeCollection<string> Items { get; } = [];
        protected override ObservableRangeCollection<string> SelectedItems { get; } = [];

        protected override Task<(IEnumerable<string> items, int totalCount)> FetchPageAsync(
            int page, int pageSize, string query, CancellationToken ct) =>
            Task.FromResult<(IEnumerable<string>, int)>((["item-1"], 1));

        protected override Task<(IEnumerable<string> items, int totalCount)> FetchMoreAsync(
            int page, int pageSize, string query, CancellationToken ct = default) =>
            Task.FromResult<(IEnumerable<string>, int)>(([], 1));

        protected override Task ExecuteDeleteAsync(IEnumerable<string> items) => Task.CompletedTask;
        protected override string BuildDeleteConfirmMessage(IList<string> items) => string.Empty;
        protected override Task NavigateToAddAsync() => Task.CompletedTask;
        protected override Task NavigateToEditAsync(string item) => Task.CompletedTask;
        protected override void RaiseEntityEmptyStateProperties() { }
    }
}
