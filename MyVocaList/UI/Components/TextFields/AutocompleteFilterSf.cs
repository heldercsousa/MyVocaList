using MyVocaList.Contracts.Models;
using Syncfusion.Maui.Inputs;

namespace MyVocaList.UI.Components.TextFields;

/// <summary>
/// The <c>SfAutocomplete</c> filter and search behaviour of <see cref="AutocompleteFieldSf"/>. It replaces the
/// built-in filter, so the menu shows exactly what the field's search returns, in the same order (REQ-ACF-16,
/// -22; BUG-056), and it disables the built-in highlight, so the first row is never pre-selected and the editor
/// never auto-appends the rest of a suggestion. The search itself (decision rules, debounce, option source)
/// stays in the field; this class only runs it per keystroke and cancels the superseded one.
/// </summary>
/// <remarks>
/// Facts from the <c>Syncfusion.Maui.Inputs</c> 35.2.12 IL (T3.3), which this class relies on:
/// <list type="bullet">
///   <item>The control awaits <see cref="GetMatchingItemsAsync"/> inside a catch-all. A cancelled or faulted
///   task changes nothing, so a superseded query is cancelled, never answered, and a failed search is caught
///   here (logged, no options) instead of reaching the control.</item>
///   <item>An empty result with an empty <c>NoResultsFoundText</c> closes the drop-down (no "No Results Found"
///   row, REQ-ACF-31).</item>
///   <item>A highlight index below 0 selects no row and skips the auto-append.</item>
/// </list>
/// Every await captures the UI synchronization context (no <c>ConfigureAwait(false)</c>): the control calls the
/// filter from its text-changed handler and writes the result to its list on the continuation.
/// </remarks>
/// <param name="search">
/// The field's search for the typed text: the options to show, or <c>null</c> for none. It throws
/// <see cref="OperationCanceledException"/> when its token is cancelled.
/// </param>
/// <param name="onSearchFailed">
/// Logs a search that failed with anything but a cancellation. The control would swallow the fault silently.
/// </param>
internal sealed class AutocompleteFilterSf(
    Func<string?, CancellationToken, Task<IReadOnlyList<AutocompleteSuggestion>?>> search,
    Action<Exception>? onSearchFailed = null)
    : IAutocompleteFilterBehavior, IAutocompleteSearchBehavior
{
    /// <summary>The highlight index that selects no row (and so never auto-appends text).</summary>
    internal const int NoHighlight = -1;

    private static readonly IReadOnlyList<AutocompleteSuggestion> NoOptions = [];

    // The latest search only. Cancelled by the next keystroke and by Cancel() (pick, blur, programmatic text).
    private CancellationTokenSource? _cts;

    /// <inheritdoc />
    public Task<object> GetMatchingItemsAsync(SfAutocomplete source, AutocompleteFilterInfo filterInfo) =>
        FilterAsync(filterInfo?.Text);

    /// <inheritdoc />
    public int GetHighlightIndex(SfAutocomplete source, AutocompleteSearchInfo searchInfo) => NoHighlight;

    /// <summary>
    /// Runs the search for <paramref name="text"/> after cancelling the previous one. Returns the options
    /// (never <c>null</c>: no options is an empty list, also after a failed search, which is reported to
    /// <c>onSearchFailed</c>). Throws <see cref="OperationCanceledException"/> when this search was superseded
    /// or cancelled.
    /// </summary>
    internal async Task<object> FilterAsync(string? text)
    {
        Cancel();

        var cts = new CancellationTokenSource();
        _cts = cts;
        try
        {
            var options = await search(text, cts.Token);

            // No await between this check and the return, so a cancel that landed during the search is seen here.
            cts.Token.ThrowIfCancellationRequested();
            return options ?? NoOptions;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Error recovery with logging (code-style § Exception Handling, pattern 4): the control would swallow the
            // fault and keep the previous query's options; log it and show none (REQ-ACF-31). A superseded search
            // still answers nothing, so it cannot close the newer query's menu.
            onSearchFailed?.Invoke(ex);
            cts.Token.ThrowIfCancellationRequested();
            return NoOptions;
        }
        finally
        {
            // This run owns cts: release it, and forget the reference if it is still the latest search.
            if (ReferenceEquals(_cts, cts))
                _cts = null;

            cts.Dispose();
        }
    }

    /// <summary>Cancels the pending or in-flight search, if any.</summary>
    internal void Cancel()
    {
        _cts?.Cancel();
        _cts = null;
    }
}
