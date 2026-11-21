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

    /// <summary>
    /// Row tap. Forwards to the ViewModel the same way CrudListView forwards DXCollectionView.Tap (code-behind),
    /// because the XAML command binding on the row recognizer never reached the command (S5b finding).
    /// The row item is the BindingContext of the recognizer's SfEffectsView host.
    /// </summary>
    private void OnRowTapped(object sender, TappedEventArgs e)
    {
        var item = ((BindableObject)sender).BindingContext;
        if (ViewModel.ItemTapCommand.CanExecute(item))
            ViewModel.ItemTapCommand.Execute(item);
    }

    /// <summary>Row press-and-hold. Forwards to ViewModel.ItemLongPressCommand (SfEffectsView.LongPressed).</summary>
    private void OnRowLongPressed(object sender, EventArgs e)
    {
        var item = ((BindableObject)sender).BindingContext;
        if (ViewModel.ItemLongPressCommand.CanExecute(item))
            ViewModel.ItemLongPressCommand.Execute(item);
    }
}
