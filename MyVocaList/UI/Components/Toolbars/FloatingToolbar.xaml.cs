namespace MyVocaList.UI.Components.Toolbars;

/// <summary>
/// M3 Floating Toolbar — a persistent pill-shaped container holding up to 5 page-specific
/// icon-only action buttons. Floats above page content at the bottom of the screen.
/// </summary>
/// <remarks>
/// Place inside a <c>HorizontalStackLayout</c> beside a FAB, both wrapped in a single-cell Grid
/// overlay with <c>VerticalOptions="End" Margin="0,0,0,16" HorizontalOptions="Center"</c>.
/// Give the list content below it <c>Margin.Bottom=80</c> to prevent overlap.
/// </remarks>
public partial class FloatingToolbar : ContentView
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Applies the MD3 selected/unselected state colors to a vibrant toolbar icon button.</summary>
    private void ApplySelectedState(DXButton button, bool isSelected)
    {
        // Vibrant toolbar bg = SecondaryContainer.
        // Selected: Primary bg + OnPrimary icon (contrasts against SecondaryContainer).
        // Unselected: transparent bg + OnSecondaryContainer icon (default).
        var bgKey = isSelected ? "Primary" : null;
        var iconKey = isSelected ? "OnPrimary" : "OnSecondaryContainer";

        button.BackgroundColor = bgKey != null &&
            Application.Current?.Resources.TryGetValue(bgKey, out var bg) == true
            ? (Color)bg
            : Colors.Transparent;

        if (Application.Current?.Resources.TryGetValue(iconKey, out var iconColor) == true)
            button.IconColor = (Color)iconColor;
    }

    // ── Constructor ───────────────────────────────────────────────────────────

    public FloatingToolbar()
    {
        InitializeComponent();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Slot 1
    // ══════════════════════════════════════════════════════════════════════════

    public static readonly BindableProperty Action1IconProperty =
        BindableProperty.Create(nameof(Action1Icon), typeof(string), typeof(FloatingToolbar), string.Empty,
            propertyChanged: (b, _, _) => ((FloatingToolbar)b).OnPropertyChanged(nameof(HasAction1)));

    /// <summary>Icon name for slot 1. Slot is hidden when null or empty.</summary>
    public string Action1Icon
    {
        get => (string)GetValue(Action1IconProperty);
        set => SetValue(Action1IconProperty, value);
    }

    public static readonly BindableProperty Action1CommandProperty =
        BindableProperty.Create(nameof(Action1Command), typeof(ICommand), typeof(FloatingToolbar));

    public ICommand Action1Command
    {
        get => (ICommand)GetValue(Action1CommandProperty);
        set => SetValue(Action1CommandProperty, value);
    }

    public static readonly BindableProperty Action1DescriptionProperty =
        BindableProperty.Create(nameof(Action1Description), typeof(string), typeof(FloatingToolbar), string.Empty);

    /// <summary>SemanticProperties.Description for TalkBack / accessibility.</summary>
    public string Action1Description
    {
        get => (string)GetValue(Action1DescriptionProperty);
        set => SetValue(Action1DescriptionProperty, value);
    }

    public static readonly BindableProperty Action1IsSelectedProperty =
        BindableProperty.Create(nameof(Action1IsSelected), typeof(bool), typeof(FloatingToolbar), false,
            propertyChanged: (b, _, n) => ((FloatingToolbar)b).ApplySelectedState(((FloatingToolbar)b).action1Button, (bool)n));

    /// <summary>
    /// When true, applies Primary background and OnPrimary icon color to indicate an
    /// active/toggled state. Contrasts against the SecondaryContainer toolbar background.
    /// </summary>
    public bool Action1IsSelected
    {
        get => (bool)GetValue(Action1IsSelectedProperty);
        set => SetValue(Action1IsSelectedProperty, value);
    }

    public bool HasAction1 => !string.IsNullOrEmpty(Action1Icon);

    // ══════════════════════════════════════════════════════════════════════════
    // Slot 2
    // ══════════════════════════════════════════════════════════════════════════

    public static readonly BindableProperty Action2IconProperty =
        BindableProperty.Create(nameof(Action2Icon), typeof(string), typeof(FloatingToolbar), string.Empty,
            propertyChanged: (b, _, _) => ((FloatingToolbar)b).OnPropertyChanged(nameof(HasAction2)));

    public string Action2Icon
    {
        get => (string)GetValue(Action2IconProperty);
        set => SetValue(Action2IconProperty, value);
    }

    public static readonly BindableProperty Action2CommandProperty =
        BindableProperty.Create(nameof(Action2Command), typeof(ICommand), typeof(FloatingToolbar));

    public ICommand Action2Command
    {
        get => (ICommand)GetValue(Action2CommandProperty);
        set => SetValue(Action2CommandProperty, value);
    }

    public static readonly BindableProperty Action2DescriptionProperty =
        BindableProperty.Create(nameof(Action2Description), typeof(string), typeof(FloatingToolbar), string.Empty);

    public string Action2Description
    {
        get => (string)GetValue(Action2DescriptionProperty);
        set => SetValue(Action2DescriptionProperty, value);
    }

    public static readonly BindableProperty Action2IsSelectedProperty =
        BindableProperty.Create(nameof(Action2IsSelected), typeof(bool), typeof(FloatingToolbar), false,
            propertyChanged: (b, _, n) => ((FloatingToolbar)b).ApplySelectedState(((FloatingToolbar)b).action2Button, (bool)n));

    public bool Action2IsSelected
    {
        get => (bool)GetValue(Action2IsSelectedProperty);
        set => SetValue(Action2IsSelectedProperty, value);
    }

    public bool HasAction2 => !string.IsNullOrEmpty(Action2Icon);

    // ══════════════════════════════════════════════════════════════════════════
    // Slot 3
    // ══════════════════════════════════════════════════════════════════════════

    public static readonly BindableProperty Action3IconProperty =
        BindableProperty.Create(nameof(Action3Icon), typeof(string), typeof(FloatingToolbar), string.Empty,
            propertyChanged: (b, _, _) => ((FloatingToolbar)b).OnPropertyChanged(nameof(HasAction3)));

    public string Action3Icon
    {
        get => (string)GetValue(Action3IconProperty);
        set => SetValue(Action3IconProperty, value);
    }

    public static readonly BindableProperty Action3CommandProperty =
        BindableProperty.Create(nameof(Action3Command), typeof(ICommand), typeof(FloatingToolbar));

    public ICommand Action3Command
    {
        get => (ICommand)GetValue(Action3CommandProperty);
        set => SetValue(Action3CommandProperty, value);
    }

    public static readonly BindableProperty Action3DescriptionProperty =
        BindableProperty.Create(nameof(Action3Description), typeof(string), typeof(FloatingToolbar), string.Empty);

    public string Action3Description
    {
        get => (string)GetValue(Action3DescriptionProperty);
        set => SetValue(Action3DescriptionProperty, value);
    }

    public static readonly BindableProperty Action3IsSelectedProperty =
        BindableProperty.Create(nameof(Action3IsSelected), typeof(bool), typeof(FloatingToolbar), false,
            propertyChanged: (b, _, n) => ((FloatingToolbar)b).ApplySelectedState(((FloatingToolbar)b).action3Button, (bool)n));

    public bool Action3IsSelected
    {
        get => (bool)GetValue(Action3IsSelectedProperty);
        set => SetValue(Action3IsSelectedProperty, value);
    }

    public bool HasAction3 => !string.IsNullOrEmpty(Action3Icon);

    // ══════════════════════════════════════════════════════════════════════════
    // Slot 4
    // ══════════════════════════════════════════════════════════════════════════

    public static readonly BindableProperty Action4IconProperty =
        BindableProperty.Create(nameof(Action4Icon), typeof(string), typeof(FloatingToolbar), string.Empty,
            propertyChanged: (b, _, _) => ((FloatingToolbar)b).OnPropertyChanged(nameof(HasAction4)));

    public string Action4Icon
    {
        get => (string)GetValue(Action4IconProperty);
        set => SetValue(Action4IconProperty, value);
    }

    public static readonly BindableProperty Action4CommandProperty =
        BindableProperty.Create(nameof(Action4Command), typeof(ICommand), typeof(FloatingToolbar));

    public ICommand Action4Command
    {
        get => (ICommand)GetValue(Action4CommandProperty);
        set => SetValue(Action4CommandProperty, value);
    }

    public static readonly BindableProperty Action4DescriptionProperty =
        BindableProperty.Create(nameof(Action4Description), typeof(string), typeof(FloatingToolbar), string.Empty);

    public string Action4Description
    {
        get => (string)GetValue(Action4DescriptionProperty);
        set => SetValue(Action4DescriptionProperty, value);
    }

    public static readonly BindableProperty Action4IsSelectedProperty =
        BindableProperty.Create(nameof(Action4IsSelected), typeof(bool), typeof(FloatingToolbar), false,
            propertyChanged: (b, _, n) => ((FloatingToolbar)b).ApplySelectedState(((FloatingToolbar)b).action4Button, (bool)n));

    public bool Action4IsSelected
    {
        get => (bool)GetValue(Action4IsSelectedProperty);
        set => SetValue(Action4IsSelectedProperty, value);
    }

    public bool HasAction4 => !string.IsNullOrEmpty(Action4Icon);

    // ══════════════════════════════════════════════════════════════════════════
    // Slot 5
    // ══════════════════════════════════════════════════════════════════════════

    public static readonly BindableProperty Action5IconProperty =
        BindableProperty.Create(nameof(Action5Icon), typeof(string), typeof(FloatingToolbar), string.Empty,
            propertyChanged: (b, _, _) => ((FloatingToolbar)b).OnPropertyChanged(nameof(HasAction5)));

    public string Action5Icon
    {
        get => (string)GetValue(Action5IconProperty);
        set => SetValue(Action5IconProperty, value);
    }

    public static readonly BindableProperty Action5CommandProperty =
        BindableProperty.Create(nameof(Action5Command), typeof(ICommand), typeof(FloatingToolbar));

    public ICommand Action5Command
    {
        get => (ICommand)GetValue(Action5CommandProperty);
        set => SetValue(Action5CommandProperty, value);
    }

    public static readonly BindableProperty Action5DescriptionProperty =
        BindableProperty.Create(nameof(Action5Description), typeof(string), typeof(FloatingToolbar), string.Empty);

    public string Action5Description
    {
        get => (string)GetValue(Action5DescriptionProperty);
        set => SetValue(Action5DescriptionProperty, value);
    }

    public static readonly BindableProperty Action5IsSelectedProperty =
        BindableProperty.Create(nameof(Action5IsSelected), typeof(bool), typeof(FloatingToolbar), false,
            propertyChanged: (b, _, n) => ((FloatingToolbar)b).ApplySelectedState(((FloatingToolbar)b).action5Button, (bool)n));

    public bool Action5IsSelected
    {
        get => (bool)GetValue(Action5IsSelectedProperty);
        set => SetValue(Action5IsSelectedProperty, value);
    }

    public bool HasAction5 => !string.IsNullOrEmpty(Action5Icon);
}
