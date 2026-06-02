using DevExpress.Maui.CollectionView;

namespace MyVocaList.UI.Pages.Songs;

public partial class SongsPage : ContentPage
{
    private readonly SongsViewModel _viewModel;

    public SongsPage(SongsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SongsViewModel.ConfirmSheetState))
        {
            var state = _viewModel.ConfirmSheetState;
            if (state == BottomSheetState.Hidden)
                confirmSheet.Close();
            else
                confirmSheet.Show(state, this);
        }
    }

    private void OnConfirmSheetStateChanged(object sender, ValueChangedEventArgs<BottomSheetState> e)
    {
        if (e.NewValue == BottomSheetState.Hidden && _viewModel.ConfirmSheetState != BottomSheetState.Hidden)
            _viewModel.ConfirmSheetState = BottomSheetState.Hidden;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (collectionView != null)
            collectionView.SelectedItems = _viewModel.SelectedSongsRaw;
        _ = _viewModel.InitializeAsync();
    }

    protected override bool OnBackButtonPressed()
    {
        if (_viewModel.ConfirmSheetState != BottomSheetState.Hidden)
        {
            _viewModel.ConfirmSheetState = BottomSheetState.Hidden;
            return true;
        }
        if (_viewModel.IsSearchMode)
        {
            _viewModel.CloseSearchCommand.Execute(null);
            return true;
        }
        return base.OnBackButtonPressed();
    }

    private void OnCollectionViewScrolled(object sender, DXCollectionViewScrolledEventArgs e)
    {
        _viewModel.IsScrolled = e.Offset > 0;
    }

    private void OnSelectionChanged(object sender, CollectionViewSelectionChangedEventArgs e)
    {
        var count = (collectionView.SelectedItems as System.Collections.ICollection)?.Count ?? 0;
        _viewModel.OnSelectionChanged(count);
    }

    private void OnItemTapped(object sender, CollectionViewGestureEventArgs e)
    {
        // Row tap = selection toggle only. Edit via FloatingToolbar edit button.
    }
}
