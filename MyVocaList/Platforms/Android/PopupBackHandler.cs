using System.Runtime.Versioning;
using Android.Views;
using Android.Window;
using AndroidView = Android.Views.View;

namespace MyVocaList.Platforms;

/// <summary>
/// Routes the system back press to a callback while a DXPopup window has focus. DXPopup shows its
/// content in a separate focused window with no back handler, so the press is otherwise swallowed.
/// API 33+: OnBackInvokedCallback on the popup window's dispatcher (PRIORITY_DEFAULT).
/// API 24–32: KEYCODE_BACK key listener on the popup content's root view.
/// </summary>
internal static class PopupBackHandler
{
    /// <summary>
    /// Attaches the back route to the window that hosts <paramref name="contentView"/>.
    /// Returns null when the view or its back dispatcher is unavailable; otherwise dispose the result to detach.
    /// </summary>
    public static IDisposable? Attach(AndroidView? contentView, Action onBack)
    {
        if (contentView is null)
            return null;

        if (OperatingSystem.IsAndroidVersionAtLeast(33))
            return AttachDispatcherCallback(contentView, onBack);

        contentView.Focusable = true;
        contentView.FocusableInTouchMode = true;
        contentView.SetOnKeyListener(new BackKeyListener(onBack));
        contentView.RequestFocus();
        return new Registration(() => contentView.SetOnKeyListener(null));
    }

    [SupportedOSPlatform("android33.0")]
    private static IDisposable? AttachDispatcherCallback(AndroidView contentView, Action onBack)
    {
        var dispatcher = contentView.FindOnBackInvokedDispatcher();
        if (dispatcher is null)
            return null;

        var callback = new BackInvokedCallback(onBack);
        dispatcher.RegisterOnBackInvokedCallback(IOnBackInvokedDispatcher.PriorityDefault, callback);
        return new Registration(() => dispatcher.UnregisterOnBackInvokedCallback(callback));
    }

    private sealed class BackInvokedCallback(Action onBack) : Java.Lang.Object, IOnBackInvokedCallback
    {
        public void OnBackInvoked() => onBack();
    }

    private sealed class BackKeyListener(Action onBack) : Java.Lang.Object, AndroidView.IOnKeyListener
    {
        public bool OnKey(AndroidView? v, Keycode keyCode, KeyEvent? e)
        {
            if (keyCode != Keycode.Back)
                return false;

            if (e?.Action == KeyEventActions.Up)
                onBack();

            return true;
        }
    }

    private sealed class Registration(Action detach) : IDisposable
    {
        public void Dispose() => detach();
    }
}
