namespace MyVocaList.UI.Components.Lists;

public partial class ListItemLeadingAvatar : ContentView
{
    public static readonly BindableProperty InitialsProperty = BindableProperty.Create(
        nameof(Initials),
        typeof(string),
        typeof(ListItemLeadingAvatar),
        defaultValue: string.Empty);

    public static readonly BindableProperty AvatarColorProperty = BindableProperty.Create(
        nameof(AvatarColor),
        typeof(Color),
        typeof(ListItemLeadingAvatar),
        defaultValue: Colors.Transparent);

    public static readonly BindableProperty InitialsColorProperty = BindableProperty.Create(
        nameof(InitialsColor),
        typeof(Color),
        typeof(ListItemLeadingAvatar),
        defaultValue: Colors.Transparent);

    public string Initials
    {
        get => (string)GetValue(InitialsProperty);
        set => SetValue(InitialsProperty, value);
    }

    public Color AvatarColor
    {
        get => (Color)GetValue(AvatarColorProperty);
        set => SetValue(AvatarColorProperty, value);
    }

    public Color InitialsColor
    {
        get => (Color)GetValue(InitialsColorProperty);
        set => SetValue(InitialsColorProperty, value);
    }

    public ListItemLeadingAvatar()
    {
        InitializeComponent();
    }
}
