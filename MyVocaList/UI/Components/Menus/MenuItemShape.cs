using Microsoft.Maui;

namespace MyVocaList.UI.Components.Menus;

/// <summary>
/// Per-corner shape of a menu item (REQ-OVFM-21). The first and last items round their outer corners to 12dp;
/// inner corners and middle items use 4dp. Pure logic, unit-tested without MAUI handlers.
/// </summary>
public static class MenuItemShape
{
    private const double OuterCornerDp = 12;
    private const double InnerCornerDp = 4;

    /// <summary>Corner radius for the item at <paramref name="index"/> in a menu of <paramref name="count"/> items.</summary>
    /// <param name="index">Zero-based position of the item.</param>
    /// <param name="count">Number of items in the menu.</param>
    /// <returns>A <see cref="CornerRadius"/> ordered TopLeft, TopRight, BottomLeft, BottomRight.</returns>
    public static CornerRadius For(int index, int count)
    {
        if (count == 1)
        {
            return new CornerRadius(OuterCornerDp);
        }

        if (index == 0)
        {
            return new CornerRadius(OuterCornerDp, OuterCornerDp, InnerCornerDp, InnerCornerDp);
        }

        if (index == count - 1)
        {
            return new CornerRadius(InnerCornerDp, InnerCornerDp, OuterCornerDp, OuterCornerDp);
        }

        return new CornerRadius(InnerCornerDp);
    }
}
