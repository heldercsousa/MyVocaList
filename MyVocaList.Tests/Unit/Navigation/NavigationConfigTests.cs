using MyVocaList.Navigation;

namespace MyVocaList.Tests.Unit.Navigation;

/// <summary>
/// Regression coverage for BUG-077: a menu item whose route is missing from
/// <see cref="NavigationConfig.PageTypes"/> silently fails to navigate — no exception,
/// no page shown. Every menu-driven route (excluding the special-cased Queue/Exit
/// routes handled directly in AppShellViewModel.NavigateAsync) must resolve to a page type.
/// </summary>
public class NavigationConfigTests
{
    // [AC] BUG-077: tapping the "About" flyout item must navigate to the About page.
    [Fact]
    public void PageTypes_ContainsEntryForAboutRoute()
    {
        Assert.True(
            NavigationConfig.PageTypes.ContainsKey(Routes.About),
            "NavigationConfig.PageTypes is missing an entry for Routes.About — " +
            "the About flyout item cannot navigate (BUG-077).");
    }

    // [AC] BUG-077: every non-special-cased menu route must have a resolvable page type,
    // preventing the same class of silent-navigation-failure bug for any future menu item.
    [Fact]
    public void PageTypes_ContainsEntryForEveryMenuRoute_ExceptSpecialCasedRoutes()
    {
        var specialCasedRoutes = new HashSet<string> { Routes.Queue, Routes.Exit };

        var menuRoutes = NavigationConfig.BuildMenuGroups(navigateCommand: null!)
            .SelectMany(g => g.Items)
            .Select(i => i.Route)
            .Where(route => !specialCasedRoutes.Contains(route))
            .Distinct();

        foreach (var route in menuRoutes)
        {
            Assert.True(
                NavigationConfig.PageTypes.ContainsKey(route),
                $"NavigationConfig.PageTypes is missing an entry for route '{route}' — " +
                "the corresponding flyout item would silently fail to navigate.");
        }
    }
}
