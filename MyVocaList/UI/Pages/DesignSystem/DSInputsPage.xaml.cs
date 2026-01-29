using System.Diagnostics;
using UraniumUI.Pages;

namespace MyVocaList.UI.Pages.DesignSystem;

/// <summary>
/// Demonstrates MD3 input controls including text fields and selection controls
/// </summary>
public partial class DSInputsPage : UraniumContentPage
{
    private readonly Stopwatch _stopwatch;

    public DSInputsPage()
    {
        _stopwatch = Stopwatch.StartNew();

        Console.WriteLine($"DSInputsPage: Constructor started");
        InitializeComponent();
        Console.WriteLine($"DSInputsPage: Constructor completed after {_stopwatch.ElapsedMilliseconds}ms");
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Console.WriteLine($"DSInputsPage: OnAppearing completed after {_stopwatch.ElapsedMilliseconds}ms from constructor");
    }
}
