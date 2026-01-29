using MyVocaList.UI.Pages.DesignSystem;

namespace MyVocaList;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

		// Register Design System routes
		Routing.RegisterRoute("DSTypographyPage", typeof(DSTypographyPage));
		Routing.RegisterRoute("DSButtonsPage", typeof(DSButtonsPage));
		Routing.RegisterRoute("DSCardsPage", typeof(DSCardsPage));
		Routing.RegisterRoute("DSInputsPage", typeof(DSInputsPage));
	}
}
