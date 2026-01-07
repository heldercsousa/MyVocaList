using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace MyVocaList.Services
{
    /// <summary>
    /// Centralized service for multilingual text normalization
    /// Supports the 11 application languages
    /// </summary>
    public class TextNormalizationService : ITextNormalizationService
    {
        // Accent mapping for normalization (no duplicates)
        private static readonly Dictionary<char, string> AccentMap = new()
        {
            // === VOWELS WITH ACCENTS ===
            // A
            {'á', "a"}, {'à', "a"}, {'ã', "a"}, {'â', "a"}, {'ä', "a"}, {'ā', "a"}, {'ă', "a"}, {'ą', "a"}, {'å', "a"},

            // E
            {'é', "e"}, {'è', "e"}, {'ê', "e"}, {'ë', "e"}, {'ē', "e"}, {'ĕ', "e"}, {'ę', "e"}, {'ě', "e"},

            // I
            {'í', "i"}, {'ì', "i"}, {'î', "i"}, {'ï', "i"}, {'ī', "i"}, {'ĭ', "i"}, {'į', "i"}, {'ı', "i"},

            // O
            {'ó', "o"}, {'ò', "o"}, {'õ', "o"}, {'ô', "o"}, {'ö', "o"}, {'ō', "o"}, {'ŏ', "o"}, {'ø', "o"},

            // U
            {'ú', "u"}, {'ù', "u"}, {'û', "u"}, {'ü', "u"}, {'ū', "u"}, {'ŭ', "u"}, {'ų', "u"}, {'ů', "u"},

            // === SPECIAL CONSONANTS ===
            {'ç', "c"}, {'ć', "c"}, {'č', "c"},
            {'ñ', "n"}, {'ń', "n"}, {'ň', "n"},
            {'ł', "l"}, {'ľ', "l"},
            {'ś', "s"}, {'š', "s"}, {'ş', "s"},
            {'ź', "z"}, {'ż', "z"}, {'ž', "z"},
            {'ř', "r"}, {'ŕ', "r"},
            {'ť', "t"}, {'ď', "d"}, {'đ', "d"},
            {'ğ', "g"}, {'ý', "y"},

            // === SPECIAL CHARACTERS ===
            {'ß', "ss"},    // German
            {'œ', "oe"},    // French
            {'æ', "ae"},    // French/Nordic
            {'þ', "th"},    // Icelandic
            {'ð', "d"}      // Icelandic
        };

        public string NormalizeName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            // Convert to lowercase first
            name = name.ToLowerInvariant();

            var result = new StringBuilder();

            foreach (char c in name)
            {
                if (AccentMap.TryGetValue(c, out string replacement))
                {
                    result.Append(replacement);
                }
                else if (char.IsLetter(c) || char.IsDigit(c) || char.IsWhiteSpace(c))
                {
                    // Keep letters, numbers and spaces (including non-Latin scripts)
                    result.Append(c);
                }
                // Remove other special characters
            }

            // Remove extra spaces and normalize
            return string.Join(" ", result.ToString()
                .Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
        }

        public bool ContainsArabicText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            // Arabic Unicode range: U+0600 to U+06FF
            return Regex.IsMatch(text, @"[\u0600-\u06FF]");
        }

        public bool ContainsAsianText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            // CJK (Chinese, Japanese, Korean) Unicode ranges
            return Regex.IsMatch(text, @"[\u4E00-\u9FFF\u3040-\u309F\u30A0-\u30FF\uAC00-\uD7AF]");
        }

        public string SanitizeInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            var result = new StringBuilder();

            foreach (char c in input)
            {
                // Keep letters (including accented), numbers, spaces and some basic symbols
                if (char.IsLetter(c) || char.IsDigit(c) || char.IsWhiteSpace(c) ||
                    c == '.' || c == '@' || c == '-' || c == '_' || c == '/')
                {
                    result.Append(c);
                }
            }

            return result.ToString().Trim();
        }

        public string NormalizeSearchTerm(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return string.Empty;

            // Sanitize first, then normalize
            var sanitized = SanitizeInput(searchTerm);
            return NormalizeName(sanitized);
        }
    }
}
