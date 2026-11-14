namespace MyVocaList.Domain.Rules;

/// <summary>
/// Rules for the one stored image reference of a song, an artist or a venue (REQ-PIC-01..04, REQ-VEN-03): either an https
/// image URL (<see cref="ImageUrlRules"/>) or a local reference <c>local:&lt;folder&gt;/&lt;32 lowercase hex&gt;</c>.
/// </summary>
public static class ImageReferenceRules
{
    public const string LocalPrefix = "local:";
    public const string SongsFolder = "songs";
    public const string ArtistsFolder = "artists";
    public const string VenuesFolder = "venues";
    public const string ThumbnailSuffix = "_t.jpg";
    public const string LargeSuffix = "_l.jpg";
    public const int ThumbnailPixels = 256;
    public const int LargePixels = 512;

    private const int NameLength = 32;
    private const string DeezerThumbnailSegment = "/250x250-";
    private const string DeezerLargeSegment = "/500x500-";

    /// <summary>Every sub-folder that may appear in a local reference (songs, artists, venues).</summary>
    public static IReadOnlyList<string> Folders { get; } = [SongsFolder, ArtistsFolder, VenuesFolder];

    /// <summary>True only for a well-formed local reference.</summary>
    public static bool IsLocal(string? reference) => TryParseLocal(reference, out _, out _);

    /// <summary>A well-formed local reference is kept as is; anything else goes through <see cref="ImageUrlRules.Normalize"/>.</summary>
    public static string? Normalize(string? value)
        => IsLocal(value) ? value : ImageUrlRules.Normalize(value);

    /// <summary>Builds <c>local:&lt;folder&gt;/&lt;id:N&gt;</c>.</summary>
    public static string CreateLocal(ImageOwnerKind kind, Guid id)
        => $"{LocalPrefix}{FolderOf(kind)}/{id:N}";

    /// <summary>The source to display for a reference and a size (REQ-PIC-03); null when there is none.</summary>
    public static string? ToDisplaySource(string? reference, ImageSize size, string imagesRoot)
    {
        if (string.IsNullOrWhiteSpace(reference))
            return null;

        if (reference.StartsWith(LocalPrefix, StringComparison.Ordinal))
        {
            if (!TryParseLocal(reference, out var folder, out var name))
                return null;
            var suffix = size == ImageSize.Large ? LargeSuffix : ThumbnailSuffix;
            return Path.Combine(imagesRoot, folder, name + suffix);
        }

        var url = ImageUrlRules.Normalize(reference);
        if (url is null)
            return null;

        if (size != ImageSize.Large)
            return url;

        var index = url.IndexOf(DeezerThumbnailSegment, StringComparison.Ordinal);
        return index < 0
            ? url
            : string.Concat(url.AsSpan(0, index), DeezerLargeSegment, url.AsSpan(index + DeezerThumbnailSegment.Length));
    }

    /// <summary>The two file paths of a well-formed local reference; false otherwise.</summary>
    public static bool TryGetLocalFiles(string? reference, string imagesRoot, out string thumbnailPath, out string largePath)
    {
        if (TryParseLocal(reference, out var folder, out var name))
        {
            thumbnailPath = Path.Combine(imagesRoot, folder, name + ThumbnailSuffix);
            largePath = Path.Combine(imagesRoot, folder, name + LargeSuffix);
            return true;
        }

        thumbnailPath = string.Empty;
        largePath = string.Empty;
        return false;
    }

    private static string FolderOf(ImageOwnerKind kind)
        => kind switch
        {
            ImageOwnerKind.Song => SongsFolder,
            ImageOwnerKind.Artist => ArtistsFolder,
            ImageOwnerKind.Venue => VenuesFolder,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown image owner kind."),
        };

    private static bool TryParseLocal(string? reference, out string folder, out string name)
    {
        folder = string.Empty;
        name = string.Empty;

        if (reference is null || !reference.StartsWith(LocalPrefix, StringComparison.Ordinal))
            return false;

        var rest = reference.AsSpan(LocalPrefix.Length);
        var slash = rest.IndexOf('/');
        if (slash < 0)
            return false;

        var folderPart = rest[..slash];
        var namePart = rest[(slash + 1)..];

        string? parsedFolder = null;
        foreach (var known in Folders)
        {
            if (folderPart.SequenceEqual(known))
            {
                parsedFolder = known;
                break;
            }
        }

        if (parsedFolder is null)
            return false;

        if (namePart.Length != NameLength)
            return false;

        foreach (var c in namePart)
        {
            if (!(c is >= '0' and <= '9' || c is >= 'a' and <= 'f'))
                return false;
        }

        folder = parsedFolder;
        name = namePart.ToString();
        return true;
    }
}
