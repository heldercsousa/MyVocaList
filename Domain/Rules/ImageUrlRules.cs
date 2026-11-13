namespace MyVocaList.Domain.Rules;

/// <summary>
/// Rules for stored image URLs (REQ-ART-03, REQ-ART-05). Only an absolute https URL of at most
/// <see cref="MaxLength"/> characters is kept; anything else is treated as no image.
/// </summary>
public static class ImageUrlRules
{
    public const int MaxLength = 500;

    /// <summary>Trims the value; returns it when it is an absolute https URL within <see cref="MaxLength"/>, else null.</summary>
    public static string? Normalize(string? value)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed) || trimmed.Length > MaxLength)
            return null;

        return Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps
            ? trimmed
            : null;
    }
}
