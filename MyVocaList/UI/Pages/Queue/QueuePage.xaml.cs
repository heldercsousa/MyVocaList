namespace MyVocaList.UI.Pages.Queue;

public partial class QueuePage : ContentPage
{
    public QueuePage()
    {
        InitializeComponent();
    }

    /// <summary>Shows the exit confirmation sheet from any caller (menu item, AppShell fallback).</summary>
    public void ShowExitConfirmation()
    {
        exitConfirmSheet.Show(BottomSheetState.HalfExpanded, this);
    }

    protected override bool OnBackButtonPressed()
    {
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
