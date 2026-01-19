namespace MyVocaList;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell());

		// TEMPORÁRIO: Abrir direto o Design System para validação
		//return new Window(new MyVocaList.UI.Pages.DesignSystem.DesignSystemPage());
	}
}
