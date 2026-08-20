namespace MyVocaList.UI.Pages.Venues;

public partial class VenuesPage : CrudListPageBase
{
    private readonly VenuesViewModel _viewModel;

    /// <summary>Exposed for compiled bindings inside DataTemplates.</summary>
    public VenuesViewModel ViewModel => _viewModel;

    protected override ICrudListViewModel ListViewModel => _viewModel;

    public VenuesPage(VenuesViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
        AttachViewModel();
    }

    /// <summary>Shows the exit confirmation sheet from any caller (menu item, AppShell fallback).</summary>
    public void ShowExitConfirmation()
    {
        exitConfirmSheet.Show(BottomSheetState.HalfExpanded, this);
    }

    protected override bool OnBackButtonPressed()
    {
        // Let the base class dismiss the CRUD delete-confirmation sheet first (if open) --
        // that takes precedence over the exit confirmation. Only show exit confirmation
        // when the base class did not handle the back press.
        if (base.OnBackButtonPressed())
            return true;

        ShowExitConfirmation();
        return true;
    }

    private void OnConfirmExit(object sender, EventArgs e)
    {
        Application.Current?.Quit();
    }

    private void OnCancelExit(object sender, EventArgs e)
    {
        exitConfirmSheet.Close();
    }
}
