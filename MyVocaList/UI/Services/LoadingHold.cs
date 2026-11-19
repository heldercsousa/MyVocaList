namespace MyVocaList.UI.Services;

/// <summary>
/// Debug-only minimum time for the loading shimmer (REQ-DLH-01..04). The start-up code sets <see cref="Duration"/>
/// inside <c>#if DEBUG</c>; every other build and every unit test keeps the zero default, so <see cref="WaitAsync"/>
/// returns at once.
/// </summary>
public static class LoadingHold
{
    /// <summary>The minimum shimmer time. Zero (the default) means no hold.</summary>
    public static TimeSpan Duration { get; set; } = TimeSpan.Zero;

    /// <summary>Completes after <see cref="Duration"/>; returns a completed task at once when the duration is zero.</summary>
    public static Task WaitAsync(CancellationToken ct) =>
        Duration <= TimeSpan.Zero ? Task.CompletedTask : Task.Delay(Duration, ct);
}
