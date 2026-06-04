using CommunityToolkit.Mvvm.Messaging;
using MyVocaList.UI.Messages;

namespace MyVocaList.UI.ViewModels;

/// <summary>
/// Provides menu structure and navigation for the AppShell flyout.
/// </summary>
public class AppShellViewModel : ViewModelBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IWhatsNewService _whatsNewService;
    private readonly IVersionCheckService _versionCheckService;

    public string AppTitle => AppInfo.Name;

    public string AppDescription => NavigationConfig.AppDescription;

    public List<MenuGroup> MenuGroups { get; }

    public IAsyncRelayCommand<string> NavigateCommand { get; }

    /// <summary>Raised when the user requests to exit the app (menu item or back at root).</summary>
    public event Action? ExitRequested;

    public AppShellViewModel(
        IServiceProvider serviceProvider,
        IWhatsNewService whatsNewService,
        IVersionCheckService versionCheckService)
    {
        _serviceProvider = serviceProvider;
        _whatsNewService = whatsNewService;
        _versionCheckService = versionCheckService;
        NavigateCommand = new AsyncRelayCommand<string>(route => NavigateAsync(route!));
        MenuGroups = NavigationConfig.BuildMenuGroups(NavigateCommand);
    }

    /// <summary>
    /// Runs startup checks (What's New + version check) and sends messages to trigger UI responses.
    /// Called once from AppShell after initialization.
    /// </summary>
    public async Task InitializeAsync(CancellationToken ct = default)
    {
        var entry = await _whatsNewService.GetPendingReleaseAsync(ct);
        if (entry is not null)
            WeakReferenceMessenger.Default.Send(new ShowWhatsNewMessage(entry));

        var updateResult = await _versionCheckService.CheckForUpdatesAsync(ct);
        if (updateResult.IsUpdateRequired)
            WeakReferenceMessenger.Default.Send(new ShowUpdateRequiredMessage(updateResult));
        else if (updateResult.IsUpdateAvailable)
            WeakReferenceMessenger.Default.Send(new ShowUpdateAvailableMessage(updateResult));
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

        var queryIndex = route.IndexOf('?');
        var baseRoute = queryIndex >= 0 ? route[..queryIndex] : route;
        var queryString = queryIndex >= 0 ? route[(queryIndex + 1)..] : null;

        if (!NavigationConfig.PageTypes.TryGetValue(baseRoute, out var pageType))
            return;

        var page = (Page)_serviceProvider.GetRequiredService(pageType);
        await Shell.Current.Navigation.PushAsync(page);

        if (queryString is not null && page.BindingContext is IQueryAttributable attributable)
        {
            var queryParams = queryString
                .Split('&', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Split('=', 2))
                .Where(p => p.Length == 2)
                .ToDictionary(p => p[0], p => (object)p[1]);
            attributable.ApplyQueryAttributes(queryParams);
        }
    }
}
