using MyVocaList.Contracts.DTOs;
using MyVocaList.Contracts.Enums;
using MyVocaList.Domain.Rules;

namespace MyVocaList.Tests.Unit.Domain;

public class LyricsOriginRulesTests
{
    public static TheoryData<LyricsLookupResult?> NoAnswerOrNoLookup()
        => new() { LyricsLookupResult.NoAnswer, (LyricsLookupResult?)null };

    public static TheoryData<LyricsOrigin?> AllOrigins()
        => new() { (LyricsOrigin?)null, LyricsOrigin.LyricsSource, LyricsOrigin.User, LyricsOrigin.NoneFound };

    public static TheoryData<string?, string?, LyricsOrigin?> UnchangedLyricsCases()
    {
        var data = new TheoryData<string?, string?, LyricsOrigin?>();
        var pairs = new (string? Stored, string? Saved)[]
        {
            ("A", "A"), ("A", " A "), (null, ""), ("", " "), (" ", null),
        };
        foreach (var (stored, saved) in pairs)
        {
            foreach (var origin in new LyricsOrigin?[] { null, LyricsOrigin.LyricsSource, LyricsOrigin.User, LyricsOrigin.NoneFound })
            {
                data.Add(stored, saved, origin);
            }
        }
        return data;
    }

    // [AC] REQ-SLD-26: Lyrics equal to the pick lookup's Found text give the origin lyrics source.
    [Fact]
    public void OnCreate_LyricsEqualFoundPickLookup_LyricsSource()
    {
        var origin = LyricsOriginRules.OnCreate("A", LyricsLookupResult.Found("A"));

        Assert.Equal(LyricsOrigin.LyricsSource, origin);
    }

    // [AC] REQ-SLD-26: Non-blank lyrics that differ from the pick lookup's Found text give the origin user.
    [Fact]
    public void OnCreate_LyricsDifferFromPickLookup_User()
    {
        var origin = LyricsOriginRules.OnCreate("B", LyricsLookupResult.Found("A"));

        Assert.Equal(LyricsOrigin.User, origin);
    }

    // [AC] REQ-SLD-26: Non-blank lyrics without a pick lookup give the origin user.
    [Fact]
    public void OnCreate_LyricsWithoutPickLookup_User()
    {
        var origin = LyricsOriginRules.OnCreate("B", null);

        Assert.Equal(LyricsOrigin.User, origin);
    }

    // [AC] REQ-SLD-26: Blank lyrics with a definite no match from the pick lookup give the origin none found.
    [Fact]
    public void OnCreate_BlankLyricsAndNoMatch_NoneFound()
    {
        var origin = LyricsOriginRules.OnCreate("  ", LyricsLookupResult.NoMatch);

        Assert.Equal(LyricsOrigin.NoneFound, origin);
    }

    // [AC] REQ-SLD-26: Blank lyrics with no answer or no pick lookup leave the origin not recorded.
    [Theory]
    [MemberData(nameof(NoAnswerOrNoLookup))]
    public void OnCreate_BlankLyricsAndNoAnswerOrNoLookup_Null(LyricsLookupResult? pickLookup)
    {
        var origin = LyricsOriginRules.OnCreate(null, pickLookup);

        Assert.Null(origin);
    }

    // [AC] REQ-SLD-26: Non-blank lyrics with a no match give the origin user, because rule 2 wins over rule 3.
    [Fact]
    public void OnCreate_NonBlankLyricsAndNoMatch_User()
    {
        var origin = LyricsOriginRules.OnCreate("B", LyricsLookupResult.NoMatch);

        Assert.Equal(LyricsOrigin.User, origin);
    }

    // [AC] REQ-SLD-27: Changed lyrics give the origin user, whatever the stored origin was.
    [Theory]
    [MemberData(nameof(AllOrigins))]
    public void OnUpdate_LyricsChanged_User(LyricsOrigin? storedOrigin)
    {
        var origin = LyricsOriginRules.OnUpdate("A", storedOrigin, "B");

        Assert.Equal(LyricsOrigin.User, origin);
    }

    // [AC] REQ-SLD-27: Emptied lyrics give the origin user; an emptied field is the user's choice.
    [Fact]
    public void OnUpdate_LyricsEmptied_User()
    {
        var origin = LyricsOriginRules.OnUpdate("A", LyricsOrigin.LyricsSource, "");

        Assert.Equal(LyricsOrigin.User, origin);
    }

    // [AC] REQ-SLD-27: Unchanged lyrics (blank equals blank, trimmed compare) keep the stored origin.
    [Theory]
    [MemberData(nameof(UnchangedLyricsCases))]
    public void OnUpdate_LyricsUnchanged_KeepsOrigin(string? stored, string? saved, LyricsOrigin? storedOrigin)
    {
        var origin = LyricsOriginRules.OnUpdate(stored, storedOrigin, saved);

        Assert.Equal(storedOrigin, origin);
    }

    // [AC] REQ-SLD-19: A Deezer song with blank lyrics and no recorded origin needs a lookup on open.
    [Fact]
    public void NeedsLookupOnOpen_DeezerBlankLyricsNoOrigin_True()
    {
        var needs = LyricsOriginRules.NeedsLookupOnOpen("Deezer", "123", "  ", null);

        Assert.True(needs);
    }

    // [AC] REQ-SLD-24: No lookup on open for a local-only song, a song with lyrics, or a song with an origin.
    [Theory]
    [InlineData(null, null, null, null)]
    [InlineData("Deezer", "123", "Some lyrics", null)]
    [InlineData("Deezer", "123", null, LyricsOrigin.User)]
    [InlineData("Deezer", "123", "", LyricsOrigin.NoneFound)]
    [InlineData("Deezer", "123", " ", LyricsOrigin.LyricsSource)]
    public void NeedsLookupOnOpen_LocalOnlyOrHasLyricsOrHasOrigin_False(
        string? provider, string? id, string? lyrics, LyricsOrigin? origin)
    {
        var needs = LyricsOriginRules.NeedsLookupOnOpen(provider, id, lyrics, origin);

        Assert.False(needs);
    }
}
