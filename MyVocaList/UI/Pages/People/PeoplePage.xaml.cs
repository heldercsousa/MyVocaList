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

        ConfirmSheetStateRequired += (_, state) =>
        {
            if (state == BottomSheetState.Hidden) confirmSheet.Close();
            else confirmSheet.Show(state, this);
        };
        SelectionItemsWireUpRequired += (_, _) =>
        {
            if (collectionView != null)
                collectionView.SelectedItems = _viewModel.SelectedPersonsRaw;
        };
    }
}
