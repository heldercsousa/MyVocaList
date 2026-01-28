using System.Diagnostics;
using UraniumUI.Pages;

namespace MyVocaList.UI.Pages.DesignSystem;

/// <summary>
/// Demonstrates all 5 MD3 button variants with icon support
/// </summary>
public partial class DSButtonsPage : UraniumContentPage
{
    private readonly Stopwatch _stopwatch;

    public DSButtonsPage()
    {
        _stopwatch = Stopwatch.StartNew();

        Console.WriteLine($"DSButtonsPage: Constructor started");
        InitializeComponent();
        Console.WriteLine($"DSButtonsPage: Constructor completed after {_stopwatch.ElapsedMilliseconds}ms");
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Console.WriteLine($"DSButtonsPage: OnAppearing completed after {_stopwatch.ElapsedMilliseconds}ms from constructor");
    }
}
