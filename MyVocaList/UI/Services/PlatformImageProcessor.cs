#if ANDROID || IOS || WINDOWS
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Graphics.Platform;
#endif

namespace MyVocaList.UI.Services;

/// <summary>
/// <see cref="IImageProcessor"/> over <c>Microsoft.Maui.Graphics.Platform.PlatformImage</c> (REQ-PIC-06/07).
/// <c>ResizeMode.Bleed</c> fills the square and crops the overflow from the centre. The plain
/// <c>net10.0</c> head (test-only) has no platform image type and throws.
/// </summary>
public sealed class PlatformImageProcessor : IImageProcessor
{
    /// <inheritdoc />
    public byte[] CropSquareJpeg(byte[] source, int maxSidePixels, float quality)
    {
#if ANDROID || IOS || WINDOWS
        using var input = new MemoryStream(source, writable: false);
        using var image = PlatformImage.FromStream(input)
            ?? throw new InvalidOperationException("The picture could not be decoded.");

        // Never enlarge: the side is the target or the shorter side of the source, whichever is smaller.
        var side = (int)Math.Min(maxSidePixels, Math.Min(image.Width, image.Height));
        if (side <= 0)
            throw new InvalidOperationException("The picture has no pixels.");

        using var square = image.Resize(side, side, ResizeMode.Bleed, disposeOriginal: false);
        using var output = new MemoryStream();
        square.Save(output, ImageFormat.Jpeg, quality);
        return output.ToArray();
#else
        throw new PlatformNotSupportedException("Image processing needs a platform target (Android, iOS or Windows).");
#endif
    }
}
