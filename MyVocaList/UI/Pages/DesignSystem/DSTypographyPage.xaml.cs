using System.Diagnostics;
using UraniumUI.Pages;

namespace MyVocaList.UI.Pages.DesignSystem;

/// <summary>
/// Demonstrates all 15 MD3 typography roles using Roboto font family
/// </summary>
public partial class DSTypographyPage : UraniumContentPage
{
    private readonly Stopwatch _stopwatch;

    public DSTypographyPage()
    {
        _stopwatch = Stopwatch.StartNew();

        Console.WriteLine($"DSTypographyPage: Constructor started");
        InitializeComponent();
        Console.WriteLine($"DSTypographyPage: Constructor completed after {_stopwatch.ElapsedMilliseconds}ms");
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Console.WriteLine($"DSTypographyPage: OnAppearing completed after {_stopwatch.ElapsedMilliseconds}ms from constructor");
    }
}
