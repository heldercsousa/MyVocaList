using MyVocaList.UI.Components;

namespace MyVocaList.UI.Services;

/// <summary>
/// Singleton hand-off between the Android share target and the song form (REQ-MSRC-06).
/// With no song form open the link stays pending and the user is told it is ready to attach.
/// </summary>
public sealed class SharedLinkInbox : ISharedLinkInbox
{
    private readonly ISnackbarComponent _snackbar;
    private readonly object _gate = new();
    private string? _pending;

    public SharedLinkInbox(ISnackbarComponent snackbar) => _snackbar = snackbar;

    public event EventHandler? LinkReceived;

    public bool HasListener => LinkReceived is not null;

    public string? TakePending()
    {
        lock (_gate)
        {
            var text = _pending;
            _pending = null;
            return text;
        }
    }

    public void Receive(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        lock (_gate) _pending = text;

        if (HasListener)
            LinkReceived?.Invoke(this, EventArgs.Empty);
        else
            MainThread.BeginInvokeOnMainThread(() => _ = _snackbar.ShowSuccessAsync("YouTube link ready to attach"));
    }
}
