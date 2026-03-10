namespace MyVocaList.UI.Components.AppBars;

public partial class SearchAppBar : ContentView
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

    // ── Leading icon ───────────────────────────────────────────────────────

    public static readonly BindableProperty LeadingIconProperty =
        BindableProperty.Create(nameof(LeadingIcon), typeof(string), typeof(SearchAppBar), string.Empty,
            propertyChanged: (b, _, _) => ((SearchAppBar)b).OnPropertyChanged(nameof(HasLeadingIcon)));

    public string LeadingIcon
    {
        get => (string)GetValue(LeadingIconProperty);
        set => SetValue(LeadingIconProperty, value);
    }

    public static readonly BindableProperty LeadingCommandProperty =
        BindableProperty.Create(nameof(LeadingCommand), typeof(ICommand), typeof(SearchAppBar));

    public ICommand LeadingCommand
    {
        get => (ICommand)GetValue(LeadingCommandProperty);
        set => SetValue(LeadingCommandProperty, value);
    }

    public bool HasLeadingIcon => !string.IsNullOrEmpty(LeadingIcon);

    // ── Trailing icon ──────────────────────────────────────────────────────

    public static readonly BindableProperty TrailingIconProperty =
        BindableProperty.Create(nameof(TrailingIcon), typeof(string), typeof(SearchAppBar), string.Empty,
            propertyChanged: (b, _, _) => ((SearchAppBar)b).OnPropertyChanged(nameof(HasTrailingIcon)));

    public string TrailingIcon
    {
        get => (string)GetValue(TrailingIconProperty);
        set => SetValue(TrailingIconProperty, value);
    }

    public static readonly BindableProperty TrailingCommandProperty =
        BindableProperty.Create(nameof(TrailingCommand), typeof(ICommand), typeof(SearchAppBar));

    public ICommand TrailingCommand
    {
        get => (ICommand)GetValue(TrailingCommandProperty);
        set => SetValue(TrailingCommandProperty, value);
    }

    public bool HasTrailingIcon => !string.IsNullOrEmpty(TrailingIcon);

    // ── IsElevated (scroll lift) ───────────────────────────────────────────

    public static readonly BindableProperty IsElevatedProperty =
        BindableProperty.Create(nameof(IsElevated), typeof(bool), typeof(SearchAppBar), false,
            propertyChanged: (b, _, _) => ((SearchAppBar)b).UpdateContainerColor());

    public bool IsElevated
    {
        get => (bool)GetValue(IsElevatedProperty);
        set => SetValue(IsElevatedProperty, value);
    }

    // ── Constructor ────────────────────────────────────────────────────────

    public SearchAppBar()
    {
        InitializeComponent();
    }

    // ── Private helpers ────────────────────────────────────────────────────

    private void UpdateContainerColor()
    {
        var key = IsElevated ? "SurfaceContainer" : "Surface";
        if (Application.Current?.Resources.TryGetValue(key, out var color) == true)
            container.BackgroundColor = (Color)color;
    }
}
