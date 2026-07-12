namespace MyVocaList.UI.Components.AutocompleteField;

public partial class AutocompleteMobileField : ContentPage
{
    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(AutocompleteMobileField), string.Empty,
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly BindableProperty PlaceholderProperty =
        BindableProperty.Create(nameof(Placeholder), typeof(string), typeof(AutocompleteMobileField), string.Empty);

    public static readonly BindableProperty SuggestionsProperty =
        BindableProperty.Create(nameof(Suggestions), typeof(IEnumerable<AutocompleteSuggestion>),
            typeof(AutocompleteMobileField), null);

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public string Placeholder
    {
        get => (string)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    public IEnumerable<AutocompleteSuggestion> Suggestions
    {
        get => (IEnumerable<AutocompleteSuggestion>)GetValue(SuggestionsProperty);
        set => SetValue(SuggestionsProperty, value);
    }

    /// <summary>Raised when the user taps a suggestion row in the Search View.</summary>
    public event EventHandler<AutocompleteSuggestion> SuggestionTapped;

    /// <summary>Raised when the user backs out (button or hardware back) without selecting a suggestion.</summary>
    public event EventHandler Cancelled;

    public AutocompleteMobileField()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        searchEdit.Focus();
    }

    private void OnBackButtonClicked(object sender, EventArgs e)
    {
        Cancelled?.Invoke(this, EventArgs.Empty);
    }

    protected override bool OnBackButtonPressed()
    {
        Cancelled?.Invoke(this, EventArgs.Empty);
        return true;
    }

    private void OnSuggestionTapped(object sender, CollectionViewGestureEventArgs e)
    {
        if (e.Item is not AutocompleteSuggestion suggestion) return;
        SuggestionTapped?.Invoke(this, suggestion);
    }
}
