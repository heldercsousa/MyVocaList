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

        // Focus the name field only in create mode, and only while it shows its editor: a defined Name
        // shows as text (REQ-PIC-25) and opens by a tap. Blur validation is bound in XAML
        // (BlurWithoutPickCommand → ValidateNameCommand).
        if (!_viewModel.IsEditMode && nameTapToEdit.IsEditorVisible)
            nameField.FocusEditor();
    }
}
