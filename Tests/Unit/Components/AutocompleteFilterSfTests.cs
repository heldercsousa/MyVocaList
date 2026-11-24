using MyVocaList.Contracts.Models;
using MyVocaList.UI.Components.TextFields;

namespace MyVocaList.Tests.Unit.Components;

public class AutocompleteFilterSfTests
{
    private static readonly AutocompleteSuggestion First = new("Johnny Cash", "JC", null!);
    private static readonly AutocompleteSuggestion Second = new("John Denver", "JD", null!);

    // [AC] REQ-UIMIG-01: the Sf menu shows the same items in the same order as the DX original (the search's own result).
    [Fact]
    public async Task FilterAsync_SearchReturnsOptions_ReturnsThemInTheSameOrder()
    {
        IReadOnlyList<AutocompleteSuggestion> options = [First, Second];
        var filter = new AutocompleteFilterSf((_, _) => Task.FromResult<IReadOnlyList<AutocompleteSuggestion>?>(options));

        var result = await filter.FilterAsync("jo");

        Assert.Equal(options, Assert.IsAssignableFrom<IEnumerable<AutocompleteSuggestion>>(result));
    }

    // [AC] REQ-UIMIG-01: the Sf search runs for the text the user typed, as the DX original does.
    [Fact]
    public async Task FilterAsync_PassesTheTypedTextToTheSearch()
    {
        string? searched = null;
        var filter = new AutocompleteFilterSf((text, _) =>
        {
            searched = text;
            return Task.FromResult<IReadOnlyList<AutocompleteSuggestion>?>([First]);
        });

        await filter.FilterAsync("joh");

        Assert.Equal("joh", searched);
    }

    // [AC] REQ-ACF-31: a search that returns no options (null) keeps the menu closed (no "Not found" row).
    [Fact]
    public async Task FilterAsync_SearchReturnsNull_ReturnsAnEmptyList()
    {
        var filter = new AutocompleteFilterSf((_, _) => Task.FromResult<IReadOnlyList<AutocompleteSuggestion>?>(null));

        var result = await filter.FilterAsync("zz");

        Assert.Empty(Assert.IsAssignableFrom<IEnumerable<AutocompleteSuggestion>>(result));
    }

    // [AC] REQ-UIMIG-01: only the latest query's options are shown; a superseded search answers nothing.
    [Fact]
    public async Task FilterAsync_NewerQuery_CancelsThePreviousSearch()
    {
        var pending = new TaskCompletionSource<IReadOnlyList<AutocompleteSuggestion>?>();
        var calls = 0;
        var filter = new AutocompleteFilterSf(async (_, token) =>
        {
            if (++calls == 1)
            {
                using var registration = token.Register(() => pending.TrySetCanceled(token));
                return await pending.Task;
            }

            return [Second];
        });

        var older = filter.FilterAsync("jo");
        var newer = await filter.FilterAsync("john");

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => older);
        Assert.Equal(new[] { Second }, Assert.IsAssignableFrom<IEnumerable<AutocompleteSuggestion>>(newer));
    }

    // [AC] REQ-UIMIG-01: a search cancelled by the field (pick, blur, text written in code) answers nothing.
    [Fact]
    public async Task Cancel_InFlightSearch_ThrowsOperationCanceled()
    {
        var pending = new TaskCompletionSource<IReadOnlyList<AutocompleteSuggestion>?>();
        var filter = new AutocompleteFilterSf(async (_, token) =>
        {
            using var registration = token.Register(() => pending.TrySetCanceled(token));
            return await pending.Task;
        });

        var search = filter.FilterAsync("jo");
        filter.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => search);
    }

    // [AC] REQ-UIMIG-01: a search that ignores its token but finishes after a cancel still answers nothing.
    [Fact]
    public async Task FilterAsync_CancelledWhileSearchIgnoresToken_ThrowsOperationCanceled()
    {
        var pending = new TaskCompletionSource<IReadOnlyList<AutocompleteSuggestion>?>();
        var filter = new AutocompleteFilterSf((_, _) => pending.Task);

        var search = filter.FilterAsync("jo");
        filter.Cancel();
        pending.SetResult([First]);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => search);
    }

    // [AC] REQ-UIMIG-01: the Sf menu pre-selects no row, as the DX original (no highlight, no auto-appended text).
    [Fact]
    public void GetHighlightIndex_ReturnsNoHighlight()
    {
        var filter = new AutocompleteFilterSf((_, _) => Task.FromResult<IReadOnlyList<AutocompleteSuggestion>?>(null));

        var index = filter.GetHighlightIndex(null!, null!);

        Assert.Equal(AutocompleteFilterSf.NoHighlight, index);
        Assert.True(index < 0);
    }
}
