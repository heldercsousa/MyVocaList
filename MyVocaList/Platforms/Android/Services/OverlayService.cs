using Android.App;
using Android.Content;
using Android.OS;
using Android.Provider;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using MyVocaList.Domain.ServicesInterfaces;
using AColor = Android.Graphics.Color;
using AFormat = Android.Graphics.Format;

namespace MyVocaList.Platforms.Android.Services;

public class OverlayService : IOverlayService
{
    private readonly Context _context;
    private IWindowManager? _windowManager;
    private TextView? _overlayView;

    public OverlayService(Context context)
    {
        _context = context;
    }

    public bool IsPermissionGranted =>
        Settings.CanDrawOverlays(_context);

    /// <inheritdoc />
    public void RequestPermission()
    {
        var intent = new Intent(
            Settings.ActionManageOverlayPermission,
            global::Android.Net.Uri.Parse($"package:{_context.PackageName}"));
        intent.AddFlags(ActivityFlags.NewTask);
        _context.StartActivity(intent);
    }

    /// <inheritdoc />
    public void Show(string singerName, string songTitle, OverlayStage stage)
    {
        // ApplicationOverlay requires Android API 26; minSdk is 24 — guard before use
        if (Build.VERSION.SdkInt < BuildVersionCodes.O) return;
        if (!IsPermissionGranted) return;

        Dismiss();

        _windowManager = (_context.GetSystemService(Context.WindowService) as IWindowManager)!;

#pragma warning disable CA1416 // Runtime API-26 guard is present above (Build.VERSION.SdkInt < BuildVersionCodes.O)
        var layoutParams = new WindowManagerLayoutParams(
            ViewGroup.LayoutParams.WrapContent,
            ViewGroup.LayoutParams.WrapContent,
            WindowManagerTypes.ApplicationOverlay,
            WindowManagerFlags.NotFocusable | WindowManagerFlags.NotTouchModal,
            AFormat.Translucent)
#pragma warning restore CA1416
        {
            Gravity = GravityFlags.Top | GravityFlags.End,
            X = 16,
            Y = 80
        };

        _overlayView = new TextView(_context)
        {
            Text = BuildLabelText(singerName, stage),
            TextSize = 16f,
            Tag = singerName
        };
        _overlayView.SetTextColor(stage == OverlayStage.Stage2 ? AColor.Red : AColor.White);
        _overlayView.SetShadowLayer(4f, 2f, 2f, AColor.Black);

        _windowManager.AddView(_overlayView, layoutParams);
        StartPulseAnimation(stage);
    }

    /// <inheritdoc />
    public void UpdateStage(OverlayStage stage)
    {
        if (_overlayView == null) return;
        _overlayView.Text = BuildLabelText(_overlayView.Tag?.ToString() ?? string.Empty, stage);
        _overlayView.SetTextColor(stage == OverlayStage.Stage2 ? AColor.Red : AColor.White);
        StartPulseAnimation(stage);
    }

    /// <inheritdoc />
    public void Dismiss()
    {
        if (_overlayView == null || _windowManager == null) return;
        try
        {
            _windowManager.RemoveView(_overlayView);
        }
        catch { /* view may already be detached */ }
        _overlayView = null;
        _windowManager = null;
    }

    private void StartPulseAnimation(OverlayStage stage)
    {
        if (_overlayView == null) return;
        var duration = stage == OverlayStage.Stage1 ? 1200L : 400L;

        var fadeOut = global::Android.Animation.ObjectAnimator.OfFloat(_overlayView, "alpha", 1f, 0f);
        fadeOut!.SetDuration(duration);
        var fadeIn = global::Android.Animation.ObjectAnimator.OfFloat(_overlayView, "alpha", 0f, 1f);
        fadeIn!.SetDuration(duration);

        var set = new global::Android.Animation.AnimatorSet();
        set.PlaySequentially(fadeOut, fadeIn);
        set.AnimationEnd += (_, _) => { if (_overlayView != null) set.Start(); };
        set.Start();
    }

    private static string BuildLabelText(string singerName, OverlayStage stage)
        => stage == OverlayStage.Stage1
            ? $"NEXT: {singerName}"
            : $"Next — {singerName} — mic now!";
}
