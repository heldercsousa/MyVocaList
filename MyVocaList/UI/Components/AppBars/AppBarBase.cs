namespace MyVocaList.UI.Components.AppBars;

/// <summary>Shared base for SmallAppBar and SearchAppBar.</summary>
/// <remarks>
/// Owns: IsElevated (liftOnScroll container color) and trailing Action1–3 slots.
/// Subclasses must implement UpdateContainerColor() to apply the color to their container Grid.
/// </remarks>
public abstract partial class AppBarBase : ContentView
{
    // ── IsElevated (liftOnScroll) ─────────────────────────────────────────

    public static readonly BindableProperty IsElevatedProperty =
        BindableProperty.Create(nameof(IsElevated), typeof(bool), typeof(AppBarBase), false,
            propertyChanged: (b, _, _) => ((AppBarBase)b).UpdateContainerColor());

    public bool IsElevated
    {
        get => (bool)GetValue(IsElevatedProperty);
        set => SetValue(IsElevatedProperty, value);
    }

    protected abstract void UpdateContainerColor();

    // ── Action 1 ──────────────────────────────────────────────────────────

    public static readonly BindableProperty Action1IconProperty =
        BindableProperty.Create(nameof(Action1Icon), typeof(string), typeof(AppBarBase), string.Empty,
            propertyChanged: (b, _, _) => ((AppBarBase)b).OnPropertyChanged(nameof(HasAction1)));

    public string Action1Icon
    {
        get => (string)GetValue(Action1IconProperty);
        set => SetValue(Action1IconProperty, value);
    }

    public static readonly BindableProperty Action1CommandProperty =
        BindableProperty.Create(nameof(Action1Command), typeof(ICommand), typeof(AppBarBase));

    public ICommand Action1Command
    {
        get => (ICommand)GetValue(Action1CommandProperty);
        set => SetValue(Action1CommandProperty, value);
    }

    public bool HasAction1 => !string.IsNullOrEmpty(Action1Icon);

    // ── Action 2 ──────────────────────────────────────────────────────────

    public static readonly BindableProperty Action2IconProperty =
        BindableProperty.Create(nameof(Action2Icon), typeof(string), typeof(AppBarBase), string.Empty,
            propertyChanged: (b, _, _) => ((AppBarBase)b).OnPropertyChanged(nameof(HasAction2)));

    public string Action2Icon
    {
        get => (string)GetValue(Action2IconProperty);
        set => SetValue(Action2IconProperty, value);
    }

    public static readonly BindableProperty Action2CommandProperty =
        BindableProperty.Create(nameof(Action2Command), typeof(ICommand), typeof(AppBarBase));

    public ICommand Action2Command
    {
        get => (ICommand)GetValue(Action2CommandProperty);
        set => SetValue(Action2CommandProperty, value);
    }

    public bool HasAction2 => !string.IsNullOrEmpty(Action2Icon);

    // ── Action 3 ──────────────────────────────────────────────────────────

    public static readonly BindableProperty Action3IconProperty =
        BindableProperty.Create(nameof(Action3Icon), typeof(string), typeof(AppBarBase), string.Empty,
            propertyChanged: (b, _, _) => ((AppBarBase)b).OnPropertyChanged(nameof(HasAction3)));

    public string Action3Icon
    {
        get => (string)GetValue(Action3IconProperty);
        set => SetValue(Action3IconProperty, value);
    }

    public static readonly BindableProperty Action3CommandProperty =
        BindableProperty.Create(nameof(Action3Command), typeof(ICommand), typeof(AppBarBase));

    public ICommand Action3Command
    {
        get => (ICommand)GetValue(Action3CommandProperty);
        set => SetValue(Action3CommandProperty, value);
    }

    public bool HasAction3 => !string.IsNullOrEmpty(Action3Icon);
}
