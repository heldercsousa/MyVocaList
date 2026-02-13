using System.Collections.Generic;
using System.Windows.Input;
using MyVocaList.UI.Models;
using MyVocaList.UI.Pages.Artists;
using MyVocaList.UI.Pages.BackupRestore;
using MyVocaList.UI.Pages.Events;
using MyVocaList.UI.Pages.People;
using MyVocaList.UI.Pages.Preferences;
using MyVocaList.UI.Pages.Venues;

namespace MyVocaList.Navigation;

/// <summary>
/// Defines the app's navigation structure: page type map and flyout menu groups.
/// </summary>
public static class NavigationConfig
{
    public static readonly Dictionary<string, Type> PageTypes = new()
    {
        [Routes.Events]      = typeof(EventsPage),
        [Routes.Venues]      = typeof(VenuesPage),
        [Routes.People]      = typeof(PeoplePage),
        [Routes.Artists]     = typeof(ArtistsPage),
        [Routes.Preferences] = typeof(PreferencesPage),
        [Routes.Backup]      = typeof(BackupRestorePage),
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
            new MenuItemDescription("Artists & Music","music_note_outlined",           Routes.Artists,     navigateCommand)
        ]),
        new MenuGroup("System", [
            new MenuItemDescription("Preferences",    "settings_outlined",             Routes.Preferences, navigateCommand),
            new MenuItemDescription("Backup & Restore","cloud_sync_outlined",          Routes.Backup,      navigateCommand),
            new MenuItemDescription("Exit",           "logout_outlined",               Routes.Exit,        navigateCommand)
        ])
    ];

    public const string AppDescription = "Karaoke Queue Manager";
}
