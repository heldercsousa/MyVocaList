using System.Diagnostics;
using UraniumUI.Pages;

namespace MyVocaList.UI.Pages.DesignSystem;

/// <summary>
/// Demonstrates all 15 MD3 typography roles using Roboto font family
/// </summary>
public partial class ComponentsPage_Typography : UraniumContentPage
{
    private readonly Stopwatch _stopwatch;

    public ComponentsPage_Typography()
    {
        _stopwatch = Stopwatch.StartNew();

        Console.WriteLine($"ComponentsPage_Typography: Constructor started");
        InitializeComponent();
        Console.WriteLine($"ComponentsPage_Typography: Constructor completed after {_stopwatch.ElapsedMilliseconds}ms");
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Console.WriteLine($"ComponentsPage_Typography: OnAppearing completed after {_stopwatch.ElapsedMilliseconds}ms from constructor");
    }
}
