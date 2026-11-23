namespace MyVocaList.UI.Components.Lists;

/// <summary>
/// Trailing selection control for <see cref="ListItem"/> in CRUD list pages.
/// Renders a 48×48 transparent tap target around a non-interactive <c>SfCheckBox</c>;
/// the tap is routed to <c>CrudListPageBase</c>'s <c>ToggleItemSelectionCommand</c>.
/// </summary>
public partial class ListItemCheckboxSf : ContentView
{
    public static readonly BindableProperty IsCheckedProperty = BindableProperty.Create(
        nameof(IsChecked),
        typeof(bool),
        typeof(ListItemCheckboxSf),
        false);

    /// <summary>Checked state of the visual checkbox. Default false.</summary>
    public bool IsChecked
    {
        get => (bool)GetValue(IsCheckedProperty);
        set => SetValue(IsCheckedProperty, value);
    }

    public ListItemCheckboxSf()
    {
        InitializeComponent();
    }
}
