namespace MyVocaList.UI.Components.AutocompleteField;

public partial class AutocompleteMobileField : ContentPage
{
    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(AutocompleteMobileField), string.Empty,
            defaultBindingMode: BindingMode.TwoWay,
            propertyChanged: (b, _, n) =>
            {
                var ctrl = (AutocompleteMobileField)b;
                var newVal = (string)n ?? "";
                // Programmatically update searchEdit with a feedback-loop guard so the TextChanged handler
                // doesn't re-trigger the same update (common pattern for two-way bindings).
                try
                {
                    ctrl._isUpdatingTextFromSearchEdit = true;
                    if (ctrl.searchEdit.Text != newVal)
                        ctrl.searchEdit.Text = newVal;
                }
                finally
                {
                    ctrl._isUpdatingTextFromSearchEdit = false;
                }
            });

    public static readonly BindableProperty PlaceholderProperty =
        BindableProperty.Create(nameof(Placeholder), typeof(string), typeof(AutocompleteMobileField), string.Empty);

    public static readonly BindableProperty SuggestionsProperty =
        BindableProperty.Create(nameof(Suggestions), typeof(IEnumerable<AutocompleteSuggestion>),
            typeof(AutocompleteMobileField), null,
            propertyChanged: (b, _, n) => ((AutocompleteMobileField)b).OnSuggestionsChanged((IEnumerable<AutocompleteSuggestion>)n));

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

    private bool _isUpdatingTextFromSearchEdit;

    public AutocompleteMobileField()
    {
        InitializeComponent();
        // Wire up trim-safe, event-driven synchronization instead of reflection-based SetBinding().
        // This ensures the component works correctly with linker trimming in Release builds (BUG-043).
        searchEdit.TextChanged += OnSearchEditTextChanged;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Focusing synchronously during the modal push animation is unreliable on Android — the
        // input loses focus and the keyboard never raises, forcing the user to re-tap (BUG-040).
        // Defer the focus until after the presentation animation settles (AC-3).
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(250), () => searchEdit.Focus());
    }

    /// <summary>
    /// Propagates searchEdit.TextChanged events into the Text property.
    /// Implements the feedback-loop guard to avoid redundant updates when Text is set programmatically.
    /// </summary>
    private void OnSearchEditTextChanged(object sender, EventArgs e)
    {
        // Only update Text if the change originated from user typing (not from programmatic Text update).
        // This mirrors the desktop pattern in AutocompleteField.xaml.cs.
        if (!_isUpdatingTextFromSearchEdit)
        {
            Text = searchEdit.Text ?? "";
        }
    }

    /// <summary>
    /// Updates the DXCollectionView.ItemsSource when the Suggestions property changes.
    /// Replaces the reflection-based SetBinding() call that was vulnerable to linker trimming (BUG-043).
    /// </summary>
    private void OnSuggestionsChanged(IEnumerable<AutocompleteSuggestion> suggestions)
    {
        suggestionsView.ItemsSource = suggestions;
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
