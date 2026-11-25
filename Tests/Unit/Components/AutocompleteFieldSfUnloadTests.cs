using System.Reflection;
using MyVocaList.Contracts.Models;
using MyVocaList.UI.Components.TextFields;
using MyVocaList.UI.Models;

namespace MyVocaList.Tests.Unit.Components;

/// <summary>
/// B13 (minor): a pending autocomplete search must not outlive the field. When the field is unloaded, its search
/// is cancelled, so it cannot open a menu on a page that is no longer shown.
/// </summary>
public class AutocompleteFieldSfUnloadTests
{
    // Regression: B13 — unloading the field cancels its pending search.
    [Fact]
    public async Task Unloaded_CancelsThePendingSearch()
    {
        var calls = 0;
        var field = new AutocompleteFieldSf(skipXamlInitForTests: true)
        {
            OptionSource = new MenuOptionSource((_, _) =>
            {
                calls++;
                return Task.FromResult<IReadOnlyList<AutocompleteSuggestion>>([]);
            }),
        };
        var filter = (AutocompleteFilterSf)typeof(AutocompleteFieldSf)
            .GetField("_filter", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(field)!;

        var pending = filter.FilterAsync("jo");
        field.OnUnloaded(null, EventArgs.Empty);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        Assert.Equal(0, calls);
    }
}
