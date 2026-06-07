namespace MyVocaList.UI.Pages.Artists;

public partial class ArtistPickerPage : ContentPage
{
    public ArtistPickerPage(ArtistPickerViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
