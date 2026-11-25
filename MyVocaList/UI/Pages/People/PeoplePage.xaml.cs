namespace MyVocaList.UI.Pages.People;

public partial class PeoplePage : CrudListPageBase
{
    private readonly PersonsViewModel _viewModel;

    /// <summary>Exposed for compiled bindings inside DataTemplates.</summary>
    public PersonsViewModel ViewModel => _viewModel;

    protected override ICrudListViewModel ListViewModel => _viewModel;

    public PeoplePage(PersonsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
        AttachViewModel();
    }
}
