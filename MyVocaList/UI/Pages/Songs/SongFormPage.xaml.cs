namespace MyVocaList.UI.Pages.Songs;

public partial class SongFormPage : ContentPage
{
    private SongFormViewModel ViewModel => (SongFormViewModel)BindingContext;

    public SongFormPage(SongFormViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        titleEdit.Focus();

        // Shell has finished applying [QueryProperty] values for this navigation by the time
        // OnAppearing runs — end the hydration window so subsequent edits are tracked as dirty.
        ViewModel.CompleteHydration();

        // BUG-020: RefreshApiKeyFlagAsync wraps SecureStorage.GetAsync in its own try-catch so a
        // corrupted Android keystore alias cannot crash this async void OnAppearing — do not
        // remove that try-catch.
        await ViewModel.RefreshApiKeyFlagAsync();
        ViewModel.InitializeArtistField(); // BUG-008: reliable artist field init after all query props set
    }

    // Bridges the MAUI Unfocused (blur) events to the ViewModel's validation commands.
    private void OnTitleUnfocused(object sender, FocusEventArgs e) =>
        ViewModel.ValidateTitleCommand.Execute(null);

    private void OnVersionUnfocused(object sender, FocusEventArgs e) =>
        ViewModel.ValidateVersionCommand.Execute(null);
}
