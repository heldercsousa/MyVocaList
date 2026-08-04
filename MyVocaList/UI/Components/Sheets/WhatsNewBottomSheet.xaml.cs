namespace MyVocaList.UI.Components.Sheets;

public partial class WhatsNewBottomSheet : ContentView
{
    private IWhatsNewService? _whatsNewService;

    public WhatsNewBottomSheet()
    {
        InitializeComponent();
    }

    public void Show(ReleaseEntry entry, IWhatsNewService whatsNewService)
    {
        _whatsNewService = whatsNewService;

        TitleLabel.Text = $"What's New in {entry.Version}";
        DateLabel.Text = entry.Date;

        HighlightsTitleLabel.IsVisible = entry.Highlights.Count > 0;
        HighlightsList.IsVisible = entry.Highlights.Count > 0;
        FixesTitleLabel.IsVisible = entry.Fixes.Count > 0;
        FixesList.IsVisible = entry.Fixes.Count > 0;

        PopulateBulletList(HighlightsList, entry.Highlights);
        PopulateBulletList(FixesList, entry.Fixes);

        Sheet.State = DevExpress.Maui.Controls.BottomSheetState.HalfExpanded;
    }

    private static void PopulateBulletList(VerticalStackLayout container, IReadOnlyList<string> items)
    {
        container.Children.Clear();
        foreach (var item in items)
        {
            container.Children.Add(new Label
            {
                Text = $"• {item}",
                StyleClass = ["Body.Medium"]
            });
        }
    }

    private void OnGotItClicked(object sender, EventArgs e)
    {
        _whatsNewService?.MarkCurrentVersionSeen();
        Sheet.State = DevExpress.Maui.Controls.BottomSheetState.Hidden;
    }
}
