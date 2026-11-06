using System.Collections;
using System.ComponentModel;
using DevExpress.Maui.Editors;

namespace MyVocaList.UI.Components.TextFields;

/// <summary>
/// The shared autocomplete field: an MD3 <em>exposed dropdown menu</em> (a text field plus a menu of
/// options, m3.material.io/components/menus) built on one DevExpress <c>AutoCompleteEdit</c>.
/// It owns all autocomplete glue (REQ-ACF-11): the options request, pick routing, pick-lock,
/// the X (clear) icon, blur forwarding, menu-open control, debounce and minimum query length.
/// A picked option is read-only until X is tapped (lock mechanism B, <c>findings.md § T1c</c>).
/// Every decision is delegated to <see cref="AutocompleteDXFieldLogic"/>; this class only maps DX
/// events to those rules and forwards to consumer commands. It validates nothing, creates nothing
/// and navigates nowhere (REQ-ACF-12).
/// </summary>
/// <remarks>
/// The name carries the <c>DX</c> suffix by Helder's ruling, an MD3-terminology exception
/// (<c>design.md § 2</c>): it distinguishes the component from the frozen
/// <c>UI/Components/AutocompleteField/</c> family (REQ-DXAC-11).
/// </remarks>
public partial class AutocompleteDXField : ContentView
{
    // ── LabelText ──────────────────────────────────────────────────────────

    /// <summary>Identifies the <see cref="LabelText"/> bindable property.</summary>
    public static readonly BindableProperty LabelTextProperty = BindableProperty.Create(
        nameof(LabelText), typeof(string), typeof(AutocompleteDXField), string.Empty);

    /// <summary>The MD3 text-field label, passed to the DX editor's <c>LabelText</c>.</summary>
    public string LabelText
    {
        get => (string)GetValue(LabelTextProperty);
        set => SetValue(LabelTextProperty, value);
    }

    // ── PlaceholderText ────────────────────────────────────────────────────

    /// <summary>Identifies the <see cref="PlaceholderText"/> bindable property.</summary>
    public static readonly BindableProperty PlaceholderTextProperty = BindableProperty.Create(
        nameof(PlaceholderText), typeof(string), typeof(AutocompleteDXField), string.Empty);

    /// <summary>The placeholder shown while the text is empty (REQ-ACF-06).</summary>
    public string PlaceholderText
    {
        get => (string)GetValue(PlaceholderTextProperty);
        set => SetValue(PlaceholderTextProperty, value);
    }

    // ── Text ───────────────────────────────────────────────────────────────

    /// <summary>Identifies the <see cref="Text"/> bindable property.</summary>
    public static readonly BindableProperty TextProperty = BindableProperty.Create(
        nameof(Text), typeof(string), typeof(AutocompleteDXField), string.Empty,
        BindingMode.TwoWay, propertyChanged: (b, _, _) => ((AutocompleteDXField)b).ApplyMode());

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
        nameof(IsLocked), typeof(bool), typeof(AutocompleteDXField), false,
        BindingMode.TwoWay, propertyChanged: (b, _, _) => ((AutocompleteDXField)b).ApplyMode());

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
        nameof(IsPickLockEnabled), typeof(bool), typeof(AutocompleteDXField), true,
        propertyChanged: (b, _, _) => ((AutocompleteDXField)b).ApplyMode());

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
        nameof(IsReadOnly), typeof(bool), typeof(AutocompleteDXField), false,
        propertyChanged: (b, _, _) => ((AutocompleteDXField)b).ApplyMode());

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
        nameof(SupportingText), typeof(string), typeof(AutocompleteDXField), string.Empty,
        propertyChanged: (b, _, _) => ((AutocompleteDXField)b).ApplySupportingText());

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
        nameof(IsSearchEnabled), typeof(bool), typeof(AutocompleteDXField), true);

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
        nameof(HasError), typeof(bool), typeof(AutocompleteDXField), false,
        propertyChanged: (b, _, _) => ((AutocompleteDXField)b).ApplySupportingText());

    /// <summary>
    /// The error state, passed to DX <c>HasError</c>. Also switches the X colour to <c>Error</c>
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
        nameof(ErrorText), typeof(string), typeof(AutocompleteDXField), string.Empty);

    /// <summary>The error message shown under the field while <see cref="HasError"/> (REQ-ACF-12c).</summary>
    public string ErrorText
    {
        get => (string)GetValue(ErrorTextProperty);
        set => SetValue(ErrorTextProperty, value);
    }

    // ── IsDropDownOpen ─────────────────────────────────────────────────────

    /// <summary>Identifies the <see cref="IsDropDownOpen"/> bindable property.</summary>
    public static readonly BindableProperty IsDropDownOpenProperty = BindableProperty.Create(
        nameof(IsDropDownOpen), typeof(bool), typeof(AutocompleteDXField), false, BindingMode.TwoWay);

    /// <summary>
    /// Whether the menu is open (TwoWay with the DX editor). Forced <c>false</c> unless the field is
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
        nameof(OptionSource), typeof(IMenuOptionSource), typeof(AutocompleteDXField));

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
        nameof(SelectCommand), typeof(ICommand), typeof(AutocompleteDXField));

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
        nameof(ClearCommand), typeof(ICommand), typeof(AutocompleteDXField));

    /// <summary>Optional; runs after X has emptied (and, when locked, unlocked) the field (REQ-ACF-06, -09).</summary>
    public ICommand ClearCommand
    {
        get => (ICommand)GetValue(ClearCommandProperty);
        set => SetValue(ClearCommandProperty, value);
    }

    // ── BlurWithoutPickCommand ─────────────────────────────────────────────

    /// <summary>Identifies the <see cref="BlurWithoutPickCommand"/> bindable property.</summary>
    public static readonly BindableProperty BlurWithoutPickCommandProperty = BindableProperty.Create(
        nameof(BlurWithoutPickCommand), typeof(ICommand), typeof(AutocompleteDXField));

    /// <summary>Runs when the editable field loses focus with no routed pick (REQ-ACF-11c, -17).</summary>
    public ICommand BlurWithoutPickCommand
    {
        get => (ICommand)GetValue(BlurWithoutPickCommandProperty);
        set => SetValue(BlurWithoutPickCommandProperty, value);
    }

    // Counts routed picks; OnItemsRequested compares it before/after its await (REQ-ACF-26).
    private int _pickCount;

    // REQ-ACF-30: one-shot flag set when a pick drops focus, so the blur it causes is not forwarded.
    // Cleared by that blur, by the next UserInput text change, or by the next focus.
    private bool _pickBlurPending;

    // ── Constructor ────────────────────────────────────────────────────────

    /// <summary>Creates the field in its default, editable state.</summary>
    public AutocompleteDXField()
    {
        InitializeComponent();
        ApplyMode();
        ApplySupportingText();
    }

    // ── Public API ─────────────────────────────────────────────────────────

    /// <summary>
    /// Focuses the inner editor; on Android this shows the soft keyboard when the editor is not
    /// read-only (DX <c>EditBase.Focus()</c>). Replaces a page's direct <c>Focus()</c> call (REQ-ACF-18).
    /// </summary>
    /// <returns><c>true</c> if the editor received focus.</returns>
    public bool FocusEditor()
    {
        var focused = editor.Focus();
        if (!focused || editor.Handler is null)
            DeferFocusUntilReady();

        return focused;
    }

    // REQ-ACF-18: Focus() is a no-op until the inner editor has a handler (T7 line 1/10). Retry once,
    // when the editor is ready (HandlerChanged or Loaded, whichever fires first), then unsubscribe.
    private void DeferFocusUntilReady()
    {
        void Retry(object sender, EventArgs e)
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
        if (editor is null)
            return;

        var mode = CurrentMode;
        switch (mode)
        {
            case FieldMode.ReadOnly:
                editor.IsReadOnly = true;
                editor.ClearIconVisibility = IconVisibility.Never;
                editor.IsEndIconVisible = false;
                IsDropDownOpen = false;
                break;

            case FieldMode.Locked:
                // Mechanism B: true read-only text; the X is the End icon (live under IsReadOnly).
                editor.IsReadOnly = true;
                editor.ClearIconVisibility = IconVisibility.Never;
                editor.IsEndIconVisible = AutocompleteDXFieldLogic.ShowClearIcon(mode, Text);
                IsDropDownOpen = false;
                editor.SelectedItem = null; // BUG-061, REQ-ACF-11e
                break;

            default: // FieldMode.Editable: the built-in clear icon is the X (shown only when non-empty).
                editor.IsReadOnly = false;
                editor.IsEndIconVisible = false;
                editor.ClearIconVisibility = IconVisibility.Auto;
                break;
        }
    }

    // Called from the HasError and SupportingText callbacks (Review Focus 5).
    private void ApplySupportingText()
    {
        if (editor is null)
            return;

        editor.HelpText = AutocompleteDXFieldLogic.BottomSupportingText(HasError, SupportingText);
    }

    // ── DX event adapters (REQ-ACF-11) ─────────────────────────────────────

    // DX invokes Request() when neither delegate is assigned (NRE inside its async-void runner),
    // so the "no request" branch hands it a delegate that returns null = no items
    // (decompiled-DLL finding 1, T4 checkpoint).
    private static readonly Func<Task<IEnumerable>> NoOptions = static () => Task.FromResult<IEnumerable>(null);

    // Raised on a System.Threading.Timer thread when RequestDelay > 0 (decompiled-DLL finding 3),
    // so UI-bound writes go through the Dispatcher. REQ-ACF-04, -12a, -12b, -13, -16, -22, -26.
    private void OnItemsRequested(object sender, ItemsRequestEventArgs e)
    {
        // REQ-ACF-13 (a)(b): the request's own text, normalised once; used for both gates and the search.
        var query = AutocompleteDXFieldLogic.SearchQuery(e.Text);
        var token = e.CancellationToken;
        var source = OptionSource;
        var pickCountAtStart = _pickCount;

        if (!AutocompleteDXFieldLogic.ShouldRequestOptions(CurrentMode, IsSearchEnabled, source, query))
        {
            e.RequestAsync = NoOptions;
            CloseMenuOnUiThread();
            return;
        }

        e.RequestAsync = async () =>
        {
            // DX awaits this delegate inside an async void with no try/catch (decompiled-DLL
            // finding 2), so cancellation is caught here: allowed try-catch pattern 1
            // (code-style-reference.md § Exception Handling). DX itself drops the result of a
            // cancelled request.
            try
            {
                var options = await source.SearchAsync(query, token);
                token.ThrowIfCancellationRequested();

                // REQ-ACF-26: a pick routed while this search ran must not reopen the menu.
                if (pickCountAtStart != _pickCount)
                    return Array.Empty<AutocompleteSuggestion>();

                // Review Focus 1: the field may have locked (or search turned off) while the search ran.
                if (!AutocompleteDXFieldLogic.ShouldRequestOptions(CurrentMode, IsSearchEnabled, source, query))
                {
                    CloseMenuOnUiThread();
                    return null;
                }

                return options; // exactly this query's options (BUG-056)
            }
            catch (OperationCanceledException)
            {
                // Cancellation requested (a newer query superseded this one) — silently return.
                return null;
            }
        };
    }

    // REQ-ACF-24: text written in code (e.g. the consumer's SelectCommand writing the picked name)
    // must close the menu; only ProgrammaticChange also resets SelectedItem. ItemSelected must not,
    // or the pick routed by OnSelectionChanged would be lost. Observed event order: unknown until the
    // T7 device re-run (see task-log T8).
    private void OnTextChanged(object sender, AutoCompleteEditTextChangedEventArgs e)
    {
        if (e.Reason == AutoCompleteEditTextChangeReason.UserInput)
        {
            _pickBlurPending = false; // REQ-ACF-30: the user is editing, so a later blur is a real one.

            // REQ-ACF-13c: DX's own clear path uses the raw length, so a raw-long, normalised-short text
            // (" a") never reaches it. Clear the list and close the menu at the keystroke, as DX does for
            // raw-short text, so a stale list cannot show. OnItemsRequested keeps the normalised gate too.
            if (!AutocompleteDXFieldLogic.ShouldRequestOptions(CurrentMode, IsSearchEnabled, OptionSource, editor.Text))
                ClearMenuOnKeystroke();

            return;
        }

        IsDropDownOpen = false;

        if (e.Reason == AutoCompleteEditTextChangeReason.ProgrammaticChange)
            editor.SelectedItem = null;
    }

    // Raised from the drop-down after DX has set SelectedItem (decompiled-DLL finding 5).
    // REQ-ACF-01, -11b, -11e, -23; BUG-061.
    private void OnSelectionChanged(object sender, EventArgs e)
    {
        // Capture first: locking runs ApplyMode, which nulls SelectedItem.
        var option = editor.SelectedItem;
        var route = AutocompleteDXFieldLogic.RoutePick(option);
        if (route == PickRoute.None)
            return; // Review Focus 2: the SelectedItem = null reset re-enters here as a no-op.

        _pickCount++; // REQ-ACF-26: invalidates in-flight option requests.

        if (AutocompleteDXFieldLogic.LockAfterPick(route, IsPickLockEnabled))
            IsLocked = true;

        var command = SelectCommand;
        if (command?.CanExecute(option) == true)
            command.Execute(option);

        editor.SelectedItem = null;
        IsDropDownOpen = false;

        // REQ-ACF-30 (b): a pick ends the input. Cursor to the end, then drop focus so the keyboard closes.
        // The blur this causes is flagged, so OnUnfocused does not forward it as a blur without pick.
        _pickBlurPending = true;
        editor.CursorPosition = editor.Text?.Length ?? 0;
        editor.Unfocus();
    }

    // REQ-ACF-30 (c): the pick-caused blur is not a blur without pick; any other blur is forwarded.
    private void OnUnfocused(object sender, FocusEventArgs e)
    {
        var isPickBlur = _pickBlurPending;
        _pickBlurPending = false;

        if (!AutocompleteDXFieldLogic.ShouldForwardBlur(editor.SelectedItem, CurrentMode, isPickBlur))
            return;

        var command = BlurWithoutPickCommand;
        if (command?.CanExecute(null) == true)
            command.Execute(null);
    }

    // Mechanism B: the End icon is the X while locked. REQ-ACF-05, -06; Review Focus 3.
    private void OnEndIconClicked(object sender, EventArgs e)
    {
        if (CurrentMode != FieldMode.Locked)
            return;

        // Order matters: the editor must be writable before FocusEditor(), because DX Focus()
        // shows the keyboard only when !IsReadOnly.
        Text = string.Empty;
        IsLocked = false;
        ApplyMode();
        ExecuteClearCommand();
        FocusEditor();
    }

    // The built-in clear icon is the X while editable; DX empties the text itself and keeps focus.
    // The event fires BEFORE DX clears the text (decompiled-DLL finding 4), so ClearCommand is
    // dispatched to run after the field is empty (design § 3.1). REQ-ACF-09.
    private void OnClearIconClicked(object sender, HandledEventArgs e)
    {
        if (CurrentMode != FieldMode.Editable)
            return;

        Dispatcher.Dispatch(ExecuteClearCommand);
    }

    private void ExecuteClearCommand()
    {
        var command = ClearCommand;
        if (command?.CanExecute(null) == true)
            command.Execute(null);
    }

    private void CloseMenuOnUiThread() => Dispatcher.Dispatch(() => IsDropDownOpen = false);

    // Same sequence DX uses for raw-short text (AsyncItemsSourceProvider): drop the list, stop the
    // loading state, close the menu. Runs on the UI thread (TextChanged is raised there). REQ-ACF-13c.
    private void ClearMenuOnKeystroke()
    {
        editor.ItemsSource = null;
        editor.IsLoadingInProgress = false;
        IsDropDownOpen = false;
    }

    // REQ-ACF-30: a new focus means the user is back in the field, so a pending pick-blur is stale.
    private void OnFocused(object sender, FocusEventArgs e) => _pickBlurPending = false;
}
