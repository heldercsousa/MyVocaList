
namespace MyVocaList.UI.Pages.Songs;

public partial class YouTubeSearchSfPage : ContentPage
{
    private readonly YouTubeSearchViewModel _viewModel;

    public YouTubeSearchSfPage(YouTubeSearchViewModel viewModel)
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
