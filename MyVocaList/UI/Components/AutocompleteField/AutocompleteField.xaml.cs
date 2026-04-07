namespace MyVocaList.UI.Components.AutocompleteField;

public partial class AutocompleteField : ContentView
{
    // ── BindableProperties ────────────────────────────────────────────────

    public static readonly BindableProperty LabelTextProperty =
        BindableProperty.Create(nameof(LabelText), typeof(string), typeof(AutocompleteField), "");

    public static readonly BindableProperty PlaceholderProperty =
        BindableProperty.Create(nameof(Placeholder), typeof(string), typeof(AutocompleteField), "");

    public static readonly BindableProperty HasErrorProperty =
        BindableProperty.Create(nameof(HasError), typeof(bool), typeof(AutocompleteField), false,
            propertyChanged: (b, _, n) => ((AutocompleteField)b).searchEdit.HasError = (bool)n);

    public static readonly BindableProperty ErrorTextProperty =
        BindableProperty.Create(nameof(ErrorText), typeof(string), typeof(AutocompleteField), "",
            propertyChanged: (b, _, n) => ((AutocompleteField)b).searchEdit.ErrorText = (string)n);

    public static readonly BindableProperty SuggestionsProperty =
        BindableProperty.Create(nameof(Suggestions), typeof(IEnumerable<AutocompleteSuggestion>),
            typeof(AutocompleteField), null,
            propertyChanged: (b, _, n) => ((AutocompleteField)b).OnSuggestionsChanged((IEnumerable<AutocompleteSuggestion>)n));

    public static readonly BindableProperty DebounceDelayProperty =
        BindableProperty.Create(nameof(DebounceDelay), typeof(int), typeof(AutocompleteField), 300);

    public static readonly BindableProperty SearchRequestedCommandProperty =
        BindableProperty.Create(nameof(SearchRequestedCommand), typeof(ICommand), typeof(AutocompleteField), null);

    public static readonly BindableProperty SuggestionSelectedCommandProperty =
        BindableProperty.Create(nameof(SuggestionSelectedCommand), typeof(ICommand), typeof(AutocompleteField), null);

    // ── Public properties ─────────────────────────────────────────────────

    public string LabelText
    {
        get => (string)GetValue(LabelTextProperty);
        set => SetValue(LabelTextProperty, value);
    }

    public string Placeholder
    {
        get => (string)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    public bool HasError
    {
        get => (bool)GetValue(HasErrorProperty);
        set => SetValue(HasErrorProperty, value);
    }

    public string ErrorText
    {
        get => (string)GetValue(ErrorTextProperty);
        set => SetValue(ErrorTextProperty, value);
    }

    public IEnumerable<AutocompleteSuggestion> Suggestions
    {
        get => (IEnumerable<AutocompleteSuggestion>)GetValue(SuggestionsProperty);
        set => SetValue(SuggestionsProperty, value);
    }

    public int DebounceDelay
    {
        get => (int)GetValue(DebounceDelayProperty);
        set => SetValue(DebounceDelayProperty, value);
    }

    public ICommand SearchRequestedCommand
    {
        get => (ICommand)GetValue(SearchRequestedCommandProperty);
        set => SetValue(SearchRequestedCommandProperty, value);
    }

    public ICommand SuggestionSelectedCommand
    {
        get => (ICommand)GetValue(SuggestionSelectedCommandProperty);
        set => SetValue(SuggestionSelectedCommandProperty, value);
    }

    // ── Private state ─────────────────────────────────────────────────────

    private readonly AutocompleteDebouncer _debouncer = new();
    private bool _isTappingSuggestion;

    // ── Constructor ───────────────────────────────────────────────────────

    public AutocompleteField()
    {
        InitializeComponent();
    }

    // ── Suggestions changed ───────────────────────────────────────────────

    private void OnSuggestionsChanged(IEnumerable<AutocompleteSuggestion> suggestions)
    {
        var list = suggestions?.ToList();
        suggestionsView.ItemsSource = list;
        overlayCard.IsVisible = list?.Count > 0;
    }

    // ── TextEdit events ───────────────────────────────────────────────────

    private void OnTextChanged(object sender, EventArgs e)
    {
        var text = searchEdit.Text ?? "";

        if (text.Length < 2)
        {
            Suggestions = null;
            return;
        }

        _debouncer.Trigger(text, DebounceDelay, t => SearchRequestedCommand?.Execute(t));
    }

    // ── Focus / blur guard ────────────────────────────────────────────────

    private void OnSearchEditFocused(object sender, FocusEventArgs e)
    {
        var list = Suggestions?.ToList();
        if (list?.Count > 0)
            overlayCard.IsVisible = true;
    }

    private async void OnSearchEditUnfocused(object sender, FocusEventArgs e)
    {
        await Task.Yield();
        if (!_isTappingSuggestion)
            overlayCard.IsVisible = false;
    }

    // ── Suggestion tap ────────────────────────────────────────────────────

    private void OnSuggestionTapped(object sender, CollectionViewGestureEventArgs e)
    {
        if (e.Item is not AutocompleteSuggestion suggestion) return;
        _isTappingSuggestion = true;
        overlayCard.IsVisible = false;
        SuggestionSelectedCommand?.Execute(suggestion);
        _isTappingSuggestion = false;
    }
}
