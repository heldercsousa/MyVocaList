#if ANDROID || IOS || MACCATALYST
using HorusStudio.Maui.MaterialDesignControls;
#endif

namespace MyVocaList;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
#if ANDROID || IOS || MACCATALYST
		MaterialDesignControls.InitializeComponents();
#endif
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell());
	}
}
