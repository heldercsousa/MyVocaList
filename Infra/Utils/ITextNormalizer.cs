using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace MyVocaList.Infra.Utils
{
    /// <summary>
    /// Interface for multilingual text normalization
    /// </summary>
    public interface ITextNormalizer
    {
        /// <summary>
        /// Normalizes name by removing accents and special characters
        /// </summary>
        string NormalizeName(string name);

        /// <summary>
        /// Detects if text contains Arabic characters (RTL)
        /// </summary>
        bool ContainsArabicText(string text);

        /// <summary>
        /// Detects if text contains Asian characters (CJK)
        /// </summary>
        bool ContainsAsianText(string text);

        /// <summary>
        /// Removes special characters from input while keeping letters, numbers, and spaces
        /// </summary>
        string SanitizeInput(string input);

        /// <summary>
        /// Normalizes search input for optimized search
        /// </summary>
        string NormalizeSearchTerm(string searchTerm);
    }
}
