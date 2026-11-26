namespace MyVocaList.UI.Components.Menus;

/// <summary>
/// Implemented by a view that hosts a <see cref="Menu"/> so overflow buttons can resolve it from the visual tree.
/// </summary>
public interface IMenuHost
{
    /// <summary>The menu this host owns, or null when it hosts none.</summary>
    Menu? HostedMenu { get; }
}
