using System.ComponentModel;
using SfSheetState = Syncfusion.Maui.Toolkit.BottomSheet.BottomSheetState;

namespace MyVocaList.UI.Pages.Songs;

/// <summary>Syncfusion copy of <see cref="SongFormPage"/>: same ViewModel, same code-behind behavior.</summary>
public partial class SongFormPage : ContentPage
{
    private const string SkeletonAnimationName = "SongFormSfPageSkeletonPulse";
    private const uint SkeletonPulseLengthMs = 1400;
    private const double SkeletonOpacityHigh = 1.0;
    private const double SkeletonOpacityLow = 0.4;

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

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // BUG-084: OnDisappearing unsubscribes the sheet-sync handler, so it must be re-attached on every
        // appearance. Detach first so the handler is never registered twice.
        ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;

        // REQ-PIC-25: a stored or pre-filled Title opens as text with no keyboard; only the editor state is focused.
        // Edit mode never focuses: the stored song loads asynchronously, so the Title can still be blank here,
        // and the focus would keep the wrapper in the editor state after the load (IsEditing = true).
        if (!ViewModel.IsEditMode && titleTapField.IsEditorVisible)
            titleField.FocusEditor();

        // Shell has finished applying [QueryProperty] values for this navigation by the time
        // OnAppearing runs — end the hydration window so subsequent edits are tracked as dirty.
        ViewModel.CompleteHydration();

        ApplySkeletonPulse();

        ViewModel.InitializeArtistField(); // BUG-008: reliable artist field init after all query props set
    }

    // Android Back does not close an SfBottomSheet: while a sheet is open, Back dismisses it through the same
    // command as Cancel (nothing is saved) and is consumed. BUG-084 / 633d4f77: Back must not leave the sheet stuck.
    protected override bool OnBackButtonPressed()
    {
        if (ViewModel.IsArtistMatchSheetVisible)
        {
            ViewModel.DismissArtistMatchSheetCommand.Execute(null);
            return true;
        }

        if (ViewModel.IsResolutionSheetVisible)
        {
            ViewModel.DismissResolutionSheetCommand.Execute(null);
            return true;
        }

        return base.OnBackButtonPressed();
    }

    // BUG-023: the sheets are driven from the ViewModel flags in code-behind, mirroring the confirmed pattern
    // in ConfirmSheet.xaml.cs (see .claude/library/dialogs-validation.md § BottomSheet State Management).
    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(SongFormViewModel.IsResolutionSheetVisible):
                SyncResolutionSheetState();
                break;
            case nameof(SongFormViewModel.IsArtistMatchSheetVisible):
                SyncArtistMatchSheetState();
                break;
            case nameof(SongFormViewModel.IsSongLoading):
                // The load can flip the flag off the UI thread; animations must start on it.
                Dispatcher.Dispatch(ApplySkeletonPulse);
                break;
        }
    }

    // Same pattern as the resolution sheet below (Task 5.3, REQ-SFT-19): the flag drives open/close.
    private void SyncArtistMatchSheetState()
    {
        if (_isSyncingArtistMatchSheet) return;

        _isSyncingArtistMatchSheet = true;
        try
        {
            if (ViewModel.IsArtistMatchSheetVisible)
            {
                artistMatchSheet.State = SfSheetState.HalfExpanded;
                artistMatchSheet.IsOpen = true;
            }
            else
            {
                artistMatchSheet.IsOpen = false;
                artistMatchSheet.State = SfSheetState.Hidden;
            }
        }
        finally
        {
            _isSyncingArtistMatchSheet = false;
        }
    }

    // BUG-084: a scrim tap, a swipe or Back hides the sheet without a ViewModel command. A user-initiated close
    // (Hidden or Collapsed) is routed through the same dismiss command the Cancel button uses, so nothing is saved
    // and the flag resets. The guard makes the close issued by SyncArtistMatchSheetState re-enter harmlessly, and the
    // IsVisible check makes late Hidden events a no-op.
    private void OnArtistMatchSheetStateChanged(object? sender, Syncfusion.Maui.Toolkit.BottomSheet.StateChangedEventArgs e)
    {
        if (_isSyncingArtistMatchSheet) return;

        var closed = e.NewState is SfSheetState.Hidden or SfSheetState.Collapsed;
        if (closed && ViewModel.IsArtistMatchSheetVisible)
        {
            _isSyncingArtistMatchSheet = true;
            try
            {
                ViewModel.DismissArtistMatchSheetCommand.Execute(null);

                // The guard makes SyncArtistMatchSheetState a no-op for the flag change the dismiss causes, so a drag
                // down (Collapsed) would leave the sheet open at its collapsed height. Close it explicitly.
                artistMatchSheet.IsOpen = false;
                artistMatchSheet.State = SfSheetState.Hidden;
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
            {
                resolutionSheet.State = SfSheetState.HalfExpanded;
                resolutionSheet.IsOpen = true;
            }
            else
            {
                resolutionSheet.IsOpen = false;
                resolutionSheet.State = SfSheetState.Hidden;
            }
        }
        finally
        {
            _isSyncingResolutionSheet = false;
        }
    }

    // BUG-084: scrim, Back and swipe hide the sheet without a ViewModel command. A user-initiated close is routed
    // through DismissResolutionSheetCommand (the Cancel path): nothing is saved, the flag resets. Sync-driven
    // closes are ignored by the guard.
    private void OnResolutionSheetStateChanged(object? sender, Syncfusion.Maui.Toolkit.BottomSheet.StateChangedEventArgs e)
    {
        if (_isSyncingResolutionSheet) return;

        var closed = e.NewState is SfSheetState.Hidden or SfSheetState.Collapsed;
        if (closed && ViewModel.IsResolutionSheetVisible)
        {
            _isSyncingResolutionSheet = true;
            try
            {
                ViewModel.DismissResolutionSheetCommand.Execute(null);

                // As above: close explicitly so a drag down (Collapsed) does not leave the sheet open.
                resolutionSheet.IsOpen = false;
                resolutionSheet.State = SfSheetState.Hidden;
            }
            finally
            {
                _isSyncingResolutionSheet = false;
            }
        }
    }

    /// <summary>Starts the skeleton opacity pulse while a stored song loads; stops it otherwise (view-only state).</summary>
    private void ApplySkeletonPulse()
    {
        if (skeletonBones == null)
            return;

        this.AbortAnimation(SkeletonAnimationName);
        skeletonBones.Opacity = SkeletonOpacityHigh;

        if (!ViewModel.IsSongLoading)
            return;

        var pulse = new Animation
        {
            { 0, 0.5, new Animation(v => skeletonBones.Opacity = v, SkeletonOpacityHigh, SkeletonOpacityLow) },
            { 0.5, 1, new Animation(v => skeletonBones.Opacity = v, SkeletonOpacityLow, SkeletonOpacityHigh) },
        };
        pulse.Commit(this, SkeletonAnimationName, length: SkeletonPulseLengthMs,
            finished: (_, _) => skeletonBones.Opacity = SkeletonOpacityHigh,
            repeat: () => ViewModel.IsSongLoading);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        this.AbortAnimation(SkeletonAnimationName);
        ViewModel.CancelLyricsLookupOnOpen();
    }
}
