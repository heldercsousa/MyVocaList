namespace MyVocaList.UI.Pages.Artists;

public partial class ArtistsPage : CrudListPageBase
{
    private readonly ArtistsViewModel _viewModel;

    protected override ICrudListViewModel ListViewModel => _viewModel;

    public ArtistsPage(ArtistsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
        AttachViewModel();
    }
}
