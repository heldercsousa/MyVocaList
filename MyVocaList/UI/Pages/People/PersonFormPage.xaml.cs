using System.ComponentModel;
using DevExpress.Maui.Editors;
using MyVocaList.Contracts.Models;

namespace MyVocaList.UI.Pages.People;

public partial class PersonFormPage : ContentPage
{
    private readonly PersonFormViewModel _viewModel;

    // Re-entrancy guard for the two-way same-name sheet sync (BUG-088), the same pattern as SongFormPage.
    private bool _isSyncingSameNameSheet;

    /// <summary>Exposed for compiled bindings inside DataTemplates.</summary>
    public PersonFormViewModel ViewModel => _viewModel;

    public PersonFormPage(PersonFormViewModel viewModel)
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
        // OnAppearing runs — end the hydration window so subsequent edits are tracked as dirty.
        _viewModel.CompleteHydration();

        // Focus the name field only in create mode
        if (!_viewModel.IsEditMode)
            nameField.FocusEditor();
    }

    // Bridges the MAUI Unfocused (blur) events to the ViewModel's validation commands.
    private void OnEmailUnfocused(object sender, FocusEventArgs e) =>
        _viewModel.ValidateEmailCommand.Execute(null);

    // BUG-088 Fix B: the Open button sits inside dx:BottomSheet content, where the command binding did not fire on device.
    // The handler only forwards the row's own BindingContext; the logic stays in OpenSameNameCommand.
    // BUG-088 T8 (b): the name field keeps focus when the sheet closes, so the loaded name would read as typing and open
    // the suggestions. A pick ends the input the same way (AutocompleteDXField unfocuses after SelectCommand), so Open does too.
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

    // The flag drives Show/Close (BUG-088, REQ-DUP-09). Same pattern as SongFormPage.
    private void SyncSameNameSheetState()
    {
        if (_isSyncingSameNameSheet) return;

        _isSyncingSameNameSheet = true;
        try
        {
            if (_viewModel.IsSameNameSheetVisible)
                sameNameSheet.Show(BottomSheetState.HalfExpanded, this);
            else
                sameNameSheet.Close();
        }
        finally
        {
            _isSyncingSameNameSheet = false;
        }
    }

    // A user-initiated Hidden (scrim, Back, swipe) goes through the same dismiss command as Cancel,
    // so nothing is saved. The guard keeps the Close() issued by SyncSameNameSheetState from re-entering.
    private void OnSameNameSheetStateChanged(object sender, ValueChangedEventArgs<BottomSheetState> e)
    {
        if (_isSyncingSameNameSheet) return;

        if (e.NewValue == BottomSheetState.Hidden && _viewModel.IsSameNameSheetVisible)
        {
            _isSyncingSameNameSheet = true;
            try
            {
                _viewModel.DismissSameNameSheetCommand.Execute(null);
            }
            finally
            {
                _isSyncingSameNameSheet = false;
            }
        }
    }
}
