using System.Text.RegularExpressions;

namespace MyVocaList.Domain.Rules;

/// <summary>
/// REQ-MSRC-12: cleans the title of a YouTube karaoke video before it is used as the MusicBrainz query.
/// The cleaning is heuristic: it removes karaoke words (free or inside brackets), bracket groups holding them,
/// and the channel name, then tidies separators. A bracket group with other content, e.g. "(Ao Vivo)", stays.
/// </summary>
public static class KaraokeTitleRules
{
    private const RegexOptions Options = RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;

    private static readonly Regex BracketGroup = new(@"[\(\[\{][^\)\]\}]*[\)\]\}]", Options);

    // Words that mark a whole bracket group as noise.
    private static readonly Regex BracketNoise = new(
        @"(?<![\p{L}\p{N}])(?:karaok[eê]|instrumental|lyrics?|letras?|cover|upgraded|playback|backing\s+track|sing\s*along|no\s+vocals?|official|video|audio|remaster(?:ed)?|clip|hd|4k|version|vers[aã]o)(?![\p{L}\p{N}])",
        Options);

    // Words removed when free in the text. "cover" and a bare "version" are left alone: they can belong to a song title.
    private static readonly Regex FreeNoise = new(
        @"(?<![\p{L}\p{N}])(?:karaok[eê](?:\s+(?:version|vers[aã]o))?|instrumental|lyrics?|upgraded|playback|backing\s+track|sing\s*along|no\s+vocals?)(?![\p{L}\p{N}])",
        Options);

    private static readonly Regex DashBetweenWords = new(@"\s[–—]\s", RegexOptions.Compiled);
    private static readonly Regex Spaces = new(@"\s+", RegexOptions.Compiled);
    private static readonly Regex DoubledDash = new(@"(?:\s[-–—])+\s[-–—]\s", RegexOptions.Compiled);
    private static readonly char[] EdgeSeparators = [' ', '-', '–', '—', '|', '•', ':', '/', '·', ','];

    /// <summary>Returns the cleaned title, or an empty string when nothing is left. Never null.</summary>
    public static string Clean(string? title, string? channelName)
    {
        if (string.IsNullOrWhiteSpace(title)) return string.Empty;

        // 1. Bracket groups holding a noise word go whole; other groups stay.
        var text = BracketGroup.Replace(title, m => BracketNoise.IsMatch(m.Value) ? " " : m.Value);

        // 2. The channel name goes, with its separators.
        if (!string.IsNullOrWhiteSpace(channelName))
            text = Regex.Replace(text, Regex.Escape(channelName.Trim()), " ", Options);

        // 3. Free noise words go.
        text = FreeNoise.Replace(text, " ");

        // Typographic dashes between words are plain separators.
        text = DashBetweenWords.Replace(text, " - ");

        // 4. Split on bar-like separators, drop empty segments, tidy each segment's edges.
        var segments = text.Split(['|', '•'])
            .Select(s => Spaces.Replace(s, " ").Trim(EdgeSeparators))
            .Where(s => s.Length > 0);
        var joined = string.Join(" - ", segments);

        // 5. A dash left doubled by the removals collapses to one.
        joined = DoubledDash.Replace(joined, " - ");
        return Spaces.Replace(joined, " ").Trim(EdgeSeparators);
    }
}
