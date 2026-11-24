using MyVocaList.Domain.Constants;
using SfSelectionChangingEventArgs = Syncfusion.Maui.Inputs.SelectionChangingEventArgs;

namespace MyVocaList.UI.Components.TextFields;

/// <summary>
/// The shared autocomplete field: an MD3 <em>exposed dropdown menu</em> (a text field plus a menu of
/// options, m3.material.io/components/menus) built on one Syncfusion <c>SfAutocomplete</c> inside an outlined
/// <c>SfTextInputLayout</c>. Syncfusion copy of <see cref="AutocompleteDXField"/>, with the same public API.
/// It owns all autocomplete glue (REQ-ACF-11): the options request, pick routing, pick-lock,
/// the X (clear) icon, blur forwarding, menu-open control, debounce and minimum query length.
/// A picked option is read-only until X is tapped (lock mechanism B, <c>findings.md § T1c</c>).
/// Every decision is delegated to <see cref="AutocompleteDXFieldLogic"/>; this class only maps Syncfusion
/// events to those rules and forwards to consumer commands. It validates nothing, creates nothing
/// and navigates nowhere (REQ-ACF-12).
/// </summary>
/// <remarks>
/// Syncfusion mapping (T3.3; facts from the <c>Syncfusion.Maui.Inputs</c> 35.2.12 IL):
/// <list type="bullet">
///   <item>The search runs in <see cref="AutocompleteFilterSf"/>, the control's filter, which the control calls on
///   each user keystroke only (never for a text written in code).</item>
///   <item>A pick is routed from <c>SelectionChanging</c> and cancelled there, so the control's
///   <c>SelectedItem</c> stays <c>null</c>: resetting it after a pick would empty the text (the control writes
///   the selected item's text, or an empty one, into the editor). The field writes the option's
///   <c>Headline</c> itself, as the DX original did through <c>DisplayMember</c>.</item>
///   <item><c>SfAutocomplete</c> has no read-only property; read-only and locked set its inner entry's
///   <c>IsReadOnly</c> and the control's <c>InputTransparent</c>, as the control does itself for a non-editable
///   combo box.</item>
/// </list>
/// </remarks>
public partial class AutocompleteFieldSf : ContentView
{
    // ── LabelText ──────────────────────────────────────────────────────────

    /// <summary>Identifies the <see cref="LabelText"/> bindable property.</summary>
    public static readonly BindableProperty LabelTextProperty = BindableProperty.Create(
        nameof(LabelText), typeof(string), typeof(AutocompleteFieldSf), string.Empty);

    /// <summary>The MD3 text-field label, passed to the layout's <c>Hint</c>.</summary>
    public string LabelText
    {
        get => (string)GetValue(LabelTextProperty);
        set => SetValue(LabelTextProperty, value);
    }

    // ── PlaceholderText ────────────────────────────────────────────────────

    /// <summary>Identifies the <see cref="PlaceholderText"/> bindable property.</summary>
    public static readonly BindableProperty PlaceholderTextProperty = BindableProperty.Create(
        nameof(PlaceholderText), typeof(string), typeof(AutocompleteFieldSf), string.Empty);

    /// <summary>The placeholder shown while the text is empty (REQ-ACF-06).</summary>
    public string PlaceholderText
    {
        get => (string)GetValue(PlaceholderTextProperty);
        set => SetValue(PlaceholderTextProperty, value);
    }

    // ── Text ───────────────────────────────────────────────────────────────

    /// <summary>Identifies the <see cref="Text"/> bindable property.</summary>
    public static readonly BindableProperty TextProperty = BindableProperty.Create(
        nameof(Text), typeof(string), typeof(AutocompleteFieldSf), string.Empty,
        BindingMode.TwoWay, propertyChanged: (b, _, _) => ((AutocompleteFieldSf)b).OnTextPropertyChanged());

    /// <summary>
    /// The editor text (TwoWay). A change re-applies the mode, so a hydration that sets
    /// <see cref="Text"/> after <see cref="IsLocked"/> still ends locked (REQ-ACF-02, -05a).
    /// </summary>
    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    // ── IsLocked ───────────────────────────────────────────────────────────

    /// <summary>Identifies the <see cref="IsLocked"/> bindable property.</summary>
    public static readonly BindableProperty IsLockedProperty = BindableProperty.Create(
        nameof(IsLocked), typeof(bool), typeof(AutocompleteFieldSf), false,
        BindingMode.TwoWay, propertyChanged: (b, _, _) => ((AutocompleteFieldSf)b).ApplyMode());

    /// <summary>
    /// The picked state (TwoWay). Set <c>true</c> by the component on a pick and <c>false</c> on X;
    /// a consumer may also set it (REQ-ACF-01, -02, -06). Honoured unless <see cref="IsPickLockEnabled"/>
    /// is <c>false</c>, which makes the field never lock (opt-out, REQ-ACF-29; ruling Q3 reversed 2026-11-05).
    /// </summary>
    public bool IsLocked
    {
        get => (bool)GetValue(IsLockedProperty);
        set => SetValue(IsLockedProperty, value);
    }

    // ── IsPickLockEnabled ──────────────────────────────────────────────────

    /// <summary>Identifies the <see cref="IsPickLockEnabled"/> bindable property.</summary>
    public static readonly BindableProperty IsPickLockEnabledProperty = BindableProperty.Create(
        nameof(IsPickLockEnabled), typeof(bool), typeof(AutocompleteFieldSf), true,
        propertyChanged: (b, _, _) => ((AutocompleteFieldSf)b).ApplyMode());

    /// <summary>
    /// Opt-out of pick-lock (REQ-ACF-29). <c>false</c> → a pick never locks the field and an
    /// <see cref="IsLocked"/> value is ignored; the pick is still routed to <see cref="SelectCommand"/>.
    /// Default <c>true</c>.
    /// </summary>
    public bool IsPickLockEnabled
    {
        get => (bool)GetValue(IsPickLockEnabledProperty);
        set => SetValue(IsPickLockEnabledProperty, value);
    }

    // ── IsReadOnly ─────────────────────────────────────────────────────────

    /// <summary>Identifies the <see cref="IsReadOnly"/> bindable property.</summary>
    public static readonly BindableProperty IsReadOnlyProperty = BindableProperty.Create(
        nameof(IsReadOnly), typeof(bool), typeof(AutocompleteFieldSf), false,
        propertyChanged: (b, _, _) => ((AutocompleteFieldSf)b).ApplyMode());

    /// <summary>
    /// Page-set read-only state: text shown, no typing, no X, no search, no menu, no blur forwarding.
    /// Wins over <see cref="IsLocked"/> and <see cref="IsSearchEnabled"/> (REQ-ACF-12b).
    /// </summary>
    public bool IsReadOnly
    {
        get => (bool)GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    // ── SupportingText ─────────────────────────────────────────────────────

    /// <summary>Identifies the <see cref="SupportingText"/> bindable property.</summary>
    public static readonly BindableProperty SupportingTextProperty = BindableProperty.Create(
        nameof(SupportingText), typeof(string), typeof(AutocompleteFieldSf), string.Empty,
        propertyChanged: (b, _, _) => ((AutocompleteFieldSf)b).ApplySupportingText());

    /// <summary>
    /// MD3 text-field supporting text under the field, in every state; replaced by
    /// <see cref="ErrorText"/> while <see cref="HasError"/> (REQ-ACF-12c).
    /// </summary>
    public string SupportingText
    {
        get => (string)GetValue(SupportingTextProperty);
        set => SetValue(SupportingTextProperty, value);
    }

    // ── IsSearchEnabled ────────────────────────────────────────────────────

    /// <summary>Identifies the <see cref="IsSearchEnabled"/> bindable property.</summary>
    public static readonly BindableProperty IsSearchEnabledProperty = BindableProperty.Create(
        nameof(IsSearchEnabled), typeof(bool), typeof(AutocompleteFieldSf), true);

    /// <summary>
    /// Page switch: <c>false</c> → no option requests and the menu stays closed; the rest of the
    /// field is unchanged (REQ-ACF-12a). Read on each options request.
    /// </summary>
    public bool IsSearchEnabled
    {
        get => (bool)GetValue(IsSearchEnabledProperty);
        set => SetValue(IsSearchEnabledProperty, value);
    }

    // ── HasError ───────────────────────────────────────────────────────────

    /// <summary>Identifies the <see cref="HasError"/> bindable property.</summary>
    public static readonly BindableProperty HasErrorProperty = BindableProperty.Create(
        nameof(HasError), typeof(bool), typeof(AutocompleteFieldSf), false,
        propertyChanged: (b, _, _) => ((AutocompleteFieldSf)b).ApplySupportingText());

    /// <summary>
    /// The error state, passed to the layout's <c>HasError</c>. Also switches the X colour to <c>Error</c>
    /// (REQ-ACF-08) and hides <see cref="SupportingText"/> (REQ-ACF-12c).
    /// </summary>
    public bool HasError
    {
        get => (bool)GetValue(HasErrorProperty);
        set => SetValue(HasErrorProperty, value);
    }

    // ── ErrorText ──────────────────────────────────────────────────────────

    /// <summary>Identifies the <see cref="ErrorText"/> bindable property.</summary>
    public static readonly BindableProperty ErrorTextProperty = BindableProperty.Create(
        nameof(ErrorText), typeof(string), typeof(AutocompleteFieldSf), string.Empty);

    /// <summary>The error message shown under the field while <see cref="HasError"/> (REQ-ACF-12c).</summary>
    public string ErrorText
    {
        get => (string)GetValue(ErrorTextProperty);
        set => SetValue(ErrorTextProperty, value);
    }

    // ── IsDropDownOpen ─────────────────────────────────────────────────────

    /// <summary>Identifies the <see cref="IsDropDownOpen"/> bindable property.</summary>
    public static readonly BindableProperty IsDropDownOpenProperty = BindableProperty.Create(
        nameof(IsDropDownOpen), typeof(bool), typeof(AutocompleteFieldSf), false, BindingMode.TwoWay);

    /// <summary>
    /// Whether the menu is open (TwoWay with the Syncfusion editor). Forced <c>false</c> unless the field is
    /// editable and search is on (REQ-ACF-04, -12a, -12b; BUG-065/066/069).
    /// </summary>
    public bool IsDropDownOpen
    {
        get => (bool)GetValue(IsDropDownOpenProperty);
        set => SetValue(IsDropDownOpenProperty, value);
    }

    // ── OptionSource ───────────────────────────────────────────────────────

    /// <summary>Identifies the <see cref="OptionSource"/> bindable property.</summary>
    public static readonly BindableProperty OptionSourceProperty = BindableProperty.Create(
        nameof(OptionSource), typeof(IMenuOptionSource), typeof(AutocompleteFieldSf));

    /// <summary>
    /// The search behind the menu. Its return value is exactly what the menu shows
    /// (REQ-ACF-16, -22; BUG-056). <c>null</c> → no requests.
    /// </summary>
    public IMenuOptionSource OptionSource
    {
        get => (IMenuOptionSource)GetValue(OptionSourceProperty);
        set => SetValue(OptionSourceProperty, value);
    }

    // ── SelectCommand ──────────────────────────────────────────────────────

    /// <summary>Identifies the <see cref="SelectCommand"/> bindable property.</summary>
    public static readonly BindableProperty SelectCommandProperty = BindableProperty.Create(
        nameof(SelectCommand), typeof(ICommand), typeof(AutocompleteFieldSf));

    /// <summary>
    /// Runs with the picked <see cref="AutocompleteSuggestion"/> after a pick has locked the field
    /// (REQ-ACF-01, -11b).
    /// </summary>
    public ICommand SelectCommand
    {
        get => (ICommand)GetValue(SelectCommandProperty);
        set => SetValue(SelectCommandProperty, value);
    }

    // ── ClearCommand ───────────────────────────────────────────────────────

    /// <summary>Identifies the <see cref="ClearCommand"/> bindable property.</summary>
    public static readonly BindableProperty ClearCommandProperty = BindableProperty.Create(
        nameof(ClearCommand), typeof(ICommand), typeof(AutocompleteFieldSf));

    /// <summary>Optional; runs after X has emptied (and, when locked, unlocked) the field (REQ-ACF-06, -09).</summary>
    public ICommand ClearCommand
    {
        get => (ICommand)GetValue(ClearCommandProperty);
        set => SetValue(ClearCommandProperty, value);
    }

    // ── BlurWithoutPickCommand ─────────────────────────────────────────────

    /// <summary>Identifies the <see cref="BlurWithoutPickCommand"/> bindable property.</summary>
    public static readonly BindableProperty BlurWithoutPickCommandProperty = BindableProperty.Create(
        nameof(BlurWithoutPickCommand), typeof(ICommand), typeof(AutocompleteFieldSf));

    /// <summary>Runs when the editable field loses focus with no routed pick (REQ-ACF-11c, -17).</summary>
    public ICommand BlurWithoutPickCommand
    {
        get => (ICommand)GetValue(BlurWithoutPickCommandProperty);
        set => SetValue(BlurWithoutPickCommandProperty, value);
    }

    // The control's filter and search behaviour; runs the search below for each user keystroke.
    private readonly AutocompleteFilterSf _filter;

    // Counts routed picks; SearchAsync compares it before/after its awaits (REQ-ACF-26).
    private int _pickCount;

    // REQ-ACF-30: one-shot flag set when a pick drops focus, so the blur it causes is not forwarded.
    // Cleared by that blur, by the next user keystroke, or by the next focus.
    private bool _pickBlurPending;

    // RC-B (REQ-ACF-25): the text the last routed pick left in the editor. A request that echoes it is not a
    // new search. Cleared by the first real edit (a keystroke whose text is not the echo).
    private string? _textAtPick;

    // The text of the last user keystroke (the control calls the filter for user input only). A Text change that
    // does not match it was written in code (REQ-ACF-24).
    private string? _lastUserText;

    // ── Constructor ────────────────────────────────────────────────────────

    /// <summary>Creates the field in its default, editable state.</summary>
    public AutocompleteFieldSf()
    {
        _filter = new AutocompleteFilterSf(SearchAsync);

        InitializeComponent();

        editor.FilterBehavior = _filter;
        editor.SearchBehavior = _filter;
        editor.SelectionChanging += OnSelectionChanging;
        editor.Focused += OnFocused;
        editor.Unfocused += OnUnfocused;
        editor.ClearButtonClicked += OnClearButtonClicked;

        // The inner entry may not exist until the control's handler is created: re-apply the mode then.
        editor.HandlerChanged += (_, _) => ApplyMode();

        ApplyMode();
        ApplySupportingText();
    }

    // ── Public API ─────────────────────────────────────────────────────────

    /// <summary>
    /// Focuses the inner editor; on Android this shows the soft keyboard when the editor is not
    /// read-only. Replaces a page's direct <c>Focus()</c> call (REQ-ACF-18).
    /// </summary>
    /// <returns>
    /// <c>true</c> if focus was requested on a ready editor; <c>false</c> if the editor has no handler yet, in which
    /// case focus is requested once it is ready (Syncfusion <c>Focus()</c> returns no result).
    /// </returns>
    public bool FocusEditor()
    {
        if (editor.Handler is null)
        {
            DeferFocusUntilReady();
            return false;
        }

        editor.Focus();
        return true;
    }

    // REQ-ACF-18: Focus() is a no-op until the inner editor has a handler (T7 line 1/10). Retry once,
    // when the editor is ready (HandlerChanged or Loaded, whichever fires first), then unsubscribe.
    private void DeferFocusUntilReady()
    {
        void Retry(object? sender, EventArgs e)
        {
            if (editor.Handler is null && sender is not VisualElement { IsLoaded: true })
                return; // HandlerChanged fired for a removal; wait for the next signal.

            editor.HandlerChanged -= Retry;
            editor.Loaded -= Retry;
            editor.Focus();
        }

        editor.HandlerChanged += Retry;
        editor.Loaded += Retry;
    }

    // ── Mode application (design.md § 4, § 5 mechanism B) ─────────────────

    private FieldMode CurrentMode => AutocompleteDXFieldLogic.Mode(IsReadOnly, IsLocked, Text, IsPickLockEnabled);

    // Called from the Text, IsLocked and IsReadOnly callbacks, so the result does not depend on the
    // order in which the binding engine assigns them (Review Focus 4).
    private void ApplyMode()
    {
        if (editor is null || layout is null)
            return;

        var mode = CurrentMode;
        switch (mode)
        {
            case FieldMode.ReadOnly:
                SetInputReadOnly(true);
                editor.IsClearButtonVisible = false;
                layout.ShowTrailingView = false;
                IsDropDownOpen = false;
                break;

            case FieldMode.Locked:
                // Mechanism B: true read-only text; the X is the trailing view.
                SetInputReadOnly(true);
                editor.IsClearButtonVisible = false;
                layout.ShowTrailingView = AutocompleteDXFieldLogic.ShowClearIcon(mode, Text);
                IsDropDownOpen = false;
                break;

            default: // FieldMode.Editable: the built-in clear button is the X (shown while focused and non-empty).
                SetInputReadOnly(false);
                layout.ShowTrailingView = false;
                editor.IsClearButtonVisible = true;
                break;
        }
    }

    // SfAutocomplete has no IsReadOnly: block taps on the control and make its inner entry read-only.
    private void SetInputReadOnly(bool isReadOnly)
    {
        editor.InputTransparent = isReadOnly;

        if (FindInnerEntry() is { } entry)
            entry.IsReadOnly = isReadOnly;
    }

    // The control's text entry (an Entry subclass inside its visual tree), or null before it is created.
    private Entry? FindInnerEntry() =>
        ((IVisualTreeElement)editor).GetVisualTreeDescendants().OfType<Entry>().FirstOrDefault();

    // Called from the HasError and SupportingText callbacks (Review Focus 5).
    private void ApplySupportingText()
    {
        if (layout is null)
            return;

        layout.HelperText = AutocompleteDXFieldLogic.BottomSupportingText(HasError, SupportingText);
        ApplyOutline();
    }

    // View-only state: MD3 outlined text field colors per state (outline, focused, error), as the DX original applied
    // BorderColor / FocusedBorderColor / its error color. The Community-suite layout does not read the app's
    // SfTextInputLayout* theme keys, so they are set here. Called from HasError, Focused and Unfocused.
    private void ApplyOutline()
    {
        if (layout is null || editor is null)
            return;

        var isActive = HasError || editor.IsFocused;
        var hintKey = HasError ? ErrorColorKey : editor.IsFocused ? FocusedColorKey : HintColorKey;

        if (TryGetToken(isActive ? hintKey : OutlineColorKey) is { } stroke)
            layout.Stroke = stroke;

        if (TryGetToken(hintKey) is { } hint)
            layout.HintLabelStyle = new Syncfusion.Maui.Core.LabelStyle { TextColor = hint };
    }

    private static Color? TryGetToken(string key) =>
        Application.Current?.Resources.TryGetValue(key, out var value) == true ? value as Color : null;

    // MaterialColors.xaml token keys for ApplyOutline.
    private const string OutlineColorKey = "Outline";
    private const string HintColorKey = "OnSurfaceVariant";
    private const string FocusedColorKey = "Primary";
    private const string ErrorColorKey = "Error";

    // ── Programmatic text (REQ-ACF-24) ─────────────────────────────────────

    // Text written in code (e.g. the consumer's SelectCommand writing the picked name) must close the menu and
    // cancel a pending search. The control calls the filter only for user input, and it does so after the Text
    // binding has pushed the keystroke here, so the check runs once the current text change has been handled.
    private void OnTextPropertyChanged()
    {
        ApplyMode();

        if (editor is null)
            return;

        var text = Text;
        Dispatcher.Dispatch(() =>
        {
            if (Text != text || text == _lastUserText)
                return; // superseded by a newer change, or a user keystroke

            _filter.Cancel();
            IsDropDownOpen = false;
        });
    }

    // ── Own search (REQ-ACF-31 amended, T12): run by AutocompleteFilterSf on each user keystroke ──

    // Started by the control's text-changed handler, on the UI thread. Every await below captures the UI
    // synchronization context (no ConfigureAwait(false)), so the checks after each await run on the UI thread.
    // Returns the options to show, or null for none (the menu closes). REQ-ACF-04, -12a, -12b, -13, -14, -16,
    // -22, -25, -26, -30, -31.
    private async Task<IReadOnlyList<AutocompleteSuggestion>?> SearchAsync(string? text, CancellationToken token)
    {
        _lastUserText = text;

        // RC-B: a keystroke whose text is not the pick echo is a real edit: it ends the pick text and the
        // pick-blur flag (REQ-ACF-25, REQ-ACF-30).
        if (!AutocompleteDXFieldLogic.IsPickEcho(text, _textAtPick))
        {
            _textAtPick = null;
            _pickBlurPending = false;
        }

        // A rejected query (pick echo, below threshold, no source, search off, locked or read-only) closes the menu
        // at once. A qualifying query leaves an open menu as it is until its options arrive (no blink while typing).
        var query = AutocompleteDXFieldLogic.SearchQuery(text);
        if (AutocompleteDXFieldLogic.IsPickEcho(query, _textAtPick)
            || !AutocompleteDXFieldLogic.ShouldRequestOptions(CurrentMode, IsSearchEnabled, OptionSource, query))
        {
            return null;
        }

        var pickCountAtStart = _pickCount;

        await Task.Delay(SearchConstants.AutocompleteDebounceMilliseconds, token);

        var source = OptionSource;
        if (source is null)
            return null;

        var options = await source.SearchAsync(query, token);
        token.ThrowIfCancellationRequested();

        // REQ-ACF-26: a pick routed while this search ran must not reopen the menu (the pick already closed it).
        if (pickCountAtStart != _pickCount)
            return null;

        // Review Focus 1: the field may have locked (or search turned off) while the search ran.
        if (!AutocompleteDXFieldLogic.ShouldRequestOptions(CurrentMode, IsSearchEnabled, source, query))
            return null;

        return AutocompleteDXFieldLogic.ShowsMenu(options) ? options : null;
    }

    // ── Pick (REQ-ACF-01, -11b, -11e, -23, -30; BUG-061) ───────────────────

    // Raised by the control when a menu row is tapped, before it changes SelectedItem. The selection is cancelled,
    // so SelectedItem stays null and the same option can be picked again (BUG-061, REQ-ACF-11e).
    private void OnSelectionChanging(object? sender, SfSelectionChangingEventArgs e)
    {
        var option = e.CurrentSelection is { Count: > 0 } selection ? selection[0] : null;
        var route = AutocompleteDXFieldLogic.RoutePick(option);
        if (route == PickRoute.None)
            return; // not a pick (e.g. the control clearing a selection): let it proceed.

        e.Cancel = true;

        _pickCount++; // REQ-ACF-26: invalidates in-flight option requests.
        _filter.Cancel(); // T12: a pending search must not open the menu after the pick.

        // The text the DX original wrote through DisplayMember before raising the pick. Recorded first, so the
        // programmatic-text check and any echo request treat it as the pick's own text.
        var suggestion = (AutocompleteSuggestion)option!;
        _textAtPick = suggestion.Headline;
        Text = suggestion.Headline;

        if (AutocompleteDXFieldLogic.LockAfterPick(route, IsPickLockEnabled))
            IsLocked = true;

        var command = SelectCommand;
        if (command?.CanExecute(option) == true)
            command.Execute(option);

        // RC-B: the text the pick leaves in the editor; the echo request for it is rejected.
        _textAtPick = Text;

        IsDropDownOpen = false;

        // REQ-ACF-30 (b): a pick ends the input. Cursor to the end, then drop focus so the keyboard closes.
        // The blur this causes is flagged, so OnUnfocused does not forward it as a blur without pick.
        _pickBlurPending = true;
        editor.CursorPosition = Text?.Length ?? 0;
        editor.Unfocus();

        // RC-C (REQ-ACF-30b): Unfocus() does not hide the soft keyboard on Android; hide it explicitly.
        _ = HideSoftKeyboardAsync();
    }

    // RC-C (REQ-ACF-30b): hide the IME after a pick. MAUI 10 SoftInputExtensions.HideSoftInputAsync on the inner
    // entry (an ITextInput); otherwise the Android InputMethodManager on the handler's platform view.
    private async Task HideSoftKeyboardAsync()
    {
        if (FindInnerEntry() is Microsoft.Maui.ITextInput textInput)
        {
            await textInput.HideSoftInputAsync(CancellationToken.None);
            return;
        }

#if ANDROID
        if (editor.Handler?.PlatformView is Android.Views.View view
            && view.Context?.GetSystemService(Android.Content.Context.InputMethodService)
                is Android.Views.InputMethods.InputMethodManager inputMethodManager)
        {
            inputMethodManager.HideSoftInputFromWindow(view.WindowToken, Android.Views.InputMethods.HideSoftInputFlags.None);
        }
#endif
    }

    // ── Blur and focus (REQ-ACF-30) ────────────────────────────────────────

    // REQ-ACF-30 (c): the pick-caused blur is not a blur without pick; any other blur is forwarded.
    private void OnUnfocused(object? sender, FocusEventArgs e)
    {
        ApplyOutline();

        // T12: a search still pending when the user leaves the field must not open the menu afterwards.
        _filter.Cancel();

        var isPickBlur = _pickBlurPending;
        _pickBlurPending = false;

        if (!AutocompleteDXFieldLogic.ShouldForwardBlur(editor.SelectedItem, CurrentMode, isPickBlur))
            return;

        var command = BlurWithoutPickCommand;
        if (command?.CanExecute(null) == true)
            command.Execute(null);
    }

    // REQ-ACF-30: a new focus means the user is back in the field, so a pending pick-blur is stale.
    private void OnFocused(object? sender, FocusEventArgs e)
    {
        _pickBlurPending = false;
        ApplyOutline();
    }

    // ── X (clear) ──────────────────────────────────────────────────────────

    // Mechanism B: the trailing X while locked. REQ-ACF-05, -06; Review Focus 3.
    private void OnLockedClearClicked(object? sender, EventArgs e)
    {
        if (CurrentMode != FieldMode.Locked)
            return;

        // Order matters: the editor must be writable before FocusEditor(), or the keyboard does not show.
        Text = string.Empty;
        IsLocked = false;
        ApplyMode();
        ExecuteClearCommand();
        FocusEditor();
    }

    // The built-in clear button is the X while editable. Syncfusion raises the event AFTER it has emptied the text
    // and focused the editor, so ClearCommand runs directly (the DX original needed a dispatch). REQ-ACF-09.
    private void OnClearButtonClicked(object? sender, EventArgs e)
    {
        if (CurrentMode != FieldMode.Editable)
            return;

        ExecuteClearCommand();
    }

    private void ExecuteClearCommand()
    {
        var command = ClearCommand;
        if (command?.CanExecute(null) == true)
            command.Execute(null);
    }
}
