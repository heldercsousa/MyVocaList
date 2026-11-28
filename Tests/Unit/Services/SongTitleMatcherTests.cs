using MyVocaList.Services;

namespace MyVocaList.Tests.Unit.Services;

/// <summary>Typo-tolerant word matching and the scored re-rank of iTunes title rows (REQ-ITS-15, REQ-ITS-16). Pure logic, no I/O.</summary>
public class SongTitleMatcherTests
{
    private static IReadOnlyList<(string Title, string Artist)> Rank(string query, params (string Title, string Artist)[] rows) =>
        SongTitleMatcher.RankRows(query, rows, r => r.Title, r => r.Artist);

    [Fact]
    public void WordsMatch_CrieAndCry_MatchWithinTwoEdits()
    {
        // [AC] REQ-ITS-15: "crie" matches "cry" (distance 2, lengths 4 and 3)
        Assert.True(SongTitleMatcher.WordsMatch("crie", "cry"));
    }

    [Fact]
    public void WordsMatch_ShortWords_RequireExactMatch()
    {
        // [AC] REQ-ITS-15: two-letter words never match by edit distance
        Assert.False(SongTitleMatcher.WordsMatch("of", "on"));
        Assert.True(SongTitleMatcher.WordsMatch("of", "of"));
    }

    [Fact]
    public void WordsMatch_ApostropheIgnored()
    {
        // [AC] REQ-ITS-15: "don't" and "dont" are the same word
        Assert.True(SongTitleMatcher.WordsMatch("don't", "dont"));
    }

    [Fact]
    public void WordsMatch_Unrelated_DoesNotMatch()
    {
        // [AC] REQ-ITS-15: distance above 2 is no match
        Assert.False(SongTitleMatcher.WordsMatch("dance", "floor"));
    }

    [Fact]
    public void RankRows_DontCrie_BareDontCryAboveDanceFloor()
    {
        // [AC] REQ-ITS-16: "Don't crie" ranks the bare "Don't Cry" above "Dance Floor" (iTunes order had it first)
        var ranked = Rank("Don't crie", ("Dance Floor", "Band A"), ("Don't Cry", "Guns N' Roses"));

        Assert.Equal("Don't Cry", ranked[0].Title);
    }

    [Fact]
    public void RankRows_ArtistWordCoversQuery_CoveringRowMovesAboveTopHit()
    {
        // [AC] REQ-ITS-16: "gun" covers the artist "Shiny Toy Guns" (prefix match), so it outranks the partial top hit
        var ranked = Rank("don't cry gun", ("Cry", "Benson Boone"), ("Don't Cry Out", "Shiny Toy Guns"));

        Assert.Equal(("Don't Cry Out", "Shiny Toy Guns"), ranked[0]);
    }

    [Fact]
    public void RankRows_TitleFit_RowWithUnmatchedTitleWordsRanksBelow()
    {
        // [AC] REQ-ITS-16: "Don't Cry Out" has an unmatched title word ("Out"); bare "Don't Cry" ranks above it
        var ranked = Rank("don't cry gun", ("Don't Cry Out", "Shiny Toy Guns"), ("Don't Cry", "Guns N' Roses"));

        Assert.Equal(("Don't Cry", "Guns N' Roses"), ranked[0]);
    }

    [Fact]
    public void RankRows_BracketedSuffix_SortsLastEvenWhenTitleFitIsEqual()
    {
        // [AC] REQ-ITS-16: "(Live)" is ignored for title fit but the bare title stays above it
        var ranked = Rank("Don't Cry", ("Don't Cry (Live)", "Guns N' Roses"), ("Don't Cry", "J Dilla"));

        Assert.Equal(("Don't Cry", "J Dilla"), ranked[0]);
    }

    [Fact]
    public void RankRows_TiesKeepItunesOrder()
    {
        // [AC] REQ-ITS-16: equal keys keep the popularity order from iTunes
        var ranked = Rank("Don't Cry", ("Don't Cry", "First"), ("Don't Cry", "Second"));

        Assert.Equal(("Don't Cry", "First"), ranked[0]);
    }

    [Fact]
    public void RankRows_NeverAddsOrDropsRows()
    {
        // [AC] REQ-ITS-15: the local step only re-orders the rows iTunes returned
        var ranked = Rank("zzzz", ("A", "x"), ("Bb", "y"), ("Ccc", "z"));

        Assert.Equal(3, ranked.Count);
    }

    [Fact]
    public void Dropped_last_word_query_is_built_only_for_multi_word_queries()
    {
        // [AC] REQ-ITS-14: retry query drops the last word; a single word has no retry
        Assert.Equal("Don't", SongTitleMatcher.DropLastWord("Don't crie"));
        Assert.Null(SongTitleMatcher.DropLastWord("Creep"));
    }
}
