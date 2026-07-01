namespace MyVocaList.UI.Pages.Venues;

public partial class VenueFormPage : ContentPage
{
    private VenueFormViewModel ViewModel => (VenueFormViewModel)BindingContext;

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

    // Bridges the MAUI Unfocused (blur) event to the ViewModel's validation command.
    private void OnNameUnfocused(object sender, FocusEventArgs e) =>
        ViewModel.ValidateNameCommand.Execute(null);
}
