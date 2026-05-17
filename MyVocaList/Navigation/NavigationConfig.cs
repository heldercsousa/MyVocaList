namespace MyVocaList.Navigation;

/// <summary>
/// Defines the app's navigation structure: page type map and flyout menu groups.
/// </summary>
public static class NavigationConfig
{
    public static readonly Dictionary<string, Type> PageTypes = new()
    {
        [Routes.Events] = typeof(EventsPage),
        [Routes.Venues] = typeof(VenuesPage),
        [Routes.People] = typeof(PeoplePage),
        [Routes.Artists] = typeof(ArtistsPage),
        [Routes.Songs] = typeof(SongsPage),
        [Routes.Preferences] = typeof(PreferencesPage),
        [Routes.Backup] = typeof(BackupRestorePage),
    };

    public static List<MenuGroup> BuildMenuGroups(ICommand navigateCommand) =>
    [
        new MenuGroup("Event", [
            new MenuItemDescription("Queue",          "format_list_numbered_outlined", Routes.Queue,       navigateCommand),
            new MenuItemDescription("Events",         "event_outlined",                Routes.Events,      navigateCommand)
        ]),
        new MenuGroup("Management", [
            new MenuItemDescription("Venues",         "nightlife_outlined",            Routes.Venues,      navigateCommand),
            new MenuItemDescription("People",         "group_outlined",                Routes.People,      navigateCommand),
        ]),
        new MenuGroup("Catalog", [
            new MenuItemDescription("Artists", "group_outlined",      Routes.Artists, navigateCommand),
            new MenuItemDescription("Songs",   "music_note_outlined", Routes.Songs,   navigateCommand),
        ]),
        new MenuGroup("System", [
            new MenuItemDescription("Preferences",    "settings_outlined",             Routes.Preferences, navigateCommand),
            new MenuItemDescription("Backup & Restore","cloud_sync_outlined",          Routes.Backup,      navigateCommand),
            new MenuItemDescription("Exit",           "logout_outlined",               Routes.Exit,        navigateCommand)
        ])
    ];

    public const string AppDescription = "Karaoke Queue Manager";
}
