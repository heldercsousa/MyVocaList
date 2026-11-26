using System.Diagnostics;

namespace MyVocaList.Services;

/// <summary>
/// Shared limiter keeping at least <see cref="MinimumIntervalMs"/> between MusicBrainz calls (REQ-MSRC-10).
/// Register as a singleton.
/// </summary>
public sealed class MusicBrainzRateLimiter
{
    /// <summary>Minimum spacing between two requests, in milliseconds.</summary>
    public const int MinimumIntervalMs = 1000;

    private readonly SemaphoreSlim _gate = new(1, 1);
    private long _lastStartTimestamp;
    private bool _hasStarted;

    /// <summary>Completes once at least <see cref="MinimumIntervalMs"/> has passed since the previous call was released.</summary>
    public async Task WaitAsync(CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            if (_hasStarted)
            {
                var elapsed = Stopwatch.GetElapsedTime(_lastStartTimestamp);
                var remaining = TimeSpan.FromMilliseconds(MinimumIntervalMs) - elapsed;
                if (remaining > TimeSpan.Zero)
                    await Task.Delay(remaining, ct);
            }
            _lastStartTimestamp = Stopwatch.GetTimestamp();
            _hasStarted = true;
        }
        finally
        {
            _gate.Release();
        }
    }
}
