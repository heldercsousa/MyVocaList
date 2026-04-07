namespace MyVocaList.UI.Pages.Venues;

public partial class VenueFormPage : ContentPage
{
    public VenueFormPage(VenueFormViewModel viewModel)
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
