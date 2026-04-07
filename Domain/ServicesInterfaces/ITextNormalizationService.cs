namespace MyVocaList.Domain.ServicesInterfaces
{
    /// <summary>Interface for multilingual text normalization.</summary>
    public interface ITextNormalizationService
    {
        /// <summary>Normalizes name by removing accents and special characters.</summary>
        string NormalizeName(string name);

        /// <summary>Detects if text contains Arabic characters (RTL).</summary>
        bool ContainsArabicText(string text);

        /// <summary>Detects if text contains Asian characters (CJK).</summary>
        bool ContainsAsianText(string text);

        /// <summary>Removes special characters keeping letters, numbers and spaces.</summary>
        string SanitizeInput(string input);

        /// <summary>Normalizes search input for optimized search.</summary>
        string NormalizeSearchTerm(string searchTerm);
    }
}
