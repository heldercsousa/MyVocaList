namespace MyVocaList.UI.Services;

public class NavigationService : INavigationService
{
    public Task GoBackAsync() => Shell.Current.GoToAsync("..");

    public Task GoToAsync(string route) => Shell.Current.GoToAsync(route);
}
