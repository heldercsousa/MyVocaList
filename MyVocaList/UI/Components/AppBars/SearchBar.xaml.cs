namespace MyVocaList.UI.Components.AppBars;

/// <summary>
/// MD3 "Search bar" (standalone/docked): persistent 56dp pill search input rendered inside
/// page content (not Shell.TitleView). No back arrow, no BackCommand, no auto-focus —
/// hardware/gesture back never touches search state.
/// </summary>
/// <remarks>
/// Subclasses <see cref="AppBarBase"/> to reuse the IsElevated liftOnScroll mechanism:
/// SurfaceContainerLow (default) → SurfaceContainer (elevated). Action1–3 slots are unused.
/// Name deliberately collides with stock Microsoft.Maui.Controls.SearchBar (MD3-name fidelity;
/// the stock control is never used in this codebase) — always use the appbars: prefix in XAML.
/// </remarks>
public partial class SearchBar : AppBarBase
{
    // ── SearchText ─────────────────────────────────────────────────────────

    public static readonly BindableProperty SearchTextProperty =
        BindableProperty.Create(nameof(SearchText), typeof(string), typeof(SearchBar), string.Empty,
            BindingMode.TwoWay);

    public string SearchText
    {
        get => (string)GetValue(SearchTextProperty);
        set => SetValue(SearchTextProperty, value);
    }

    // ── Placeholder ────────────────────────────────────────────────────────

    public static readonly BindableProperty PlaceholderProperty =
        BindableProperty.Create(nameof(Placeholder), typeof(string), typeof(SearchBar), "Search...");

    public string Placeholder
    {
        get => (string)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    // ── Constructor ────────────────────────────────────────────────────────

    public SearchBar()
    {
        InitializeComponent();
    }

    // ── AppBarBase implementation ──────────────────────────────────────────

    protected override void UpdateContainerColor()
    {
        var key = IsElevated ? "SurfaceContainer" : "SurfaceContainerLow";
        if (Application.Current?.Resources.TryGetValue(key, out var color) == true)
            container.BackgroundColor = (Color)color;
    }
}
