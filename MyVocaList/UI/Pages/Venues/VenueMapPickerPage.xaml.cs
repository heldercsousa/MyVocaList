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
        string? type;
        double lat = 0;
        double lon = 0;

        // A malformed or incomplete message from the web map is logged and ignored (it must not crash the page).
        try
        {
            using var document = JsonDocument.Parse(e.Message);
            var root = document.RootElement;
            type = root.GetProperty("type").GetString();

            if (type == "tap")
            {
                lat = root.GetProperty("lat").GetDouble();
                lon = root.GetProperty("lon").GetDouble();
            }
        }
        catch (JsonException ex)
        {
            Debug.WriteLine($"VenueMapPickerPage: malformed map message ignored: {ex.Message}");
            return;
        }
        catch (KeyNotFoundException ex)
        {
            Debug.WriteLine($"VenueMapPickerPage: incomplete map message ignored: {ex.Message}");
            return;
        }
        catch (InvalidOperationException ex)
        {
            Debug.WriteLine($"VenueMapPickerPage: map message with wrong value kind ignored: {ex.Message}");
            return;
        }

        switch (type)
        {
            case "ready":
                // The web view raises this event off the UI thread; the ViewModel state is bound to views.
                await MainThread.InvokeOnMainThreadAsync(() => ViewModel.InitializeAsync());
                break;

            case "tap":
                await MainThread.InvokeOnMainThreadAsync(() => ViewModel.OnMapTappedAsync(lat, lon));
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
