namespace MyVocaList.UI.Models;

/// <summary>
/// Groups related menu items under a section header.
/// </summary>
public record MenuGroup(string GroupTitle, List<MenuItemDescription> Items);
