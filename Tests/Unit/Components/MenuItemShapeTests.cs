using Microsoft.Maui;
using MyVocaList.UI.Components.Menus;

namespace MyVocaList.Tests.Unit.Components;

public class MenuItemShapeTests
{
    // [AC] REQ-OVFM-21: a single item has 12dp corners on all four sides.
    [Fact]
    public void For_SingleItem_All12()
    {
        var shape = MenuItemShape.For(index: 0, count: 1);

        AssertCorners(shape, topLeft: 12, topRight: 12, bottomLeft: 12, bottomRight: 12);
    }

    // [AC] REQ-OVFM-21: the first item has 12dp top corners and 4dp bottom corners.
    [Fact]
    public void For_First_Top12Bottom4()
    {
        var shape = MenuItemShape.For(index: 0, count: 3);

        AssertCorners(shape, topLeft: 12, topRight: 12, bottomLeft: 4, bottomRight: 4);
    }

    // [AC] REQ-OVFM-21: the last item has 4dp top corners and 12dp bottom corners.
    [Fact]
    public void For_Last_Top4Bottom12()
    {
        var shape = MenuItemShape.For(index: 2, count: 3);

        AssertCorners(shape, topLeft: 4, topRight: 4, bottomLeft: 12, bottomRight: 12);
    }

    // [AC] REQ-OVFM-21: a middle item has 4dp corners on all four sides.
    [Fact]
    public void For_Middle_All4()
    {
        var shape = MenuItemShape.For(index: 1, count: 3);

        AssertCorners(shape, topLeft: 4, topRight: 4, bottomLeft: 4, bottomRight: 4);
    }

    private static void AssertCorners(CornerRadius shape, double topLeft, double topRight, double bottomLeft, double bottomRight)
    {
        Assert.Equal(topLeft, shape.TopLeft);
        Assert.Equal(topRight, shape.TopRight);
        Assert.Equal(bottomLeft, shape.BottomLeft);
        Assert.Equal(bottomRight, shape.BottomRight);
    }
}
