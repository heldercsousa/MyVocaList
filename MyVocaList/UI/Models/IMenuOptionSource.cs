using MyVocaList.Contracts.Models;

namespace MyVocaList.UI.Models;

/// <summary>
/// Supplies autocomplete suggestions for a query. See `design.md § 3.2`.
/// </summary>
public interface IMenuOptionSource
{
    /// <summary>Returns THIS query's options (BUG-056: never read back from an observable).</summary>
    Task<IReadOnlyList<AutocompleteSuggestion>> SearchAsync(string query, CancellationToken ct);
}

/// <summary>
/// Adapts a delegate to <see cref="IMenuOptionSource"/>.
/// </summary>
/// <param name="search">The query function backing <see cref="SearchAsync"/>.</param>
public sealed class MenuOptionSource(Func<string, CancellationToken, Task<IReadOnlyList<AutocompleteSuggestion>>> search)
    : IMenuOptionSource
{
    /// <inheritdoc />
    public Task<IReadOnlyList<AutocompleteSuggestion>> SearchAsync(string query, CancellationToken ct) => search(query, ct);
}
