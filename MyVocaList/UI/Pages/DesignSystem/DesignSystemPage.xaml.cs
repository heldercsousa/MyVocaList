using Microsoft.Extensions.Logging;
using System.Diagnostics;
using UraniumUI.Pages;

namespace MyVocaList.UI.Pages.DesignSystem;

/// <summary>
/// Demonstrates fundamental UI components using UraniumUI Material Design 3
/// </summary>
public partial class DesignSystemPage : UraniumContentPage
{
    private readonly ILogger<DesignSystemPage> _logger;
    private readonly Stopwatch _stopwatch;

    public DesignSystemPage(ILogger<DesignSystemPage> logger)
    {
        _logger = logger;
        _stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("DesignSystemPage: Constructor started");
        InitializeComponent();
        _logger.LogInformation("DesignSystemPage: Constructor completed after {ElapsedMs}ms", _stopwatch.ElapsedMilliseconds);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _logger.LogInformation("DesignSystemPage: OnAppearing completed after {ElapsedMs}ms from constructor", _stopwatch.ElapsedMilliseconds);
    }
}