using MyVocaList.UI.ViewModels;

namespace MyVocaList.UI.Components.Menus;

/// <summary>
/// Selection rule for a menu item (REQ-OVFM-19). Pure logic, unit-tested without MAUI handlers.
/// The command runs first, so it can read the target before the menu clears it; the menu closes afterwards,
/// and only when it is still open.
/// </summary>
public static class MenuItemSelection
{
    /// <summary>Runs <paramref name="item"/>'s command when it can execute, then closes the menu if it is still open and <paramref name="closeOnItemSelected"/> is true.</summary>
    /// <param name="item">The selected menu item.</param>
    /// <param name="isOpen">Reports whether the menu is still open after the command ran.</param>
    /// <param name="close">Closes the menu.</param>
    /// <param name="closeOnItemSelected">The menu's <c>CloseOnItemSelected</c> value.</param>
    public static void Select(MenuItemModel item, Func<bool> isOpen, Action close, bool closeOnItemSelected)
    {
        if (item.Command is { } command && command.CanExecute(item.CommandParameter))
        {
            command.Execute(item.CommandParameter);
        }

        if (closeOnItemSelected && isOpen())
        {
            close();
        }
    }
}
