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

#pragma warning disable CS0618 // Obsolete events used until Step 7d migrates this page to CrudListView
        ConfirmSheetStateRequired += (_, state) =>
        {
            if (state == BottomSheetState.Hidden) confirmSheet.Close();
            else confirmSheet.Show(state, this);
        };
        SelectionItemsWireUpRequired += (_, _) =>
        {
            if (collectionView != null)
                collectionView.SelectedItems = _viewModel.SelectedSongsRaw;
        };
#pragma warning restore CS0618
    }

    private void OnItemTapped(object sender, CollectionViewGestureEventArgs e)
    {
        // Row tap = selection toggle only. Edit via FloatingToolbar edit button.
    }
}
