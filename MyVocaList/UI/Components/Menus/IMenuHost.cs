namespace MyVocaList.UI.Components.Menus;

/// <summary>
/// Implemented by a view that hosts a <see cref="MenuSf"/>. An <c>OverflowMenuButtonSf</c> without an explicit menu walks its
/// ancestors and uses the nearest <see cref="HostedMenu"/> (REQ-OVFM-20).
/// </summary>
public interface IMenuHost
{
    /// <summary>The menu this host owns, or null when it hosts none.</summary>
    MenuSf? HostedMenu { get; }
}
