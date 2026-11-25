namespace MyVocaList.UI.Components.Lists;

/// <summary>
/// Trailing overflow icon button for <see cref="ListItemOverflowButtonSf"/> rows in CRUD list pages.
/// Thin wrapper around <c>OverflowMenuButton</c>: the row's command (<c>OpenItemMenuCommand</c>) is bound on the
/// page's ViewModel and receives the row item. Hidden in selection mode through its own <c>IsVisible</c> binding.
/// </summary>
public partial class ListItemOverflowButtonSf : ContentView
{
    public ListItemOverflowButtonSf()
    {
        InitializeComponent();
    }
}
