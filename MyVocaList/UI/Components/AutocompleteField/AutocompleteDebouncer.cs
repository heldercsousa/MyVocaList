namespace MyVocaList.UI.Components.AutocompleteField;

/// <summary>
/// Plain debounce helper — cancels any in-flight delay and starts a new one.
/// Extracted for unit testability (no MAUI runtime dependency).
/// </summary>
internal sealed class AutocompleteDebouncer
{
    private CancellationTokenSource _cts;
    private readonly Action<Action> _dispatcher;

    /// <summary>
    /// Creates a debouncer. The optional <paramref name="dispatcher"/> overrides the default
    /// <see cref="MainThread.BeginInvokeOnMainThread"/> — useful for unit tests.
    /// </summary>
    internal AutocompleteDebouncer(Action<Action> dispatcher = null)
    {
        _dispatcher = dispatcher ?? (a => MainThread.BeginInvokeOnMainThread(a));
    }

    /// <summary>
    /// Cancels any pending debounce and starts a new one.
    /// After <paramref name="delayMs"/> elapses without interruption,
    /// <paramref name="onElapsed"/> is invoked with <paramref name="text"/>.
    /// </summary>
    internal void Trigger(string text, int delayMs, Action<string> onElapsed)
    {
        try
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }
        catch { /* ignore disposal races on CancellationTokenSource */ }

        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(delayMs, token);
                if (token.IsCancellationRequested) return;
                _dispatcher(() => onElapsed?.Invoke(text));
            }
            catch (OperationCanceledException) { /* ignore */ }
        }, token);
    }

    /// <summary>Cancels any pending debounce without starting a new one.</summary>
    internal void Cancel()
    {
        try
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }
        catch { /* ignore disposal races on CancellationTokenSource */ }
        _cts = null;
    }
}
