using UraniumUI.Pages;

namespace MyVocaList.UI.Pages.DesignSystem;

public partial class HomePage : UraniumContentPage
{
	public HomePage()
	{
		InitializeComponent();
	}

    /// <summary>
    /// Opens the URL in the default browser when a link is tapped
    /// </summary>
    private async void OnUrlTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is string url)
        {
            await Launcher.OpenAsync(url);
        }
    }
}