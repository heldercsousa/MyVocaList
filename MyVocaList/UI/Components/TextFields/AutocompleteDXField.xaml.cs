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
    /// a consumer may also set it (REQ-ACF-01, -02, -06). Always honoured: no opt-out (ruling Q3).
    /// </summary>
    public bool IsLocked
    {
        get => (bool)GetValue(IsLockedProperty);
        set => SetValue(IsLockedProperty, value);
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
    public bool FocusEditor() => editor.Focus();

    // ── Mode application (design.md § 4, § 5 mechanism B) ─────────────────

    private FieldMode CurrentMode => AutocompleteDXFieldLogic.Mode(IsReadOnly, IsLocked, Text);

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
        var text = e.Text; // the request's own text, not editor.Text
        var token = e.CancellationToken;
        var source = OptionSource;

        if (!AutocompleteDXFieldLogic.ShouldRequestOptions(CurrentMode, IsSearchEnabled, source, text))
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
                var options = await source.SearchAsync(text, token);
                token.ThrowIfCancellationRequested();

                // Review Focus 1: the field may have locked (or search turned off) while the search ran.
                if (!AutocompleteDXFieldLogic.ShouldRequestOptions(CurrentMode, IsSearchEnabled, source, text))
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

    // Raised from the drop-down after DX has set SelectedItem (decompiled-DLL finding 5).
    // REQ-ACF-01, -11b, -11e, -23; BUG-061.
    private void OnSelectionChanged(object sender, EventArgs e)
    {
        // Capture first: locking runs ApplyMode, which nulls SelectedItem.
        var option = editor.SelectedItem;
        var route = AutocompleteDXFieldLogic.RoutePick(option);
        if (route == PickRoute.None)
            return; // Review Focus 2: the SelectedItem = null reset re-enters here as a no-op.

        IsLocked = AutocompleteDXFieldLogic.LockAfterPick(route);

        var command = SelectCommand;
        if (command?.CanExecute(option) == true)
            command.Execute(option);

        editor.SelectedItem = null;
        IsDropDownOpen = false;
    }

    // Blur forwarding only; the component never writes Text here (REQ-ACF-11c, -12b, -17, -19).
    private void OnUnfocused(object sender, FocusEventArgs e)
    {
        if (!AutocompleteDXFieldLogic.ShouldForwardBlur(editor.SelectedItem, CurrentMode))
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
}
