using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using MyVocaList.UI.Services;
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

		// Register Services
		builder.Services.AddSingleton<IThreadSafeDialogService, ThreadSafeDialogService>();

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

		// Theme colors loaded from MaterialColors.xaml (no hard-coded values)
		var darkTheme = new MaterialTheme
		{
			Primary = ThemeResourceLoader.GetColor("Primary"),
			OnPrimary = ThemeResourceLoader.GetColor("OnPrimary"),
			PrimaryContainer = ThemeResourceLoader.GetColor("PrimaryContainer"),
			OnPrimaryContainer = ThemeResourceLoader.GetColor("OnPrimaryContainer"),
			Secondary = ThemeResourceLoader.GetColor("Secondary"),
			OnSecondary = ThemeResourceLoader.GetColor("OnSecondary"),
			SecondaryContainer = ThemeResourceLoader.GetColor("SecondaryContainer"),
			OnSecondaryContainer = ThemeResourceLoader.GetColor("OnSecondaryContainer"),
			Error = ThemeResourceLoader.GetColor("Error"),
			OnError = ThemeResourceLoader.GetColor("OnError"),
			ErrorContainer = ThemeResourceLoader.GetColor("ErrorContainer"),
			OnErrorContainer = ThemeResourceLoader.GetColor("OnErrorContainer"),
			Surface = ThemeResourceLoader.GetColor("Surface"),
			OnSurface = ThemeResourceLoader.GetColor("OnSurface"),
			SurfaceVariant = ThemeResourceLoader.GetColor("SurfaceVariant"),
			OnSurfaceVariant = ThemeResourceLoader.GetColor("OnSurfaceVariant"),
			Outline = ThemeResourceLoader.GetColor("Outline"),
			OutlineVariant = ThemeResourceLoader.GetColor("OutlineVariant")
		};

		options.ConfigureThemes(darkTheme: darkTheme, lightTheme: null);

		// US English date/time format
		options.ConfigureStringFormat(new MaterialFormatOptions
		{
			DateFormat = "MM/dd/yyyy",
			TimeFormat = "h:mm tt"
		});
	}
#endif
}
