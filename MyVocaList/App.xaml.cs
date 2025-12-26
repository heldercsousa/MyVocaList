namespace MyVocaList;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();

		//MainPage = new AppShell();

		// TEMPORÁRIO: Abrir direto o Design System para validação
        this.MainPage = new MyVocaList.UI.Pages.DesignSystem.DesignSystemPage();
    }
}
