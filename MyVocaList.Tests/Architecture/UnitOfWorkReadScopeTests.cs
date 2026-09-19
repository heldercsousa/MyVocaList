using System.Text;
using System.Text.RegularExpressions;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Architecture;

/// <summary>
/// REQ-UOW-50 — the permanent, executing gate for REQ-UOW-36, REQ-UOW-37 and REQ-UOW-43. A Python
/// file walk pasted into a task-log catches a captive-field regression once, on the day it is run; it
/// does not catch the next service added six months from now. This test does, every build.
/// <para>
/// It reads every <c>Services/*.cs</c> source file from disk (never <c>grep</c>/<c>rg</c> — proxied
/// through a lossy wrapper, not admissible evidence per this change's spec) and asserts, per service
/// file:
/// </para>
/// <list type="bullet">
/// <item>(a) no governed <c>_</c>-prefixed field is dereferenced <b>outside</b> an
/// <c>ExecuteAsync</c>/<c>ExecuteReadAsync</c> lambda body;</item>
/// <item>(b) no governed <c>_</c>-prefixed field is dereferenced <b>inside</b> a lambda body either —
/// inside, collaborators come from <c>sp</c>;</item>
/// <item>(c) no <c>ExecuteReadAsync</c> lambda body references <c>_providers</c>,
/// <c>IMusicMetadataProvider</c>, or <c>FetchFromProviders</c>.</item>
/// </list>
/// </summary>
public class UnitOfWorkReadScopeTests
{
    /// <summary>
    /// REQ-UOW-50 (i) — the governed field set, defined mechanically so the rule survives a
    /// repository added later without needing this test edited.
    /// <para>
    /// A field is governed if its declared type name matches <c>^I[A-Za-z0-9]*Repository$</c>, OR its
    /// declared type is one of this closed, explicitly enumerated list of data-service-typed fields
    /// (services that themselves reach a repository, so their identity likewise determines which
    /// <c>AppDbContext</c> is used — census taken over <c>Services/*.cs</c>, 2026-08-25). Extending
    /// this list is a deliberate edit; adding a new data service without adding it here is a gap the
    /// reviewer must catch.
    /// </para>
    /// </summary>
    private static readonly string[] GovernedDataServiceTypes =
    [
        "IArtistService",
        "ISongService",
        "IArtistResolutionService",
        "ISongResolutionService",
        "ISongKaraokeUrlService",
    ];

    private static readonly Regex RepositoryTypeRegex = new(
        @"^I[A-Za-z0-9]*Repository$", RegexOptions.Compiled);

    /// <summary>Matches a <c>private readonly &lt;Type&gt; _field;</c> field declaration.</summary>
    private static readonly Regex FieldDeclarationRegex = new(
        @"private\s+readonly\s+(?<type>I[A-Za-z0-9]*)\s+(?<name>_\w+)\s*;", RegexOptions.Compiled);

    /// <summary>Matches a <c>.ExecuteReadAsync(</c> / <c>.ExecuteAsync(</c> invocation site.</summary>
    private static readonly Regex UowCallRegex = new(
        @"\.(?<kind>ExecuteReadAsync|ExecuteAsync)\s*(<[^;{}]*?>)?\s*\(", RegexOptions.Compiled);

    /// <summary>
    /// Files that are explicitly out of scope for this change (`design.md`, `requirements.md §
    /// Explicitly out of scope`) and simply pass. Kept here only so a future reviewer sees the
    /// allow-list is intentionally empty of exceptions and closed by design, not by omission. Adding
    /// a real exception here is a deliberate edit that requires a comment explaining why.
    /// </summary>
    private static readonly string[] AllowListedFiles = [];

    // ── (ii) Non-zero-file guard ────────────────────────────────────────────

    [Fact]
    // [AC] REQ-UOW-50(ii): a missing/moved Services/ directory must fail loudly, not silently pass
    // over an empty file set. LocateSource/LocateDirectory already throw if the tree is absent; this
    // is the Assert.NotEmpty + count-floor backstop the spec requires in addition to that throw.
    public void ServicesDirectory_IsFoundAndNonEmpty()
    {
        var serviceFiles = GetServiceFiles();

        Assert.NotEmpty(serviceFiles);
        Assert.True(
            serviceFiles.Length >= 25,
            $"Expected at least 25 files under Services/ (30 as of 2026-08-25); found {serviceFiles.Length}. " +
            "A collapse to a near-empty set means this gate is guarding nothing.");
    }

    // ── (a) + (b) — REQ-UOW-36/37 ───────────────────────────────────────────

    [Fact]
    // [AC] REQ-UOW-50(a)/(b): no governed repository- or data-service-typed field is dereferenced
    // anywhere in a service file except from inside an ExecuteAsync/ExecuteReadAsync lambda's own
    // `sp`-resolved local — i.e. the field itself must never be dereferenced at all, inside or
    // outside a lambda body.
    public void NoGovernedField_IsDereferencedAnywhere_InsideOrOutsideALambda()
    {
        var serviceFiles = GetServiceFiles();
        Assert.NotEmpty(serviceFiles);

        var violations = new List<string>();

        foreach (var path in serviceFiles)
        {
            var fileName = Path.GetFileName(path);
            if (AllowListedFiles.Contains(fileName)) continue;

            var raw = File.ReadAllText(path);
            var clean = StripCommentsAndStringLiterals(raw);

            var governedFields = FieldDeclarationRegex.Matches(clean)
                .Cast<System.Text.RegularExpressions.Match>()
                .Where(m => IsGoverned(m.Groups["type"].Value))
                .Select(m => m.Groups["name"].Value)
                .Distinct()
                .ToList();

            if (governedFields.Count == 0) continue;

            foreach (var field in governedFields)
            {
                // Anchor on `_field` immediately followed by `.` — a true dereference, not a
                // substring hit inside another identifier or bare mention.
                var derefRegex = new Regex(Regex.Escape(field) + @"\.", RegexOptions.Compiled);
                foreach (System.Text.RegularExpressions.Match m in derefRegex.Matches(clean))
                {
                    var line = LineNumberAt(clean, m.Index);
                    violations.Add($"{fileName}:{line} — '{field}.' dereferenced (governed field; must be " +
                                    "resolved from the lambda's own `sp` inside ExecuteAsync/ExecuteReadAsync, " +
                                    "never from the constructor field, per REQ-UOW-36/37).");
                }
            }
        }

        Assert.True(violations.Count == 0,
            "REQ-UOW-36/37 violation(s) — governed field dereferenced directly instead of via `sp`:\n" +
            string.Join("\n", violations));
    }

    // ── (c) — REQ-UOW-43 ────────────────────────────────────────────────────

    [Fact]
    // [AC] REQ-UOW-50(c): no ExecuteReadAsync lambda body may reach a network-backed collaborator —
    // holding a DbContext scope open across an HTTP round trip is prohibited.
    public void NoExecuteReadAsyncLambda_ReferencesAProviderOrFetchIdentifier()
    {
        var serviceFiles = GetServiceFiles();
        Assert.NotEmpty(serviceFiles);

        string[] forbiddenIdentifiers = ["_providers", "IMusicMetadataProvider", "FetchFromProviders"];
        var violations = new List<string>();

        foreach (var path in serviceFiles)
        {
            var fileName = Path.GetFileName(path);
            if (AllowListedFiles.Contains(fileName)) continue;

            var raw = File.ReadAllText(path);
            var clean = StripCommentsAndStringLiterals(raw);

            foreach (var lambda in FindUowLambdas(clean))
            {
                if (lambda.Kind != "ExecuteReadAsync") continue;

                var body = clean.Substring(lambda.Start, lambda.End - lambda.Start);
                foreach (var identifier in forbiddenIdentifiers)
                {
                    var idRegex = new Regex($@"\b{Regex.Escape(identifier)}\b", RegexOptions.Compiled);
                    if (idRegex.IsMatch(body))
                    {
                        var line = LineNumberAt(clean, lambda.Start);
                        violations.Add($"{fileName}:{line} — ExecuteReadAsync lambda references " +
                                        $"'{identifier}' (REQ-UOW-43: network I/O must stay outside the " +
                                        "unit-of-work read scope).");
                    }
                }
            }
        }

        Assert.True(violations.Count == 0,
            "REQ-UOW-43 violation(s) — network-backed identifier inside an ExecuteReadAsync lambda:\n" +
            string.Join("\n", violations));
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static bool IsGoverned(string typeName) =>
        RepositoryTypeRegex.IsMatch(typeName) || GovernedDataServiceTypes.Contains(typeName);

    /// <summary>
    /// (iv) Path resolution reuses the proven walk-up-from-BaseDirectory shape already established by
    /// <c>UnitOfWorkCompositionTests.LocateSource</c> — via the independent, non-owned shared helper
    /// <see cref="TestSourcePaths"/> rather than a hand-rolled relative path.
    /// </summary>
    private static string[] GetServiceFiles()
    {
        var servicesDir = TestSourcePaths.LocateDirectory("Services");
        return Directory.GetFiles(servicesDir, "*.cs", SearchOption.TopDirectoryOnly);
    }

    private readonly record struct UowLambda(string Kind, int Start, int End);

    /// <summary>
    /// Finds every <c>.ExecuteReadAsync(</c> / <c>.ExecuteAsync(</c> invocation's <c>async sp =&gt;
    /// { ... }</c> lambda body span in <paramref name="clean"/> (already comment/string-stripped
    /// text), using brace balancing from the first <c>{</c> following the call to its matching
    /// <c>}</c>. Every call site in this codebase uses this brace-delimited shape (verified 2026-09-18
    /// across all 30 Services/*.cs files — no expression-bodied ExecuteAsync/ExecuteReadAsync lambda
    /// exists); a call not in this shape is skipped rather than mis-parsed, since (a)/(b)'s
    /// whole-file-minus-lambda-regions scan still catches a field dereferenced at the call site itself.
    /// </summary>
    private static List<UowLambda> FindUowLambdas(string clean)
    {
        var result = new List<UowLambda>();

        foreach (System.Text.RegularExpressions.Match call in UowCallRegex.Matches(clean))
        {
            var braceStart = clean.IndexOf('{', call.Index);
            if (braceStart < 0) continue;

            // Reject if something other than whitespace/`async sp =>` sits between the call's `(`
            // and the `{` — guards against accidentally matching an unrelated open brace far away.
            var between = clean.Substring(call.Index + call.Length, braceStart - (call.Index + call.Length));
            if (!Regex.IsMatch(between, @"^\s*async\s+\w+\s*=>\s*$")) continue;

            var depth = 0;
            var end = -1;
            for (var i = braceStart; i < clean.Length; i++)
            {
                if (clean[i] == '{') depth++;
                else if (clean[i] == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        end = i + 1;
                        break;
                    }
                }
            }

            if (end < 0) continue; // unbalanced — should not happen in compiling source; skip defensively

            result.Add(new UowLambda(call.Groups["kind"].Value, braceStart, end));
        }

        return result;
    }

    private static int LineNumberAt(string text, int index)
    {
        var line = 1;
        for (var i = 0; i < index && i < text.Length; i++)
        {
            if (text[i] == '\n') line++;
        }
        return line;
    }

    /// <summary>
    /// REQ-UOW-50(iii) — strips <c>//</c>, <c>/* */</c> and XML-doc <c>///</c> comment content, plus
    /// the contents of regular, verbatim (<c>@"..."</c>) and interpolated (<c>$"..."</c>,
    /// <c>$@"..."</c>) string and char literals, replacing each with equal-length whitespace so line
    /// numbers stay accurate for diagnostics. Field checks are then anchored on <c>_field.</c>, not a
    /// bare identifier occurrence, per the same requirement.
    /// </summary>
    private static string StripCommentsAndStringLiterals(string text)
    {
        var sb = new StringBuilder(text.Length);
        var i = 0;
        var n = text.Length;

        while (i < n)
        {
            var c = text[i];

            if (c == '/' && i + 1 < n && text[i + 1] == '/')
            {
                var j = text.IndexOf('\n', i);
                if (j < 0) j = n;
                AppendBlanked(sb, text, i, j);
                i = j;
            }
            else if (c == '/' && i + 1 < n && text[i + 1] == '*')
            {
                var j = text.IndexOf("*/", i + 2, StringComparison.Ordinal);
                j = j < 0 ? n : j + 2;
                AppendBlanked(sb, text, i, j);
                i = j;
            }
            else if ((c == '@' && i + 1 < n && text[i + 1] == '"') ||
                     (c == '$' && i + 1 < n && (text[i + 1] == '@' || text[i + 1] == '"')))
            {
                var isVerbatim = c == '@' || (i + 2 < n && text[i + 1] == '@');
                var quoteStart = text.IndexOf('"', i);
                var j = quoteStart + 1;
                if (isVerbatim)
                {
                    while (j < n)
                    {
                        if (text[j] == '"')
                        {
                            if (j + 1 < n && text[j + 1] == '"') { j += 2; continue; }
                            j += 1;
                            break;
                        }
                        j++;
                    }
                }
                else
                {
                    while (j < n)
                    {
                        if (text[j] == '\\') { j += 2; continue; }
                        if (text[j] == '"') { j += 1; break; }
                        j++;
                    }
                }
                AppendBlanked(sb, text, i, Math.Min(j, n));
                i = Math.Min(j, n);
            }
            else if (c == '"')
            {
                var j = i + 1;
                while (j < n)
                {
                    if (text[j] == '\\') { j += 2; continue; }
                    if (text[j] == '"') { j += 1; break; }
                    j++;
                }
                AppendBlanked(sb, text, i, Math.Min(j, n));
                i = Math.Min(j, n);
            }
            else if (c == '\'')
            {
                var j = i + 1;
                while (j < n)
                {
                    if (text[j] == '\\') { j += 2; continue; }
                    if (text[j] == '\'') { j += 1; break; }
                    j++;
                }
                AppendBlanked(sb, text, i, Math.Min(j, n));
                i = Math.Min(j, n);
            }
            else
            {
                sb.Append(c);
                i++;
            }
        }

        return sb.ToString();
    }

    private static void AppendBlanked(StringBuilder sb, string text, int start, int end)
    {
        for (var k = start; k < end; k++)
        {
            sb.Append(text[k] == '\n' ? '\n' : ' ');
        }
    }
}
