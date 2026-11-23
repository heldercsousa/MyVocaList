using MyVocaList.UI.Components.Menus;

namespace MyVocaList.Tests.Unit.Components;

public class MenuResolutionTests
{
    // [AC] REQ-OVFM-20: an explicit menu wins over any ancestor host menu.
    [Fact]
    public void Resolve_ExplicitWins()
    {
        var ancestors = new object[] { "host-a" };

        var resolved = MenuResolution.Resolve<string>("explicit", ancestors, HostMenuOf);

        Assert.Equal("explicit", resolved);
    }

    // [AC] REQ-OVFM-20: without an explicit menu, the nearest ancestor host's menu is used.
    [Fact]
    public void Resolve_NearestHost()
    {
        var ancestors = new object[] { "host-near", "host-far" };

        var resolved = MenuResolution.Resolve<string>(null, ancestors, HostMenuOf);

        Assert.Equal("menu-near", resolved);
    }

    // [AC] REQ-OVFM-20: an ancestor whose host has no menu is skipped in favour of the next one.
    [Fact]
    public void Resolve_SkipsHostWithNullMenu()
    {
        var ancestors = new object[] { "host-empty", "host-far" };

        var resolved = MenuResolution.Resolve<string>(null, ancestors, HostMenuOf);

        Assert.Equal("menu-far", resolved);
    }

    // [AC] REQ-OVFM-20: with no explicit menu and no ancestor host menu, the result is null.
    [Fact]
    public void Resolve_NoneReturnsNull()
    {
        var ancestors = new object[] { "host-empty", "not-a-host" };

        var resolved = MenuResolution.Resolve<string>(null, ancestors, HostMenuOf);

        Assert.Null(resolved);
    }

    private static string? HostMenuOf(object ancestor) => ancestor switch
    {
        "host-near" => "menu-near",
        "host-far" => "menu-far",
        _ => null,
    };
}
