using System.Windows.Input;

namespace MyVocaList.UI.Components.Menus;

/// <summary>
/// MD3 overflow icon button (<c>more_vert_outlined</c>, StandardIconButton, 48×48). On tap it resolves a <see cref="Menu"/>:
/// the explicit <see cref="Menu"/> property, else the nearest ancestor <see cref="IMenuHost"/> (REQ-OVFM-20). It anchors that
/// menu to itself, then either runs <see cref="Command"/> (the ViewModel opens the menu through its IsOpen binding) or shows
/// the menu directly. While the menu is open, the button shows the pressed state layer (REQ-OVFM-22).
/// Has no dependency on any page, ViewModel or list type (REQ-OVFM-25).
/// </summary>
public partial class OverflowMenuButton : ContentView
{
    /// <summary>MD3 pressed state-layer opacity applied to On surface variant.</summary>
    private const float PressedStateLayerOpacity = 0.10f;

    /// <summary>The menu this button opens. Null falls back to the nearest <see cref="IMenuHost"/> ancestor.</summary>
    public static readonly BindableProperty MenuProperty = BindableProperty.Create(
        nameof(Menu),
        typeof(Menu),
        typeof(OverflowMenuButton),
        null);

    /// <summary>Runs on tap instead of showing the menu directly. The ViewModel is then responsible for opening the menu.</summary>
    public static readonly BindableProperty CommandProperty = BindableProperty.Create(
        nameof(Command),
        typeof(ICommand),
        typeof(OverflowMenuButton),
        null);

    /// <summary>Parameter passed to <see cref="Command"/>. Falls back to the BindingContext when null.</summary>
    public static readonly BindableProperty CommandParameterProperty = BindableProperty.Create(
        nameof(CommandParameter),
        typeof(object),
        typeof(OverflowMenuButton),
        null);

    /// <summary>Accessibility description of the button.</summary>
    public static readonly BindableProperty DescriptionProperty = BindableProperty.Create(
        nameof(Description),
        typeof(string),
        typeof(OverflowMenuButton),
        "More options");

    private Menu? _pressedMenu;

    public OverflowMenuButton()
    {
        InitializeComponent();
    }

    /// <summary>The menu this button opens. Null falls back to the nearest <see cref="IMenuHost"/> ancestor.</summary>
    public Menu? Menu
    {
        get => (Menu?)GetValue(MenuProperty);
        set => SetValue(MenuProperty, value);
    }

    /// <summary>Runs on tap instead of showing the menu directly.</summary>
    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    /// <summary>Parameter passed to <see cref="Command"/>. Falls back to the BindingContext when null.</summary>
    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    /// <summary>Accessibility description of the button.</summary>
    public string Description
    {
        get => (string)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    private void OnClicked(object? sender, EventArgs e)
    {
        var menu = MenuResolution.Resolve(Menu, AncestorsOf(this), host => (host as IMenuHost)?.HostedMenu);
        if (menu is null)
        {
            Serilog.Log.ForContext("SourceContext", nameof(OverflowMenuButton))
                .Warning("No Menu set and no IMenuHost ancestor found; overflow menu not opened");
            return;
        }

        menu.PlacementTarget = this;

        if (Command is { } command)
        {
            var parameter = CommandParameter ?? BindingContext;
            if (command.CanExecute(parameter))
            {
                command.Execute(parameter);
            }
        }
        else
        {
            menu.Show(this);
        }

        if (menu.IsOpen)
        {
            BeginPressedState(menu);
        }
    }

    private void BeginPressedState(Menu menu)
    {
        if (ReferenceEquals(_pressedMenu, menu))
        {
            return;
        }

        _pressedMenu?.Closed -= OnPressedMenuClosed;
        _pressedMenu = menu;
        menu.Closed += OnPressedMenuClosed;
        button.BackgroundColor = PressedStateLayer();
    }

    private void OnPressedMenuClosed(object? sender, EventArgs e)
    {
        _pressedMenu?.Closed -= OnPressedMenuClosed;
        _pressedMenu = null;
        button.BackgroundColor = Colors.Transparent;
    }

    private static Color PressedStateLayer()
    {
        return Application.Current?.Resources.TryGetValue("OnSurfaceVariant", out var value) == true && value is Color color
            ? color.WithAlpha(PressedStateLayerOpacity)
            : Colors.Transparent;
    }

    private static IEnumerable<object> AncestorsOf(Element start)
    {
        for (Element? current = start.Parent; current is not null; current = current.Parent)
        {
            yield return current;
        }
    }
}
