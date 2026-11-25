namespace MyVocaList.UI.Components.Lists;

public partial class ListItemLeadingImage : ContentView
{
    public static readonly BindableProperty ImageSourceProperty = BindableProperty.Create(
        nameof(ImageSource),
        typeof(ImageSource),
        typeof(ListItemLeadingImage),
        defaultValue: null);

    public static readonly BindableProperty AspectProperty = BindableProperty.Create(
        nameof(Aspect),
        typeof(Aspect),
        typeof(ListItemLeadingImage),
        defaultValue: Aspect.AspectFill);

    /// <summary>Icon name shown under the image (grey container). Null: no icon and no container background.</summary>
    public static readonly BindableProperty PlaceholderIconProperty = BindableProperty.Create(
        nameof(PlaceholderIcon),
        typeof(string),
        typeof(ListItemLeadingImage),
        defaultValue: null);

    public string PlaceholderIcon
    {
        get => (string)GetValue(PlaceholderIconProperty);
        set => SetValue(PlaceholderIconProperty, value);
    }

    public ImageSource ImageSource
    {
        get => (ImageSource)GetValue(ImageSourceProperty);
        set => SetValue(ImageSourceProperty, value);
    }

    public Aspect Aspect
    {
        get => (Aspect)GetValue(AspectProperty);
        set => SetValue(AspectProperty, value);
    }

    public ListItemLeadingImage()
    {
        InitializeComponent();
    }
}
