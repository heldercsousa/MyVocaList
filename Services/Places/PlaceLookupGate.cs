namespace MyVocaList.Services.Places;

/// <summary>
/// Shared usage rules for the place lookup provider (REQ-VEN-34). A singleton: every scope shares one turn queue
/// and one cache. Requests are serialised, two sent requests start at least <see cref="MinimumIntervalMilliseconds"/>
/// apart, and the last <see cref="MaximumCachedAnswers"/> answers are remembered by key. A cache hit returns without
/// waiting. Only an answer that <c>send</c> returns is remembered; a send that throws is never cached.
/// </summary>
public sealed class PlaceLookupGate(TimeProvider timeProvider)
{
    /// <summary>Minimum time between the starts of two sent requests (REQ-VEN-34: one request per second).</summary>
    public const int MinimumIntervalMilliseconds = 1000;

    /// <summary>Number of answers kept in memory; the oldest-added is dropped first (REQ-VEN-34).</summary>
    public const int MaximumCachedAnswers = 100;

    private readonly SemaphoreSlim _turn = new(1, 1);
    private readonly Dictionary<string, object> _answers = [];
    private readonly Queue<string> _insertionOrder = new();
    private readonly object _cacheSync = new();
    private long? _lastSendStarted;

    /// <summary>
    /// Returns the remembered answer for <paramref name="key"/>, or waits for this caller's turn, honours the spacing
    /// and then calls <paramref name="send"/>. Exceptions from <paramref name="send"/> propagate unchanged.
    /// </summary>
    /// <param name="key">Identity of the request; equal keys share one answer.</param>
    /// <param name="send">The provider call. It receives the token to pass to the provider.</param>
    /// <param name="ct">The caller's token; cancellation while waiting throws <see cref="OperationCanceledException"/>.</param>
    public async Task<T> RunAsync<T>(string key, Func<CancellationToken, Task<T>> send, CancellationToken ct) where T : class
    {
        if (TryGetRemembered<T>(key, out var remembered))
            return remembered;

        await _turn.WaitAsync(ct);
        try
        {
            // An identical request may have been answered while this one was queued (REQ-VEN-34).
            if (TryGetRemembered<T>(key, out remembered))
                return remembered;

            if (_lastSendStarted is long previousStart)
            {
                var elapsed = timeProvider.GetElapsedTime(previousStart);
                var remaining = TimeSpan.FromMilliseconds(MinimumIntervalMilliseconds) - elapsed;
                if (remaining > TimeSpan.Zero)
                    await Task.Delay(remaining, timeProvider, ct);
            }

            // Recorded before the call: a failed send still counts as a request made.
            _lastSendStarted = timeProvider.GetTimestamp();
            var answer = await send(ct);
            if (answer is not null)
                Remember(key, answer);
            return answer;
        }
        finally
        {
            _turn.Release();
        }
    }

    private bool TryGetRemembered<T>(string key, out T answer) where T : class
    {
        lock (_cacheSync)
        {
            if (_answers.TryGetValue(key, out var stored) && stored is T typed)
            {
                answer = typed;
                return true;
            }
        }

        answer = null!;
        return false;
    }

    private void Remember(string key, object answer)
    {
        lock (_cacheSync)
        {
            if (_answers.ContainsKey(key))
            {
                _answers[key] = answer;
                return;
            }

            if (_answers.Count >= MaximumCachedAnswers)
                _answers.Remove(_insertionOrder.Dequeue());

            _answers[key] = answer;
            _insertionOrder.Enqueue(key);
        }
    }
}
