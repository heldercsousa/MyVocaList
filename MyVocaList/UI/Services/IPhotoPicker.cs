namespace MyVocaList.UI.Services;

/// <summary>Lets the user pick one photo from the device library (REQ-PIC-05). Wrapped so ViewModels never touch <c>MediaPicker</c>.</summary>
public interface IPhotoPicker
{
    /// <summary>Opens the photo picker; returns the photo stream, or null when the user cancels.</summary>
    Task<Stream?> PickPhotoAsync(CancellationToken ct = default);
}
