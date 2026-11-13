using MyVocaList.Navigation;

namespace MyVocaList.UI.Services;

public class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task GoBackAsync() => Shell.Current.GoToAsync("..");

    public Task GoToAsync(string route) => Shell.Current.GoToAsync(route);

    /// <remarks>
    /// Same mechanism as the flyout (AppShellViewModel): DI-resolve a fresh page, PushAsync it onto the current
    /// Shell stack. The query is applied BEFORE the push so the page's first OnAppearing already sees its context.
    /// </remarks>
    public async Task PushAsync(string route)
    {
        var queryIndex = route.IndexOf('?');
        var baseRoute = queryIndex >= 0 ? route[..queryIndex] : route;
        var queryString = queryIndex >= 0 ? route[(queryIndex + 1)..] : null;

        if (!NavigationConfig.PageTypes.TryGetValue(baseRoute, out var pageType))
            throw new InvalidOperationException($"No page is registered for route '{baseRoute}'.");

        var page = (Page)_serviceProvider.GetRequiredService(pageType);

        if (queryString is not null && page.BindingContext is IQueryAttributable attributable)
        {
            var queryParams = queryString
                .Split('&', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Split('=', 2))
                .Where(p => p.Length == 2)
                .ToDictionary(p => p[0], p => (object)Uri.UnescapeDataString(p[1]));
            attributable.ApplyQueryAttributes(queryParams);
        }

        await Shell.Current.Navigation.PushAsync(page);
    }

    public Task PopAsync() => Shell.Current.Navigation.PopAsync();
}
