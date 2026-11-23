using Microsoft.Maui.Controls.Shapes;

namespace MyVocaList.UI.Components.Media;

/// <summary>
/// The form picture field (REQ-IPF-01..04; supersedes the size and button set of REQ-PIC-13/15 and REQ-VEN-07..09):
/// a centred square picture (a circle when <see cref="IsCircle"/>) with a round remove icon button over its top-end
/// corner, and the size hint. The side is computed from the field's own allocated width. The picture layers the
/// placeholder icon under <see cref="ThumbnailSource"/> under <see cref="ImageSource"/>, like <c>ListItemLeadingImageSf</c>.
/// A tap on the picture runs <see cref="PickCommand"/> (add, or replace when there is a picture).
/// The component decides nothing about pictures: the ViewModel owns <see cref="HasImage"/> and the commands.
/// </summary>
public partial class ImagePickerFieldSf : ContentView
{
    /// <summary>Corner radius of the song picture's rounded square (REQ-PIC-13).</summary>
    private const double SquareCornerRadius = 12;

    /// <summary>Horizontal inset from the field's allocated width on each side (REQ-IPF-01).</summary>
    private const double FormSideInset = 40;

    /// <summary>Largest side of the picture (REQ-IPF-01).</summary>
    private const double MaximumSide = 360;

    /// <summary>
    /// Side used before the field has a usable width (first layout, or a width of at most
    /// <see cref="MinimumLayoutWidth"/>). Also the lower bound of the computed side.
    /// </summary>
    private const double MinimumSide = 120;

    /// <summary>Allocated widths at or below this are treated as "not laid out yet".</summary>
    private const double MinimumLayoutWidth = 80;

    /// <summary>The side currently applied to the picture host, frame and image layers.</summary>
    private double _side;

    // ── ImageSource ────────────────────────────────────────────────────────

    /// <summary>Identifies the <see cref="ImageSource"/> bindable property.</summary>
    public static readonly BindableProperty ImageSourceProperty = BindableProperty.Create(
        nameof(ImageSource), typeof(ImageSource), typeof(ImagePickerFieldSf), null);

    /// <summary>The large picture, drawn on top.</summary>
    public ImageSource ImageSource
    {
        get => (ImageSource)GetValue(ImageSourceProperty);
        set => SetValue(ImageSourceProperty, value);
    }

    // ── ThumbnailSource ────────────────────────────────────────────────────

    /// <summary>Identifies the <see cref="ThumbnailSource"/> bindable property.</summary>
    public static readonly BindableProperty ThumbnailSourceProperty = BindableProperty.Create(
        nameof(ThumbnailSource), typeof(ImageSource), typeof(ImagePickerFieldSf), null);

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
        nameof(PlaceholderIcon), typeof(string), typeof(ImagePickerFieldSf), DefaultPlaceholderIcon);

    /// <summary>The icon under both pictures (REQ-PIC-14). Default <c>music_note_outlined</c>.</summary>
    public string PlaceholderIcon
    {
        get => (string)GetValue(PlaceholderIconProperty);
        set => SetValue(PlaceholderIconProperty, value);
    }

    // ── IsCircle ───────────────────────────────────────────────────────────

    /// <summary>Identifies the <see cref="IsCircle"/> bindable property.</summary>
    public static readonly BindableProperty IsCircleProperty = BindableProperty.Create(
        nameof(IsCircle), typeof(bool), typeof(ImagePickerFieldSf), false,
        propertyChanged: (b, _, _) => ((ImagePickerFieldSf)b).ApplyShape());

    /// <summary><c>true</c>: a circle (artist); <c>false</c>: a 12 dp rounded square (song). REQ-PIC-13.</summary>
    public bool IsCircle
    {
        get => (bool)GetValue(IsCircleProperty);
        set => SetValue(IsCircleProperty, value);
    }

    // ── HasImage ───────────────────────────────────────────────────────────

    /// <summary>Identifies the <see cref="HasImage"/> bindable property.</summary>
    public static readonly BindableProperty HasImageProperty = BindableProperty.Create(
        nameof(HasImage), typeof(bool), typeof(ImagePickerFieldSf), false,
        propertyChanged: (b, _, _) => ((ImagePickerFieldSf)b).ApplyState());

    /// <summary>Whether the form has a picture: shows the remove button, switches the tap hint and hides the size hint (REQ-IPF-03, REQ-PIC-16).</summary>
    public bool HasImage
    {
        get => (bool)GetValue(HasImageProperty);
        set => SetValue(HasImageProperty, value);
    }

    // ── HintText ───────────────────────────────────────────────────────────

    /// <summary>Identifies the <see cref="HintText"/> bindable property.</summary>
    public static readonly BindableProperty HintTextProperty = BindableProperty.Create(
        nameof(HintText), typeof(string), typeof(ImagePickerFieldSf), string.Empty,
        propertyChanged: (b, _, _) => ((ImagePickerFieldSf)b).ApplyState());

    /// <summary>The size hint, shown only while <see cref="HasImage"/> is <c>false</c> (REQ-PIC-16).</summary>
    public string HintText
    {
        get => (string)GetValue(HintTextProperty);
        set => SetValue(HintTextProperty, value);
    }

    // ── PickCommand ────────────────────────────────────────────────────────

    /// <summary>Identifies the <see cref="PickCommand"/> bindable property.</summary>
    public static readonly BindableProperty PickCommandProperty = BindableProperty.Create(
        nameof(PickCommand), typeof(ICommand), typeof(ImagePickerFieldSf), null);

    /// <summary>Runs on a tap on the picture: adds a picture when there is none, replaces it otherwise (REQ-IPF-02).</summary>
    public ICommand PickCommand
    {
        get => (ICommand)GetValue(PickCommandProperty);
        set => SetValue(PickCommandProperty, value);
    }

    // ── RemoveCommand ──────────────────────────────────────────────────────

    /// <summary>Identifies the <see cref="RemoveCommand"/> bindable property.</summary>
    public static readonly BindableProperty RemoveCommandProperty = BindableProperty.Create(
        nameof(RemoveCommand), typeof(ICommand), typeof(ImagePickerFieldSf), null);

    /// <summary>Runs on a tap on the corner remove button (REQ-IPF-03, REQ-PIC-20).</summary>
    public ICommand RemoveCommand
    {
        get => (ICommand)GetValue(RemoveCommandProperty);
        set => SetValue(RemoveCommandProperty, value);
    }

    // ── IsBusy ─────────────────────────────────────────────────────────────

    /// <summary>Identifies the <see cref="IsBusy"/> bindable property.</summary>
    public static readonly BindableProperty IsBusyProperty = BindableProperty.Create(
        nameof(IsBusy), typeof(bool), typeof(ImagePickerFieldSf), false,
        propertyChanged: (b, _, _) => ((ImagePickerFieldSf)b).ApplyState());

    /// <summary>While <c>true</c> the picture tap and the remove button are disabled (a picture is being imported; REQ-IPF-04).</summary>
    public bool IsBusy
    {
        get => (bool)GetValue(IsBusyProperty);
        set => SetValue(IsBusyProperty, value);
    }

    // ── Constructor ────────────────────────────────────────────────────────

    /// <summary>Creates the field with no picture: the placeholder, no remove button and no hint text.</summary>
    public ImagePickerFieldSf()
    {
        InitializeComponent();
        ApplyShape();
        ApplySide(MinimumSide);
        ApplyState();
    }

    // ── Layout ─────────────────────────────────────────────────────────────

    /// <summary>Recomputes the picture side when the field's allocated width changes (REQ-IPF-01).</summary>
    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);

        var side = ComputeSide(width);
        if (side != _side)
            ApplySide(side);
    }

    /// <summary>
    /// The square side for an allocated width: <c>width - 2 * 40</c>, at most 360, and never below
    /// <see cref="MinimumSide"/>. A width at or below <see cref="MinimumLayoutWidth"/> (not laid out yet) gives the minimum.
    /// </summary>
    private static double ComputeSide(double width)
    {
        if (width <= MinimumLayoutWidth)
            return MinimumSide;

        return Math.Max(MinimumSide, Math.Min(width - 2 * FormSideInset, MaximumSide));
    }

    // Sets the size of the host, the frame and both image layers (width and height together). Child sizes only:
    // the field's own WidthRequest is never touched, so the layout does not feed back into itself.
    private void ApplySide(double side)
    {
        if (pictureHost is null)
            return;

        _side = side;

        pictureHost.WidthRequest = side;
        pictureHost.HeightRequest = side;
        pictureFrame.WidthRequest = side;
        pictureFrame.HeightRequest = side;
        thumbnailImage.WidthRequest = side;
        thumbnailImage.HeightRequest = side;
        largeImage.WidthRequest = side;
        largeImage.HeightRequest = side;
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
        if (removeButton is null)
            return;

        var hasImage = HasImage;

        removeButton.IsVisible = hasImage;
        removeButton.IsEnabled = !IsBusy;

        SemanticProperties.SetHint(pictureFrame, hasImage
            ? "Double tap to replace the picture"
            : "Double tap to choose a picture");

        hintLabel.IsVisible = !hasImage && !string.IsNullOrWhiteSpace(HintText);
    }

    // A tap on the picture adds or replaces the picture (REQ-IPF-02), never while busy (REQ-IPF-04).
    private void OnPictureTapped(object? sender, TappedEventArgs e)
    {
        if (IsBusy)
            return;

        var command = PickCommand;
        if (command?.CanExecute(null) == true)
            command.Execute(null);
    }
}
