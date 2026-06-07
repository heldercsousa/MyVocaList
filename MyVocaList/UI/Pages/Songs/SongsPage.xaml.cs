namespace MyVocaList.UI.Pages.Songs;

public partial class SongsPage : CrudListPageBase
{
    private readonly SongsViewModel _viewModel;

    protected override ICrudListViewModel ListViewModel => _viewModel;

    public SongsPage(SongsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
        AttachViewModel();
    }
}
