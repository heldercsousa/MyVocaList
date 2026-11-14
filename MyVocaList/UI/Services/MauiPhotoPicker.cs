namespace MyVocaList.UI.Services;

/// <summary>
/// <see cref="IPhotoPicker"/> over <see cref="MediaPicker"/> (.NET 10: <c>PickPhotosAsync</c>; <c>PickPhotoAsync</c> is obsolete).
/// The picker limits the photo to 1600 px and applies the EXIF rotation before the crop (REQ-PIC-08).
/// </summary>
public sealed class MauiPhotoPicker : IPhotoPicker
{
    private const int MaxPickedSidePixels = 1600;
    private const int PickCompressionQuality = 90;

    /// <inheritdoc />
    public async Task<Stream?> PickPhotoAsync(CancellationToken ct = default)
    {
        // The picker must be opened on the UI thread; the caller is a command on it.
        var results = await MediaPicker.Default.PickPhotosAsync(new MediaPickerOptions
        {
            SelectionLimit = 1,
            MaximumWidth = MaxPickedSidePixels,
            MaximumHeight = MaxPickedSidePixels,
            CompressionQuality = PickCompressionQuality,
            RotateImage = true,
        });

        // Cancelling a multi-select pick returns an empty list.
        var first = results?.FirstOrDefault();
        return first is null ? null : await first.OpenReadAsync();
    }
}
