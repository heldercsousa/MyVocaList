namespace MyVocaList.Domain.ServicesInterfaces;

/// <summary>Platform image work, kept behind an interface so the store is testable off-device.</summary>
public interface IImageProcessor
{
    /// <summary>
    /// Crops the largest centred square and scales it to <c>min(maxSidePixels, shorter side)</c>,
    /// returned as JPEG. Throws when the input cannot be decoded.
    /// </summary>
    byte[] CropSquareJpeg(byte[] source, int maxSidePixels, float quality);
}
