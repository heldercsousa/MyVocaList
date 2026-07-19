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
