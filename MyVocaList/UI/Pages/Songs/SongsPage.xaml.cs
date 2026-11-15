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

    // BUG-086 round 3: CrudListPageBase.OnNavigatedTo always sets the hamburger (CRUD lists are top-level). The catalog
    // is a pushed sub-page, so it overrides the icon AFTER the base call; the governed base class is not changed.
    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        _viewModel.ApplyAppBarNavigation();
    }
}
