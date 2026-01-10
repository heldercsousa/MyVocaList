using Microsoft.Extensions.Logging;
using System.Diagnostics;
using UraniumUI.Pages;

namespace MyVocaList.UI.Pages.DesignSystem;

public partial class HomePage : UraniumContentPage
{
    private readonly ILogger<HomePage> _logger;
    private readonly Stopwatch _stopwatch;

    public HomePage(ILogger<HomePage> logger)
    {
        _logger = logger;
        _stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("HomePage: Constructor started");
        InitializeComponent();
        _logger.LogInformation("HomePage: Constructor completed after {ElapsedMs}ms", _stopwatch.ElapsedMilliseconds);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _logger.LogInformation("HomePage: OnAppearing completed after {ElapsedMs}ms from constructor", _stopwatch.ElapsedMilliseconds);
    }

    /// <summary>
    /// Opens the URL in the default browser when a link is tapped
    /// </summary>
    private async void OnUrlTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is string url)
        {
            await Launcher.OpenAsync(url);
        }
    }
}