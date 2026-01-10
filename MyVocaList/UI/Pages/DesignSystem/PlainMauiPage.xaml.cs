using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace MyVocaList.UI.Pages.DesignSystem;

/// <summary>
/// Vanilla MAUI implementation for performance comparison with UraniumUI version
/// </summary>
public partial class PlainMauiPage : ContentPage
{
    private readonly ILogger<PlainMauiPage> _logger;
    private readonly Stopwatch _stopwatch;

    public PlainMauiPage(ILogger<PlainMauiPage> logger)
    {
        _logger = logger;
        _stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("PlainMauiPage: Constructor started");
        InitializeComponent();
        _logger.LogInformation("PlainMauiPage: Constructor completed after {ElapsedMs}ms", _stopwatch.ElapsedMilliseconds);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _logger.LogInformation("PlainMauiPage: OnAppearing completed after {ElapsedMs}ms from constructor", _stopwatch.ElapsedMilliseconds);
    }
}
