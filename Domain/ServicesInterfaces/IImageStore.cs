using MyVocaList.Domain.Rules;

namespace MyVocaList.Domain.ServicesInterfaces;

/// <summary>Where the app keeps uploaded pictures: the app-private images folder.</summary>
/// <param name="RootDirectory">Absolute path of the images folder.</param>
public sealed record ImageStoreOptions(string RootDirectory);

/// <summary>Stores uploaded pictures as two square JPEG files and resolves what to display (REQ-PIC-03..11).</summary>
public interface IImageStore
{
    /// <summary>Crops, compresses and stores a picked photo; on failure nothing is kept.</summary>
    Task<(bool success, string message, string? reference)> ImportAsync(
        Stream source, ImageOwnerKind kind, CancellationToken ct = default);

    /// <summary>The source to display for a stored reference and size, or null.</summary>
    string? ResolveDisplaySource(string? reference, ImageSize size);

    /// <summary>Deletes unreferenced files older than <paramref name="minimumAge"/>; returns how many.</summary>
    Task<int> DeleteUnreferencedAsync(
        IReadOnlyCollection<string> liveReferences, TimeSpan minimumAge, CancellationToken ct = default);
}
