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

    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(AutocompleteField), "",
            defaultBindingMode: BindingMode.TwoWay,
            propertyChanged: (b, _, n) =>
            {
                var ctrl = (AutocompleteField)b;
                var newVal = (string)n ?? "";
                // Guard against feedback loop when OnTextChanged drives this property
                if (ctrl.searchEdit.Text != newVal)
                    ctrl.searchEdit.Text = newVal;
            });

    public static readonly BindableProperty SearchRequestedCommandProperty =
        BindableProperty.Create(nameof(SearchRequestedCommand), typeof(ICommand), typeof(AutocompleteField), null);

    public static readonly BindableProperty SuggestionSelectedCommandProperty =
        BindableProperty.Create(nameof(SuggestionSelectedCommand), typeof(ICommand), typeof(AutocompleteField), null);

    public static readonly BindableProperty BlurredWithoutSelectionCommandProperty =
        BindableProperty.Create(nameof(BlurredWithoutSelectionCommand), typeof(ICommand), typeof(AutocompleteField), null);

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

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
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

    /// <summary>
    /// Invoked when the user blurs the autocomplete field without having tapped a suggestion.
    /// Allows the ViewModel to clear the field or restore the previous valid selection.
    /// </summary>
    public ICommand BlurredWithoutSelectionCommand
    {
        get => (ICommand)GetValue(BlurredWithoutSelectionCommandProperty);
        set => SetValue(BlurredWithoutSelectionCommandProperty, value);
    }

    // ── Private state ─────────────────────────────────────────────────────

    private readonly AutocompleteDebouncer _debouncer = new();
    private readonly MobileFieldReopenGuard _reopenGuard = new();
    private bool _isTappingSuggestion;

    /// <summary>
    /// Resolved via the app's DI container by default (same singleton MauiProgram.cs registers
    /// for <c>IDeviceInfo</c>); settable so tests can inject a mock without a MAUI runtime.
    /// AutocompleteField has no constructor-injection path — it's instantiated by the compiled
    /// XAML of consumer pages — so this service-locator seam is the pragmatic equivalent.
    /// </summary>
    internal IDeviceInfo DeviceInfo { get; set; }

    internal bool IsCompactWindow => AutocompleteWindowClass.IsCompactWindow(DeviceInfo);

    // ── Constructor ───────────────────────────────────────────────────────

    public AutocompleteField()
    {
        InitializeComponent();
        DeviceInfo = IPlatformApplication.Current?.Services.GetService<IDeviceInfo>();
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
        Text = text;

        if (text.Length < 2)
        {
            _debouncer.Cancel();
            Suggestions = null;
            return;
        }

        _debouncer.Trigger(text, DebounceDelay, t => SearchRequestedCommand?.Execute(t));
    }

    // ── Focus / blur guard ────────────────────────────────────────────────

    private async void OnSearchEditFocused(object sender, FocusEventArgs e)
    {
        if (IsCompactWindow)
        {
            if (_reopenGuard.RequestShowOnFocus())
            {
                await ShowMobileFieldAsync();
            }
            else
            {
                // Suppressed: either the Search View is already open, or this is the one-shot
                // automatic refocus that follows a dismissal. Keep the field blurred so the
                // keyboard and Search View do not reappear (BUG-041/042).
                searchEdit.Unfocus();
            }
            return;
        }

        var list = Suggestions?.ToList();
        if (list?.Count > 0)
            overlayCard.IsVisible = true;
    }

    private async Task ShowMobileFieldAsync()
    {
        searchEdit.Unfocus();

        var mobileField = new AutocompleteMobileField
        {
            Placeholder = Placeholder
        };
        mobileField.SetBinding(AutocompleteMobileField.TextProperty,
            new Binding(nameof(Text), BindingMode.TwoWay, source: this));
        mobileField.SetBinding(AutocompleteMobileField.SuggestionsProperty,
            new Binding(nameof(Suggestions), source: this));

        mobileField.SuggestionTapped += OnMobileFieldSuggestionTapped;
        mobileField.Cancelled += OnMobileFieldCancelled;

        await Shell.Current.Navigation.PushModalAsync(mobileField);
    }

    private async void OnMobileFieldSuggestionTapped(object sender, AutocompleteSuggestion suggestion)
    {
        var mobileField = (AutocompleteMobileField)sender;
        mobileField.SuggestionTapped -= OnMobileFieldSuggestionTapped;
        mobileField.Cancelled -= OnMobileFieldCancelled;

        SuggestionSelectedCommand?.Execute(suggestion);
        _reopenGuard.NotifyDismissed();
        await Shell.Current.Navigation.PopModalAsync();
    }

    private async void OnMobileFieldCancelled(object sender, EventArgs e)
    {
        var mobileField = (AutocompleteMobileField)sender;
        mobileField.SuggestionTapped -= OnMobileFieldSuggestionTapped;
        mobileField.Cancelled -= OnMobileFieldCancelled;

        BlurredWithoutSelectionCommand?.Execute(null);
        _reopenGuard.NotifyDismissed();
        await Shell.Current.Navigation.PopModalAsync();
    }

    private async void OnSearchEditUnfocused(object sender, FocusEventArgs e)
    {
        await Task.Yield();
        if (!_isTappingSuggestion && !_reopenGuard.IsShowing)
        {
            overlayCard.IsVisible = false;
            BlurredWithoutSelectionCommand?.Execute(null);
        }
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
