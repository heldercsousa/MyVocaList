using System.Windows.Input;

namespace MyVocaList.UI.ViewModels;

/// <summary>One entry of the list item overflow menu, as the UI binds it. UI-agnostic: only <see cref="ICommand"/> is referenced.</summary>
/// <param name="Text">Label shown in the menu item.</param>
/// <param name="Icon">Icon resource name shown leading the label.</param>
/// <param name="Command">Command run when the item is selected; CanExecute drives the enabled state.</param>
/// <param name="CommandParameter">Parameter passed to <paramref name="Command"/>.</param>
public sealed record MenuItemModel(string Text, string Icon, ICommand Command, object? CommandParameter = null);
