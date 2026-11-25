using Microsoft.Extensions.Logging;
using MyVocaList.UI.Collections;

namespace MyVocaList.Tests.Unit.ViewModels;

/// <summary>
/// BUG-089 (Major, suspected race): a load that started for an older search must not change the rows of a newer
/// search. Each test holds one old load open with a gate, runs a newer search to completion, then releases the old
/// load and checks that its rows never appear. Timing is controlled by the gates; the search debounce is waited for
/// by polling with a deadline.
/// </summary>
public class CrudListStaleLoadTests
{
    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(5);

    // Regression: BUG-089 B6 — a load-more page fetched for the old query must not be appended after a newer search.
    [Fact]
    public async Task LoadMore_StartedBeforeNewerSearch_DoesNotAppendItsPageToTheNewerRows()
    {
        var sut = new GatedCrudListViewModel();
        sut.PageFetch = (query, _) => Task.FromResult<(IEnumerable<string>, int)>(
            query is null ? (["old-1"], 5) : (["new-1"], 1));
        await sut.InitializeAsync();

        var moreGate = new TaskCompletionSource<(IEnumerable<string>, int)>(TaskCreationOptions.RunContinuationsAsynchronously);
        var moreStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        sut.MoreFetch = () =>
        {
            moreStarted.TrySetResult();
            return moreGate.Task;
        };

        sut.LoadMoreCommand.Execute(null);
        await WaitFor(moreStarted.Task);

        sut.SearchText = "new";
        await WaitUntil(() => sut.ItemsList.Contains("new-1"));

        moreGate.SetResult((["old-page-2"], 5));
        await Task.Delay(200);

        Assert.DoesNotContain("old-page-2", sut.ItemsList);
        Assert.Equal(["new-1"], sut.ItemsList);
    }

    // Regression: BUG-089 B7 — a pull-to-refresh started before a newer search must not overwrite the newer rows.
    [Fact]
    public async Task Refresh_StartedBeforeNewerSearch_DoesNotOverwriteTheNewerRows()
    {
        var sut = new GatedCrudListViewModel();
        sut.PageFetch = (query, _) => Task.FromResult<(IEnumerable<string>, int)>(
            query is null ? (["old-1"], 1) : (["new-1"], 1));
        await sut.InitializeAsync();

        var refreshGate = new TaskCompletionSource<(IEnumerable<string>, int)>(TaskCreationOptions.RunContinuationsAsynchronously);
        var refreshStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        sut.PageFetch = (query, _) =>
        {
            if (query is not null)
                return Task.FromResult<(IEnumerable<string>, int)>((["new-1"], 1));

            refreshStarted.TrySetResult();
            return refreshGate.Task;
        };

        sut.RefreshCommand.Execute(null);
        await WaitFor(refreshStarted.Task);

        sut.SearchText = "new";
        await WaitUntil(() => sut.ItemsList.Contains("new-1"));

        refreshGate.SetResult((["stale-refresh"], 1));
        await Task.Delay(200);

        Assert.DoesNotContain("stale-refresh", sut.ItemsList);
        Assert.Equal(["new-1"], sut.ItemsList);
    }

    // [AC] B5 (search error handling): a failed debounced search logs the full detail, keeps the current rows,
    // and throws nothing unobserved. No raw exception text is shown; the CRUD list has no message mechanism.
    [Fact]
    public async Task Search_FetchThrows_LogsTheFailureAndKeepsTheCurrentRows()
    {
        var logger = new Mock<ILogger>();
        var sut = new GatedCrudListViewModel(logger.Object);
        sut.PageFetch = (query, _) => query is null
            ? Task.FromResult<(IEnumerable<string>, int)>((["old-1"], 1))
            : throw new InvalidOperationException("db went away");
        await sut.InitializeAsync();

        sut.SearchText = "boom";
        await WaitUntil(() => logger.Invocations.Any(i => i.Method.Name == nameof(ILogger.Log) && (LogLevel)i.Arguments[0] == LogLevel.Error));
        await WaitUntil(() => !sut.IsInitialLoading);

        logger.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.Is<Exception>(e => e is InvalidOperationException),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        Assert.Equal(["old-1"], sut.ItemsList);
        Assert.False(sut.IsInitialLoading);
    }

    // [AC] REQ-DLH-05: the shimmer shows from the first keystroke until the newest search applies its rows. Keystrokes
    // closer than the 300 ms debounce keep it on: it is not switched off and on between keystrokes.
    [Fact]
    public async Task TypingFasterThanTheDebounce_KeepsTheShimmerOnUntilTheLastSearchApplies()
    {
        var sut = new GatedCrudListViewModel();
        sut.PageFetch = (query, _) => Task.FromResult<(IEnumerable<string>, int)>(
            query is null ? (["old-1"], 1) : ([query + "-1"], 1));
        await sut.InitializeAsync();

        var shimmerChanges = new List<bool>();
        sut.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ICrudListViewModel.IsInitialLoading))
                shimmerChanges.Add(sut.IsInitialLoading);
        };

        sut.SearchText = "a";
        await Task.Delay(20);
        sut.SearchText = "ab";
        await Task.Delay(20);
        sut.SearchText = "abc";

        await WaitUntil(() => sut.ItemsList.Contains("abc-1"));

        Assert.Equal([true, false], shimmerChanges);
        Assert.False(sut.IsInitialLoading);
    }

    private static async Task WaitFor(Task task)
    {
        var completed = await Task.WhenAny(task, Task.Delay(TestTimeout));
        Assert.Same(task, completed); // guard: a regression must fail fast, not hang
    }

    private static async Task WaitUntil(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow + TestTimeout;
        while (!condition() && DateTime.UtcNow < deadline)
            await Task.Delay(20);
        Assert.True(condition(), "Condition not reached before the timeout");
    }

    private sealed class GatedCrudListViewModel : CrudListViewModelBase<string>
    {
        public GatedCrudListViewModel(ILogger? logger = null)
            : base(logger ?? Mock.Of<ILogger>())
        {
        }

        public Func<string?, CancellationToken, Task<(IEnumerable<string>, int)>> PageFetch { get; set; } =
            (_, _) => Task.FromResult<(IEnumerable<string>, int)>((["default"], 1));

        public Func<Task<(IEnumerable<string>, int)>> MoreFetch { get; set; } =
            () => Task.FromResult<(IEnumerable<string>, int)>((["more"], 1));

        public IList<string> ItemsList => Items.ToList();

        protected override ObservableRangeCollection<string> Items { get; } = [];
        protected override ObservableRangeCollection<string> SelectedItems { get; } = [];

        protected override Task<(IEnumerable<string> items, int totalCount)> FetchPageAsync(
            int page, int pageSize, string query, CancellationToken ct) =>
            PageFetch(query, ct);

        protected override Task<(IEnumerable<string> items, int totalCount)> FetchMoreAsync(
            int page, int pageSize, string query, CancellationToken ct = default) =>
            MoreFetch();

        protected override Task ExecuteDeleteAsync(IEnumerable<string> items) => Task.CompletedTask;
        protected override string BuildDeleteConfirmMessage(IList<string> items) => string.Empty;
        protected override Task NavigateToAddAsync() => Task.CompletedTask;
        protected override Task NavigateToEditAsync(string item) => Task.CompletedTask;
        protected override void RaiseEntityEmptyStateProperties() { }
    }
}
