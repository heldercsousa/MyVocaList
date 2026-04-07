namespace MyVocaList.UI.Components.Lists;

public partial class ListItemLeadingMonogram : ContentView
{
    public static readonly BindableProperty InitialsProperty = BindableProperty.Create(
        nameof(Initials),
        typeof(string),
        typeof(ListItemLeadingMonogram),
        defaultValue: string.Empty);

    public static readonly BindableProperty MonogramColorProperty = BindableProperty.Create(
        nameof(MonogramColor),
        typeof(Color),
        typeof(ListItemLeadingMonogram),
        defaultValue: Colors.Transparent);

    public static readonly BindableProperty InitialsColorProperty = BindableProperty.Create(
        nameof(InitialsColor),
        typeof(Color),
        typeof(ListItemLeadingMonogram),
        defaultValue: Colors.Transparent);

    public string Initials
    {
        get => (string)GetValue(InitialsProperty);
        set => SetValue(InitialsProperty, value);
    }

    public Color MonogramColor
    {
        get => (Color)GetValue(MonogramColorProperty);
        set => SetValue(MonogramColorProperty, value);
    }

    public Color InitialsColor
    {
        get => (Color)GetValue(InitialsColorProperty);
        set => SetValue(InitialsColorProperty, value);
    }

    public ListItemLeadingMonogram()
    {
        InitializeComponent();
    }
}
