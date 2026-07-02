namespace MyVocaList.UI.Pages.Artists;

public partial class ArtistFormPage : ContentPage
{
    private readonly ArtistFormViewModel _viewModel;

    public ArtistFormPage(ArtistFormViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Shell has finished applying [QueryProperty] values for this navigation by the time
        // OnAppearing runs — end the hydration window so subsequent edits are tracked as dirty.
        _viewModel.CompleteHydration();

        // Focus the name field only in create mode
        if (!_viewModel.IsEditMode)
            nameEdit.Focus();
    }

    // Bridges the MAUI Unfocused (blur) event to the ViewModel's validation command.
    private void OnNameUnfocused(object sender, FocusEventArgs e) =>
        _viewModel.ValidateNameCommand.Execute(null);
}
