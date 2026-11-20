namespace MyVocaList.UI.Components.Menus;

/// <summary>
/// Menu lookup rule for an <c>OverflowMenuButton</c> (REQ-OVFM-20). Pure logic: the menu type is generic,
/// so the rule is unit-tested without referencing <c>Menu</c>.
/// </summary>
public static class MenuResolution
{
    /// <summary>
    /// Returns <paramref name="explicitMenu"/> when set; otherwise the first non-null menu that
    /// <paramref name="hostMenu"/> reports over <paramref name="ancestors"/> (nearest first); otherwise null.
    /// </summary>
    /// <param name="explicitMenu">The menu set directly on the button, or null.</param>
    /// <param name="ancestors">The visual ancestors of the button, nearest first.</param>
    /// <param name="hostMenu">Returns the menu hosted by an ancestor, or null when the ancestor hosts none.</param>
    public static TMenu? Resolve<TMenu>(TMenu? explicitMenu, IEnumerable<object> ancestors, Func<object, TMenu?> hostMenu)
        where TMenu : class
    {
        if (explicitMenu is not null)
        {
            return explicitMenu;
        }

        foreach (var ancestor in ancestors)
        {
            var menu = hostMenu(ancestor);
            if (menu is not null)
            {
                return menu;
            }
        }

        return null;
    }
}
