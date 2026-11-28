using System.ComponentModel;
using MyVocaList.UI.ViewModels;

namespace MyVocaList.UI.Pages.Songs;

/// <summary>
/// Hosts the YouTube IFrame player (REQ-MSRC-07). The web view's base URL is the app's package id so the
/// player gets a valid origin (avoids error 153). Leaving the page clears the source, so nothing plays in the background.
/// </summary>
public partial class YouTubePlayerPage : ContentPage
{
    private YouTubePlayerViewModel ViewModel => (YouTubePlayerViewModel)BindingContext;

    public YouTubePlayerPage(YouTubePlayerViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ApplyHtml();
    }

    protected override void OnDisappearing()
    {
        ViewModel.StopCommand.Execute(null);
        base.OnDisappearing();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(YouTubePlayerViewModel.Html))
            ApplyHtml();
    }

    private void ApplyHtml()
    {
        playerView.Source = string.IsNullOrEmpty(ViewModel.Html)
            ? null
            : new HtmlWebViewSource { Html = ViewModel.Html, BaseUrl = "https://" + AppInfo.PackageName };
    }
}
