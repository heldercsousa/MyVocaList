using System.ComponentModel;
using DevExpress.Maui.Editors;

namespace MyVocaList.UI.Components.TextFields;

/// <summary>
/// Tap-to-edit wrapper around any form editor (REQ-PIC-23..26, REQ-PIC-30; design.md § 2.5 of the
/// form-pictures change). A defined value shows as text (type style <see cref="TextStyleClass"/>, at most
/// <see cref="MaxLines"/> lines, a trailing edit icon); a tap opens the wrapped <see cref="Editor"/> with the
/// cursor in it. The editor state shows while the value is blank, while <see cref="IsEditing"/> or while
/// <see cref="HasError"/>. Leaving the editor (blur, or a pick of a wrapped <see cref="AutocompleteDXField"/>)
/// returns to the text state. Every state decision is delegated to <see cref="TapToEditFieldLogic"/>.
/// </summary>
/// <remarks>
/// The wrapper adds no member to <see cref="AutocompleteDXField"/> and does not change it (design.md § 2.5).
/// It reaches the wrapped field only through its public surface: <c>IsLocked</c>, <c>FocusEditor()</c> and
/// <c>PropertyChanged</c>.
/// </remarks>
[ContentProperty(nameof(Editor))]
public partial class TapToEditField : ContentView
{
    // ── Editor (content property) ──────────────────────────────────────────

    /// <summary>Identifies the <see cref="Editor"/> bindable property.</summary>
    public static readonly BindableProperty EditorProperty = BindableProperty.Create(
        nameof(Editor), typeof(View), typeof(TapToEditField), null,
        propertyChanged: (b, o, n) => ((TapToEditField)b).OnEditorChanged((View?)o, (View?)n));

    /// <summary>The wrapped editor, shown in the editor state (the XAML content of the wrapper).</summary>
    public View Editor
    {
        get => (View)GetValue(EditorProperty);
        set => SetValue(EditorProperty, value);
    }

    // ── Text ───────────────────────────────────────────────────────────────

    /// <summary>Identifies the <see cref="Text"/> bindable property.</summary>
    public static readonly BindableProperty TextProperty = BindableProperty.Create(
        nameof(Text), typeof(string), typeof(TapToEditField), string.Empty,
        propertyChanged: (b, _, _) => ((TapToEditField)b).ApplyState());

    /// <summary>The value shown in the text state; bind it to the same value as the editor.</summary>
    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    // ── TextStyleClass ─────────────────────────────────────────────────────

    /// <summary>The default type style of the text (MD3 body large).</summary>
    public const string DefaultTextStyleClass = "Body.Large";

    /// <summary>Identifies the <see cref="TextStyleClass"/> bindable property.</summary>
    public static readonly BindableProperty TextStyleClassProperty = BindableProperty.Create(
        nameof(TextStyleClass), typeof(string), typeof(TapToEditField), DefaultTextStyleClass,
        propertyChanged: (b, _, _) => ((TapToEditField)b).ApplyTextStyle());

    /// <summary>
    /// The type style class of the text (a <c>Label</c> class of <c>MaterialStyles.xaml</c>, e.g.
    /// <c>Headline.Medium</c> or <c>Title.Large</c>). Default <c>Body.Large</c>.
    /// </summary>
    public string TextStyleClass
    {
        get => (string)GetValue(TextStyleClassProperty);
        set => SetValue(TextStyleClassProperty, value);
    }

    // ── MaxLines ───────────────────────────────────────────────────────────

    /// <summary>The default number of lines before the ellipsis (REQ-PIC-23).</summary>
    public const int DefaultMaxLines = 3;

    /// <summary>Identifies the <see cref="MaxLines"/> bindable property.</summary>
    public static readonly BindableProperty MaxLinesProperty = BindableProperty.Create(
        nameof(MaxLines), typeof(int), typeof(TapToEditField), DefaultMaxLines);

    /// <summary>Lines of text before the ellipsis. Default 3.</summary>
    public int MaxLines
    {
        get => (int)GetValue(MaxLinesProperty);
        set => SetValue(MaxLinesProperty, value);
    }

    // ── IsEditIconVisible ──────────────────────────────────────────────────

    /// <summary>Identifies the <see cref="IsEditIconVisible"/> bindable property.</summary>
    public static readonly BindableProperty IsEditIconVisibleProperty = BindableProperty.Create(
        nameof(IsEditIconVisible), typeof(bool), typeof(TapToEditField), true);

    /// <summary>Whether the trailing <c>edit_outlined</c> icon marks the text as editable. Default <c>true</c>.</summary>
    public bool IsEditIconVisible
    {
        get => (bool)GetValue(IsEditIconVisibleProperty);
        set => SetValue(IsEditIconVisibleProperty, value);
    }

    // ── HasError ───────────────────────────────────────────────────────────

    /// <summary>Identifies the <see cref="HasError"/> bindable property.</summary>
    public static readonly BindableProperty HasErrorProperty = BindableProperty.Create(
        nameof(HasError), typeof(bool), typeof(TapToEditField), false,
        propertyChanged: (b, _, _) => ((TapToEditField)b).ApplyState());

    /// <summary>While <c>true</c> the editor stays visible, so its error shows (REQ-PIC-24).</summary>
    public bool HasError
    {
        get => (bool)GetValue(HasErrorProperty);
        set => SetValue(HasErrorProperty, value);
    }

    // ── IsEditing ──────────────────────────────────────────────────────────

    /// <summary>Identifies the <see cref="IsEditing"/> bindable property.</summary>
    public static readonly BindableProperty IsEditingProperty = BindableProperty.Create(
        nameof(IsEditing), typeof(bool), typeof(TapToEditField), false, BindingMode.TwoWay,
        propertyChanged: (b, _, _) => ((TapToEditField)b).ApplyState());

    /// <summary>
    /// The editor state requested by a tap (TwoWay). Set <c>true</c> by a tap on the text and when the inner
    /// editor gains focus; reset to <c>false</c> by a blur that is not followed by a new focus, and by a pick.
    /// </summary>
    public bool IsEditing
    {
        get => (bool)GetValue(IsEditingProperty);
        set => SetValue(IsEditingProperty, value);
    }

    // ── ReleasesPickOnEdit ─────────────────────────────────────────────────

    /// <summary>Identifies the <see cref="ReleasesPickOnEdit"/> bindable property.</summary>
    public static readonly BindableProperty ReleasesPickOnEditProperty = BindableProperty.Create(
        nameof(ReleasesPickOnEdit), typeof(bool), typeof(TapToEditField), true);

    /// <summary>
    /// When <c>true</c> (default), a tap on the text sets a locked wrapped <see cref="AutocompleteDXField"/>'s
    /// <c>IsLocked</c> to <c>false</c> and keeps its text (REQ-PIC-26).
    /// </summary>
    public bool ReleasesPickOnEdit
    {
        get => (bool)GetValue(ReleasesPickOnEditProperty);
        set => SetValue(ReleasesPickOnEditProperty, value);
    }

    // ── IsEditorVisible (read-only) ────────────────────────────────────────

    /// <summary>
    /// <c>true</c> while the editor state shows (the opposite of the text state). Pages read it, e.g. to
    /// focus the editor in <c>OnAppearing</c> only when it is visible. Raises <c>PropertyChanged</c>.
    /// </summary>
    public bool IsEditorVisible { get; private set; } = true;

    // The first DevExpress EditBase / MAUI InputView in the editor's tree, while subscribed.
    private VisualElement? _innerEditor;

    // The editor whose Loaded/HandlerChanged is watched until its inner editor can be found.
    private View? _retryHost;

    // The wrapped field, while its PropertyChanged is watched for IsLocked.
    private AutocompleteDXField? _autocomplete;

    // ── Constructor ────────────────────────────────────────────────────────

    /// <summary>Creates the wrapper with no editor; the editor state shows until a value is set.</summary>
    public TapToEditField()
    {
        InitializeComponent();
        ApplyTextStyle();
        ApplyState();

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    // ── Lifecycle: subscribe on Loaded, unsubscribe on Unloaded and on an editor change ──

    private void OnLoaded(object? sender, EventArgs e) => Attach();

    private void OnUnloaded(object? sender, EventArgs e) => Detach();

    private void OnEditorChanged(View? oldEditor, View? newEditor)
    {
        Detach();

        editorHost.Content = newEditor;

        if (IsLoaded)
            Attach();

        ApplyState();
    }

    // Idempotent: always starts from a clean state, so a repeated Loaded never subscribes twice.
    private void Attach()
    {
        Detach();

        if (Editor is AutocompleteDXField field)
        {
            _autocomplete = field;
            field.PropertyChanged += OnAutocompletePropertyChanged;
        }

        AttachInnerEditor();
    }

    private void AttachInnerEditor()
    {
        var editor = Editor;
        if (editor is null)
            return;

        var inner = FindInnerEditor(editor);
        if (inner is null)
        {
            // The inner editor may not exist until the editor's handler is created: retry then.
            _retryHost = editor;
            editor.Loaded += OnRetryHostReady;
            editor.HandlerChanged += OnRetryHostReady;
            return;
        }

        _innerEditor = inner;
        inner.Focused += OnInnerEditorFocused;
        inner.Unfocused += OnInnerEditorUnfocused;
    }

    private void Detach()
    {
        DetachRetryHost();

        if (_innerEditor is not null)
        {
            _innerEditor.Focused -= OnInnerEditorFocused;
            _innerEditor.Unfocused -= OnInnerEditorUnfocused;
            _innerEditor = null;
        }

        if (_autocomplete is not null)
        {
            _autocomplete.PropertyChanged -= OnAutocompletePropertyChanged;
            _autocomplete = null;
        }
    }

    private void DetachRetryHost()
    {
        if (_retryHost is null)
            return;

        _retryHost.Loaded -= OnRetryHostReady;
        _retryHost.HandlerChanged -= OnRetryHostReady;
        _retryHost = null;
    }

    private void OnRetryHostReady(object? sender, EventArgs e)
    {
        if (!ReferenceEquals(sender, Editor) || _innerEditor is not null)
            return;

        DetachRetryHost();
        AttachInnerEditor();
    }

    // The editor itself when it is a DevExpress EditBase or a MAUI InputView, else the first such descendant.
    private static VisualElement? FindInnerEditor(View editor)
    {
        if (IsInputEditor(editor))
            return editor;

        return ((IVisualTreeElement)editor)
            .GetVisualTreeDescendants()
            .OfType<VisualElement>()
            .FirstOrDefault(IsInputEditor);
    }

    private static bool IsInputEditor(VisualElement element) => element is EditBase or InputView;

    // ── State application ─────────────────────────────────────────────────

    // Called from the Text, IsEditing, HasError and Editor callbacks, so the result does not depend on the
    // order in which the binding engine assigns them.
    private void ApplyState()
    {
        if (textPresenter is null || editorHost is null)
            return;

        var showsText = TapToEditFieldLogic.ShowsText(Text, IsEditing, HasError);

        // Hiding a focused editor (e.g. IsEditing reset by the page) would leave the keyboard open.
        if (showsText && _innerEditor?.IsFocused == true)
            _innerEditor.Unfocus();

        textPresenter.IsVisible = showsText;
        editorHost.IsVisible = !showsText;

        if (IsEditorVisible == !showsText)
            return;

        IsEditorVisible = !showsText;
        OnPropertyChanged(nameof(IsEditorVisible));
    }

    private void ApplyTextStyle()
    {
        if (textLabel is null)
            return;

        var styleClass = string.IsNullOrWhiteSpace(TextStyleClass) ? DefaultTextStyleClass : TextStyleClass;
        textLabel.StyleClass = [styleClass];
    }

    // ── Tap on the text (REQ-PIC-26) ───────────────────────────────────────

    private void OnTextTapped(object? sender, TappedEventArgs e)
    {
        // (1) Release a picked value; the text stays. IsLocked is TwoWay, so the ViewModel sees it.
        if (Editor is AutocompleteDXField field
            && TapToEditFieldLogic.ShouldReleasePick(ReleasesPickOnEdit, field.IsLocked))
        {
            field.IsLocked = false;
        }

        // (2) Show the editor.
        IsEditing = true;

        // (3) Focus it once the host is visible and layout has run.
        Dispatcher.Dispatch(FocusWrappedEditor);
    }

    private void FocusWrappedEditor()
    {
        var editor = Editor;
        if (editor is null || !editorHost.IsVisible)
            return;

        if (editor is AutocompleteDXField field)
        {
            field.FocusEditor();
            return;
        }

        (_innerEditor ?? FindInnerEditor(editor))?.Focus();
    }

    // ── Leaving the editor state (REQ-PIC-25) ──────────────────────────────

    // A focus that did not come from a tap (e.g. typing into a blank field, or a page focusing the editor)
    // is an edit too: without it, the first non-blank keystroke would switch to the text state mid-typing.
    private void OnInnerEditorFocused(object? sender, FocusEventArgs e) => IsEditing = true;

    // The delay lets a pick, the X icon and blur validation (which may set HasError) finish first.
    private void OnInnerEditorUnfocused(object? sender, FocusEventArgs e)
    {
        var blurred = _innerEditor;
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(TapToEditFieldLogic.BlurSettleMilliseconds), () =>
        {
            // Stale: the editor was replaced or the wrapper was unloaded during the delay.
            if (blurred is null || !ReferenceEquals(blurred, _innerEditor))
                return;

            if (TapToEditFieldLogic.ShouldEndEditingAfterBlur(blurred.IsFocused))
                IsEditing = false;
        });
    }

    // A pick locks the wrapped field: back to the text state. Dispatched so the field's own pick handling
    // (select command, unfocus, keyboard hide) completes before its host is hidden.
    private void OnAutocompletePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(AutocompleteDXField.IsLocked) || sender is not AutocompleteDXField { IsLocked: true })
            return;

        Dispatcher.Dispatch(() =>
        {
            if (ReferenceEquals(sender, _autocomplete) && _autocomplete.IsLocked)
                IsEditing = false;
        });
    }
}
