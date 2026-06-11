namespace MyVocaList.UI.Pages.Songs;

public partial class SongPickerPage : ContentPage
{
    public SongPickerPage(QueueSongPickerViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
