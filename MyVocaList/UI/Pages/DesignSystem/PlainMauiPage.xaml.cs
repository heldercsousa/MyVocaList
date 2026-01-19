using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace MyVocaList.UI.Pages.DesignSystem;

/// <summary>
/// Vanilla MAUI implementation for performance comparison with UraniumUI version
/// </summary>
public partial class PlainMauiPage : ContentPage
{
    private readonly Stopwatch _stopwatch;

    public PlainMauiPage()
    {
        _stopwatch = Stopwatch.StartNew();

        Console.WriteLine($"PlainMauiPage: Constructor started");
        InitializeComponent();
        Console.WriteLine($"PlainMauiPage: Constructor completed after {_stopwatch.ElapsedMilliseconds}ms");
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Console.WriteLine($"PlainMauiPage: OnAppearing completed after {_stopwatch.ElapsedMilliseconds}ms from constructor");
    }
}
