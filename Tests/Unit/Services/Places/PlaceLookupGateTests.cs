using MyVocaList.Services.Places;

namespace MyVocaList.Tests.Unit.Services.Places;

/// <summary>
/// V7c — <see cref="PlaceLookupGate"/>: one request at a time, at least 1,000 ms between sent requests,
/// last 100 answers remembered (REQ-VEN-34). Time is driven by <see cref="ManualTimeProvider"/>.
/// </summary>
public class PlaceLookupGateTests
{
    private static readonly TimeSpan OneSecond = TimeSpan.FromMilliseconds(1000);

    [Fact]
    // [AC] REQ-VEN-34: a second request starts only 1,000 ms after the previous one started
    public async Task RunAsync_SecondKeyStartedAfter100ms_IsSentOnlyAt1000msFromFirstSend()
    {
        var time = new ManualTimeProvider();
        var gate = new PlaceLookupGate(time);
        var sentAt = new List<long>();

        var first = await gate.RunAsync("a", _ => { sentAt.Add(time.GetTimestamp()); return Task.FromResult("A"); }, CancellationToken.None);
        Assert.Equal("A", first);
        Assert.Single(sentAt);

        time.Advance(TimeSpan.FromMilliseconds(100));
        var second = gate.RunAsync("b", _ => { sentAt.Add(time.GetTimestamp()); return Task.FromResult("B"); }, CancellationToken.None);
        await time.WaitForPendingTimerAsync();

        time.Advance(TimeSpan.FromMilliseconds(899));   // total 999 ms since the first send started
        await Task.Delay(20);
        Assert.False(second.IsCompleted);
        Assert.Single(sentAt);

        time.Advance(TimeSpan.FromMilliseconds(1));     // total 1,000 ms
        Assert.Equal("B", await second);
        Assert.Equal(2, sentAt.Count);
        Assert.Equal(OneSecond, time.GetElapsedTime(sentAt[0], sentAt[1]));
    }

    [Fact]
    // [AC] REQ-VEN-34: the same key twice sends once and both callers get the same answer
    public async Task RunAsync_SameKeyTwice_SendsOnce()
    {
        var time = new ManualTimeProvider();
        var gate = new PlaceLookupGate(time);
        var sends = 0;
        Task<string> Send(CancellationToken _) { sends++; return Task.FromResult("answer"); }

        var first = await gate.RunAsync("k", Send, CancellationToken.None);
        var second = await gate.RunAsync("k", Send, CancellationToken.None);

        Assert.Equal(1, sends);
        Assert.Equal(first, second);
    }

    [Fact]
    // [AC] REQ-VEN-34: an identical call queued behind an in-flight call is answered from the cache, not sent again
    public async Task RunAsync_IdenticalCallQueuedWhileFirstInFlight_SendsOnce()
    {
        var time = new ManualTimeProvider();
        var gate = new PlaceLookupGate(time);
        var sends = 0;
        var release = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

        var first = gate.RunAsync("k", _ => { sends++; return release.Task; }, CancellationToken.None);
        var queued = gate.RunAsync("k", _ => { sends++; return Task.FromResult("second"); }, CancellationToken.None);
        await Task.Delay(20);
        Assert.False(queued.IsCompleted);

        release.SetResult("answer");

        Assert.Equal("answer", await first);
        Assert.Equal("answer", await queued);
        Assert.Equal(1, sends);
    }

    [Fact]
    // [AC] REQ-VEN-34: a failed send is never remembered; the exception reaches the caller and a retry sends again
    public async Task RunAsync_SendThrows_ExceptionReachesCallerAndNothingIsCached()
    {
        var time = new ManualTimeProvider();
        var gate = new PlaceLookupGate(time);
        var sends = 0;

        await Assert.ThrowsAsync<HttpRequestException>(() => gate.RunAsync<string>("k", _ =>
        {
            sends++;
            throw new HttpRequestException("boom");
        }, CancellationToken.None));

        time.Advance(OneSecond);
        var answer = await gate.RunAsync("k", _ => { sends++; return Task.FromResult("ok"); }, CancellationToken.None);

        Assert.Equal("ok", answer);
        Assert.Equal(2, sends);
    }

    [Fact]
    // [AC] REQ-VEN-34: the cache keeps 100 answers; the 101st evicts the oldest-added one
    public async Task RunAsync_101DistinctKeys_OldestAnswerIsEvicted()
    {
        var time = new ManualTimeProvider();
        var gate = new PlaceLookupGate(time);
        var sentKeys = new List<string>();

        for (var i = 0; i <= PlaceLookupGate.MaximumCachedAnswers; i++)
        {
            var key = $"k{i}";
            time.Advance(OneSecond);
            await gate.RunAsync(key, _ => { sentKeys.Add(key); return Task.FromResult(key); }, CancellationToken.None);
        }
        Assert.Equal(PlaceLookupGate.MaximumCachedAnswers + 1, sentKeys.Count);

        // k1 is still remembered (checked first, because the repeat of k0 would evict it).
        time.Advance(OneSecond);
        await gate.RunAsync("k1", _ => { sentKeys.Add("k1"); return Task.FromResult("k1"); }, CancellationToken.None);
        Assert.Equal(PlaceLookupGate.MaximumCachedAnswers + 1, sentKeys.Count);

        // k0 was dropped, so it is sent again.
        time.Advance(OneSecond);
        await gate.RunAsync("k0", _ => { sentKeys.Add("k0"); return Task.FromResult("k0"); }, CancellationToken.None);
        Assert.Equal(PlaceLookupGate.MaximumCachedAnswers + 2, sentKeys.Count);
        Assert.Equal("k0", sentKeys[^1]);
    }

    [Fact]
    // [AC] REQ-VEN-34: a caller cancelled while waiting for its turn gets OperationCanceledException and nothing is sent
    public async Task RunAsync_CancelledWhileWaitingForTurn_ThrowsAndDoesNotSend()
    {
        var time = new ManualTimeProvider();
        var gate = new PlaceLookupGate(time);
        var release = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var first = gate.RunAsync("a", _ => release.Task, CancellationToken.None);

        using var cts = new CancellationTokenSource();
        var sent = false;
        var waiting = gate.RunAsync("b", _ => { sent = true; return Task.FromResult("B"); }, cts.Token);
        await Task.Delay(20);

        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiting);
        Assert.False(sent);

        release.SetResult("A");
        await first;
    }
}

/// <summary>
/// Test clock for the place lookup tests (shared with <c>PlaceLookupServiceTests</c>): time moves only when a
/// test calls <see cref="Advance"/>, and <see cref="Task.Delay(TimeSpan, TimeProvider, CancellationToken)"/> completes
/// when the clock passes its due time. Avoids a NuGet dependency on <c>Microsoft.Extensions.Time.Testing</c>.
/// </summary>
internal sealed class ManualTimeProvider : TimeProvider
{
    private readonly object _sync = new();
    private readonly List<ManualTimer> _timers = [];
    private long _nowTicks;

    public override long TimestampFrequency => TimeSpan.TicksPerSecond;

    public override long GetTimestamp()
    {
        lock (_sync)
            return _nowTicks;
    }

    public override DateTimeOffset GetUtcNow() =>
        new DateTimeOffset(2026, 11, 14, 0, 0, 0, TimeSpan.Zero).AddTicks(GetTimestamp());

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        lock (_sync)
        {
            var timer = new ManualTimer(this, callback, state, _nowTicks + dueTime.Ticks);
            _timers.Add(timer);
            return timer;
        }
    }

    /// <summary>Moves the clock forward and fires every timer that became due.</summary>
    public void Advance(TimeSpan by)
    {
        List<ManualTimer> due;
        lock (_sync)
        {
            _nowTicks += by.Ticks;
            due = _timers.Where(t => t.DueTicks <= _nowTicks).ToList();
            foreach (var timer in due)
                _timers.Remove(timer);
        }

        foreach (var timer in due)
            timer.Fire();
    }

    /// <summary>Waits (in real time) until a delay has been scheduled, so the test can advance the clock.</summary>
    public async Task WaitForPendingTimerAsync()
    {
        for (var i = 0; i < 500; i++)
        {
            lock (_sync)
            {
                if (_timers.Count > 0)
                    return;
            }

            await Task.Delay(2);
        }

        throw new TimeoutException("No timer was scheduled.");
    }

    internal void Remove(ManualTimer timer)
    {
        lock (_sync)
            _timers.Remove(timer);
    }
}

internal sealed class ManualTimer(ManualTimeProvider owner, TimerCallback callback, object? state, long dueTicks) : ITimer
{
    public long DueTicks { get; } = dueTicks;

    public void Fire() => callback(state);

    public bool Change(TimeSpan dueTime, TimeSpan period) => true;

    public void Dispose() => owner.Remove(this);

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}
