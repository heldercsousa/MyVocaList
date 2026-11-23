namespace MyVocaList.UI.Components.AppBars;

public partial class SmallAppBarSf : AppBarBase
{
    // ── Title ──────────────────────────────────────────────────────────────

    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(nameof(Title), typeof(string), typeof(SmallAppBarSf), string.Empty);

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    // ── Subtitle ───────────────────────────────────────────────────────────

    public static readonly BindableProperty SubtitleProperty =
        BindableProperty.Create(nameof(Subtitle), typeof(string), typeof(SmallAppBarSf), string.Empty,
            propertyChanged: (b, _, _) => ((SmallAppBarSf)b).OnPropertyChanged(nameof(HasSubtitle)));

    public string Subtitle
    {
        get => (string)GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public bool HasSubtitle => !string.IsNullOrEmpty(Subtitle);

    // ── Navigation icon ────────────────────────────────────────────────────

    public static readonly BindableProperty NavigationIconProperty =
        BindableProperty.Create(nameof(NavigationIcon), typeof(string), typeof(SmallAppBarSf), string.Empty,
            propertyChanged: (b, _, _) => ((SmallAppBarSf)b).OnPropertyChanged(nameof(HasNavigationIcon)));

    public string NavigationIcon
    {
        get => (string)GetValue(NavigationIconProperty);
        set => SetValue(NavigationIconProperty, value);
    }

    public static readonly BindableProperty NavigationCommandProperty =
        BindableProperty.Create(nameof(NavigationCommand), typeof(ICommand), typeof(SmallAppBarSf));

    public ICommand NavigationCommand
    {
        get => (ICommand)GetValue(NavigationCommandProperty);
        set => SetValue(NavigationCommandProperty, value);
    }

    public bool HasNavigationIcon => !string.IsNullOrEmpty(NavigationIcon);

    // ── Constructor ────────────────────────────────────────────────────────

    public SmallAppBarSf()
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
}
