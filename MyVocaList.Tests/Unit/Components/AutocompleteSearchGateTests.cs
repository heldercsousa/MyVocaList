using MyVocaList.UI.Components.AutocompleteField;

namespace MyVocaList.Tests.Unit.Components;

public class AutocompleteSearchGateTests
{
    [Fact]
    // [AC] AC-4: a 2+ character query drives the search (the phone Search View must yield
    // suggestions — BUG-043 returned zero because the search never fired).
    public void ShouldTriggerSearch_TwoCharacters_ReturnsTrue()
    {
        Assert.True(AutocompleteSearchGate.ShouldTriggerSearch("ab"));
    }

    [Fact]
    // [AC] AC-4: a longer query also drives the search.
    public void ShouldTriggerSearch_ThreeCharacters_ReturnsTrue()
    {
        Assert.True(AutocompleteSearchGate.ShouldTriggerSearch("abc"));
    }

    [Fact]
    // [AC] AC-10: a single character is below the debounce threshold — no search.
    public void ShouldTriggerSearch_SingleCharacter_ReturnsFalse()
    {
        Assert.False(AutocompleteSearchGate.ShouldTriggerSearch("a"));
    }

    [Fact]
    public void ShouldTriggerSearch_Empty_ReturnsFalse()
    {
        Assert.False(AutocompleteSearchGate.ShouldTriggerSearch(""));
    }

    [Fact]
    public void ShouldTriggerSearch_Null_ReturnsFalse()
    {
        Assert.False(AutocompleteSearchGate.ShouldTriggerSearch(null));
    }
}
