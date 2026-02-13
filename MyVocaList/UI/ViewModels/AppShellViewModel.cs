using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using MyVocaList.UI.Models;
using MyVocaList.UI.Pages.Artists;
using MyVocaList.UI.Pages.BackupRestore;
using MyVocaList.UI.Pages.Events;
using MyVocaList.UI.Pages.People;
using MyVocaList.UI.Pages.Preferences;
using MyVocaList.UI.Pages.Venues;

namespace MyVocaList.UI.ViewModels;

/// <summary>
/// Provides menu structure and navigation for the AppShell flyout.
/// </summary>
public class AppShellViewModel
{
    private readonly IServiceProvider _serviceProvider;

    private static readonly Dictionary<string, Type> PageTypes = new()
    {
        ["events"]      = typeof(EventsPage),
        ["venues"]      = typeof(VenuesPage),
        ["people"]      = typeof(PeoplePage),
        ["artists"]     = typeof(ArtistsPage),
        ["preferences"] = typeof(PreferencesPage),
        ["backup"]      = typeof(BackupRestorePage),
    };

    public string AppTitle => "MyVocaList";

    public string AppDescription => "Karaoke Queue Manager";

    public List<MenuGroup> MenuGroups { get; }

    public ICommand NavigateCommand { get; }

    /// <summary>Raised when the user requests to exit the app (menu item or back at root).</summary>
    public event Action? ExitRequested;

    public AppShellViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
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
                new MenuItemDescription("Backup & Restore", "cloud_sync_outlined", "backup", NavigateCommand),
                new MenuItemDescription("Exit", "logout_outlined", "exit", NavigateCommand)
            ])
        ];
    }

    private async Task NavigateAsync(string route)
    {
        Shell.Current.FlyoutIsPresented = false;

        // "queue" is the Shell root FlyoutItem — pop all pushed pages to return to it
        if (route == "queue")
        {
            await Shell.Current.Navigation.PopToRootAsync(animated: false);
            return;
        }

        if (route == "exit")
        {
            await Shell.Current.Navigation.PopToRootAsync(animated: false);
            ExitRequested?.Invoke();
            return;
        }

        if (!PageTypes.TryGetValue(route, out var pageType))
            return;

        // Resolve a fresh page instance from DI and push it onto the navigation stack.
        // This lets the device back button pop back to the previous page instead of exiting.
        var page = (Page)_serviceProvider.GetRequiredService(pageType);
        await Shell.Current.Navigation.PushAsync(page);
    }
}
