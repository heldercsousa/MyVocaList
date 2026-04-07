namespace MyVocaList.UI.Components.Lists;

public partial class ListItemLeadingIcon : ContentView
{
    public static readonly BindableProperty IconProperty = BindableProperty.Create(
        nameof(Icon),
        typeof(string),
        typeof(ListItemLeadingIcon),
        "");

    public static readonly BindableProperty IconColorProperty = BindableProperty.Create(
        nameof(IconColor),
        typeof(Color),
        typeof(ListItemLeadingIcon),
        Colors.Transparent);

    public string Icon
    {
        get => (string)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public Color IconColor
    {
        get => (Color)GetValue(IconColorProperty);
        set => SetValue(IconColorProperty, value);
    }

    public ListItemLeadingIcon()
    {
        InitializeComponent();
    }
}
