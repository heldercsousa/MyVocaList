using System.Diagnostics;
using UraniumUI.Pages;

namespace MyVocaList.UI.Pages.DesignSystem;

/// <summary>
/// Navigation hub for Material Design 3 component library
/// </summary>
public partial class DSPage : UraniumContentPage
{
    private readonly Stopwatch _stopwatch;

    public DSPage()
    {
        _stopwatch = Stopwatch.StartNew();

        Console.WriteLine($"DSPage: Constructor started");
        InitializeComponent();
        Console.WriteLine($"DSPage: Constructor completed after {_stopwatch.ElapsedMilliseconds}ms");
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Console.WriteLine($"DSPage: OnAppearing completed after {_stopwatch.ElapsedMilliseconds}ms from constructor");
    }

    private async void NavigateToTypography(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("DSTypographyPage");

    private async void NavigateToButtons(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("DSButtonsPage");

    private async void NavigateToCards(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("DSCardsPage");

    private async void NavigateToInputs(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("DSInputsPage");
}