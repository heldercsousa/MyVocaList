namespace MyVocaList.UI.ViewModels;

/// <summary>One page-specific entry of the list item overflow menu (MD3 vertical menu item).</summary>
/// <param name="Text">Label shown in the menu item.</param>
/// <param name="Icon">Icon resource name shown leading the label.</param>
/// <param name="Execute">Runs the action for the target list item, after the menu has closed.</param>
public sealed record ItemMenuAction(string Text, string Icon, Func<object, Task> Execute);
