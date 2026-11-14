using Microsoft.Maui.Controls.Shapes;

namespace MyVocaList.UI.Components.Media;

/// <summary>
/// The form picture field (REQ-PIC-13..16; design.md § 2.5 of the form-pictures change): a centred column
/// of the 120 dp picture (rounded square, or circle when <see cref="IsCircle"/>), the actions ("Add picture",
/// or "Replace picture" + "Remove picture") and the size hint. The picture layers the placeholder icon under
/// <see cref="ThumbnailSource"/> under <see cref="ImageSource"/>, like <c>ListItemLeadingImage</c>.
/// The component decides nothing about pictures: the ViewModel owns <see cref="HasImage"/> and the commands.
/// </summary>
public partial class ImagePickerField : ContentView
{
    /// <summary>Corner radius of the song picture's rounded square (REQ-PIC-13).</summary>
    private const double SquareCornerRadius = 12;

    // ── ImageSource ────────────────────────────────────────────────────────

    /// <summary>Identifies the <see cref="ImageSource"/> bindable property.</summary>
    public static readonly BindableProperty ImageSourceProperty = BindableProperty.Create(
        nameof(ImageSource), typeof(ImageSource), typeof(ImagePickerField), null);

    /// <summary>The large picture, drawn on top.</summary>
    public ImageSource ImageSource
    {
        get => (ImageSource)GetValue(ImageSourceProperty);
        set => SetValue(ImageSourceProperty, value);
    }

    // ── ThumbnailSource ────────────────────────────────────────────────────

    /// <summary>Identifies the <see cref="ThumbnailSource"/> bindable property.</summary>
    public static readonly BindableProperty ThumbnailSourceProperty = BindableProperty.Create(
        nameof(ThumbnailSource), typeof(ImageSource), typeof(ImagePickerField), null);

    /// <summary>Drawn under the large picture: shows first, and stays when the large one fails.</summary>
    public ImageSource ThumbnailSource
    {
        get => (ImageSource)GetValue(ThumbnailSourceProperty);
        set => SetValue(ThumbnailSourceProperty, value);
    }

    // ── PlaceholderIcon ────────────────────────────────────────────────────

    /// <summary>The default placeholder icon (the song placeholder).</summary>
    public const string DefaultPlaceholderIcon = "music_note_outlined";

    /// <summary>Identifies the <see cref="PlaceholderIcon"/> bindable property.</summary>
    public static readonly BindableProperty PlaceholderIconProperty = BindableProperty.Create(
        nameof(PlaceholderIcon), typeof(string), typeof(ImagePickerField), DefaultPlaceholderIcon);

    /// <summary>The icon under both pictures (REQ-PIC-14). Default <c>music_note_outlined</c>.</summary>
    public string PlaceholderIcon
    {
        get => (string)GetValue(PlaceholderIconProperty);
        set => SetValue(PlaceholderIconProperty, value);
    }

    // ── IsCircle ───────────────────────────────────────────────────────────

    /// <summary>Identifies the <see cref="IsCircle"/> bindable property.</summary>
    public static readonly BindableProperty IsCircleProperty = BindableProperty.Create(
        nameof(IsCircle), typeof(bool), typeof(ImagePickerField), false,
        propertyChanged: (b, _, _) => ((ImagePickerField)b).ApplyShape());

    /// <summary><c>true</c>: a circle (artist); <c>false</c>: a 12 dp rounded square (song). REQ-PIC-13.</summary>
    public bool IsCircle
    {
        get => (bool)GetValue(IsCircleProperty);
        set => SetValue(IsCircleProperty, value);
    }

    // ── HasImage ───────────────────────────────────────────────────────────

    /// <summary>Identifies the <see cref="HasImage"/> bindable property.</summary>
    public static readonly BindableProperty HasImageProperty = BindableProperty.Create(
        nameof(HasImage), typeof(bool), typeof(ImagePickerField), false,
        propertyChanged: (b, _, _) => ((ImagePickerField)b).ApplyState());

    /// <summary>Whether the form has a picture: switches the buttons and the hint (REQ-PIC-15, -16).</summary>
    public bool HasImage
    {
        get => (bool)GetValue(HasImageProperty);
        set => SetValue(HasImageProperty, value);
    }

    // ── HintText ───────────────────────────────────────────────────────────

    /// <summary>Identifies the <see cref="HintText"/> bindable property.</summary>
    public static readonly BindableProperty HintTextProperty = BindableProperty.Create(
        nameof(HintText), typeof(string), typeof(ImagePickerField), string.Empty,
        propertyChanged: (b, _, _) => ((ImagePickerField)b).ApplyState());

    /// <summary>The size hint, shown only while <see cref="HasImage"/> is <c>false</c> (REQ-PIC-16).</summary>
    public string HintText
    {
        get => (string)GetValue(HintTextProperty);
        set => SetValue(HintTextProperty, value);
    }

    // ── PickCommand ────────────────────────────────────────────────────────

    /// <summary>Identifies the <see cref="PickCommand"/> bindable property.</summary>
    public static readonly BindableProperty PickCommandProperty = BindableProperty.Create(
        nameof(PickCommand), typeof(ICommand), typeof(ImagePickerField), null);

    /// <summary>Runs on "Add picture", "Replace picture" and a tap on the picture (REQ-PIC-15).</summary>
    public ICommand PickCommand
    {
        get => (ICommand)GetValue(PickCommandProperty);
        set => SetValue(PickCommandProperty, value);
    }

    // ── RemoveCommand ──────────────────────────────────────────────────────

    /// <summary>Identifies the <see cref="RemoveCommand"/> bindable property.</summary>
    public static readonly BindableProperty RemoveCommandProperty = BindableProperty.Create(
        nameof(RemoveCommand), typeof(ICommand), typeof(ImagePickerField), null);

    /// <summary>Runs on "Remove picture" (REQ-PIC-15, -20).</summary>
    public ICommand RemoveCommand
    {
        get => (ICommand)GetValue(RemoveCommandProperty);
        set => SetValue(RemoveCommandProperty, value);
    }

    // ── IsBusy ─────────────────────────────────────────────────────────────

    /// <summary>Identifies the <see cref="IsBusy"/> bindable property.</summary>
    public static readonly BindableProperty IsBusyProperty = BindableProperty.Create(
        nameof(IsBusy), typeof(bool), typeof(ImagePickerField), false,
        propertyChanged: (b, _, _) => ((ImagePickerField)b).ApplyState());

    /// <summary>While <c>true</c> the actions and the picture tap are disabled (a picture is being imported).</summary>
    public bool IsBusy
    {
        get => (bool)GetValue(IsBusyProperty);
        set => SetValue(IsBusyProperty, value);
    }

    // ── Constructor ────────────────────────────────────────────────────────

    /// <summary>Creates the field with no picture: the placeholder, "Add picture" and no hint text.</summary>
    public ImagePickerField()
    {
        InitializeComponent();
        ApplyShape();
        ApplyState();
    }

    // ── State application ─────────────────────────────────────────────────

    private void ApplyShape()
    {
        if (pictureFrame is null)
            return;

        pictureFrame.StrokeShape = IsCircle
            ? new Ellipse()
            : new RoundRectangle { CornerRadius = new CornerRadius(SquareCornerRadius) };
    }

    // Called from the HasImage, HintText and IsBusy callbacks, so the result does not depend on binding order.
    private void ApplyState()
    {
        if (addButton is null)
            return;

        var hasImage = HasImage;
        var isEnabled = !IsBusy;

        addButton.IsVisible = !hasImage;
        replaceButton.IsVisible = hasImage;
        removeButton.IsVisible = hasImage;

        addButton.IsEnabled = isEnabled;
        replaceButton.IsEnabled = isEnabled;
        removeButton.IsEnabled = isEnabled;

        hintLabel.IsVisible = !hasImage && !string.IsNullOrWhiteSpace(HintText);
    }

    // A tap on the picture does what the first button does (REQ-PIC-15), never while busy.
    private void OnPictureTapped(object? sender, TappedEventArgs e)
    {
        if (IsBusy)
            return;

        var command = PickCommand;
        if (command?.CanExecute(null) == true)
            command.Execute(null);
    }
}
