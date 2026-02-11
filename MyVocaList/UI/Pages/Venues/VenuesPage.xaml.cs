using DevExpress.Maui.CollectionView;
using DevExpress.Maui.Controls;
using DevExpress.Maui.Core;
using MyVocaList.Contracts.DTOs.List;
using MyVocaList.UI.ViewModels;

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

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(VenuesViewModel.BottomSheetState))
        {
            var state = _viewModel.BottomSheetState;
            if (state == BottomSheetState.Hidden)
                editBottomSheet.Close();
            else
                editBottomSheet.Show(state, this);
        }
    }

    private void OnItemTapped(object sender, CollectionViewGestureEventArgs e)
    {
        if (e.Item is VenueListItemDto item)
            _viewModel.TapCommand.Execute(item);
    }

    private void OnItemLongPressed(object sender, CollectionViewGestureEventArgs e)
    {
        if (e.Item is VenueListItemDto item)
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.LongPress);
            _viewModel.EnterMultiSelectMode(item);
        }
    }

    private void OnSelectionChanged(object sender, CollectionViewSelectionChangedEventArgs e)
    {
        var count = collectionView.SelectedItems?.Cast<object>().Count() ?? 0;
        _viewModel.OnSelectionChanged(count);
    }

    private void OnBottomSheetStateChanged(object sender, ValueChangedEventArgs<BottomSheetState> e)
    {
        if (e.NewValue == BottomSheetState.Hidden && _viewModel.BottomSheetState != BottomSheetState.Hidden)
            _viewModel.BottomSheetState = BottomSheetState.Hidden;
    }
}
