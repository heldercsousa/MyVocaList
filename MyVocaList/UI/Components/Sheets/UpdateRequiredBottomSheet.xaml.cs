namespace MyVocaList.UI.Components.Sheets;

/// <summary>
/// MD3 modal bottom sheet — hard update block (non-dismissible).
/// The sheet stays open until the user taps "Update Now" and updates the app.
/// </summary>
public partial class UpdateRequiredBottomSheet : ContentView
{
    private string _storeUrl = string.Empty;

    public UpdateRequiredBottomSheet()
    {
        InitializeComponent();
    }

    /// <summary>Populates the sheet with the update requirement message and shows it.</summary>
    public void Show(UpdateCheckResult result)
    {
        _storeUrl = result.StoreUrl;
        MessageLabel.Text = string.IsNullOrWhiteSpace(result.UpdateMessage)
            ? "This version is no longer supported. Please update to continue."
            : result.UpdateMessage;
        Sheet.State = BottomSheetState.HalfExpanded;
    }

    private async void OnUpdateNowClicked(object sender, EventArgs e)
    {
        if (!string.IsNullOrEmpty(_storeUrl))
            await Launcher.OpenAsync(_storeUrl);
        // Sheet stays open — user must update before continuing.
    }
}
