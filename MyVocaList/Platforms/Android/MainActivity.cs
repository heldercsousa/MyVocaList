using Android.App;
using Android.Content;
using Android.Content.PM;
using MyVocaList.Domain.ServicesInterfaces;

namespace MyVocaList;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
// REQ-MSRC-06: lets the YouTube app share a video link to MyVocaList.
[IntentFilter(new[] { Intent.ActionSend }, Categories = new[] { Intent.CategoryDefault }, DataMimeType = "text/plain")]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Android.OS.Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        ForwardSharedText(Intent);
    }

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        ForwardSharedText(intent);
    }

    private static void ForwardSharedText(Intent? intent)
    {
        if (intent?.Action != Intent.ActionSend) return;

        var text = intent.GetStringExtra(Intent.ExtraText);
        if (string.IsNullOrWhiteSpace(text)) return;

        IPlatformApplication.Current?.Services.GetService<ISharedLinkInbox>()?.Receive(text);
    }
}
