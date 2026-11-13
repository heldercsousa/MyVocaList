namespace MyVocaList.UI.Services;

public interface INavigationService
{
    Task GoBackAsync();

    /// <summary>Navigates to a Shell route (absolute or relative, query string allowed).</summary>
    Task GoToAsync(string route);

    /// <summary>
    /// Pushes a top-level menu page (a key of <c>NavigationConfig.PageTypes</c>) on top of the current stack,
    /// applying the optional query (<c>?key=value&amp;...</c>) before the page appears. Use for sub-pages that
    /// must keep the Shell root (and therefore the exit confirmation and back behaviour) intact.
    /// </summary>
    Task PushAsync(string route);

    /// <summary>Pops the page that was pushed with <see cref="PushAsync"/>.</summary>
    Task PopAsync();
}
