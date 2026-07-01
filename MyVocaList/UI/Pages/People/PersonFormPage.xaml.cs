namespace MyVocaList.UI.Pages.People;

public partial class PersonFormPage : ContentPage
{
    private readonly PersonFormViewModel _viewModel;

    /// <summary>Exposed for compiled bindings inside DataTemplates.</summary>
    public PersonFormViewModel ViewModel => _viewModel;

    public PersonFormPage(PersonFormViewModel viewModel)
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
            nameField.Focus();
    }

    // Bridges the MAUI Unfocused (blur) events to the ViewModel's validation commands.
    private void OnBirthdayUnfocused(object sender, FocusEventArgs e) =>
        _viewModel.ValidateBirthdayCommand.Execute(null);

    private void OnEmailUnfocused(object sender, FocusEventArgs e) =>
        _viewModel.ValidateEmailCommand.Execute(null);
}
