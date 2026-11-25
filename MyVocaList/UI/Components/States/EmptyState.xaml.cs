namespace MyVocaList.UI.Components.States;

/// <summary>
/// MD3 Empty state component. Shows an illustration icon, a headline, and optional supporting text.
/// Set <see cref="Illustration"/>, <see cref="Headline"/>, and optionally <see cref="SupportingText"/>.
/// Control visibility via <c>IsVisible</c> on this component.
/// </summary>
public partial class EmptyState : ContentView
{
    public static readonly BindableProperty IllustrationProperty =
        BindableProperty.Create(nameof(Illustration), typeof(string), typeof(EmptyState), string.Empty);

    public static readonly BindableProperty HeadlineProperty =
        BindableProperty.Create(nameof(Headline), typeof(string), typeof(EmptyState), string.Empty);

    public static readonly BindableProperty SupportingTextProperty =
        BindableProperty.Create(nameof(SupportingText), typeof(string), typeof(EmptyState), string.Empty,
            propertyChanged: (b, _, n) =>
            {
                var c = (EmptyState)b;
                c.supportingLabel.IsVisible = !string.IsNullOrEmpty((string)n);
            });

    /// <summary>Icon name for the illustration slot (e.g. "nightlife_outlined").</summary>
    public string Illustration
    {
        get => (string)GetValue(IllustrationProperty);
        set => SetValue(IllustrationProperty, value);
    }

    /// <summary>Primary text displayed below the illustration.</summary>
    public string Headline
    {
        get => (string)GetValue(HeadlineProperty);
        set => SetValue(HeadlineProperty, value);
    }

    /// <summary>Optional secondary text. Hidden when null or empty.</summary>
    public string SupportingText
    {
        get => (string)GetValue(SupportingTextProperty);
        set => SetValue(SupportingTextProperty, value);
    }

    public EmptyState()
    {
        InitializeComponent();
    }
}
