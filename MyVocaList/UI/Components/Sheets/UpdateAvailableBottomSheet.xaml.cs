namespace MyVocaList.UI.Components.Sheets;

/// <summary>
/// MD3 modal bottom sheet — soft update nudge (dismissible).
/// Users can dismiss it and continue using the app.
/// </summary>
public partial class UpdateAvailableBottomSheet : ContentView
{
    private string _storeUrl = string.Empty;

    public UpdateAvailableBottomSheet()
    {
        InitializeComponent();
    }

    /// <summary>Populates the sheet with update details and shows it.</summary>
    public void Show(UpdateCheckResult result)
    {
        _storeUrl = result.StoreUrl;
        TitleLabel.Text = "Update Available";
        BodyLabel.Text = $"Version {result.LatestVersion} is ready. Update for the latest features and fixes.";
        Sheet.State = BottomSheetState.HalfExpanded;
    }

    private async void OnUpdateNowClicked(object sender, EventArgs e)
    {
        Sheet.State = BottomSheetState.Hidden;
        if (!string.IsNullOrEmpty(_storeUrl))
            await Launcher.OpenAsync(_storeUrl);
    }

    private void OnLaterClicked(object sender, EventArgs e)
        => Sheet.State = BottomSheetState.Hidden;
}
