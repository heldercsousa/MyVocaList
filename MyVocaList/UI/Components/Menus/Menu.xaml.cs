using System.Collections.Specialized;
using System.Windows.Input;
using Microsoft.Maui;

namespace MyVocaList.UI.Components.Menus;

/// <summary>
/// MD3 vertical menu (Menus - Common). Wraps one <c>DXPopup</c>. Item buttons are built from <see cref="ItemsSource"/>
/// with the <c>MenuItem</c> style, per-corner shape (REQ-OVFM-21) and an <c>IsEnabled</c> sync with
/// <c>Command.CanExecute</c> (REQ-OVFM-24). Selection follows <see cref="MenuItemSelection"/> (REQ-OVFM-19).
/// Has no dependency on any page or ViewModel type (REQ-OVFM-25).
/// </summary>
public partial class Menu : ContentView
{
    /// <summary>Items shown in the menu, in order. Rebuilt on every change (also observes <see cref="INotifyCollectionChanged"/>).</summary>
    public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(
        nameof(ItemsSource),
        typeof(IEnumerable<MenuItemModel>),
        typeof(Menu),
        null,
        propertyChanged: (bindable, oldValue, newValue) =>
            ((Menu)bindable).OnItemsSourceChanged((IEnumerable<MenuItemModel>?)newValue));

    /// <summary>Open state. TwoWay by default; mirrors the popup's live <c>IsOpen</c> in both directions.</summary>
    public static readonly BindableProperty IsOpenProperty = BindableProperty.Create(
        nameof(IsOpen),
        typeof(bool),
        typeof(Menu),
        false,
        defaultBindingMode: BindingMode.TwoWay,
        propertyChanged: (bindable, oldValue, newValue) => ((Menu)bindable).OnIsOpenChanged((bool)newValue));

    /// <summary>The view the popup is anchored to. Set by <see cref="Show"/> or by the host before opening.</summary>
    public static readonly BindableProperty PlacementTargetProperty = BindableProperty.Create(
        nameof(PlacementTarget),
        typeof(View),
        typeof(Menu),
        null,
        propertyChanged: (bindable, oldValue, newValue) =>
            ((Menu)bindable).popup.PlacementTarget = (View?)newValue);

    /// <summary>Whether selecting an item closes the menu after its command runs. Default true.</summary>
    public static readonly BindableProperty CloseOnItemSelectedProperty = BindableProperty.Create(
        nameof(CloseOnItemSelected),
        typeof(bool),
        typeof(Menu),
        true);

    private readonly List<(ICommand Command, EventHandler Handler)> _commandSubscriptions = [];
    private INotifyCollectionChanged? _observedSource;

#if ANDROID
    private IDisposable? _menuBackRoute;
#endif

    public Menu()
    {
        InitializeComponent();
        popup.Opened += OnPopupOpened;
        popup.Closed += OnPopupClosed;
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

    private void OnIsOpenChanged(bool isOpen)
    {
        if (popup.IsOpen != isOpen)
        {
            popup.IsOpen = isOpen;
        }
    }

    private void OnPopupOpened(object? sender, EventArgs e)
    {
        if (!IsOpen)
        {
            IsOpen = true;
        }

        AttachBackRoute();
        FocusFirstItem();
        Opened?.Invoke(this, EventArgs.Empty);
    }

    private void OnPopupClosed(object? sender, EventArgs e)
    {
        DetachBackRoute();

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
        var menuItemStyle = Application.Current?.Resources.TryGetValue("MenuItem", out var styleValue) == true ? styleValue as Style : null;

        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];
            var button = new DXButton
            {
                Style = menuItemStyle,
                Content = item.Text,
                Icon = item.Icon,
                CornerRadius = MenuItemShape.For(index, items.Count),
                IsEnabled = CanRun(item),
            };

            button.Clicked += (_, _) => MenuItemSelection.Select(item, () => IsOpen, Close, CloseOnItemSelected);

            if (item.Command is { } command)
            {
                EventHandler handler = (_, _) => button.IsEnabled = CanRun(item);
                command.CanExecuteChanged += handler;
                _commandSubscriptions.Add((command, handler));
            }

            itemsHost.Children.Add(button);
        }
    }

    private static bool CanRun(MenuItemModel item) => item.Command is null || item.Command.CanExecute(item.CommandParameter);

    private void FocusFirstItem()
    {
        if (itemsHost.Children.Count > 0 && itemsHost.Children[0] is View first)
        {
            SemanticExtensions.SetSemanticFocus(first);
        }
    }

    private void AttachBackRoute()
    {
#if ANDROID
        _menuBackRoute?.Dispose();
        var contentView = popup.Content?.Handler?.PlatformView as Android.Views.View;
        _menuBackRoute = MyVocaList.Platforms.PopupBackHandler.Attach(contentView, () => Close());
        if (_menuBackRoute is null)
            Serilog.Log.ForContext("SourceContext", nameof(Menu))
                .Warning("Menu back route not attached: popup content view or back dispatcher unavailable");
#endif
    }

    private void DetachBackRoute()
    {
#if ANDROID
        _menuBackRoute?.Dispose();
        _menuBackRoute = null;
#endif
    }
}
