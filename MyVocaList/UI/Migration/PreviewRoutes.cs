#if DEBUG
namespace MyVocaList.UI.Migration;

/// <summary>
/// Debug-only route registry for the Syncfusion page copies awaiting cut-over.
/// Contextual routes (<c>&lt;list&gt;-sf/&lt;form&gt;</c>) do not resolve (verified T0.1c, 2026-11-23),
/// so every form gets its own <c>&lt;form&gt;-sf</c> entry.
/// </summary>
public static class PreviewRoutes
{
    public const string PreviewRoute = "syncfusion-preview";

    // Add one line per Sf page copy, e.g.: ("Venues", "venues-sf", typeof(VenuesSfPage)),
    private static readonly List<(string Title, string Route, Type Page)> _entries = new()
    {
        ("Venues", "venues-sf", typeof(VenuesSfPage)),
    };

    /// <summary>Preview entries shown on the Syncfusion preview page.</summary>
    public static IReadOnlyList<(string Title, string Route, Type Page)> Entries => _entries;

    /// <summary>Registers the preview page and every entry route. Call once at startup.</summary>
    public static void Register()
    {
        Routing.RegisterRoute(PreviewRoute, typeof(SyncfusionPreviewPage));
        foreach (var (_, route, page) in _entries)
            Routing.RegisterRoute(route, page);
    }
}
#endif
