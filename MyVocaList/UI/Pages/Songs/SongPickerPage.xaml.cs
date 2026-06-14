namespace MyVocaList.UI.Pages.Songs;

public partial class SongPickerPage : ContentPage
{
    public SongPickerPage(SongPickerViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
