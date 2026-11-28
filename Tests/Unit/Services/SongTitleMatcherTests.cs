using MyVocaList.Services;

namespace MyVocaList.Tests.Unit.Services;

/// <summary>Typo-tolerant word matching and local re-rank of iTunes title rows (REQ-ITS-15, REQ-ITS-16). Pure logic, no I/O.</summary>
public class SongTitleMatcherTests
{
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
    public void Rerank_DontCrie_PutsDontCryFirstDespiteItunesOrder()
    {
        // [AC] REQ-ITS-16: "Don't crie" ranks "Don't Cry" above "Dance Floor" (iTunes order had it second)
        var titles = new[] { "Dance Floor", "Don't Cry" };

        var ranked = SongTitleMatcher.RerankByCoverage("Don't crie", titles, t => t);

        Assert.Equal(new[] { "Don't Cry", "Dance Floor" }, ranked);
    }

    [Fact]
    public void Rerank_CoveringRowFromArtistText_MovesAbovePartialTopHit()
    {
        // [AC] REQ-ITS-16: "gun" covers the artist "Guns N' Roses" (title+artist text); a partial top hit stays below it
        var rows = new[] { ("Cry", "Benson Boone"), ("Don't Cry Out", "Shiny Toy Guns") };

        var ranked = SongTitleMatcher.RerankByCoverage("don't cry gun", rows, r => $"{r.Item1} {r.Item2}");

        Assert.Equal(("Don't Cry Out", "Shiny Toy Guns"), ranked[0]);
    }

    [Fact]
    public void Rerank_PartialRowNeverMovesAboveCoveringRow()
    {
        // [AC] REQ-ITS-16: a partial match (Kirk-style "don't cry" without "gun") keeps its place below covering rows
        var rows = new[] { "Don't Cry (Live)", "Dance Floor", "Don't Cry Out Guns" };

        var ranked = SongTitleMatcher.RerankByCoverage("don't cry gun", rows, t => t);

        Assert.Equal(new[] { "Don't Cry Out Guns", "Don't Cry (Live)", "Dance Floor" }, ranked);
    }

    [Fact]
    public void Rerank_TiesKeepItunesOrder()
    {
        // [AC] REQ-ITS-16: equal scores keep the popularity order from iTunes
        var titles = new[] { "Don't Cry (Live)", "Don't Cry" };

        var ranked = SongTitleMatcher.RerankByCoverage("Don't Cry", titles, t => t);

        Assert.Equal(new[] { "Don't Cry (Live)", "Don't Cry" }, ranked);
    }

    [Fact]
    public void Rerank_NeverAddsOrDropsRows()
    {
        // [AC] REQ-ITS-15: the local step only re-orders the rows iTunes returned
        var titles = new[] { "A", "Bb", "Ccc" };

        var ranked = SongTitleMatcher.RerankByCoverage("zzzz", titles, t => t);

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
