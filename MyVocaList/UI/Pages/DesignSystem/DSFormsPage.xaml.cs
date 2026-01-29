using System.Diagnostics;
using UraniumUI.Pages;

namespace MyVocaList.UI.Pages.DesignSystem;

/// <summary>
/// Demonstrates UraniumUI automatic form generation from ViewModel properties
/// </summary>
public partial class DSFormsPage : UraniumContentPage
{
    private readonly Stopwatch _stopwatch;

    public DSFormsPage()
    {
        _stopwatch = Stopwatch.StartNew();

        Console.WriteLine($"DSFormsPage: Constructor started");
        InitializeComponent();
        Console.WriteLine($"DSFormsPage: Constructor completed after {_stopwatch.ElapsedMilliseconds}ms");
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Console.WriteLine($"DSFormsPage: OnAppearing completed after {_stopwatch.ElapsedMilliseconds}ms from constructor");
    }
}
