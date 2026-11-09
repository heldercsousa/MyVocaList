using System.Globalization;
using System.Text;

namespace MyVocaList.Services;

public sealed record BuildIdentity(string Version, string ShortSha, string BuiltAtLocal);

public sealed record BuildDetails(string Branch, IReadOnlyList<string> UncommittedPaths,
                                  IReadOnlyList<string> RecentCommits)
{
    public bool IsDirty => UncommittedPaths.Count > 0;
}

public sealed record BuildStatusItem(string Title, string Gate);

public sealed record BuildStatus(IReadOnlyList<BuildStatusItem> InProgress,
                                 IReadOnlyList<string> DoneThisMonth);

/// <summary>One list row. <c>SupportingText</c> is empty when there is no supporting line.</summary>
public sealed record BuildRow(string Text, string SupportingText);

public sealed record BuildGroup(string Key, string Label, IReadOnlyList<BuildRow> Rows);

/// <summary>
/// Pure parsing/grouping of build identity data (assembly attributes and embedded build resources).
/// </summary>
public static class BuildInfo
{
    public const string DetailsResourceName = "build-details.txt";
    public const string StatusResourceName = "build-status.txt";

    private const int ShortShaLength = 7;
    private const int PorcelainPrefixLength = 3;

    public static BuildIdentity ParseIdentity(string? informationalVersion,
                                              string? buildTimestampUtc, TimeZoneInfo zone)
    {
        var version = "";
        var sha = "";
        if (!string.IsNullOrWhiteSpace(informationalVersion))
        {
            var plus = informationalVersion.IndexOf('+');
            if (plus < 0)
            {
                version = informationalVersion;
            }
            else
            {
                version = informationalVersion[..plus];
                var full = informationalVersion[(plus + 1)..];
                sha = full.Length > ShortShaLength ? full[..ShortShaLength] : full;
            }
        }

        var builtAt = "";
        if (!string.IsNullOrWhiteSpace(buildTimestampUtc)
            && DateTimeOffset.TryParse(buildTimestampUtc, CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind, out var parsed))
        {
            builtAt = TimeZoneInfo.ConvertTime(parsed, zone)
                .ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
        }

        return new BuildIdentity(version, sha, builtAt);
    }

    public static BuildDetails? ReadDetails(Stream? stream, string shortSha)
    {
        if (stream is null) return null;

        string text;
        using (var reader = new StreamReader(stream, Encoding.UTF8))
            text = reader.ReadToEnd();

        var branch = new List<string>();
        var uncommitted = new List<string>();
        var commits = new List<string>();
        List<string>? current = null;

        foreach (var raw in text.Split('\n'))
        {
            var line = raw.TrimEnd('\r');
            switch (line)
            {
                case "#branch": current = branch; continue;
                case "#uncommitted": current = uncommitted; continue;
                case "#commits": current = commits; continue;
            }

            if (current is null || string.IsNullOrWhiteSpace(line)) continue;

            if (ReferenceEquals(current, uncommitted))
                current.Add(line.Length > PorcelainPrefixLength ? line[PorcelainPrefixLength..] : line.Trim());
            else
                current.Add(line);
        }

        if (branch.Count == 0) return null;

        var name = branch[0].Trim();
        if (name == "HEAD") name = $"detached @ {shortSha}";

        return new BuildDetails(name, uncommitted, commits);
    }

    public static IReadOnlyList<BuildGroup> ToGroups(BuildDetails? details, BuildStatus? status)
    {
        var groups = new List<BuildGroup>();

        if (details is not null)
        {
            if (details.UncommittedPaths.Count > 0)
                groups.Add(Group("uncommitted", "Uncommitted",
                    details.UncommittedPaths.Select(p => new BuildRow(p, "")).ToList()));
            if (details.RecentCommits.Count > 0)
                groups.Add(Group("changes", "Changes",
                    details.RecentCommits.Select(c => new BuildRow(c, "")).ToList()));
        }

        if (status is not null)
        {
            if (status.InProgress.Count > 0)
                groups.Add(Group("inprogress", "In progress",
                    status.InProgress.Select(i => new BuildRow(i.Title, i.Gate)).ToList()));
            if (status.DoneThisMonth.Count > 0)
                groups.Add(Group("done", "Done",
                    status.DoneThisMonth.Select(t => new BuildRow(t, "")).ToList()));
        }

        return groups;
    }

    public static int DefaultGroupIndex(IReadOnlyList<BuildGroup> groups)
    {
        if (groups.Count == 0) return -1;
        for (var i = 0; i < groups.Count; i++)
            if (groups[i].Key == "inprogress") return i;
        for (var i = 0; i < groups.Count; i++)
            if (groups[i].Key == "changes") return i;
        return 0;
    }

    private static BuildGroup Group(string key, string label, IReadOnlyList<BuildRow> rows) =>
        new(key, $"{label} ({rows.Count})", rows);
}
