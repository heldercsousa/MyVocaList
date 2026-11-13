namespace MyVocaList.UI.Components.Lists;

public partial class ListItemLeadingAvatar : ContentView
{
    public static readonly BindableProperty ImageSourceProperty = BindableProperty.Create(
        nameof(ImageSource),
        typeof(ImageSource),
        typeof(ListItemLeadingAvatar),
        defaultValue: null);

    public static readonly BindableProperty PlaceholderIconProperty = BindableProperty.Create(
        nameof(PlaceholderIcon),
        typeof(string),
        typeof(ListItemLeadingAvatar),
        defaultValue: "account_circle_outlined");

    public ImageSource ImageSource
    {
        get => (ImageSource)GetValue(ImageSourceProperty);
        set => SetValue(ImageSourceProperty, value);
    }

    public string PlaceholderIcon
    {
        get => (string)GetValue(PlaceholderIconProperty);
        set => SetValue(PlaceholderIconProperty, value);
    }

    public ListItemLeadingAvatar()
    {
        InitializeComponent();
    }
}
