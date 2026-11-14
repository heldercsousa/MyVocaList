using System.Diagnostics;
using System.Text.Json;
using Microsoft.Maui.Controls;
using MyVocaList.Domain.Rules;
using MyVocaList.UI.ViewModels;

namespace MyVocaList.UI.Pages.Venues;

/// <summary>
/// Code-behind of the venue map picker. It only bridges the web map's raw messages and the ViewModel, and sets
/// the web view's user agent (tile usage policy). It holds no business logic and requests no device location.
/// </summary>
public partial class VenueMapPickerPage : ContentPage
{
    // Same application identity as the other outbound HTTP clients (tile usage policy identifies the app).
    private const string AppUserAgent = "MyVocaList/1.0 (https://github.com/heldercsousa/myvocalist)";

    private VenueMapPickerViewModel ViewModel => (VenueMapPickerViewModel)BindingContext;

    public VenueMapPickerPage(VenueMapPickerViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;

        mapView.RawMessageReceived += OnRawMessageReceived;
        mapView.HandlerChanged += OnMapHandlerChanged;
        viewModel.ViewRequested += OnViewRequested;
        Unloaded += OnUnloaded;
    }

    // Shell applies the lat/lon/q query parameters to the BindingContext (QueryProperty on the ViewModel).

    private async void OnRawMessageReceived(object? sender, HybridWebViewRawMessageReceivedEventArgs e)
    {
        using var document = JsonDocument.Parse(e.Message);
        var root = document.RootElement;
        var type = root.GetProperty("type").GetString();

        switch (type)
        {
            case "ready":
                await ViewModel.InitializeAsync();
                break;

            case "tap":
                await ViewModel.OnMapTappedAsync(root.GetProperty("lat").GetDouble(), root.GetProperty("lon").GetDouble());
                break;

            default:
                Debug.WriteLine($"VenueMapPickerPage: unknown map message type '{type}'");
                break;
        }
    }

    private void OnViewRequested(object? sender, MapView view)
    {
        var json = JsonSerializer.Serialize(new
        {
            type = "view",
            lat = view.Latitude,
            lon = view.Longitude,
            zoom = view.Zoom,
            marker = view.ShowMarker
        });

        MainThread.BeginInvokeOnMainThread(() => mapView.SendRawMessage(json));
    }

    private void OnMapHandlerChanged(object? sender, EventArgs e)
    {
#if ANDROID
        if (mapView.Handler?.PlatformView is Android.Webkit.WebView androidView)
            androidView.Settings.UserAgentString = AppUserAgent;
#elif IOS
        if (mapView.Handler?.PlatformView is WebKit.WKWebView wkWebView)
            wkWebView.CustomUserAgent = AppUserAgent;
#endif
    }

    private void OnUnloaded(object? sender, EventArgs e)
    {
        mapView.RawMessageReceived -= OnRawMessageReceived;
        mapView.HandlerChanged -= OnMapHandlerChanged;
        ViewModel.ViewRequested -= OnViewRequested;
        Unloaded -= OnUnloaded;
    }
}
