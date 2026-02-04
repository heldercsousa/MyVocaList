using CommunityToolkit.Maui;
using DevExpress.Maui;
using Microsoft.Extensions.Logging;
using MyVocaList.UI.Services;

namespace MyVocaList;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseDevExpress(useLocalization: false)
            .UseDevExpressCollectionView()
            .UseDevExpressControls()
            .UseDevExpressEditors()
            .ConfigureFonts(fonts =>
            {
                // Roboto for Material Design 3
                fonts.AddFont("Roboto-Regular.ttf", "RobotoRegular");
                fonts.AddFont("Roboto-Medium.ttf", "RobotoMedium");
                fonts.AddFont("Roboto-Bold.ttf", "RobotoBold");
            });

        // Register Services
        builder.Services.AddSingleton<IThreadSafeDialogService, ThreadSafeDialogService>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}