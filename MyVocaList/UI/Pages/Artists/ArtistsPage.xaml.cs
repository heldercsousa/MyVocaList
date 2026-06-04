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

        ConfirmSheetStateRequired += (_, state) =>
        {
            if (state == BottomSheetState.Hidden) confirmSheet.Close();
            else confirmSheet.Show(state, this);
        };
        SelectionItemsWireUpRequired += (_, _) =>
        {
            if (collectionView != null)
                collectionView.SelectedItems = _viewModel.SelectedArtistsRaw;
        };
    }
}
