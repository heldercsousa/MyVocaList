using UraniumUI.Pages;

namespace MyVocaList.UI.Components;

/// <summary>
/// Floating Action Button - UraniumContentPage.Attachments feature
/// </summary>
public class MvlFabButtonTest : ImageButton, IPageAttachment
{
    public MvlFabButtonTest()
    {
        BackgroundColor = Color.FromArgb("#512BD4");
        WidthRequest = 56;
        HeightRequest = 56;
        CornerRadius = 28;

        // Add shadow effect
        Shadow = new Shadow
        {
            Brush = Colors.Black,
            Offset = new Point(2, 2),
            Radius = 8,
            Opacity = 0.3f
        };

        Clicked += OnFabClicked;
    }

    /// <summary>
    /// Determines the Z-index position of the attachment
    /// </summary>
    public AttachmentPosition AttachmentPosition => AttachmentPosition.Front;

    /// <summary>
    /// Called when the attachment is added to the page
    /// </summary>
    public void OnAttached(UraniumContentPage attachedPage)
    {
        // Position FAB at bottom-right corner
        var margin = 20;

        // Use anchoring to position relative to page bounds
        this.HorizontalOptions = LayoutOptions.End;
        this.VerticalOptions = LayoutOptions.End;
        this.Margin = new Thickness(0, 0, margin, margin);
    }

    /// <summary>
    /// Handle FAB click event
    /// </summary>
    private async void OnFabClicked(object? sender, EventArgs e)
    {
        // Animate the button
        await this.ScaleTo(0.9, 100);
        await this.ScaleTo(1.0, 100);

        // Show alert
        if (Parent is Page page)
        {
            await page.DisplayAlert(
                "FAB Clicked",
                "This is a Floating Action Button attached to the UraniumContentPage. It demonstrates the page attachment feature!",
                "OK");
        }
    }
}