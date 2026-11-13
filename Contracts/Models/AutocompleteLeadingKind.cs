namespace MyVocaList.Contracts.Models;

/// <summary>
/// Which leading element an <see cref="AutocompleteSuggestion"/> option row shows.
/// </summary>
public enum AutocompleteLeadingKind
{
    /// <summary>Text only; no leading element.</summary>
    None,

    /// <summary>A 56 dp rounded-square image (e.g. a song cover).</summary>
    Image,

    /// <summary>A 40 dp circular avatar (e.g. an artist picture).</summary>
    Avatar,
}
