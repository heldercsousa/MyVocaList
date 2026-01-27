using MyVocaList.UI.Pages.DesignSystem;

namespace MyVocaList;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

		// Register Design System routes
		Routing.RegisterRoute("ComponentsPage_Typography", typeof(ComponentsPage_Typography));
	}
}
