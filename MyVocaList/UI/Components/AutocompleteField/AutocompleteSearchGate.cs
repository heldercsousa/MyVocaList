namespace MyVocaList.UI.Components.AutocompleteField;

/// <summary>
/// Pure gate deciding whether a query is long enough to trigger a suggestion search — extracted
/// for unit testability (no MAUI runtime dependency), mirroring <see cref="AutocompleteWindowClass"/>.
/// </summary>
internal static class AutocompleteSearchGate
{
    /// <summary>Minimum query length before a search is issued.</summary>
    internal const int MinSearchLength = 2;

    internal static bool ShouldTriggerSearch(string text) =>
        !string.IsNullOrEmpty(text) && text.Length >= MinSearchLength;
}
