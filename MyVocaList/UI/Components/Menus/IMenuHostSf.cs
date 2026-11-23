namespace MyVocaList.UI.Components.Menus;

/// <summary>
/// Migration copy of <see cref="IMenuHost"/> typed to <see cref="MenuSf"/>; merged back into <see cref="IMenuHost"/> in Phase 5.
/// </summary>
public interface IMenuHostSf
{
    /// <summary>The menu this host owns, or null when it hosts none.</summary>
    MenuSf? HostedMenu { get; }
}
