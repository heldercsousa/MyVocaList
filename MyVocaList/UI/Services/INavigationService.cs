namespace MyVocaList.UI.Services;

public interface INavigationService
{
    Task GoBackAsync();

    /// <summary>Navigates to a Shell route (absolute or relative, query string allowed).</summary>
    Task GoToAsync(string route);
}
