namespace MyVocaList.UI.Pages.Songs;

public partial class YouTubeSearchPage : ContentPage
{
    public YouTubeSearchPage(YouTubeSearchViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
