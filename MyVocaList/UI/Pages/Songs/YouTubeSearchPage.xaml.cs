
namespace MyVocaList.UI.Pages.Songs;

public partial class YouTubeSearchPage : ContentPage
{
    private readonly YouTubeSearchViewModel _viewModel;

    public YouTubeSearchPage(YouTubeSearchViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    private void OnItemTapped(object? sender, Syncfusion.Maui.ListView.ItemTappedEventArgs e)
    {
        if (e.DataItem is YouTubeSearchResultDto dto && _viewModel.SelectResultCommand.CanExecute(dto))
            _viewModel.SelectResultCommand.Execute(dto);
    }
}
