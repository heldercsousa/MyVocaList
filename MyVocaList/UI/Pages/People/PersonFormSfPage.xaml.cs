using System.ComponentModel;
using MyVocaList.Contracts.Models;
using SfSheetState = Syncfusion.Maui.Toolkit.BottomSheet.BottomSheetState;

namespace MyVocaList.UI.Pages.People;

/// <summary>Syncfusion copy of <see cref="PersonFormPage"/>: same ViewModel, same code-behind behavior.</summary>
public partial class PersonFormSfPage : ContentPage
{
    private readonly PersonFormViewModel _viewModel;

    // Re-entrancy guard for the two-way same-name sheet sync (BUG-088), the same pattern as SongFormPage.
    private bool _isSyncingSameNameSheet;

    /// <summary>Exposed for compiled bindings inside DataTemplates.</summary>
    public PersonFormViewModel ViewModel => _viewModel;

    public PersonFormSfPage(PersonFormViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Shell has finished applying [QueryProperty] values for this navigation by the time
        // OnAppearing runs: end the hydration window so subsequent edits are tracked as dirty.
        _viewModel.CompleteHydration();

        // Focus the name field only in create mode
        if (!_viewModel.IsEditMode)
            nameField.FocusEditor();
    }

    // Android Back does not close an SfBottomSheet: while the same-name sheet is open, Back dismisses it
    // through the same command as Cancel (nothing is saved) and is consumed.
    protected override bool OnBackButtonPressed()
    {
        if (_viewModel.IsSameNameSheetVisible)
        {
            _viewModel.DismissSameNameSheetCommand.Execute(null);
            return true;
        }

        return base.OnBackButtonPressed();
    }

    // Bridges the MAUI Unfocused (blur) events to the ViewModel's validation commands.
    private void OnEmailUnfocused(object sender, FocusEventArgs e) =>
        _viewModel.ValidateEmailCommand.Execute(null);

    // BUG-088 Fix B: the Open button sits inside the sheet content, where the command binding did not fire on device.
    // The handler only forwards the row's own BindingContext; the logic stays in OpenSameNameCommand.
    // BUG-088 T8 (b): the name field keeps focus when the sheet closes, so the loaded name would read as typing and open
    // the suggestions. A pick ends the input the same way, so Open does too. The drop-down open state is reset by the
    // ViewModel (IsNameDropDownOpen, bound TwoWay in XAML), because Unfocus alone does not close it.
    private void OnSameNameOpenClicked(object? sender, EventArgs e)
    {
        if (sender is BindableObject { BindingContext: AutocompleteSuggestion match })
        {
            nameField.Unfocus();
            _viewModel.OpenSameNameCommand.Execute(match);
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PersonFormViewModel.IsSameNameSheetVisible))
            SyncSameNameSheetState();
    }

    // The flag drives open/close (BUG-088, REQ-DUP-09). Same pattern as SongFormPage.
    private void SyncSameNameSheetState()
    {
        if (_isSyncingSameNameSheet) return;

        _isSyncingSameNameSheet = true;
        try
        {
            if (_viewModel.IsSameNameSheetVisible)
            {
                sameNameSheet.State = SfSheetState.HalfExpanded;
                sameNameSheet.IsOpen = true;
            }
            else
            {
                sameNameSheet.IsOpen = false;
                sameNameSheet.State = SfSheetState.Hidden;
            }
        }
        finally
        {
            _isSyncingSameNameSheet = false;
        }
    }

    // A user-initiated close (scrim, swipe down: Hidden or Collapsed) goes through the same dismiss command as Cancel,
    // so nothing is saved. The guard keeps the close issued by SyncSameNameSheetState from re-entering.
    private void OnSameNameSheetStateChanged(object? sender, Syncfusion.Maui.Toolkit.BottomSheet.StateChangedEventArgs e)
    {
        if (_isSyncingSameNameSheet) return;

        var closed = e.NewState is SfSheetState.Hidden or SfSheetState.Collapsed;
        if (closed && _viewModel.IsSameNameSheetVisible)
        {
            _isSyncingSameNameSheet = true;
            try
            {
                _viewModel.DismissSameNameSheetCommand.Execute(null);

                // The guard makes SyncSameNameSheetState a no-op for the flag change the dismiss causes, so a drag
                // down (Collapsed) would leave the sheet open at its collapsed height. Close it explicitly.
                sameNameSheet.IsOpen = false;
                sameNameSheet.State = SfSheetState.Hidden;
            }
            finally
            {
                _isSyncingSameNameSheet = false;
            }
        }
    }
}
