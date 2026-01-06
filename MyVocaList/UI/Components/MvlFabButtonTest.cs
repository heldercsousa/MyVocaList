using UraniumUI.Pages;

namespace MyVocaList.UI.Components;

/// <summary>
/// Floating Action Button - UraniumContentPage.Attachments feature
/// </summary>
public class MvlFabButtonTest : ImageButton, IPageAttachment
{
    public MvlFabButtonTest()
    {
        // Em vez de cor fixa, usamos o estilo que definimos no MaterialStyles.xaml
        // Isso garante que ele use seu gradiente "Ouro" em qualquer plataforma.
        this.Style = (Style)Application.Current.Resources["FabContainer"];

        this.WidthRequest = 56;
        this.HeightRequest = 56;

        // O ícone deve vir do Material Symbols para ser 100% MD3
        // Você pode usar o Label customizado que já criamos
        var icon = new Label { Style = (Style)Application.Current.Resources["FabIcon"] };
        // Se estiver usando StatefulContentView, adicione o ícone como conteúdo

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