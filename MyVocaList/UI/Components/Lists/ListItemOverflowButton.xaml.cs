using MyVocaList.UI.Components;

namespace MyVocaList.UI.Components.Lists;

/// <summary>
/// Trailing overflow icon button for <see cref="ListItem"/> rows in CRUD list pages.
/// On tap it opens the item menu hosted by the nearest <see cref="CrudListView"/>, anchored to this view.
/// Hidden in selection mode through its own <c>IsVisible</c> binding.
/// </summary>
public partial class ListItemOverflowButton : ContentView
{
    public ListItemOverflowButton()
    {
        InitializeComponent();
    }

    private void OnClicked(object? sender, EventArgs e)
    {
        var host = FindHost();
        if (host is null)
        {
            Serilog.Log.ForContext("SourceContext", nameof(ListItemOverflowButton))
                .Warning("No CrudListView ancestor found; item menu not opened");
            return;
        }

        host.ShowItemMenu(this, BindingContext);
    }

    private CrudListView? FindHost()
    {
        Element? current = Parent;
        while (current is not null)
        {
            if (current is CrudListView host)
                return host;
            current = current.Parent;
        }

        return null;
    }
}
