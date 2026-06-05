namespace MyVocaList.UI.Pages.Venues;

public partial class VenuesPage : CrudListPageBase
{
    private readonly VenuesViewModel _viewModel;

    /// <summary>Exposed for compiled bindings inside DataTemplates.</summary>
    public VenuesViewModel ViewModel => _viewModel;

    protected override ICrudListViewModel ListViewModel => _viewModel;

    public VenuesPage(VenuesViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
        AttachViewModel();

#pragma warning disable CS0618 // Obsolete events used until Step 7b migrates this page to CrudListView
        ConfirmSheetStateRequired += (_, state) =>
        {
            if (state == BottomSheetState.Hidden) confirmSheet.Close();
            else confirmSheet.Show(state, this);
        };
        SelectionItemsWireUpRequired += (_, _) =>
        {
            if (collectionView != null)
                collectionView.SelectedItems = _viewModel.SelectedVenuesRaw;
        };
#pragma warning restore CS0618
    }
}
