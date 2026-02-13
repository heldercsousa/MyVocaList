using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using MyVocaList.Navigation;
using MyVocaList.UI.Models;

namespace MyVocaList.UI.ViewModels;

/// <summary>
/// Provides menu structure and navigation for the AppShell flyout.
/// </summary>
public class AppShellViewModel
{
    private readonly IServiceProvider _serviceProvider;

    public string AppTitle => AppInfo.Name;

    public string AppDescription => NavigationConfig.AppDescription;

    public List<MenuGroup> MenuGroups { get; }

    public ICommand NavigateCommand { get; }

    /// <summary>Raised when the user requests to exit the app (menu item or back at root).</summary>
    public event Action? ExitRequested;

    public AppShellViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        NavigateCommand = new Command<string>(async route => await NavigateAsync(route));
        MenuGroups = NavigationConfig.BuildMenuGroups(NavigateCommand);
    }

    private async Task NavigateAsync(string route)
    {
        Shell.Current.FlyoutIsPresented = false;

        if (route == Routes.Queue)
        {
            await Shell.Current.Navigation.PopToRootAsync(animated: false);
            return;
        }

        if (route == Routes.Exit)
        {
            await Shell.Current.Navigation.PopToRootAsync(animated: false);
            ExitRequested?.Invoke();
            return;
        }

        if (!NavigationConfig.PageTypes.TryGetValue(route, out var pageType))
            return;

        var page = (Page)_serviceProvider.GetRequiredService(pageType);
        await Shell.Current.Navigation.PushAsync(page);
    }
}
