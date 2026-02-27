namespace MyVocaList.UI.Pages.Venues;

public partial class VenuesPage : ContentPage
{
    private readonly VenuesViewModel _viewModel;

    public VenuesPage(VenuesViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;

        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Assign the ViewModel's SelectedVenues directly to avoid reflection-based binding
        // DXCollectionView.SelectedItems requires IList; use the collection from ViewModel
        if (collectionView != null)
            collectionView.SelectedItems = _viewModel.SelectedVenues;

        _ = _viewModel.InitializeAsync();
    }

    private void OnViewModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(VenuesViewModel.ConfirmSheetState))
        {
            var state = _viewModel.ConfirmSheetState;
            if (state == BottomSheetState.Hidden)
                confirmSheet.Close();
            else
                confirmSheet.Show(state, this);
        }
    }

    private void OnItemTapped(object sender, CollectionViewGestureEventArgs e)
    {
        // When in multi-select mode, DXCollectionView (SelectionMode.Multiple) toggles
        // selection natively on tap. Calling TapCommand here would double-toggle and undo it.
        if (_viewModel.IsMultiSelectMode) return;

        if (e.Item is VenueListItemDto item)
            _viewModel.TapCommand.Execute(item);
    }

    private void OnItemLongPressed(object sender, CollectionViewGestureEventArgs e)
    {
        if (e.Item is not VenueListItemDto item) return;
        _viewModel.EnterMultiSelectMode(item);
        HapticFeedback.Default.Perform(HapticFeedbackType.LongPress);
    }

    private void OnSwipeItemShowing(object sender, SwipeItemShowingEventArgs e)
    {
        if (_viewModel.IsMultiSelectMode)
            e.Cancel = true;
    }

    protected override bool OnBackButtonPressed()
    {
        if (_viewModel.ConfirmSheetState != BottomSheetState.Hidden)
        {
            _viewModel.ConfirmSheetState = BottomSheetState.Hidden;
            return true;
        }

        if (_viewModel.IsMultiSelectMode)
        {
            _viewModel.ExitMultiSelectMode();
            return true;
        }

        return false;
    }

    private void OnSelectionChanged(object sender, CollectionViewSelectionChangedEventArgs e)
    {
        var count = (collectionView.SelectedItems as System.Collections.ICollection)?.Count ?? 0;
        _viewModel.OnSelectionChanged(count);
    }

    private void OnConfirmSheetStateChanged(object sender, ValueChangedEventArgs<BottomSheetState> e)
    {
        if (e.NewValue == BottomSheetState.Hidden && _viewModel.ConfirmSheetState != BottomSheetState.Hidden)
            _viewModel.ConfirmSheetState = BottomSheetState.Hidden;
    }
}
