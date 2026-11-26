using System.Windows.Input;

namespace MyVocaList.UI.Components.TextFields;

/// <summary>
/// MD3 outlined text field built from stock MAUI (<see cref="Border"/> around an <see cref="Entry"/>).
/// Used where <c>SfTextInputLayout</c> fails (search bar).
/// </summary>
public partial class OutlinedTextField : ContentView
{
    private const double RestingStrokeThickness = 1;
    private const double FocusedStrokeThickness = 2;

    public static readonly BindableProperty TextProperty = BindableProperty.Create(
        nameof(Text), typeof(string), typeof(OutlinedTextField), string.Empty, BindingMode.TwoWay);

    public static readonly BindableProperty PlaceholderProperty = BindableProperty.Create(
        nameof(Placeholder), typeof(string), typeof(OutlinedTextField), string.Empty);

    public static readonly BindableProperty IsPasswordProperty = BindableProperty.Create(
        nameof(IsPassword), typeof(bool), typeof(OutlinedTextField), false);

    public static readonly BindableProperty KeyboardProperty = BindableProperty.Create(
        nameof(Keyboard), typeof(Keyboard), typeof(OutlinedTextField), Keyboard.Default);

    public static readonly BindableProperty ReturnCommandProperty = BindableProperty.Create(
        nameof(ReturnCommand), typeof(ICommand), typeof(OutlinedTextField), null);

    public static readonly BindableProperty ClearButtonVisibilityProperty = BindableProperty.Create(
        nameof(ClearButtonVisibility), typeof(ClearButtonVisibility), typeof(OutlinedTextField),
        ClearButtonVisibility.Never);

    /// <summary>Backs <see cref="ShowOutline"/>.</summary>
    public static readonly BindableProperty ShowOutlineProperty = BindableProperty.Create(
        nameof(ShowOutline), typeof(bool), typeof(OutlinedTextField), true,
        propertyChanged: static (b, _, _) => ((OutlinedTextField)b).ApplyOutline(isFocused: false));

    /// <summary>Backs <see cref="ReturnType"/>.</summary>
    public static readonly BindableProperty ReturnTypeProperty = BindableProperty.Create(
        nameof(ReturnType), typeof(ReturnType), typeof(OutlinedTextField), ReturnType.Default);

    public OutlinedTextField()
    {
        InitializeComponent();
    }

    /// <summary>
    /// When <c>false</c> the field draws no outline (transparent stroke, thickness 0) in both the resting and the
    /// focused state, for use inside a container that supplies its own shape, such as the search bar pill.
    /// Defaults to <c>true</c>.
    /// </summary>
    public bool ShowOutline
    {
        get => (bool)GetValue(ShowOutlineProperty);
        set => SetValue(ShowOutlineProperty, value);
    }

    /// <summary>The action key shown on the soft keyboard (for example <see cref="ReturnType.Search"/>), forwarded to the inner Entry.</summary>
    public ReturnType ReturnType
    {
        get => (ReturnType)GetValue(ReturnTypeProperty);
        set => SetValue(ReturnTypeProperty, value);
    }

    /// <summary>Raised when the user finishes input (return key), forwarded from the inner Entry.</summary>
    public event EventHandler? Completed;

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

    public bool IsPassword
    {
        get => (bool)GetValue(IsPasswordProperty);
        set => SetValue(IsPasswordProperty, value);
    }

    public Keyboard Keyboard
    {
        get => (Keyboard)GetValue(KeyboardProperty);
        set => SetValue(KeyboardProperty, value);
    }

    public ICommand? ReturnCommand
    {
        get => (ICommand?)GetValue(ReturnCommandProperty);
        set => SetValue(ReturnCommandProperty, value);
    }

    public ClearButtonVisibility ClearButtonVisibility
    {
        get => (ClearButtonVisibility)GetValue(ClearButtonVisibilityProperty);
        set => SetValue(ClearButtonVisibilityProperty, value);
    }

    private void OnEntryFocused(object? sender, FocusEventArgs e) => ApplyOutline(isFocused: true);

    private void OnEntryUnfocused(object? sender, FocusEventArgs e) => ApplyOutline(isFocused: false);

    private void OnEntryCompleted(object? sender, EventArgs e) => Completed?.Invoke(this, e);

    private void ApplyOutline(bool isFocused)
    {
        if (!ShowOutline)
        {
            outline.Stroke = Colors.Transparent;
            outline.StrokeThickness = 0;
            return;
        }

        var colorKey = isFocused ? "Primary" : "Outline";
        if (Application.Current?.Resources.TryGetValue(colorKey, out var color) == true && color is Color stroke)
        {
            outline.Stroke = new SolidColorBrush(stroke);
        }

        outline.StrokeThickness = isFocused ? FocusedStrokeThickness : RestingStrokeThickness;
    }
}

/// <summary>
/// Entry used only inside <see cref="OutlinedTextField"/>. The subclass lets a handler mapping strip the native
/// Android underline for this control alone, leaving every other <see cref="Entry"/> in the app untouched.
/// </summary>
public sealed class OutlinedTextFieldSfEntry : Entry
{
    static OutlinedTextFieldSfEntry()
    {
        Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping(
            nameof(OutlinedTextFieldSfEntry),
            static (handler, view) =>
            {
                if (view is not OutlinedTextFieldSfEntry)
                {
                    return;
                }
#if ANDROID
                handler.PlatformView.Background = null;
#elif IOS || MACCATALYST
                handler.PlatformView.BorderStyle = UIKit.UITextBorderStyle.None;
#endif
            });
    }
}
