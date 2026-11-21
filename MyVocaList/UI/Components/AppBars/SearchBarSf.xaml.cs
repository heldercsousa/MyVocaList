namespace MyVocaList.UI.Components.AppBars;

/// <summary>
/// SPIKE (task/spike-syncfusion): Syncfusion copy of <see cref="SearchBar"/>. Not a replacement; the original is untouched.
/// </summary>
public partial class SearchBarSf : ContentView
{
    public static readonly BindableProperty SearchTextProperty =
        BindableProperty.Create(nameof(SearchText), typeof(string), typeof(SearchBarSf), string.Empty,
            BindingMode.TwoWay);

    public string SearchText
    {
        get => (string)GetValue(SearchTextProperty);
        set => SetValue(SearchTextProperty, value);
    }

    public static readonly BindableProperty PlaceholderProperty =
        BindableProperty.Create(nameof(Placeholder), typeof(string), typeof(SearchBarSf), "Search...");

    public string Placeholder
    {
        get => (string)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    /// <summary>Accepted for parity with <see cref="SearchBar"/>; not rendered by the Sf copy (spike gap).</summary>
    public static readonly BindableProperty IsElevatedProperty =
        BindableProperty.Create(nameof(IsElevated), typeof(bool), typeof(SearchBarSf), false);

    public bool IsElevated
    {
        get => (bool)GetValue(IsElevatedProperty);
        set => SetValue(IsElevatedProperty, value);
    }

    public SearchBarSf()
    {
        InitializeComponent();
    }
}
