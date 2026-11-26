using System.Text.RegularExpressions;

namespace MyVocaList.Domain.Rules;

/// <summary>
/// Extracts the 11-character video id from a pasted or shared YouTube link (REQ-MSRC-06).
/// Only the id is ever stored; playlist-only and non-YouTube links are rejected.
/// </summary>
public static class YouTubeLinkRules
{
    private static readonly Regex IdPattern = new("^[A-Za-z0-9_-]{11}$", RegexOptions.Compiled);

    private static readonly string[] PathPrefixes = ["/shorts/", "/embed/"];

    /// <summary>
    /// Accepts <c>youtu.be/{id}</c>, <c>(www.|m.|music.)youtube.com/watch?v={id}</c>, <c>/shorts/{id}</c> and <c>/embed/{id}</c>
    /// anywhere inside the text (shared text often carries a title around the link).
    /// </summary>
    public static bool TryExtractVideoId(string? text, out string videoId)
    {
        videoId = string.Empty;
        if (string.IsNullOrWhiteSpace(text)) return false;

        foreach (var token in text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
        {
            var candidate = token.Contains("://", StringComparison.Ordinal) ? token : "https://" + token;
            if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri)) continue;
            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) continue;

            var id = ExtractFromUri(uri);
            if (id is null) continue;

            videoId = id;
            return true;
        }

        return false;
    }

    private static string? ExtractFromUri(Uri uri)
    {
        var host = uri.Host.ToLowerInvariant();

        if (host == "youtu.be")
            return Validate(uri.AbsolutePath.Trim('/').Split('/')[0]);

        if (host is not ("youtube.com" or "www.youtube.com" or "m.youtube.com" or "music.youtube.com"))
            return null;

        var path = uri.AbsolutePath;
        if (path == "/watch")
        {
            foreach (var pair in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = pair.Split('=', 2);
                if (parts.Length == 2 && parts[0] == "v") return Validate(parts[1]);
            }
            return null;
        }

        foreach (var prefix in PathPrefixes)
        {
            if (path.StartsWith(prefix, StringComparison.Ordinal))
                return Validate(path[prefix.Length..].Split('/')[0]);
        }

        return null;
    }

    private static string? Validate(string candidate) => IdPattern.IsMatch(candidate) ? candidate : null;
}
