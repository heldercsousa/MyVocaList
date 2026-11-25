using SfBottomSheetState = Syncfusion.Maui.Toolkit.BottomSheet.BottomSheetState;

namespace MyVocaList.UI.Pages.Venues;

public partial class VenuesSfPage : CrudListPageBase
{
    private readonly VenuesViewModel _viewModel;

    /// <summary>Exposed for compiled bindings inside DataTemplates.</summary>
    public VenuesViewModel ViewModel => _viewModel;

    protected override ICrudListViewModel ListViewModel => _viewModel;

    public VenuesSfPage(VenuesViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
        AttachViewModel();
    }

    /// <summary>Shows the exit confirmation sheet.</summary>
    public void ShowExitConfirmation()
    {
        exitConfirmSheet.State = SfBottomSheetState.HalfExpanded;
        exitConfirmSheet.IsOpen = true;
    }

    protected override bool OnBackButtonPressed()
    {
        // Android Back does not close an SfBottomSheet, so the exit sheet is closed here first.
        if (exitConfirmSheet.IsOpen)
        {
            CloseExitConfirmation();
            return true;
        }

        // Then the CRUD back order (menu -> item-actions sheet -> confirm sheet -> selection);
        // the exit sheet shows only when the base class did not handle the press.
        if (base.OnBackButtonPressed())
            return true;

        ShowExitConfirmation();
        return true;
    }

    private void CloseExitConfirmation()
    {
        exitConfirmSheet.IsOpen = false;
        exitConfirmSheet.State = SfBottomSheetState.Hidden;
    }

    private void OnConfirmExit(object sender, EventArgs e)
    {
        Application.Current?.Quit();
    }

    private void OnCancelExit(object sender, EventArgs e)
    {
        CloseExitConfirmation();
    }
}
