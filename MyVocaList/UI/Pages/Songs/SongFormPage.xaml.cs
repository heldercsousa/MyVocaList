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
        artistEdit.SelectedItem = null; // BUG-061: no stale lingering row on edit-mode initial load
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
            // BUG-056: take the current query's matches DIRECTLY from the search (return value),
            // never by reading ViewModel.ArtistSuggestions — that observable is assigned on a
            // background UI dispatch that may not have landed yet, which produced first-empty/stale reads.
            var matches = await ViewModel.SearchArtistsCoreAsync(text);
            token.ThrowIfCancellationRequested();

            // REQ-ACREATE-02/03/10: append one synthetic "create new artist" sentinel row as the
            // LAST item for any non-whitespace typed text (whether or not matches exist). Glue only:
            // the raw typed text is carried in Headline; the distinct ➕ render is done by the XAML
            // ItemTemplate (T8), and the actual creation happens in the VM command (T7 VM path).
            var results = new List<AutocompleteSuggestion>(matches);
            // BUG-054a: do not re-append the create sentinel when the field is already locked to a
            // selected artist, or when the query text is exactly that artist's name (the editor
            // re-requests items after a lock sets ArtistSearchText to the chosen name).
            var suppressCreateRow = ViewModel.IsArtistLocked
                || string.Equals(text, ViewModel.SelectedArtistName, StringComparison.Ordinal);
            if (!string.IsNullOrWhiteSpace(text) && !suppressCreateRow)
                results.Add(new AutocompleteSuggestion(text, string.Empty, text) { IsCreateNew = true });
            return results;
        };
    }

    // Routes drop-down selection: the create-sentinel goes to the inline-create command with the
    // raw typed text (carried in Headline); a real match goes to the existing selection command.
    private void OnArtistSelectionChanged(object sender, EventArgs e)
    {
        if (artistEdit.SelectedItem is not AutocompleteSuggestion suggestion)
            return;

        if (suggestion.IsCreateNew)
        {
            // BUG-049-style guard: an in-flight create must not be re-triggered by a second selection event.
            if (ViewModel.CreateArtistInlineCommand.CanExecute(suggestion.Headline))
                ViewModel.CreateArtistInlineCommand.Execute(suggestion.Headline);
        }
        else
        {
            ViewModel.SelectArtistCommand.Execute(suggestion);
        }

        // BUG-061: the DX AutoCompleteEdit keeps the picked row marked as SelectedItem, so it
        // lingers highlighted in the suggestion list until tapped a second time. Reset it once the
        // pick has been routed to the ViewModel — the field itself already shows the chosen name.
        artistEdit.SelectedItem = null;
    }

    // Blur without a selection → existing VM blur command (same trigger the old component used).
    private void OnArtistUnfocused(object sender, FocusEventArgs e)
    {
        if (artistEdit.SelectedItem is null)
            ViewModel.ArtistBlurredWithoutSelectionCommand.Execute(null);
    }

    // BUG-060/REQ-ACREATE-15: the Clear (X) icon empties the editor's Text via its own binding,
    // but a locked (IsReadOnly=true) field must also unlock so the user can search/create a
    // different artist. Thin forwarding to the VM — see SongFormViewModel.ClearArtist.
    private void OnArtistClearIconClicked(object sender, EventArgs e) =>
        ViewModel.ClearArtistCommand.Execute(null);

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
