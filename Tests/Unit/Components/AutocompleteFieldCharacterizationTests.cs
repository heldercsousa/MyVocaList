using System.Diagnostics;
using System.Reflection;
using MyVocaList.Contracts.Models;
using MyVocaList.Domain.Constants;
using MyVocaList.UI.Components.TextFields;
using MyVocaList.UI.Models;

namespace MyVocaList.Tests.Unit.Components;

/// <summary>
/// Characterization tests that pin the current wait-then-search behaviour of <see cref="AutocompleteField"/>
/// (the Syncfusion autocomplete). They describe what the code does today, so a later refactor of the search
/// routine has a safety net. The search routine is private; it is reached through reflection on the
/// <c>SearchAsync</c> method, which is the one the control's filter calls on each keystroke.
/// </summary>
public class AutocompleteFieldCharacterizationTests
{
    private static readonly AutocompleteSuggestion First = new("Johnny Cash", "JC", null!);

    private static readonly MethodInfo SearchMethod = typeof(AutocompleteField).GetMethod(
        "SearchAsync", BindingFlags.NonPublic | BindingFlags.Instance)
        ?? throw new InvalidOperationException("AutocompleteField.SearchAsync not found");

    private static Task<IReadOnlyList<AutocompleteSuggestion>?> Search(AutocompleteField field, string? text, CancellationToken ct) =>
        (Task<IReadOnlyList<AutocompleteSuggestion>?>)SearchMethod.Invoke(field, [text, ct])!;

    // [AC] REQ-ACF-14: a qualifying query waits the shared debounce delay before the option source is asked.
    // Empty options are used on purpose: a non-empty menu measures row templates, which need the app's XAML resources.
    [Fact]
    public async Task Search_QualifyingQuery_WaitsTheDebounceDelayBeforeAskingTheSource()
    {
        var calls = 0;
        var field = new AutocompleteField(skipXamlInitForTests: true)
        {
            OptionSource = new MenuOptionSource((_, _) =>
            {
                calls++;
                return Task.FromResult<IReadOnlyList<AutocompleteSuggestion>>([]);
            }),
        };

        var clock = Stopwatch.StartNew();
        var result = await Search(field, "jo", CancellationToken.None);
        clock.Stop();

        Assert.Equal(1, calls);
        Assert.Null(result); // no options: the menu stays closed (REQ-ACF-31)
        Assert.True(clock.ElapsedMilliseconds >= SearchConstants.AutocompleteDebounceMilliseconds - 20,
            $"Expected at least the debounce delay, measured {clock.ElapsedMilliseconds} ms");
    }

    // [AC] REQ-ACF-13: a query shorter than the minimum never waits and never asks the option source.
    [Fact]
    public async Task Search_BelowMinimumLength_ReturnsNullWithoutAskingTheSource()
    {
        var calls = 0;
        var field = new AutocompleteField(skipXamlInitForTests: true)
        {
            OptionSource = new MenuOptionSource((_, _) =>
            {
                calls++;
                return Task.FromResult<IReadOnlyList<AutocompleteSuggestion>>([First]);
            }),
        };

        var result = await Search(field, "j", CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(0, calls);
    }

    // [AC] REQ-ACF-26: a keystroke that supersedes a waiting search cancels it; the superseded search answers nothing.
    [Fact]
    public async Task Search_CancelledDuringTheWait_ThrowsOperationCanceledAndDoesNotAskTheSource()
    {
        var calls = 0;
        var field = new AutocompleteField(skipXamlInitForTests: true)
        {
            OptionSource = new MenuOptionSource((_, _) =>
            {
                calls++;
                return Task.FromResult<IReadOnlyList<AutocompleteSuggestion>>([First]);
            }),
        };
        using var cts = new CancellationTokenSource();

        var pending = Search(field, "jo", cts.Token);
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        Assert.Equal(0, calls);
    }

    // [AC] REQ-ACF-31: the search for the field's text returns nothing when the option source has no options.
    [Fact]
    public async Task Search_SourceReturnsNoOptions_ReturnsNull()
    {
        var field = new AutocompleteField(skipXamlInitForTests: true)
        {
            OptionSource = new MenuOptionSource((_, _) => Task.FromResult<IReadOnlyList<AutocompleteSuggestion>>([])),
        };

        var result = await Search(field, "zz", CancellationToken.None);

        Assert.Null(result);
    }
}
