namespace MyVocaList.UI.Components.AppBars;

public partial class SmallAppBar : ContentView
{
    // ── Title ──────────────────────────────────────────────────────────────

    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(nameof(Title), typeof(string), typeof(SmallAppBar), string.Empty);

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    // ── Subtitle ───────────────────────────────────────────────────────────

    public static readonly BindableProperty SubtitleProperty =
        BindableProperty.Create(nameof(Subtitle), typeof(string), typeof(SmallAppBar), string.Empty,
            propertyChanged: (b, _, _) => ((SmallAppBar)b).UpdateSubtitleVisibility());

    public string Subtitle
    {
        get => (string)GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public bool HasSubtitle => !string.IsNullOrEmpty(Subtitle);

    // ── Navigation icon ────────────────────────────────────────────────────

    public static readonly BindableProperty NavigationIconProperty =
        BindableProperty.Create(nameof(NavigationIcon), typeof(string), typeof(SmallAppBar), string.Empty,
            propertyChanged: (b, _, _) => ((SmallAppBar)b).UpdateNavIconVisibility());

    public string NavigationIcon
    {
        get => (string)GetValue(NavigationIconProperty);
        set => SetValue(NavigationIconProperty, value);
    }

    public static readonly BindableProperty NavigationCommandProperty =
        BindableProperty.Create(nameof(NavigationCommand), typeof(ICommand), typeof(SmallAppBar));

    public ICommand NavigationCommand
    {
        get => (ICommand)GetValue(NavigationCommandProperty);
        set => SetValue(NavigationCommandProperty, value);
    }

    public bool HasNavigationIcon => !string.IsNullOrEmpty(NavigationIcon);

    // ── Action 1 ───────────────────────────────────────────────────────────

    public static readonly BindableProperty Action1IconProperty =
        BindableProperty.Create(nameof(Action1Icon), typeof(string), typeof(SmallAppBar), string.Empty,
            propertyChanged: (b, _, _) => ((SmallAppBar)b).UpdateActionVisibility());

    public string Action1Icon
    {
        get => (string)GetValue(Action1IconProperty);
        set => SetValue(Action1IconProperty, value);
    }

    public static readonly BindableProperty Action1CommandProperty =
        BindableProperty.Create(nameof(Action1Command), typeof(ICommand), typeof(SmallAppBar));

    public ICommand Action1Command
    {
        get => (ICommand)GetValue(Action1CommandProperty);
        set => SetValue(Action1CommandProperty, value);
    }

    public bool HasAction1 => !string.IsNullOrEmpty(Action1Icon);

    // ── Action 2 ───────────────────────────────────────────────────────────

    public static readonly BindableProperty Action2IconProperty =
        BindableProperty.Create(nameof(Action2Icon), typeof(string), typeof(SmallAppBar), string.Empty,
            propertyChanged: (b, _, _) => ((SmallAppBar)b).UpdateActionVisibility());

    public string Action2Icon
    {
        get => (string)GetValue(Action2IconProperty);
        set => SetValue(Action2IconProperty, value);
    }

    public static readonly BindableProperty Action2CommandProperty =
        BindableProperty.Create(nameof(Action2Command), typeof(ICommand), typeof(SmallAppBar));

    public ICommand Action2Command
    {
        get => (ICommand)GetValue(Action2CommandProperty);
        set => SetValue(Action2CommandProperty, value);
    }

    public bool HasAction2 => !string.IsNullOrEmpty(Action2Icon);

    // ── Action 3 ───────────────────────────────────────────────────────────

    public static readonly BindableProperty Action3IconProperty =
        BindableProperty.Create(nameof(Action3Icon), typeof(string), typeof(SmallAppBar), string.Empty,
            propertyChanged: (b, _, _) => ((SmallAppBar)b).UpdateActionVisibility());

    public string Action3Icon
    {
        get => (string)GetValue(Action3IconProperty);
        set => SetValue(Action3IconProperty, value);
    }

    public static readonly BindableProperty Action3CommandProperty =
        BindableProperty.Create(nameof(Action3Command), typeof(ICommand), typeof(SmallAppBar));

    public ICommand Action3Command
    {
        get => (ICommand)GetValue(Action3CommandProperty);
        set => SetValue(Action3CommandProperty, value);
    }

    public bool HasAction3 => !string.IsNullOrEmpty(Action3Icon);

    // ── IsElevated (scroll lift) ───────────────────────────────────────────

    public static readonly BindableProperty IsElevatedProperty =
        BindableProperty.Create(nameof(IsElevated), typeof(bool), typeof(SmallAppBar), false,
            propertyChanged: (b, _, _) => ((SmallAppBar)b).UpdateContainerColor());

    public bool IsElevated
    {
        get => (bool)GetValue(IsElevatedProperty);
        set => SetValue(IsElevatedProperty, value);
    }

    // ── Constructor ────────────────────────────────────────────────────────

    public SmallAppBar()
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

    private void UpdateSubtitleVisibility() => OnPropertyChanged(nameof(HasSubtitle));

    private void UpdateNavIconVisibility() => OnPropertyChanged(nameof(HasNavigationIcon));

    private void UpdateActionVisibility()
    {
        OnPropertyChanged(nameof(HasAction1));
        OnPropertyChanged(nameof(HasAction2));
        OnPropertyChanged(nameof(HasAction3));
    }
}
