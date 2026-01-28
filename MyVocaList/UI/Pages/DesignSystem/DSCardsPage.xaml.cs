using System.Diagnostics;
using UraniumUI.Pages;

namespace MyVocaList.UI.Pages.DesignSystem;

/// <summary>
/// Demonstrates MD3 card types using MDC components and Frame fallbacks
/// </summary>
public partial class DSCardsPage : UraniumContentPage
{
    private readonly Stopwatch _stopwatch;

    public DSCardsPage()
    {
        _stopwatch = Stopwatch.StartNew();

        Console.WriteLine($"DSCardsPage: Constructor started");
        InitializeComponent();
        Console.WriteLine($"DSCardsPage: Constructor completed after {_stopwatch.ElapsedMilliseconds}ms");
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Console.WriteLine($"DSCardsPage: OnAppearing completed after {_stopwatch.ElapsedMilliseconds}ms from constructor");
    }
}
