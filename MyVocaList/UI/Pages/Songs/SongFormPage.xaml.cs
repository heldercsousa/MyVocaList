using System.ComponentModel;
using DevExpress.Maui.Editors;
using MyVocaList.Contracts.Models;

namespace MyVocaList.UI.Pages.Songs;

public partial class SongFormPage : ContentPage
{
    private SongFormViewModel ViewModel => (SongFormViewModel)BindingContext;

    // Re-entrancy guards for the two-way sheet-state sync (BUG-023): prevent the
    // ViewModel -> code-behind -> ViewModel round trip from looping when a sheet
    // is closed programmatically in response to the flag it was opened by.
    private bool _isSyncingResolutionSheet;
    private bool _isSyncingMergeSheet;

    public SongFormPage(SongFormViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        titleEdit.Focus();

        // Shell has finished applying [QueryProperty] values for this navigation by the time
        // OnAppearing runs — end the hydration window so subsequent edits are tracked as dirty.
        ViewModel.CompleteHydration();

        // BUG-020: RefreshApiKeyFlagAsync wraps SecureStorage.GetAsync in its own try-catch so a
        // corrupted Android keystore alias cannot crash this async void OnAppearing — do not
        // remove that try-catch.
        await ViewModel.RefreshApiKeyFlagAsync();
        ViewModel.InitializeArtistField(); // BUG-008: reliable artist field init after all query props set
    }

    // ── DX AutoCompleteEdit glue (T3, 2026-07-19 change spec) ────────────────────────────
    // Thin forwarding only: the editor's AsyncItemsSourceProvider owns debounce (RequestDelay=300)
    // and stale-request cancellation; the search itself runs through the EXISTING VM command path
    // (SearchArtistsCommand → IArtistService) so gates and behavior stay in the ViewModel.
    private void OnArtistItemsRequested(object sender, ItemsRequestEventArgs e)
    {
        var text = e.Text;
        var token = e.CancellationToken;
        e.RequestAsync = async () =>
        {
            await ViewModel.SearchArtistsCommand.ExecuteAsync(text);
            token.ThrowIfCancellationRequested();
            return ViewModel.ArtistSuggestions;
        };
    }

    // Forwards drop-down selection to the existing VM selection command.
    private void OnArtistSelectionChanged(object sender, EventArgs e)
    {
        if (artistEdit.SelectedItem is AutocompleteSuggestion suggestion)
            ViewModel.SelectArtistCommand.Execute(suggestion);
    }

    // Blur without a selection → existing VM blur command (same trigger the old component used).
    private void OnArtistUnfocused(object sender, FocusEventArgs e)
    {
        if (artistEdit.SelectedItem is null)
            ViewModel.ArtistBlurredWithoutSelectionCommand.Execute(null);
    }

    // Bridges the MAUI Unfocused (blur) events to the ViewModel's validation commands.
    private void OnTitleUnfocused(object sender, FocusEventArgs e) =>
        ViewModel.ValidateTitleCommand.Execute(null);

    private void OnVersionUnfocused(object sender, FocusEventArgs e) =>
        ViewModel.ValidateVersionCommand.Execute(null);

    // BUG-023: resolutionSheet/mergeSheet lost their state binding when IsExpanded was removed
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
            case nameof(SongFormViewModel.IsMergeSheetVisible):
                SyncMergeSheetState();
                break;
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

    private void SyncMergeSheetState()
    {
        if (_isSyncingMergeSheet) return;

        _isSyncingMergeSheet = true;
        try
        {
            if (ViewModel.IsMergeSheetVisible)
                mergeSheet.Show(BottomSheetState.HalfExpanded, this);
            else
                mergeSheet.Close();
        }
        finally
        {
            _isSyncingMergeSheet = false;
        }
    }

    // Syncs the sheet closing (e.g. Close() completing) back to the ViewModel flag so the two
    // stay consistent. AllowDismiss="False" on both sheets means the user cannot swipe-dismiss;
    // closing is always driven by a ViewModel command (Cancel/Confirm), so this is primarily a
    // safety net against the flag and the visual state ever diverging.
    private void OnResolutionSheetStateChanged(object sender, ValueChangedEventArgs<BottomSheetState> e)
    {
        if (_isSyncingResolutionSheet) return;

        if (e.NewValue == BottomSheetState.Hidden && ViewModel.IsResolutionSheetVisible)
        {
            _isSyncingResolutionSheet = true;
            try
            {
                ViewModel.IsResolutionSheetVisible = false;
            }
            finally
            {
                _isSyncingResolutionSheet = false;
            }
        }
    }

    private void OnMergeSheetStateChanged(object sender, ValueChangedEventArgs<BottomSheetState> e)
    {
        if (_isSyncingMergeSheet) return;

        if (e.NewValue == BottomSheetState.Hidden && ViewModel.IsMergeSheetVisible)
        {
            _isSyncingMergeSheet = true;
            try
            {
                ViewModel.IsMergeSheetVisible = false;
            }
            finally
            {
                _isSyncingMergeSheet = false;
            }
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
    }
}
