using MyVocaList.Domain.Constants;

namespace MyVocaList.Tests.Unit.Domain;

public class SearchConstantsTests
{
    [Fact]
    // [AC] REQ-ACF-14: every autocomplete field shares one 300 ms debounce delay.
    public void AutocompleteDebounceMilliseconds_Is300()
        => Assert.Equal(300, SearchConstants.AutocompleteDebounceMilliseconds);

    [Fact]
    // [AC] REQ-SFT-08: a remote search call is bounded at 3,000 ms.
    public void RemoteSearchTimeoutMilliseconds_Is3000()
        => Assert.Equal(3000, SearchConstants.RemoteSearchTimeoutMilliseconds);
}
