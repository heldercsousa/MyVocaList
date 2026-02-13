using DevExpress.Maui.CollectionView;
using DevExpress.Maui.Controls;
using DevExpress.Maui.Core;
using MyVocaList.Contracts.DTOs.List;
using MyVocaList.UI.ViewModels;
using Microsoft.Maui.Controls;

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

        // Garante que o SelectedItems do control receba a cole��o exata da ViewModel
        // (evita BindingDiagnostics / reflection cost / discrep�ncias entre inst�ncias)
        try
        {
            if (collectionView != null && _viewModel != null)
            {
                // DXCollectionView.SelectedItems aceita IList � usamos a cole��o da VM
                collectionView.SelectedItems = _viewModel.SelectedVenues;
            }
        }
        catch
        {
            // fail silently � n�o interromper exibi��o da p�gina
        }

        _ = _viewModel.InitializeAsync();
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
        else if (e.PropertyName == nameof(VenuesViewModel.ConfirmSheetState))
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

    protected override bool OnBackButtonPressed()
    {
        if (_viewModel.ConfirmSheetState != DevExpress.Maui.Controls.BottomSheetState.Hidden)
        {
            _viewModel.ConfirmSheetState = DevExpress.Maui.Controls.BottomSheetState.Hidden;
            return true;
        }

        if (_viewModel.BottomSheetState != DevExpress.Maui.Controls.BottomSheetState.Hidden)
        {
            _viewModel.BottomSheetState = DevExpress.Maui.Controls.BottomSheetState.Hidden;
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

    private void OnBottomSheetStateChanged(object sender, ValueChangedEventArgs<BottomSheetState> e)
    {
        if (e.NewValue == BottomSheetState.Hidden && _viewModel.BottomSheetState != BottomSheetState.Hidden)
            _viewModel.BottomSheetState = BottomSheetState.Hidden;
    }

    private void OnConfirmSheetStateChanged(object sender, ValueChangedEventArgs<BottomSheetState> e)
    {
        if (e.NewValue == BottomSheetState.Hidden && _viewModel.ConfirmSheetState != BottomSheetState.Hidden)
            _viewModel.ConfirmSheetState = BottomSheetState.Hidden;
    }
}
