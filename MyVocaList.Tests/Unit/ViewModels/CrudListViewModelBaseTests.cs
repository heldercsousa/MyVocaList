using System.Collections.Concurrent;
using MyVocaList.UI.Collections;

namespace MyVocaList.Tests.Unit.ViewModels;

public class CrudListViewModelBaseTests
{
    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(5);

    [Fact]
    // Regression: page-load-frozen — fetch must not run on the UI SynchronizationContext
    public async Task InitializeAsync_WithSynchronizationContext_ExecutesFetchOffContext()
    {
        var original = SynchronizationContext.Current;
        using var uiContext = new PumpSynchronizationContext();
        SynchronizationContext contextDuringFetch = null;
        var contextCaptured = false;
        var sut = new TestCrudListViewModel(
            onFetchPage: () => { contextDuringFetch = SynchronizationContext.Current; contextCaptured = true; });

        try
        {
            SynchronizationContext.SetSynchronizationContext(uiContext);
            var initTask = sut.InitializeAsync();
            SynchronizationContext.SetSynchronizationContext(original);

            var completed = await Task.WhenAny(initTask, Task.Delay(TestTimeout));
            Assert.Same(initTask, completed); // guard: a regression must fail fast, not hang
            await initTask;
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(original);
        }

        Assert.True(contextCaptured);
        Assert.NotSame(uiContext, contextDuringFetch);
    }

    [Fact]
    // Regression: page-load-frozen — fetch must not run on the UI SynchronizationContext
    public async Task LoadMoreCommand_WithSynchronizationContext_ExecutesFetchOffContext()
    {
        var original = SynchronizationContext.Current;
        using var uiContext = new PumpSynchronizationContext();
        SynchronizationContext contextDuringFetch = null;
        var fetchInvoked = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var sut = new TestCrudListViewModel(
            onFetchMore: () =>
            {
                contextDuringFetch = SynchronizationContext.Current;
                fetchInvoked.TrySetResult();
            });

        try
        {
            SynchronizationContext.SetSynchronizationContext(uiContext);
            sut.LoadMoreCommand.Execute(null);
            SynchronizationContext.SetSynchronizationContext(original);

            var completed = await Task.WhenAny(fetchInvoked.Task, Task.Delay(TestTimeout));
            Assert.Same(fetchInvoked.Task, completed); // guard: a regression must fail fast, not hang

            // LoadMoreCommand is fire-and-forget — wait for the load to finish
            // (items land via RunOnUiThread) before tearing down the pump.
            var deadline = DateTime.UtcNow + TestTimeout;
            while (sut.ItemCount == 0 && DateTime.UtcNow < deadline)
                await Task.Delay(10);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(original);
        }

        Assert.NotSame(uiContext, contextDuringFetch);
    }

    // ── Test double ───────────────────────────────────────────────────────

    private sealed class TestCrudListViewModel : CrudListViewModelBase<string>
    {
        private readonly Action _onFetchPage;
        private readonly Action _onFetchMore;

        public TestCrudListViewModel(Action onFetchPage = null, Action onFetchMore = null)
            : base(Mock.Of<ILogger>())
        {
            _onFetchPage = onFetchPage;
            _onFetchMore = onFetchMore;
        }

        public int ItemCount => Items.Count;

        protected override ObservableRangeCollection<string> Items { get; } = [];
        protected override ObservableRangeCollection<string> SelectedItems { get; } = [];

        protected override Task<(IEnumerable<string> items, int totalCount)> FetchPageAsync(
            int page, int pageSize, string query, CancellationToken ct)
        {
            _onFetchPage?.Invoke();
            return Task.FromResult<(IEnumerable<string>, int)>((["item-1"], 1));
        }

        protected override Task<(IEnumerable<string> items, int totalCount)> FetchMoreAsync(
            int page, int pageSize, string query, CancellationToken ct = default)
        {
            _onFetchMore?.Invoke();
            return Task.FromResult<(IEnumerable<string>, int)>((["item-more"], 2));
        }

        protected override Task ExecuteDeleteAsync(IEnumerable<string> items) => Task.CompletedTask;
        protected override string BuildDeleteConfirmMessage(IList<string> items) => string.Empty;
        protected override Task NavigateToAddAsync() => Task.CompletedTask;
        protected override Task NavigateToEditAsync(string item) => Task.CompletedTask;
        protected override void RaiseEntityEmptyStateProperties() { }
    }

    // ── Single-threaded pump simulating the UI SynchronizationContext ─────
    // Posted continuations execute on a dedicated thread that has this
    // context installed, so `await Task.Yield()` continuations actually run
    // (a non-pumping fake would hang the test).

    private sealed class PumpSynchronizationContext : SynchronizationContext, IDisposable
    {
        private readonly BlockingCollection<(SendOrPostCallback callback, object state)> _queue = new();
        private readonly Thread _pumpThread;

        public PumpSynchronizationContext()
        {
            _pumpThread = new Thread(Pump) { IsBackground = true, Name = "FakeUiThread" };
            _pumpThread.Start();
        }

        public override void Post(SendOrPostCallback d, object state)
        {
            try { _queue.Add((d, state)); }
            catch (InvalidOperationException)
            {
                // Pump torn down (test already failed its timeout guard) — run the
                // continuation on the thread pool so SUT finally blocks still execute
                // (a swallowed post would strand the static DbLoadGate and hang
                // every later test).
                ThreadPool.QueueUserWorkItem(_ => d(state));
            }
        }

        private void Pump()
        {
            SetSynchronizationContext(this);
            foreach (var (callback, state) in _queue.GetConsumingEnumerable())
                callback(state);
        }

        public void Dispose() => _queue.CompleteAdding();
    }
}
