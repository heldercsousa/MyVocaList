#if DEBUG
namespace MyVocaList.UI.Migration;

/// <summary>Display row for one preview entry (tuple fields are not bindable).</summary>
public sealed record PreviewItem(string Title, string Route);

public partial class SyncfusionPreviewPage : ContentPage
{
    public SyncfusionPreviewPage()
    {
        InitializeComponent();
        BindableLayout.SetItemsSource(
            EntriesLayout,
            PreviewRoutes.Entries.Select(e => new PreviewItem(e.Title, e.Route)).ToList());
        EmptyLabel.IsVisible = PreviewRoutes.Entries.Count == 0;
    }

    private async void OnEntryTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is string route)
            await Shell.Current.GoToAsync(route);
    }
}
#endif
