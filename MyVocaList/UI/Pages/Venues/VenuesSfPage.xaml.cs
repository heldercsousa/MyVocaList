namespace MyVocaList.UI.Pages.Venues;

/// <summary>
/// SPIKE (task/spike-syncfusion): Syncfusion copy of <see cref="VenuesPage"/>. Reuses VenuesViewModel unchanged.
/// </summary>
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

    /// <summary>Shows the exit confirmation sheet (Syncfusion SfBottomSheet).</summary>
    public void ShowExitConfirmation()
    {
        exitConfirmSheet.Show();
    }

    protected override bool OnBackButtonPressed()
    {
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
