using Microsoft.Extensions.Logging;
using System.Diagnostics;
using UraniumUI.Pages;

namespace MyVocaList.UI.Pages.DesignSystem;

/// <summary>
/// Demonstrates fundamental UI components using UraniumUI Material Design 3
/// </summary>
public partial class DesignSystemPage : UraniumContentPage
{
    private readonly Stopwatch _stopwatch;

    public DesignSystemPage()
    {
        _stopwatch = Stopwatch.StartNew();

        Console.WriteLine($"DesignSystemPage: Constructor started");
        InitializeComponent();
        Console.WriteLine($"DesignSystemPage: Constructor completed after {_stopwatch.ElapsedMilliseconds}ms");
    }

    //protected override void OnAppearing()
    //{
    //    base.OnAppearing();
    //    Console.WriteLine($"DesignSystemPage: OnAppearing completed after {_stopwatch.ElapsedMilliseconds}ms from constructor");
    //}
}