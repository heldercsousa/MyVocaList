using MyVocaList.Domain.Rules;
using MyVocaList.UI.Services;

namespace MyVocaList.UI.Converters;

/// <summary>
/// Image reference (URL or <c>local:</c>) to the source to display (REQ-PIC-03). <c>ConverterParameter</c>
/// "Large" selects the large size; anything else the thumbnail.
/// </summary>
public sealed class ImageReferenceConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        var size = string.Equals(parameter as string, "Large", StringComparison.OrdinalIgnoreCase)
            ? ImageSize.Large
            : ImageSize.Thumbnail;

        return ImageReferenceRules.ToDisplaySource(value as string, size, AppImagePaths.ImagesRoot);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        => throw new NotSupportedException();
}
