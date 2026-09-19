namespace MyVocaList.Tests.Infrastructure;

/// <summary>
/// Shared source-tree path resolution for tests that read production source files as text
/// (architecture tests, composition-drift tests). Walks up from <see cref="AppContext.BaseDirectory"/>
/// until <paramref name="relativePath"/> is found, throwing loudly if it never is — a missing tree
/// must fail the test, never silently yield an empty result set.
/// </summary>
/// <remarks>
/// Deliberately independent of (not extracted from)
/// <c>UnitOfWorkCompositionTests.LocateSource</c> — that method is <c>private</c> and
/// <c>UnitOfWorkCompositionTests.cs</c> is not in REQ-UOW-49's closed test-file carve-out, so this
/// change does not edit it. The two implementations are intentionally identical in behavior.
/// </remarks>
internal static class TestSourcePaths
{
    public static string LocateSource(string relativePath)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, relativePath);
            if (File.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        throw new FileNotFoundException($"{relativePath} not found walking up from the test output directory.");
    }

    public static string LocateDirectory(string relativeDirectoryPath)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, relativeDirectoryPath);
            if (Directory.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException(
            $"{relativeDirectoryPath} not found walking up from the test output directory.");
    }
}
