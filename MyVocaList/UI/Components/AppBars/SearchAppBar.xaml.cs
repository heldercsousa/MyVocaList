using System.Runtime.CompilerServices;

namespace MyVocaList.UI.Components.AppBars;

public partial class SearchAppBar : AppBarBase
{
    // ── SearchText ─────────────────────────────────────────────────────────

    public static readonly BindableProperty SearchTextProperty =
        BindableProperty.Create(nameof(SearchText), typeof(string), typeof(SearchAppBar), string.Empty,
            BindingMode.TwoWay);

    public string SearchText
    {
        get => (string)GetValue(SearchTextProperty);
        set => SetValue(SearchTextProperty, value);
    }

    // ── Placeholder ────────────────────────────────────────────────────────

    public static readonly BindableProperty PlaceholderProperty =
        BindableProperty.Create(nameof(Placeholder), typeof(string), typeof(SearchAppBar), "Search...");

    public string Placeholder
    {
        get => (string)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    // ── BackCommand ────────────────────────────────────────────────────────

    public static readonly BindableProperty BackCommandProperty =
        BindableProperty.Create(nameof(BackCommand), typeof(ICommand), typeof(SearchAppBar));

    public ICommand BackCommand
    {
        get => (ICommand)GetValue(BackCommandProperty);
        set => SetValue(BackCommandProperty, value);
    }

    // ── Constructor ────────────────────────────────────────────────────────

    public SearchAppBar()
    {
        InitializeComponent();
    }

    // ── AppBarBase implementation ──────────────────────────────────────────

    protected override void UpdateContainerColor()
    {
        var key = IsElevated ? "SurfaceContainer" : "Surface";
        if (Application.Current?.Resources.TryGetValue(key, out var color) == true)
            container.BackgroundColor = (Color)color;
    }

    // ── Auto-focus when shown ──────────────────────────────────────────────

    protected override void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        if (propertyName == nameof(IsVisible) && IsVisible)
            searchEdit?.Focus();
    }

    // ── Leading button: always dismisses search ────────────────────────────

    private void OnLeadingButtonClicked(object sender, EventArgs e)
    {
        SearchText = string.Empty;
        searchEdit.Unfocus();
        BackCommand?.Execute(null);
    }
}
