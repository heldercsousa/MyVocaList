namespace MyVocaList.UI.Pages.Artists;

public partial class ArtistFormPage : ContentPage
{
    public ArtistFormPage(ArtistFormViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        nameEdit.Focus();
    }
}
