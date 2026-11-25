using System.Collections.Concurrent;
using System.Threading;
using MyVocaList.UI.Collections;
using MyVocaList.UI.Services;

namespace MyVocaList.Tests.Unit.ViewModels;

/// <summary>
/// REQ-DLH-05: every load that replaces the rows of a CRUD list shows the shimmer (<c>IsInitialLoading</c>) until the
/// newest load applies its rows; pull-to-refresh and load-more do not show it. In debug builds the hold applies to
/// each load that shows the shimmer. Same collection as <see cref="LoadingHoldTests"/>: the hold is static.
/// </summary>
[Collection(LoadingHoldCollection.Name)]
public class LoadingHoldShimmerTests
{
    private static readonly TimeSpan HoldForTest = TimeSpan.FromMilliseconds(200);
    private static readonly TimeSpan EarlyProbe = TimeSpan.FromMilliseconds(50);
    private static readonly TimeSpan WaitLimit = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan NoHoldBudget = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan LongHold = TimeSpan.FromSeconds(10);

    // [AC] REQ-DLH-05: a search shows the shimmer until the search results replace the rows.
    [Fact]
    public async Task Search_ShowsShimmerUntilTheNewRowsApply()
    {
        var sut = await InitializedAsync();
        sut.AutoRespond = false;

        sut.SearchText = "abc";
        var search = await sut.NextFetchAsync();

        Assert.True(sut.IsInitialLoading);
        search.Complete("abc-1");
        await WaitUntilAsync(() => !sut.IsInitialLoading);

        Assert.Equal("abc-1", Assert.Single(sut.Rows));
    }

    // [AC] REQ-DLH-05: a filter reload (ReloadAsync) shows the shimmer until its rows apply.
    [Fact]
    public async Task FilterReload_ShowsShimmerUntilTheNewRowsApply()
    {
        var sut = await InitializedAsync();
        sut.AutoRespond = false;

        var reload = sut.Reload();
        var pending = await sut.NextFetchAsync();

        Assert.True(sut.IsInitialLoading);
        pending.Complete("filtered-1");
        await reload;

        Assert.False(sut.IsInitialLoading);
        Assert.Equal("filtered-1", Assert.Single(sut.Rows));
    }

    // [AC] REQ-DLH-05: a later visit (InitializeAsync after the first load) shows the shimmer until its rows apply.
    [Fact]
    public async Task LaterVisit_ShowsShimmerUntilTheNewRowsApply()
    {
        var sut = await InitializedAsync();
        sut.AutoRespond = false;

        var visit = sut.InitializeAsync();
        var pending = await sut.NextFetchAsync();

        Assert.True(sut.IsInitialLoading);
        pending.Complete("visit-1");
        await visit;

        Assert.False(sut.IsInitialLoading);
    }

    // [AC] REQ-DLH-05: pull-to-refresh keeps the refresh circle; it does not show the shimmer.
    [Fact]
    public async Task PullToRefresh_DoesNotShowShimmer()
    {
        var sut = await InitializedAsync();
        sut.AutoRespond = false;

        var refresh = sut.Refresh();
        var pending = await sut.NextFetchAsync();

        Assert.False(sut.IsInitialLoading);
        pending.Complete("refreshed-1");
        await refresh;

        Assert.False(sut.IsInitialLoading);
    }

    // [AC] REQ-DLH-05: load-more keeps the load-more indicator; it does not show the shimmer.
    [Fact]
    public async Task LoadMore_DoesNotShowShimmer()
    {
        var sut = await InitializedAsync();
        sut.AutoRespond = false;

        sut.LoadMoreCommand.Execute(null);
        var pending = await sut.NextFetchAsync();

        Assert.False(sut.IsInitialLoading);
        pending.Complete("item-2");
        await WaitUntilAsync(() => sut.Rows.Count == 2);
    }

    // [AC] REQ-DLH-05: a superseded search does not end the shimmer; only the newest search applies its rows and ends it.
    [Fact]
    public async Task SupersededSearch_DoesNotEndTheShimmerEarly()
    {
        var sut = await InitializedAsync();
        sut.AutoRespond = false;

        sut.SearchText = "a";
        var older = await sut.NextFetchAsync();
        sut.SearchText = "ab";
        var newer = await sut.NextFetchAsync();

        Assert.True(sut.IsInitialLoading);

        older.Complete("a-1");
        await Task.Delay(EarlyProbe);
        Assert.True(sut.IsInitialLoading);

        newer.Complete("ab-1");
        await WaitUntilAsync(() => !sut.IsInitialLoading);

        Assert.Equal("ab-1", Assert.Single(sut.Rows));
    }

    // [AC] REQ-DLH-05: in debug builds the hold applies to a search that shows the shimmer.
    [Fact]
    public async Task Search_InDebugBuild_HoldsTheShimmer()
    {
        var sut = await InitializedAsync();
        sut.AutoRespond = false;
        var original = LoadingHold.Duration;
        try
        {
            LoadingHold.Duration = HoldForTest;

            sut.SearchText = "abc";
            var search = await sut.NextFetchAsync();
            search.Complete("abc-1");
            await Task.Delay(EarlyProbe);

            Assert.True(sut.IsInitialLoading);
            await WaitUntilAsync(() => !sut.IsInitialLoading);
            Assert.Equal("abc-1", Assert.Single(sut.Rows));
        }
        finally
        {
            LoadingHold.Duration = original;
        }
    }

    // [AC] REQ-DLH-05: pull-to-refresh gets no debug hold; the hold applies only to loads that show the shimmer.
    [Fact]
    public async Task PullToRefresh_InDebugBuild_DoesNotWait()
    {
        var sut = await InitializedAsync();
        var original = LoadingHold.Duration;
        try
        {
            LoadingHold.Duration = LongHold;

            await sut.Refresh().WaitAsync(NoHoldBudget);
        }
        finally
        {
            LoadingHold.Duration = original;
        }
    }

    // [AC] REQ-DLH-05: empty states follow a search: the empty state shows only after the search applies no rows.
    [Fact]
    public async Task Search_EmptyStateShowsOnlyAfterTheSearchAppliesNoRows()
    {
        var sut = await InitializedAsync();
        sut.AutoRespond = false;

        sut.SearchText = "zzz";
        var search = await sut.NextFetchAsync();

        Assert.False(sut.IsEmpty);
        search.Complete();
        await WaitUntilAsync(() => sut.Rows.Count == 0);

        Assert.False(sut.IsInitialLoading);
        Assert.True(sut.IsEmpty);
        Assert.True(sut.IsEmptyNoResults);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static async Task<GatedListViewModel> InitializedAsync()
    {
        var sut = new GatedListViewModel();
        await sut.InitializeAsync();
        return sut;
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow + WaitLimit;
        while (!condition() && DateTime.UtcNow < deadline)
            await Task.Delay(10);
    }

    /// <summary>One fetch the test has to answer; the fetch stays open until <see cref="Complete"/> is called.</summary>
    private sealed class PendingFetch
    {
        public TaskCompletionSource<(IEnumerable<string> items, int totalCount)> Result { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void Complete(params string[] items) => Result.TrySetResult((items, items.Length));
    }

    /// <summary>
    /// Minimal list view model. With <see cref="AutoRespond"/> true, fetches answer at once (first page: one of two
    /// rows, so load-more is possible); otherwise each fetch is queued for the test to answer.
    /// </summary>
    private sealed class GatedListViewModel : CrudListViewModelBase<string>
    {
        private readonly ConcurrentQueue<PendingFetch> _pending = new();
        private readonly SemaphoreSlim _arrived = new(0);

        public GatedListViewModel() : base(Mock.Of<ILogger>()) { }

        public bool AutoRespond { get; set; } = true;

        public ObservableRangeCollection<string> Rows => Items;

        protected override ObservableRangeCollection<string> Items { get; } = [];
        protected override ObservableRangeCollection<string> SelectedItems { get; } = [];

        public Task Reload() => ReloadAsync();

        public Task Refresh() => RefreshAsync();

        public async Task<PendingFetch> NextFetchAsync()
        {
            if (!await _arrived.WaitAsync(WaitLimit))
                throw new TimeoutException("No fetch arrived within the wait limit.");
            _pending.TryDequeue(out var pending);
            return pending!;
        }

        protected override Task<(IEnumerable<string> items, int totalCount)> FetchPageAsync(
            int page, int pageSize, string query, CancellationToken ct) => FetchAsync(ct);

        protected override Task<(IEnumerable<string> items, int totalCount)> FetchMoreAsync(
            int page, int pageSize, string query, CancellationToken ct = default) => FetchAsync(ct);

        private async Task<(IEnumerable<string> items, int totalCount)> FetchAsync(CancellationToken ct)
        {
            if (AutoRespond)
                return (new[] { "item-1" }, 2);

            var pending = new PendingFetch();
            _pending.Enqueue(pending);
            _arrived.Release();
            return await pending.Result.Task.WaitAsync(ct);
        }

        protected override Task ExecuteDeleteAsync(IEnumerable<string> items) => Task.CompletedTask;
        protected override string BuildDeleteConfirmMessage(IList<string> items) => string.Empty;
        protected override Task NavigateToAddAsync() => Task.CompletedTask;
        protected override Task NavigateToEditAsync(string item) => Task.CompletedTask;
        protected override void RaiseEntityEmptyStateProperties() { }
    }
}
