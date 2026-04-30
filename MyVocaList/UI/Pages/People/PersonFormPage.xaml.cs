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

        // Focus the name field only in create mode
        if (!_viewModel.IsEditMode)
            nameField.Focus();
    }
}
