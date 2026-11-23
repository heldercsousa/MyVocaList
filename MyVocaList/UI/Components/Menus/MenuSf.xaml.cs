using System.Collections.Specialized;
using System.Windows.Input;
using MyVocaList.UI.Behaviors;
using Syncfusion.Maui.Toolkit.Buttons;
using Syncfusion.Maui.Toolkit.Popup;

namespace MyVocaList.UI.Components.Menus;

/// <summary>
/// MD3 vertical menu (Menus - Common), Syncfusion copy of <see cref="Menu"/>. Wraps one <c>SfPopup</c>. Item buttons are built
/// from <see cref="ItemsSource"/> with per-corner shape (REQ-OVFM-21) and an <c>IsEnabled</c> sync with
/// <c>Command.CanExecute</c> (REQ-OVFM-24). Selection follows <see cref="MenuItemSelection"/> (REQ-OVFM-19).
/// Has no dependency on any page or ViewModel type (REQ-OVFM-25).
/// </summary>
public partial class MenuSf : ContentView
{
    /// <summary>Items shown in the menu, in order. Rebuilt on every change (also observes <see cref="INotifyCollectionChanged"/>).</summary>
    public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(
        nameof(ItemsSource),
        typeof(IEnumerable<MenuItemModel>),
        typeof(MenuSf),
        null,
        propertyChanged: (bindable, oldValue, newValue) =>
            ((MenuSf)bindable).OnItemsSourceChanged((IEnumerable<MenuItemModel>?)newValue));

    /// <summary>Open state. TwoWay by default; mirrors the live popup <c>IsOpen</c> in both directions.</summary>
    public static readonly BindableProperty IsOpenProperty = BindableProperty.Create(
        nameof(IsOpen),
        typeof(bool),
        typeof(MenuSf),
        false,
        defaultBindingMode: BindingMode.TwoWay,
        propertyChanged: (bindable, oldValue, newValue) => ((MenuSf)bindable).OnIsOpenChanged((bool)newValue));

    /// <summary>The view the popup is anchored to. Set by <see cref="Show"/> or by the host before opening.</summary>
    public static readonly BindableProperty PlacementTargetProperty = BindableProperty.Create(
        nameof(PlacementTarget),
        typeof(View),
        typeof(MenuSf),
        null);

    /// <summary>Whether selecting an item closes the menu after its command runs. Default true.</summary>
    public static readonly BindableProperty CloseOnItemSelectedProperty = BindableProperty.Create(
        nameof(CloseOnItemSelected),
        typeof(bool),
        typeof(MenuSf),
        true);

    private readonly List<(ICommand Command, EventHandler Handler)> _commandSubscriptions = [];
    private readonly VerticalStackLayout itemsHost = new()
    {
        Padding = new Thickness(4, 2),
        MinimumWidthRequest = 112,
        MaximumWidthRequest = 280,
    };

    private INotifyCollectionChanged? _observedSource;

    public MenuSf()
    {
        InitializeComponent();
        popup.ContentTemplate = new DataTemplate(() => itemsHost);
        popup.Opened += OnPopupOpened;
        popup.Closed += OnPopupClosed;
        ApplyPopupStyle();
    }

    /// <summary>Items shown in the menu, in order.</summary>
    public IEnumerable<MenuItemModel>? ItemsSource
    {
        get => (IEnumerable<MenuItemModel>?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    /// <summary>Open state. Setting true opens the menu; setting false closes it.</summary>
    public bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    /// <summary>The view the popup is anchored to.</summary>
    public View? PlacementTarget
    {
        get => (View?)GetValue(PlacementTargetProperty);
        set => SetValue(PlacementTargetProperty, value);
    }

    /// <summary>Whether selecting an item closes the menu after its command runs. Default true.</summary>
    public bool CloseOnItemSelected
    {
        get => (bool)GetValue(CloseOnItemSelectedProperty);
        set => SetValue(CloseOnItemSelectedProperty, value);
    }

    /// <summary>Raised when the menu opens.</summary>
    public event EventHandler? Opened;

    /// <summary>Raised for every close path: outside tap, back, item selection, or <see cref="IsOpen"/> set to false.</summary>
    public event EventHandler? Closed;

    /// <summary>Anchors the menu to <paramref name="anchor"/> and opens it.</summary>
    /// <param name="anchor">The view the menu opens from.</param>
    public void Show(View anchor)
    {
        PlacementTarget = anchor;
        IsOpen = true;
    }

    /// <summary>Closes the menu.</summary>
    public void Close() => IsOpen = false;

    private void ApplyPopupStyle()
    {
        popup.PopupStyle = new PopupStyle
        {
            PopupBackground = new SolidColorBrush(ResolveColor("SurfaceContainerLow")),
            CornerRadius = new CornerRadius(16),
            HasShadow = true,
            StrokeThickness = 0,
        };
    }

    private void OnIsOpenChanged(bool isOpen)
    {
        if (popup.IsOpen == isOpen)
        {
            return;
        }

        if (!isOpen)
        {
            popup.Dismiss();
        }
        else if (PlacementTarget is { } target)
        {
            // DX aligned the menu right edge with the anchor right edge. AlignBottom puts the popup below the anchor with the left
            // edges aligned, so shift it left by (anchor width - menu width) to line the right edges up.
            var menuWidth = itemsHost.Measure(itemsHost.MaximumWidthRequest, double.PositiveInfinity).Width;
            popup.ShowRelativeToView(target, PopupRelativePosition.AlignBottom, target.Width - menuWidth, 0);
        }
        else
        {
            popup.Show();
        }
    }

    private void OnPopupOpened(object? sender, EventArgs e)
    {
        if (!IsOpen)
        {
            IsOpen = true;
        }

        FocusFirstItem();
        Opened?.Invoke(this, EventArgs.Empty);
    }

    private void OnPopupClosed(object? sender, EventArgs e)
    {
        if (IsOpen)
        {
            IsOpen = false;
        }

        Closed?.Invoke(this, EventArgs.Empty);
    }

    private void OnItemsSourceChanged(IEnumerable<MenuItemModel>? newValue)
    {
        if (_observedSource is not null)
        {
            _observedSource.CollectionChanged -= OnSourceCollectionChanged;
            _observedSource = null;
        }

        if (newValue is INotifyCollectionChanged observable)
        {
            observable.CollectionChanged += OnSourceCollectionChanged;
            _observedSource = observable;
        }

        RebuildItems();
    }

    private void OnSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => RebuildItems();

    private void RebuildItems()
    {
        foreach (var (command, handler) in _commandSubscriptions)
        {
            command.CanExecuteChanged -= handler;
        }

        _commandSubscriptions.Clear();
        itemsHost.Children.Clear();

        if (ItemsSource is null)
        {
            return;
        }

        var items = ItemsSource.ToList();

        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];
            var button = new SfButton
            {
                Background = Brush.Transparent,
                Text = item.Text,
                ImageSource = item.Icon,
                HorizontalOptions = LayoutOptions.Fill,
                HeightRequest = 44,
                Padding = new Thickness(12, 0),
                CornerRadius = MenuItemShape.For(index, items.Count),
                Content = BuildItemContent(!string.IsNullOrEmpty(item.Icon)),
            };
            ApplyEnabled(button, item);

            button.Clicked += (_, _) => MenuItemSelection.Select(item, () => IsOpen, Close, CloseOnItemSelected);

            if (item.Command is { } command)
            {
                EventHandler handler = (_, _) => ApplyEnabled(button, item);
                command.CanExecuteChanged += handler;
                _commandSubscriptions.Add((command, handler));
            }

            itemsHost.Children.Add(button);
        }
    }

    // SfButton cannot tint its own ImageSource, so the content is a template (Image tinted through IconTint plus a Label) that
    // reads ImageSource and Text from the ancestor SfButton, as the SfFlyoutMenuButton style does.
    private static DataTemplate BuildItemContent(bool hasIcon) => new(() =>
    {
        var ancestor = new RelativeBindingSource(RelativeBindingSourceMode.FindAncestor, typeof(SfButton));

        var icon = new Image { WidthRequest = 20, HeightRequest = 20, VerticalOptions = LayoutOptions.Center };
        icon.SetBinding(Image.SourceProperty, new Binding(nameof(SfButton.ImageSource), source: ancestor));
        IconTint.SetColor(icon, ResolveColor("OnSurfaceVariant"));

        var label = new Label { VerticalOptions = LayoutOptions.Center, FontFamily = "RobotoMedium", FontSize = 14, TextColor = ResolveColor("OnSurface") };
        label.SetBinding(Label.TextProperty, new Binding(nameof(SfButton.Text), source: ancestor));

        var row = new HorizontalStackLayout { Spacing = 12, VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.Start };
        if (hasIcon)
        {
            row.Children.Add(icon);
        }

        row.Children.Add(label);
        return row;
    });

    private static Color ResolveColor(string key) =>
        Application.Current?.Resources.TryGetValue(key, out var value) == true && value is Color color ? color : Colors.Transparent;

    // Disabled items dim to 0.38 like the SfFlyoutMenuButton Disabled state (the custom content ignores IsEnabled on its own).
    private static void ApplyEnabled(SfButton button, MenuItemModel item)
    {
        var enabled = CanRun(item);
        button.IsEnabled = enabled;
        button.Opacity = enabled ? 1 : 0.38;
    }

    private static bool CanRun(MenuItemModel item) => item.Command is null || item.Command.CanExecute(item.CommandParameter);

    private void FocusFirstItem()
    {
        if (itemsHost.Children.Count > 0 && itemsHost.Children[0] is View first)
        {
            SemanticExtensions.SetSemanticFocus(first);
        }
    }
}
