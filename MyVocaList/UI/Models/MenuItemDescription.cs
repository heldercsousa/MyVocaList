namespace MyVocaList.UI.Models;

/// <summary>
/// Describes a navigation menu item in the flyout.
/// </summary>
public record MenuItemDescription(string Name, string IconSource, string Route, ICommand NavigateCommand);
