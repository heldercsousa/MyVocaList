using Microsoft.Extensions.DependencyInjection;
using MyVocaList.Navigation;

namespace MyVocaList.UI.Services;

/// <summary>
/// Singleton hand-off between the Android share target and the song form (REQ-MSRC-06, REQ-MSRC-11).
/// A song form that is open takes the link itself. With none open, the shared video opens the Song form: the stored
/// song's edit form when a song already holds the video, else the add form, which takes the pending link when it appears.
/// </summary>
public class SharedLinkInbox : ISharedLinkInbox
{
    private static readonly TimeSpan ReadyTimeout = TimeSpan.FromSeconds(30);

    private readonly IServiceScopeFactory _scopes;
    private readonly INavigationService _navigation;
    private readonly ILogger<SharedLinkInbox> _logger;
    private readonly object _gate = new();
    private string? _pending;

    public SharedLinkInbox(IServiceScopeFactory scopes, INavigationService navigation, ILogger<SharedLinkInbox> logger)
    {
        _scopes = scopes;
        _navigation = navigation;
        _logger = logger;
    }

    public event EventHandler? LinkReceived;

    public bool HasListener => LinkReceived is not null;

    /// <summary>The routing of the last received link; completes when it navigated, was dropped or failed. Exposed for tests.</summary>
    public Task RoutingTask { get; private set; } = Task.CompletedTask;

    /// <summary>True once the Shell can navigate; a share that starts the app arrives before that.</summary>
    protected virtual bool IsNavigationReady => Shell.Current?.CurrentPage is not null;

    protected virtual TimeSpan ReadyPollInterval => TimeSpan.FromMilliseconds(200);

    /// <summary>Navigation runs on the main thread; the routing itself may continue on any thread.</summary>
    protected virtual Task RunOnMainThreadAsync(Func<Task> action) => MainThread.InvokeOnMainThreadAsync(action);

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
            RoutingTask = RouteAsync(text);
    }

    /// <summary>REQ-MSRC-11: opens the Song form for the shared video. Failures are logged; the link then stays pending.</summary>
    private async Task RouteAsync(string text)
    {
        try
        {
            if (!await WaitUntilReadyAsync())
            {
                _logger.LogWarning("Shared link not routed: the Shell was not ready within {Timeout}", ReadyTimeout);
                return;
            }

            SharedVideoTarget? target;
            using (var scope = _scopes.CreateScope())
                target = await scope.ServiceProvider.GetRequiredService<ISharedVideoService>().ResolveTargetAsync(text);

            if (target is null)
            {
                _logger.LogInformation("Shared text is not a YouTube link; ignored");
                ClearPendingIf(text);
                return;
            }

            string route;
            if (target.SongId is { } songId)
            {
                ClearPendingIf(text);   // the stored song already holds the video: nothing is left to attach
                route = $"{Routes.SongForm}?songId={songId}";
            }
            else
            {
                route = Routes.SongForm;   // the add form takes the pending link when it appears
            }

            await RunOnMainThreadAsync(() => _navigation.GoToAsync(route));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Opening the song form for a shared link failed");
        }
    }

    private async Task<bool> WaitUntilReadyAsync()
    {
        var deadline = DateTime.UtcNow + ReadyTimeout;
        while (!IsNavigationReady)
        {
            if (DateTime.UtcNow >= deadline) return false;
            await Task.Delay(ReadyPollInterval);
        }

        return true;
    }

    private void ClearPendingIf(string text)
    {
        lock (_gate)
        {
            if (_pending == text) _pending = null;
        }
    }
}
