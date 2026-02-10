using System.Windows.Input;
using MyVocaList.UI.Models;

namespace MyVocaList.UI.ViewModels;

/// <summary>
/// Provides menu structure and navigation for the AppShell flyout.
/// </summary>
public class AppShellViewModel
{
    public string AppTitle => "MyVocaList";

    public string AppDescription => "Karaoke Queue Manager";

    public List<MenuGroup> MenuGroups { get; }

    public ICommand NavigateCommand { get; }

    public AppShellViewModel()
    {
        NavigateCommand = new Command<string>(async route => await NavigateAsync(route));
        MenuGroups = BuildMenuGroups();
    }

    private List<MenuGroup> BuildMenuGroups()
    {
        return
        [
            new MenuGroup("Event", [
                new MenuItemDescription("Queue", "format_list_numbered_outlined", "queue", NavigateCommand),
                new MenuItemDescription("Events", "event_outlined", "events", NavigateCommand)
            ]),
            new MenuGroup("Management", [
                new MenuItemDescription("Venues", "nightlife_outlined", "venues", NavigateCommand),
                new MenuItemDescription("People", "group_outlined", "people", NavigateCommand),
                new MenuItemDescription("Artists & Music", "music_note_outlined", "artists", NavigateCommand)
            ]),
            new MenuGroup("System", [
                new MenuItemDescription("Preferences", "settings_outlined", "preferences", NavigateCommand),
                new MenuItemDescription("Backup & Restore", "cloud_sync_outlined", "backup", NavigateCommand)
            ])
        ];
    }

    private static async Task NavigateAsync(string route)
    {
        Shell.Current.FlyoutIsPresented = false;
        await Shell.Current.GoToAsync($"//{route}");
    }
}
