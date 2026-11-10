using System.Text;

namespace MyVocaList.Tests.Unit.Services;

public class BuildInfoTests
{
    private const string Sha7 = "9f9907c";

    private const string DetailsText =
        "#branch\ndevelop\n#uncommitted\n M MyVocaList/a.cs\nR  old.cs -> new name.cs\n#commits\nmerge: BUG-085 — x\nfeat: 50% done; ok\n";

    private static MemoryStream Utf8(string text) => new(Encoding.UTF8.GetBytes(text));

    [Fact]
    public void ParseIdentity_WithSha_SplitsVersionAndShortSha()
    {
        // [AC] REQ-BID-01: version shown is the real MinVer version, SHA split off
        // [AC] REQ-BID-02 covered by ShortSha assertion below
        var id = BuildInfo.ParseIdentity("0.2.0-alpha.0.5+9f9907cd5fedc53e2f", null, TimeZoneInfo.Utc);

        Assert.Equal("0.2.0-alpha.0.5", id.Version);
        Assert.Equal(Sha7, id.ShortSha);
    }

    [Fact]
    public void ParseIdentity_NoPlus_EmptySha()
    {
        // [AC] REQ-BID-02: no SHA in the version -> empty short SHA
        var id = BuildInfo.ParseIdentity("0.2.0", null, TimeZoneInfo.Utc);

        Assert.Equal("0.2.0", id.Version);
        Assert.Equal("", id.ShortSha);
    }

    [Fact]
    public void ParseIdentity_UtcToLocal_Formats()
    {
        // [AC] REQ-BID-03: UTC build timestamp shown as local yyyy-MM-dd HH:mm
        var zone = TimeZoneInfo.CreateCustomTimeZone("t", TimeSpan.FromHours(-3), "t", "t");

        var id = BuildInfo.ParseIdentity("0.2.0+abc", "2026-11-09T21:16:00.0000000Z", zone);

        Assert.Equal("2026-11-09 18:16", id.BuiltAtLocal);
    }

    [Fact]
    public void ParseIdentity_NullInputs_ReturnEmptyStrings()
    {
        // [AC] REQ-BID-03: missing attributes never throw
        var id = BuildInfo.ParseIdentity(null, null, TimeZoneInfo.Utc);

        Assert.Equal("", id.Version);
        Assert.Equal("", id.ShortSha);
        Assert.Equal("", id.BuiltAtLocal);
    }

    [Fact]
    public void ReadDetails_Branch()
    {
        // [AC] REQ-BID-07: branch name is shown
        var d = BuildInfo.ReadDetails(Utf8(DetailsText), Sha7);

        Assert.Equal("develop", d!.Branch);
    }

    [Fact]
    public void ReadDetails_DetachedHead_ShowsShortSha()
    {
        // [AC] REQ-BID-07: detached HEAD displays as "detached @ <sha7>"
        var d = BuildInfo.ReadDetails(Utf8(DetailsText.Replace("develop", "HEAD")), Sha7);

        Assert.Equal("detached @ 9f9907c", d!.Branch);
    }

    [Fact]
    public void ReadDetails_PorcelainLines_TrimsStatusPrefix()
    {
        // [AC] REQ-BID-09: uncommitted paths listed with status prefix trimmed
        var d = BuildInfo.ReadDetails(Utf8(DetailsText), Sha7);

        Assert.Equal(new[] { "MyVocaList/a.cs", "old.cs -> new name.cs" }, d!.UncommittedPaths);
        Assert.True(d.IsDirty);
    }

    [Fact]
    public void ReadDetails_EmptyUncommitted_NotDirty()
    {
        // [AC] REQ-BID-09: empty #uncommitted section -> no warning
        var d = BuildInfo.ReadDetails(Utf8("#branch\ndevelop\n#uncommitted\n#commits\nfeat: a\n"), Sha7);

        Assert.False(d!.IsDirty);
        Assert.Empty(d.UncommittedPaths);
    }

    [Fact]
    public void ReadDetails_CommitsInOrder()
    {
        // [AC] REQ-BID-08: recent commit subjects in file order
        var d = BuildInfo.ReadDetails(Utf8(DetailsText), Sha7);

        Assert.Equal("merge: BUG-085 — x", d!.RecentCommits[0]);
        Assert.Equal("feat: 50% done; ok", d.RecentCommits[1]);
    }

    [Fact]
    public void ReadDetails_FewerThanTenCommits()
    {
        // [AC] REQ-BID-08: fewer than 10 subjects is tolerated
        var d = BuildInfo.ReadDetails(Utf8(DetailsText), Sha7);

        Assert.Equal(2, d!.RecentCommits.Count);
    }

    [Fact]
    public void ReadDetails_SubjectWithSemicolonAndPercent_Verbatim()
    {
        // [AC] REQ-BID-08: ';' and '%' in subjects shown verbatim
        var d = BuildInfo.ReadDetails(Utf8(DetailsText), Sha7);

        Assert.Contains("feat: 50% done; ok", d!.RecentCommits);
    }

    [Fact]
    public void ReadDetails_Crlf_SameResult()
    {
        // [AC] REQ-BID-08: CRLF input parses identically to LF
        var lf = BuildInfo.ReadDetails(Utf8(DetailsText), Sha7);
        var crlf = BuildInfo.ReadDetails(Utf8(DetailsText.Replace("\n", "\r\n")), Sha7);

        Assert.Equal(lf!.Branch, crlf!.Branch);
        Assert.Equal(lf.UncommittedPaths, crlf.UncommittedPaths);
        Assert.Equal(lf.RecentCommits, crlf.RecentCommits);
    }

    [Fact]
    public void ReadDetails_NullStream_ReturnsNull()
    {
        // [AC] REQ-BID-10: no embedded resource -> no details
        Assert.Null(BuildInfo.ReadDetails(null, Sha7));
    }

    [Fact]
    public void ReadDetails_EmptyStream_ReturnsNull()
    {
        // [AC] REQ-BID-10: empty resource -> no details
        Assert.Null(BuildInfo.ReadDetails(Utf8(""), Sha7));
    }

    [Fact]
    public void ToGroups_HidesEmptyGroups_LabelsWithCounts()
    {
        // [AC] REQ-BID-08: clean build shows only the Changes group with its count
        var clean = BuildInfo.ReadDetails(Utf8(DetailsText.Replace(" M MyVocaList/a.cs\nR  old.cs -> new name.cs\n", "")), Sha7);
        var cleanGroups = BuildInfo.ToGroups(clean, null);

        Assert.Single(cleanGroups);
        Assert.Equal("changes", cleanGroups[0].Key);
        Assert.Equal("Changes (2)", cleanGroups[0].Label);

        // [AC] REQ-BID-09: dirty build shows Uncommitted first
        var dirty = BuildInfo.ToGroups(BuildInfo.ReadDetails(Utf8(DetailsText), Sha7), null);

        Assert.Equal("uncommitted", dirty[0].Key);
        Assert.Equal("Uncommitted (2)", dirty[0].Label);
        Assert.Equal("changes", dirty[1].Key);
    }

    [Fact]
    public void DefaultGroupIndex_PrefersInProgressThenChanges()
    {
        // [AC] REQ-BID-08: default chip is In progress, else Changes
        var rows = new List<BuildRow>();
        var groups = new List<BuildGroup>
        {
            new("uncommitted", "Uncommitted (1)", rows),
            new("changes", "Changes (1)", rows),
            new("inprogress", "In progress (1)", rows),
        };

        Assert.Equal(2, BuildInfo.DefaultGroupIndex(groups));
        Assert.Equal(1, BuildInfo.DefaultGroupIndex(groups.Take(2).ToList()));
        Assert.Equal(0, BuildInfo.DefaultGroupIndex(groups.Take(1).ToList()));
    }

    [Fact]
    public void DefaultGroupIndex_EmptyList_ReturnsMinusOne()
    {
        // [AC] REQ-BID-10: no groups -> no selection
        Assert.Equal(-1, BuildInfo.DefaultGroupIndex(new List<BuildGroup>()));
    }

    [Fact]
    public void ReadStatus_InProgressWithGate()
    {
        // [AC] REQ-BID-11: in-progress item shows its title and gate
        var s = BuildInfo.ReadStatus(Utf8("P\tAlpha\tVerify on device\nD\tDone one\n"));

        Assert.Single(s!.InProgress);
        Assert.Equal("Alpha", s.InProgress[0].Title);
        Assert.Equal("Verify on device", s.InProgress[0].Gate);
    }

    [Fact]
    public void ReadStatus_EmptyGate()
    {
        // [AC] REQ-BID-11: in-progress item with an empty gate field keeps an empty gate
        var s = BuildInfo.ReadStatus(Utf8("P\tAlpha\t\n"));

        Assert.Single(s!.InProgress);
        Assert.Equal("", s.InProgress[0].Gate);
    }

    [Fact]
    public void ReadStatus_Done()
    {
        // [AC] REQ-BID-12: done-this-month titles are kept in file order
        var s = BuildInfo.ReadStatus(Utf8("D\tDone one\nD\tDone two\n"));

        Assert.Equal(new[] { "Done one", "Done two" }, s!.DoneThisMonth);
    }

    [Fact]
    public void ReadStatus_MalformedLines_Ignored()
    {
        // [AC] REQ-BID-13: lines that do not match P/D shapes are skipped, not fatal
        var s = BuildInfo.ReadStatus(Utf8("garbage line\nP\tOk\tg\nX\tnope\n"));

        Assert.Single(s!.InProgress);
        Assert.Equal("Ok", s.InProgress[0].Title);
        Assert.Empty(s.DoneThisMonth);
    }

    [Fact]
    public void ReadStatus_NullStream_ReturnsNull()
    {
        // [AC] REQ-BID-13: no status resource -> no status, nothing throws
        Assert.Null(BuildInfo.ReadStatus(null));
    }

    [Fact]
    public void ToGroups_WithStatus_AddsInProgressAndDone()
    {
        // [AC] REQ-BID-11: in-progress row's supporting text is its gate
        // [AC] REQ-BID-12: done rows appear under the Done group
        var status = BuildInfo.ReadStatus(Utf8("P\tAlpha\tVerify on device\nD\tDone one\n"));

        var groups = BuildInfo.ToGroups(null, status);

        Assert.Equal(new[] { "inprogress", "done" }, groups.Select(g => g.Key).ToArray());
        Assert.Equal("In progress (1)", groups[0].Label);
        Assert.Equal("Done (1)", groups[1].Label);
        Assert.Equal("Verify on device", groups[0].Rows[0].SupportingText);
        Assert.Equal("", groups[1].Rows[0].SupportingText);
    }

    [Fact]
    public void DefaultGroupIndex_InProgressPresent_SelectsIt()
    {
        // [AC] REQ-BID-11: with an in-progress group present it is the default chip
        var clean = BuildInfo.ReadDetails(Utf8("#branch\ndevelop\n#uncommitted\n#commits\nfeat: a\n"), Sha7);
        var status = BuildInfo.ReadStatus(Utf8("P\tAlpha\tg\n"));

        var groups = BuildInfo.ToGroups(clean, status);

        Assert.Equal("inprogress", groups[BuildInfo.DefaultGroupIndex(groups)].Key);
    }
}
