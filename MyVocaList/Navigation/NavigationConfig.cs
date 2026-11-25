#if DEBUG
using MyVocaList.UI.Migration;
#endif

namespace MyVocaList.Navigation;

/// <summary>
/// Defines the app's navigation structure: page type map and flyout menu groups.
/// </summary>
public static class NavigationConfig
{
    public static readonly Dictionary<string, Type> PageTypes = new()
    {
        [Routes.Venues] = typeof(VenuesSfPage),
        [Routes.People] = typeof(PeopleSfPage),
        [Routes.Artists] = typeof(ArtistsPage),
        [Routes.Songs] = typeof(SongsPage),
        [Routes.Preferences] = typeof(SettingsSfPage),
        [Routes.Backup] = typeof(BackupRestoreSfPage),
        [Routes.About] = typeof(AboutSfPage),
#if DEBUG
        // Satisfies the NavigationConfigTests guard (every menu route has a PageTypes entry). The flyout item itself
        // navigates by Shell route (see BuildMenuGroups) because SyncfusionPreviewPage is not DI-registered.
        [PreviewRoutes.PreviewRoute] = typeof(SyncfusionPreviewPage),
#endif
    };

    public static List<MenuGroup> BuildMenuGroups(ICommand navigateCommand) =>
    [
        new MenuGroup("Management", [
            new MenuItemDescription("Venues",         "nightlife_outlined",            Routes.Venues,      navigateCommand),
            new MenuItemDescription("Singers",        "group_outlined",                Routes.People,      navigateCommand),
        ]),
        new MenuGroup("Catalog", [
            new MenuItemDescription("Artists", "group_outlined",      Routes.Artists, navigateCommand),
            new MenuItemDescription("Songs",   "music_note_outlined", Routes.Songs,   navigateCommand),
        ]),
        new MenuGroup("System", [
            new MenuItemDescription("Preferences",    "settings_outlined",             Routes.Preferences, navigateCommand),
            new MenuItemDescription("Backup & Restore","cloud_sync_outlined",          Routes.Backup,      navigateCommand),
            new MenuItemDescription("About",          "info_outlined",                 Routes.About,       navigateCommand),
#if DEBUG
            // Debug-only: navigates by Shell route (page is not DI-registered, so the PageTypes/PushAsync path does not apply).
            new MenuItemDescription("Syncfusion preview", "contrast_outlined", PreviewRoutes.PreviewRoute, new Command(async () =>
            {
                Shell.Current.FlyoutIsPresented = false;
                await Shell.Current.GoToAsync(PreviewRoutes.PreviewRoute);
            })),
#endif
            new MenuItemDescription("Exit",           "logout_outlined",               Routes.Exit,        navigateCommand)
        ])
    ];

    public const string AppDescription = "Karaoke Queue Manager";
}
