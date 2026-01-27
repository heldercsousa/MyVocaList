using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using UraniumUI;
#if ANDROID || IOS || MACCATALYST
using HorusStudio.Maui.MaterialDesignControls;
#endif

namespace MyVocaList;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
			.UseUraniumUI()
			.UseUraniumUIMaterial()
#if ANDROID || IOS || MACCATALYST
			.UseMaterialDesignControls(ConfigureMDC)
#endif
			.ConfigureFonts(fonts =>
			{
				// MD3 Default: Roboto
				fonts.AddFont("Roboto-Regular.ttf", "RobotoRegular");
				fonts.AddFont("Roboto-Medium.ttf", "RobotoMedium");
				fonts.AddFont("Roboto-Bold.ttf", "RobotoBold");

				// Material Symbols Icons
				fonts.AddMaterialSymbolsFonts();
			});

		// Register Services (ThreadSafeDialogService added in Task 5)

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}

#if ANDROID || IOS || MACCATALYST
	private static void ConfigureMDC(MaterialDesignControlsBuilder options)
	{
#if DEBUG
		options.EnableDebug();
#endif
		options.OnException((sender, ex) =>
		{
			System.Diagnostics.Debug.WriteLine($"[MDC] {sender}: {ex}");
		});

		options.ConfigureThemesFromResources();
		options.ConfigureStringFormat(new MaterialFormatOptions
		{
			DateFormat = "dd/MM/yyyy",
			TimeFormat = "HH:mm"
		});
	}
#endif
}
