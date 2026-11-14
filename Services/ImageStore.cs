using MyVocaList.Domain.Rules;
using MyVocaList.Domain.ServicesInterfaces;

namespace MyVocaList.Services;

/// <inheritdoc />
public class ImageStore : IImageStore
{
    /// <summary>Largest photo accepted (REQ-PIC-10).</summary>
    public const long MaxSourceBytes = 25L * 1024 * 1024;

    /// <summary>The one message shown for every import failure (REQ-PIC-10).</summary>
    public const string FailureMessage = "Could not use this picture.";

    internal const float ThumbnailQuality = 0.75f;
    internal const float LargeQuality = 0.80f;

    private readonly ImageStoreOptions _options;
    private readonly IImageProcessor _processor;
    private readonly ILogger<ImageStore> _logger;

    public ImageStore(ImageStoreOptions options, IImageProcessor processor, ILogger<ImageStore> logger)
    {
        _options = options;
        _processor = processor;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<(bool success, string message, string? reference)> ImportAsync(
        Stream source, ImageOwnerKind kind, CancellationToken ct = default)
    {
        var reference = ImageReferenceRules.CreateLocal(kind, Guid.NewGuid());
        ImageReferenceRules.TryGetLocalFiles(reference, _options.RootDirectory, out var thumbnailPath, out var largePath);
        var thumbnailTemp = thumbnailPath + ".tmp";
        var largeTemp = largePath + ".tmp";

        try
        {
            var bytes = await ReadLimitedAsync(source, ct);
            if (bytes is null)
            {
                _logger.LogWarning("Picture refused: larger than {Limit} bytes", MaxSourceBytes);
                return (false, FailureMessage, null);
            }

            var thumbnail = _processor.CropSquareJpeg(bytes, ImageReferenceRules.ThumbnailPixels, ThumbnailQuality);
            var large = _processor.CropSquareJpeg(bytes, ImageReferenceRules.LargePixels, LargeQuality);

            Directory.CreateDirectory(Path.GetDirectoryName(thumbnailPath)!);
            await File.WriteAllBytesAsync(thumbnailTemp, thumbnail, ct);
            await File.WriteAllBytesAsync(largeTemp, large, ct);
            File.Move(thumbnailTemp, thumbnailPath);
            File.Move(largeTemp, largePath);

            return (true, string.Empty, reference);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Expected failure at the file / decoder boundary: translated to the tuple (design § 4).
            _logger.LogWarning(ex, "Picture import failed");
            TryDelete(thumbnailTemp);
            TryDelete(largeTemp);
            TryDelete(thumbnailPath);
            TryDelete(largePath);
            return (false, FailureMessage, null);
        }
        catch (OperationCanceledException)
        {
            TryDelete(thumbnailTemp);
            TryDelete(largeTemp);
            TryDelete(thumbnailPath);
            TryDelete(largePath);
            throw;
        }
    }

    /// <inheritdoc />
    public string? ResolveDisplaySource(string? reference, ImageSize size)
        => ImageReferenceRules.ToDisplaySource(reference, size, _options.RootDirectory);

    /// <inheritdoc />
    public Task<int> DeleteUnreferencedAsync(
        IReadOnlyCollection<string> liveReferences, TimeSpan minimumAge, CancellationToken ct = default)
    {
        var liveNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var reference in liveReferences)
        {
            if (ImageReferenceRules.TryGetLocalFiles(reference, _options.RootDirectory, out var thumbnail, out _))
                liveNames.Add(Path.GetFileName(thumbnail)[..^ImageReferenceRules.ThumbnailSuffix.Length]);
        }

        var cutoff = DateTime.UtcNow - minimumAge;
        var deleted = 0;

        foreach (var folder in ImageReferenceRules.Folders)
        {
            ct.ThrowIfCancellationRequested();
            var directory = Path.Combine(_options.RootDirectory, folder);
            if (!Directory.Exists(directory))
                continue;

            foreach (var file in Directory.EnumerateFiles(directory))
            {
                ct.ThrowIfCancellationRequested();
                var name = StripSuffix(Path.GetFileName(file));
                if (liveNames.Contains(name) || File.GetLastWriteTimeUtc(file) >= cutoff)
                    continue;

                try
                {
                    File.Delete(file);
                    deleted++;
                }
                catch (IOException ex)
                {
                    // One locked or vanished file must not stop the sweep; it is retried at the next start.
                    _logger.LogWarning(ex, "Could not delete orphan image file {File}", file);
                }
            }
        }

        return Task.FromResult(deleted);
    }

    private static string StripSuffix(string fileName)
    {
        foreach (var suffix in new[] { ImageReferenceRules.ThumbnailSuffix, ImageReferenceRules.LargeSuffix })
        {
            if (fileName.EndsWith(suffix, StringComparison.Ordinal))
                return fileName[..^suffix.Length];
        }

        return fileName;
    }

    private static async Task<byte[]?> ReadLimitedAsync(Stream source, CancellationToken ct)
    {
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = await source.ReadAsync(chunk, ct)) > 0)
        {
            if (buffer.Length + read > MaxSourceBytes)
                return null;
            buffer.Write(chunk, 0, read);
        }

        return buffer.ToArray();
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (IOException)
        {
            // Best-effort cleanup; the orphan sweep removes any leftover after 24 hours.
        }
    }
}
