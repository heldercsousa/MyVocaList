namespace MyVocaList.UI.Components.Lists;

public partial class ListItemLeadingAvatarSf : ContentView
{
    public static readonly BindableProperty ImageSourceProperty = BindableProperty.Create(
        nameof(ImageSource),
        typeof(ImageSource),
        typeof(ListItemLeadingAvatarSf),
        defaultValue: null);

    public static readonly BindableProperty PlaceholderIconProperty = BindableProperty.Create(
        nameof(PlaceholderIcon),
        typeof(string),
        typeof(ListItemLeadingAvatarSf),
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

    public ListItemLeadingAvatarSf()
    {
        InitializeComponent();
    }
}
