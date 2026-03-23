namespace MyVocaList.UI.Components.Lists;

public partial class ListItem : ContentView
{
    public static readonly BindableProperty HeadlineProperty =
        BindableProperty.Create(nameof(Headline), typeof(string), typeof(ListItem), "",
            propertyChanged: (b, _, n) => ((ListItem)b).headlineLabel.Text = (string)n);

    public static readonly BindableProperty OverlineProperty =
        BindableProperty.Create(nameof(Overline), typeof(string), typeof(ListItem), "",
            propertyChanged: (b, _, n) =>
            {
                var item = (ListItem)b;
                item.overlineLabel.Text = (string)n;
                item.overlineLabel.IsVisible = !string.IsNullOrEmpty((string)n);
            });

    public static readonly BindableProperty SupportingTextProperty =
        BindableProperty.Create(nameof(SupportingText), typeof(string), typeof(ListItem), "",
            propertyChanged: (b, _, n) =>
            {
                var item = (ListItem)b;
                item.supportingLabel.Text = (string)n;
                item.supportingLabel.IsVisible = !string.IsNullOrEmpty((string)n);
                item.supportingLabel.MaxLines = item.SupportingMaxLines;
            });

    public static readonly BindableProperty SupportingMaxLinesProperty =
        BindableProperty.Create(nameof(SupportingMaxLines), typeof(int), typeof(ListItem), 1,
            propertyChanged: (b, _, n) =>
            {
                var item = (ListItem)b;
                item.supportingLabel.MaxLines = (int)n;
                item.UpdateSlotAlignment();
            });

    public static readonly BindableProperty LeadingContentProperty =
        BindableProperty.Create(nameof(LeadingContent), typeof(View), typeof(ListItem), null,
            propertyChanged: (b, _, n) =>
            {
                var item = (ListItem)b;
                item.leadingSlot.Content = n as View;
                item.leadingSlot.IsVisible = n != null;
            });

    public static readonly BindableProperty TrailingContentProperty =
        BindableProperty.Create(nameof(TrailingContent), typeof(View), typeof(ListItem), null,
            propertyChanged: (b, _, n) =>
            {
                var item = (ListItem)b;
                item.trailingSlot.Content = n as View;
                item.trailingSlot.IsVisible = n != null;
            });

    public static readonly BindableProperty IsSelectedProperty =
        BindableProperty.Create(nameof(IsSelected), typeof(bool), typeof(ListItem), false,
            propertyChanged: (b, _, _) => ((ListItem)b).UpdateContainerColor());

    public static readonly BindableProperty IsInteractiveProperty =
        BindableProperty.Create(nameof(IsInteractive), typeof(bool), typeof(ListItem), true,
            propertyChanged: (b, _, _) => ((ListItem)b).UpdateInteractivity());

    public string Headline
    {
        get => (string)GetValue(HeadlineProperty);
        set => SetValue(HeadlineProperty, value);
    }

    public string Overline
    {
        get => (string)GetValue(OverlineProperty);
        set => SetValue(OverlineProperty, value);
    }

    public string SupportingText
    {
        get => (string)GetValue(SupportingTextProperty);
        set => SetValue(SupportingTextProperty, value);
    }

    public int SupportingMaxLines
    {
        get => (int)GetValue(SupportingMaxLinesProperty);
        set => SetValue(SupportingMaxLinesProperty, value);
    }

    public View LeadingContent
    {
        get => (View)GetValue(LeadingContentProperty);
        set => SetValue(LeadingContentProperty, value);
    }

    public View TrailingContent
    {
        get => (View)GetValue(TrailingContentProperty);
        set => SetValue(TrailingContentProperty, value);
    }

    public bool IsSelected
    {
        get => (bool)GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    public bool IsInteractive
    {
        get => (bool)GetValue(IsInteractiveProperty);
        set => SetValue(IsInteractiveProperty, value);
    }

    public ListItem()
    {
        InitializeComponent();
        UpdateContainerColor();
        UpdateInteractivity();
    }

    private void UpdateSlotAlignment()
    {
        bool top = SupportingMaxLines >= 2;
        leadingSlot.VerticalOptions  = top ? LayoutOptions.Start : LayoutOptions.Center;
        leadingSlot.Margin           = top ? new Thickness(0, 8, 16, 0) : new Thickness(0, 0, 16, 0);
        textColumn.VerticalOptions   = top ? LayoutOptions.Start : LayoutOptions.Center;
        textColumn.Margin            = top ? new Thickness(0, 8, 0, 0) : Thickness.Zero;
        trailingSlot.VerticalOptions = top ? LayoutOptions.Start : LayoutOptions.Center;
        trailingSlot.Margin          = top ? new Thickness(8, 8, 0, 0) : new Thickness(8, 0, 0, 0);
    }

    private void UpdateContainerColor()
    {
        var key = IsSelected ? "SecondaryContainer" : "Surface";
        if (Application.Current?.Resources.TryGetValue(key, out var c) == true)
            container.BackgroundColor = (Color)c;
    }

    private void UpdateInteractivity() => InputTransparent = !IsInteractive;
}
