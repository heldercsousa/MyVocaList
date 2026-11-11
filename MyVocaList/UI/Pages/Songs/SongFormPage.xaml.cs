using System.ComponentModel;

namespace MyVocaList.UI.Pages.Songs;

public partial class SongFormPage : ContentPage
{
    private SongFormViewModel ViewModel => (SongFormViewModel)BindingContext;

    // Re-entrancy guard for the two-way sheet-state sync (BUG-023): prevents the
    // ViewModel -> code-behind -> ViewModel round trip from looping when the sheet
    // is closed programmatically in response to the flag it was opened by.
    private bool _isSyncingResolutionSheet;
    private bool _isSyncingArtistMatchSheet;

    public SongFormPage(SongFormViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // BUG-084: OnDisappearing unsubscribes the sheet-sync handler, so it must be re-attached on every
        // appearance. Detach first so the handler is never registered twice.
        ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;

        titleField.FocusEditor();

        // Shell has finished applying [QueryProperty] values for this navigation by the time
        // OnAppearing runs — end the hydration window so subsequent edits are tracked as dirty.
        ViewModel.CompleteHydration();

        // BUG-020: RefreshApiKeyFlagAsync wraps SecureStorage.GetAsync in its own try-catch so a
        // corrupted Android keystore alias cannot crash this async void OnAppearing — do not
        // remove that try-catch.
        await ViewModel.RefreshApiKeyFlagAsync();
        ViewModel.InitializeArtistField(); // BUG-008: reliable artist field init after all query props set
    }

    private void OnVersionUnfocused(object sender, FocusEventArgs e) =>
        ViewModel.ValidateVersionCommand.Execute(null);

    // BUG-023: resolutionSheet lost its state binding when IsExpanded was removed
    // without replacement. dx:BottomSheet has no bindable "open" property that can be driven
    // declaratively from XAML — Show()/Close() require a host Page — so the sync is done here in
    // code-behind, mirroring the confirmed pattern in ConfirmSheet.xaml.cs (see
    // .claude/library/dialogs-validation.md § BottomSheet State Management).
    private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(SongFormViewModel.IsResolutionSheetVisible):
                SyncResolutionSheetState();
                break;
            case nameof(SongFormViewModel.IsArtistMatchSheetVisible):
                SyncArtistMatchSheetState();
                break;
        }
    }

    // Same pattern as the resolution sheet above (Task 5.3, REQ-SFT-19): the flag drives Show/Close.
    private void SyncArtistMatchSheetState()
    {
        if (_isSyncingArtistMatchSheet) return;

        _isSyncingArtistMatchSheet = true;
        try
        {
            if (ViewModel.IsArtistMatchSheetVisible)
                artistMatchSheet.Show(BottomSheetState.HalfExpanded, this);
            else
                artistMatchSheet.Close();
        }
        finally
        {
            _isSyncingArtistMatchSheet = false;
        }
    }

    // BUG-084: AllowDismiss is True, so a scrim tap, the Android Back key or a swipe hides the sheet
    // without a ViewModel command. A user-initiated Hidden is routed through the same dismiss command the
    // Cancel button uses, so nothing is saved and the flag resets. The guard makes Close() calls issued by
    // the ViewModel (Sync*) re-enter harmlessly, and the IsVisible check makes late Hidden events a no-op.
    private void OnArtistMatchSheetStateChanged(object sender, ValueChangedEventArgs<BottomSheetState> e)
    {
        if (_isSyncingArtistMatchSheet) return;

        if (e.NewValue == BottomSheetState.Hidden && ViewModel.IsArtistMatchSheetVisible)
        {
            _isSyncingArtistMatchSheet = true;
            try
            {
                ViewModel.DismissArtistMatchSheetCommand.Execute(null);
            }
            finally
            {
                _isSyncingArtistMatchSheet = false;
            }
        }
    }

    private void SyncResolutionSheetState()
    {
        if (_isSyncingResolutionSheet) return;

        _isSyncingResolutionSheet = true;
        try
        {
            if (ViewModel.IsResolutionSheetVisible)
                resolutionSheet.Show(BottomSheetState.HalfExpanded, this);
            else
                resolutionSheet.Close();
        }
        finally
        {
            _isSyncingResolutionSheet = false;
        }
    }

    // BUG-084: AllowDismiss is True, so scrim, Back and swipe hide the sheet without a ViewModel command.
    // A user-initiated Hidden is routed through DismissResolutionSheetCommand (the Cancel path): nothing is
    // saved, the flag and VersionEntryRequired reset. Sync-driven Close() calls are ignored by the guard.
    private void OnResolutionSheetStateChanged(object sender, ValueChangedEventArgs<BottomSheetState> e)
    {
        if (_isSyncingResolutionSheet) return;

        if (e.NewValue == BottomSheetState.Hidden && ViewModel.IsResolutionSheetVisible)
        {
            _isSyncingResolutionSheet = true;
            try
            {
                ViewModel.DismissResolutionSheetCommand.Execute(null);
            }
            finally
            {
                _isSyncingResolutionSheet = false;
            }
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
    }
}
