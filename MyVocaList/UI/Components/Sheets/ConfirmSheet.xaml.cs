namespace MyVocaList.UI.Components.Sheets;

/// <summary>
/// MD3 modal bottom sheet for confirming a destructive action.
/// Bind <see cref="SheetState"/> TwoWay to a ViewModel property to open/close.
/// </summary>
public partial class ConfirmSheet : ContentView
{
    private bool _isSyncing;

    public static readonly BindableProperty SheetStateProperty =
        BindableProperty.Create(nameof(SheetState), typeof(BottomSheetState), typeof(ConfirmSheet),
            BottomSheetState.Hidden,
            propertyChanged: (b, _, n) => ((ConfirmSheet)b).OnSheetStateChanged((BottomSheetState)n));

    public static readonly BindableProperty MessageProperty =
        BindableProperty.Create(nameof(Message), typeof(string), typeof(ConfirmSheet), string.Empty,
            propertyChanged: (b, _, n) => ((ConfirmSheet)b).messageLabel.Text = (string)n);

    public static readonly BindableProperty ActionTextProperty =
        BindableProperty.Create(nameof(ActionText), typeof(string), typeof(ConfirmSheet), string.Empty,
            propertyChanged: (b, _, n) => ((ConfirmSheet)b).actionButton.Content = (string)n);

    public static readonly BindableProperty ActionCommandProperty =
        BindableProperty.Create(nameof(ActionCommand), typeof(ICommand), typeof(ConfirmSheet));

    public static readonly BindableProperty DismissCommandProperty =
        BindableProperty.Create(nameof(DismissCommand), typeof(ICommand), typeof(ConfirmSheet));

    public BottomSheetState SheetState
    {
        get => (BottomSheetState)GetValue(SheetStateProperty);
        set => SetValue(SheetStateProperty, value);
    }

    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public string ActionText
    {
        get => (string)GetValue(ActionTextProperty);
        set => SetValue(ActionTextProperty, value);
    }

    public ICommand ActionCommand
    {
        get => (ICommand)GetValue(ActionCommandProperty);
        set => SetValue(ActionCommandProperty, value);
    }

    public ICommand DismissCommand
    {
        get => (ICommand)GetValue(DismissCommandProperty);
        set => SetValue(DismissCommandProperty, value);
    }

    public ConfirmSheet()
    {
        InitializeComponent();
    }

    private void OnSheetStateChanged(BottomSheetState newState)
    {
        if (_isSyncing) return;

        var host = this.GetParentPage();
        if (host == null) return;

        if (newState == BottomSheetState.Hidden)
            bottomSheet.Close();
        else
            bottomSheet.Show(newState, host);
    }

    private void OnStateChanged(object sender, ValueChangedEventArgs<BottomSheetState> e)
    {
        // Sync sheet dismissal (user swipe) back to the ViewModel via TwoWay binding
        if (e.NewValue != SheetState)
        {
            _isSyncing = true;
            SheetState = e.NewValue;
            _isSyncing = false;
        }
    }
}

// Extension to traverse the visual tree for the containing Page
file static class VisualElementExtensions
{
    public static Page GetParentPage(this VisualElement element)
    {
        var parent = element.Parent;
        while (parent != null)
        {
            if (parent is Page page)
                return page;
            parent = parent.Parent;
        }
        return null;
    }
}
