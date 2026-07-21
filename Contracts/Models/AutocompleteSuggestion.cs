namespace MyVocaList.Contracts.Models;

/// <summary>
/// A single result item surfaced by <c>AutocompleteField</c>.
/// </summary>
/// <param name="Headline">Primary display text (e.g. person's full name).</param>
/// <param name="SupportingText">Optional secondary line (e.g. email or birthday). Null or empty = 1-line row.</param>
/// <param name="Data">The original entity object. The caller casts this in <c>SuggestionSelectedCommand</c>.</param>
public record AutocompleteSuggestion(string Headline, string SupportingText, object Data)
{
    /// <summary>
    /// True when this suggestion is the synthetic "create new" sentinel row appended to the
    /// results list (e.g. "Create '{typed text}'"), rather than a real entity match.
    /// The raw typed text is carried in <see cref="Headline"/>.
    /// </summary>
    public bool IsCreateNew { get; init; }
}
