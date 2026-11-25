namespace MyVocaList.UI.Pages.Artists;

public partial class ArtistsSfPage : CrudListPageBase
{
    private readonly ArtistsViewModel _viewModel;

    protected override ICrudListViewModel ListViewModel => _viewModel;

    public ArtistsSfPage(ArtistsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
        AttachViewModel();
    }
}
