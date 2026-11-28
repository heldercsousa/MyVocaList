namespace MyVocaList.Services;

/// <summary>
/// Sliding one-minute budget for iTunes Search API calls: at most <see cref="MaxCallsPerMinute"/> calls (REQ-ITS-06).
/// Register as a singleton, so every scope shares one budget.
/// </summary>
public sealed class ITunesRequestBudget
{
    /// <summary>Apple's published guideline: about 20 calls per minute.</summary>
    public const int MaxCallsPerMinute = 20;

    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    private readonly TimeProvider _time;
    private readonly Queue<DateTimeOffset> _calls = new();
    private readonly object _gate = new();

    public ITunesRequestBudget(TimeProvider time) => _time = time;

    /// <summary>Records a call and returns true when it is within the budget; false when the call must be skipped.</summary>
    public bool TryTake()
    {
        lock (_gate)
        {
            var now = _time.GetUtcNow();
            while (_calls.Count > 0 && now - _calls.Peek() >= Window)
                _calls.Dequeue();

            if (_calls.Count >= MaxCallsPerMinute)
                return false;

            _calls.Enqueue(now);
            return true;
        }
    }
}
