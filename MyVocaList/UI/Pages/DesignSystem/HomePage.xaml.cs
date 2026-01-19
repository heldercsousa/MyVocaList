using Microsoft.Extensions.Logging;
using System.Diagnostics;
using UraniumUI.Pages;

namespace MyVocaList.UI.Pages.DesignSystem;

public partial class HomePage : UraniumContentPage
{

    private readonly Stopwatch _stopwatch;

    public HomePage()
    {
        _stopwatch = Stopwatch.StartNew();

        Console.WriteLine($"HomePage: Constructor started");
        InitializeComponent();
        Console.WriteLine($"HomePage: Constructor completed after {_stopwatch.ElapsedMilliseconds}ms");
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Console.WriteLine($"HomePage: OnAppearing completed after {_stopwatch.ElapsedMilliseconds}ms from constructor");
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