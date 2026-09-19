using System.Text;
using System.Text.RegularExpressions;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Architecture;

/// <summary>
/// REQ-UOW-50 — the permanent, executing gate for REQ-UOW-36, REQ-UOW-37 and REQ-UOW-43. A Python
/// file walk pasted into a task-log catches a captive-field regression once, on the day it is run; it
/// does not catch the next service added six months from now. This test does, every build.
/// <para>
/// <b>Detection anchor (F2 hardening, 2026-09-19):</b> the original gate keyed on the
/// <c>_</c>-prefixed <i>field declaration</i>. An adversarial verifier proved that shape evadable —
/// no access modifier, a primary constructor with no field at all, a field initialiser, a
/// multi-declarator, a <c>static</c> field, a using-alias, or a property instead of a field all
/// slipped a live captive dependency past the old detector, because each of those shapes either
/// doesn't match the field-declaration regex at all or hides the field behind syntax the regex never
/// anticipated.
/// </para>
/// <para>
/// This version keys on the <b>constructor parameter list</b> instead — classic and primary (C# 12)
/// alike. Every captive dependency <i>must</i> be injected, so the constructor parameter list is the
/// one shape-independent anchor: a service cannot hold a repository it was never given, regardless of
/// how it chooses to store the reference afterward (field, property, primary-constructor capture, or
/// nothing at all). For a classic constructor, a governed parameter is resolved to whatever identifier
/// it gets assigned to in the constructor body (field or property, any name, any modifier); for a
/// primary constructor, the parameter name itself is directly usable throughout the class body in C#
/// 12, so it is governed as-is. The resulting identifier set is then checked for a bare dereference
/// (<c>identifier.</c>) anywhere in the file — inside or outside an <c>ExecuteAsync</c>/
/// <c>ExecuteReadAsync</c> lambda; inside, collaborators must come from the lambda's own <c>sp</c>.
/// </para>
/// <para><b>Honesty about limits (required by this change's spec):</b> this is still a text/regex
/// approach, not a Roslyn semantic model, so it has real, known blind spots:</para>
/// <list type="bullet">
/// <item>a <c>using</c> type alias that renames a governed interface to something not matching
/// <c>^I[A-Za-z0-9]*Repository$</c> and not in <see cref="GovernedDataServiceTypes"/> evades type
/// recognition — the parameter's declared type name is never resolved semantically;</item>
/// <item>a classic-constructor parameter with no direct <c>target = paramName;</c> assignment (e.g.
/// stored via a helper method call, a tuple, or a collection) falls back to treating the parameter
/// name itself as the governed identifier and scanning the whole file for it — this can both
/// false-positive (an unrelated identifier sharing the parameter's short name) and, more importantly,
/// can still be evaded by any indirection more complex than a direct assignment;</item>
/// <item>it does not understand C# scoping — a governed identifier name reused as an unrelated local
/// variable elsewhere in the file is indistinguishable from a real dereference of the governed
/// dependency (this makes the gate conservative/noisy, not permissive, which is the correct failure
/// direction for a gate).</item>
/// </list>
/// <para>
/// Rather than a silent skip, a service file that calls <c>ExecuteReadAsync</c>/<c>ExecuteAsync</c>
/// but has zero detected governed constructor parameters is itself flagged as a violation requiring a
/// human decision — see <c>NoGovernedField_IsDereferencedAnywhere_InsideOrOutsideALambda</c>.
/// </para>
/// </summary>
public class UnitOfWorkReadScopeTests
{
    /// <summary>
    /// REQ-UOW-50 (i) — the governed field set, defined mechanically so the rule survives a
    /// repository added later without needing this test edited.
    /// <para>
    /// A dependency is governed if its declared type name matches <c>^I[A-Za-z0-9]*Repository$</c>,
    /// OR its declared type is one of this closed, explicitly enumerated list of data-service-typed
    /// dependencies (services that themselves reach a repository, so their identity likewise
    /// determines which <c>AppDbContext</c> is used — census taken over <c>Services/**/*.cs</c>,
    /// 2026-08-25). Extending this list is a deliberate edit; adding a new data service without
    /// adding it here is a gap the reviewer must catch.
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

    /// <summary>
    /// Matches a class declaration, capturing an optional primary-constructor parameter list
    /// (C# 12) immediately following the class name / generic type parameters.
    /// </summary>
    private static readonly Regex ClassDeclRegex = new(
        @"\bclass\s+(?<cname>\w+)\s*(?:<[^>]*>)?\s*(?:\((?<pparams>[^\)]*)\))?",
        RegexOptions.Compiled);

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
            $"Expected at least 25 files under Services/ (31 as of 2026-09-19, walked recursively — " +
            "F2 hardening closed the Services/Mappers/ subfolder blind spot); found {serviceFiles.Length}. " +
            "A collapse to a near-empty set means this gate is guarding nothing.");
    }

    [Fact]
    // [AC] REQ-UOW-50: a floor on the number of *governed constructor parameters* detected across
    // Services/**/*.cs, not just files. Every governed dependency is dead by design post-change
    // (assigned/captured in the constructor, never dereferenced outside `sp`) — so if a future cleanup
    // deletes them all, this floor makes that collapse fail loudly instead of the dereference test
    // silently passing because it has nothing left to scan. A genuine, deliberate drop below the floor
    // (e.g. a repository interface removed outright) requires updating this number in the same commit
    // as the removal — not just deleting the test.
    public void GovernedFieldDeclarations_MeetCountFloor()
    {
        var serviceFiles = GetServiceFiles();
        Assert.NotEmpty(serviceFiles);

        var totalGovernedIdentifiers = 0;
        foreach (var path in serviceFiles)
        {
            var clean = StripCommentsAndStringLiterals(File.ReadAllText(path));
            totalGovernedIdentifiers += ExtractGovernedIdentifiers(clean).Count;
        }

        Assert.True(
            totalGovernedIdentifiers >= 15,
            $"Expected at least 15 governed (repository- or data-service-typed) constructor " +
            $"parameters across Services/**/*.cs (20+ as of 2026-09-19); found {totalGovernedIdentifiers}. " +
            "A drop below the floor means either a real, deliberate cleanup (update this floor " +
            "explicitly) or the constructor-parameter detector has silently broken and is no longer " +
            "finding what it should — either way it needs a human decision, not a silent pass.");
    }

    // ── (a) + (b) — REQ-UOW-36/37 ───────────────────────────────────────────

    [Fact]
    // [AC] REQ-UOW-50(a)/(b): no governed repository- or data-service-typed dependency is dereferenced
    // anywhere in a service file except from inside an ExecuteAsync/ExecuteReadAsync lambda's own
    // `sp`-resolved local — i.e. the injected dependency must never be dereferenced at all, inside or
    // outside a lambda body, regardless of whether it is held as a field, a property, or a primary-
    // constructor-captured parameter.
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

            var governedIdentifiers = ExtractGovernedIdentifiers(clean);

            if (governedIdentifiers.Count == 0)
            {
                // Replaces the old silent `continue`. A service file that reaches into a unit-of-work
                // read/write scope but has zero detected governed constructor parameters is suspicious,
                // not clean — it could mean the file legitimately has no governed dependency, OR it
                // could mean the detector's declaration-anchoring has a blind spot for this file's
                // shape. Fail loudly and require a human to look, per this change's spec.
                if (UowCallRegex.IsMatch(clean))
                {
                    violations.Add($"{fileName} — calls ExecuteAsync/ExecuteReadAsync but zero governed " +
                                    "constructor parameters were detected. This must be resolved by a human: " +
                                    "either the file genuinely has no governed dependency (add it to the " +
                                    "explicitly-commented AllowListedFiles), or the constructor-parameter " +
                                    "detector has a blind spot for this file's shape (e.g. a using type " +
                                    "alias) and needs to be extended.");
                }
                continue;
            }

            foreach (var identifier in governedIdentifiers)
            {
                // Anchor on `identifier` immediately followed by `.` — a true dereference, not a
                // substring hit inside another identifier or bare mention.
                var derefRegex = new Regex(Regex.Escape(identifier) + @"\.", RegexOptions.Compiled);
                foreach (System.Text.RegularExpressions.Match m in derefRegex.Matches(clean))
                {
                    var line = LineNumberAt(clean, m.Index);
                    violations.Add($"{fileName}:{line} — '{identifier}.' dereferenced (governed " +
                                    "constructor-injected dependency; must be resolved from the lambda's " +
                                    "own `sp` inside ExecuteAsync/ExecuteReadAsync, never from a captive " +
                                    "field/property/primary-constructor capture, per REQ-UOW-36/37).");
                }
            }
        }

        Assert.True(violations.Count == 0,
            "REQ-UOW-36/37 violation(s) — governed dependency dereferenced directly instead of via `sp`, " +
            "or a detector blind-spot needs human review:\n" +
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

    /// <summary>
    /// Reduces a captured type-name — which may be fully qualified
    /// (<c>MyVocaList.Domain.RepositoryInterface.IArtistRepository</c>) and/or carry a generic
    /// argument list — to the bare interface-name segment before checking governance, so a
    /// qualification or generic wrapper cannot be used to evade detection.
    /// </summary>
    private static string SimpleTypeName(string typeName)
    {
        var withoutGenerics = typeName.IndexOf('<') is var idx && idx >= 0 ? typeName[..idx] : typeName;
        var lastDot = withoutGenerics.LastIndexOf('.');
        return lastDot >= 0 ? withoutGenerics[(lastDot + 1)..] : withoutGenerics;
    }

    private static bool IsGoverned(string typeName)
    {
        var simple = SimpleTypeName(typeName);
        return RepositoryTypeRegex.IsMatch(simple) || GovernedDataServiceTypes.Contains(simple);
    }

    /// <summary>
    /// (iv) Path resolution reuses the proven walk-up-from-BaseDirectory shape already established by
    /// <c>UnitOfWorkCompositionTests.LocateSource</c> — via the independent, non-owned shared helper
    /// <see cref="TestSourcePaths"/> rather than a hand-rolled relative path.
    /// <para>F2 hardening (2026-09-19): walks <c>AllDirectories</c>, not just the top level — a
    /// service filed under a subfolder such as <c>Services/Mappers/</c> was previously invisible to
    /// this gate entirely.</para>
    /// </summary>
    private static string[] GetServiceFiles()
    {
        var servicesDir = TestSourcePaths.LocateDirectory("Services");
        return Directory.GetFiles(servicesDir, "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") &&
                        !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .ToArray();
    }

    /// <summary>
    /// (F2 hardening core) — extracts the set of governed identifiers for a comment/string-stripped
    /// source file by walking every class declaration's constructor parameter list (primary and
    /// classic), independent of how — or whether — the dependency is subsequently stored in a field.
    /// </summary>
    private static List<string> ExtractGovernedIdentifiers(string clean)
    {
        var identifiers = new List<string>();

        foreach (System.Text.RegularExpressions.Match classMatch in ClassDeclRegex.Matches(clean))
        {
            var cname = classMatch.Groups["cname"].Value;

            // Primary constructor (C# 12): parameters captured directly in the class declaration are
            // usable throughout the class body under their own parameter name — no field required.
            if (classMatch.Groups["pparams"].Success)
            {
                foreach (var (type, name) in ParseParams(classMatch.Groups["pparams"].Value))
                {
                    if (IsGoverned(type)) identifiers.Add(name);
                }
            }

            // Classic constructor(s): method with the same name as the class. Resolve each governed
            // parameter to whatever identifier it is assigned to in the constructor body (field or
            // property, any access modifier, any name) — or, if no direct assignment is found, fall
            // back to the parameter name itself (documented blind spot — see class-level doc comment).
            var ctorRegex = new Regex(
                @"(?:public|private|protected|internal)(?:\s+(?:public|private|protected|internal))*\s+" +
                Regex.Escape(cname) + @"\s*\((?<params>[^\)]*)\)\s*(?::[^\{]*)?\{",
                RegexOptions.Compiled);

            foreach (System.Text.RegularExpressions.Match ctorMatch in ctorRegex.Matches(clean))
            {
                var braceStart = ctorMatch.Index + ctorMatch.Length - 1;
                var bodyEnd = FindMatchingBraceEnd(clean, braceStart);
                if (bodyEnd < 0) continue;

                var body = clean.Substring(braceStart, bodyEnd - braceStart);

                foreach (var (type, name) in ParseParams(ctorMatch.Groups["params"].Value))
                {
                    if (!IsGoverned(type)) continue;

                    var assignRegex = new Regex(
                        @"(?<target>[\w\.]+)\s*=\s*\b" + Regex.Escape(name) + @"\b\s*;",
                        RegexOptions.Compiled);
                    var assignMatch = assignRegex.Match(body);
                    if (assignMatch.Success)
                    {
                        var target = assignMatch.Groups["target"].Value;
                        var lastDot = target.LastIndexOf('.');
                        identifiers.Add(lastDot >= 0 ? target[(lastDot + 1)..] : target);
                    }
                    else
                    {
                        // Documented blind spot: indirect capture (helper method, tuple, collection).
                        // Fall back to the raw parameter name so the gate stays conservative rather
                        // than silent.
                        identifiers.Add(name);
                    }
                }
            }
        }

        return identifiers.Distinct().ToList();
    }

    /// <summary>
    /// Splits a parameter-list string into <c>(type, name)</c> pairs, ignoring attributes,
    /// <c>this</c>/<c>ref</c>/<c>out</c>/<c>in</c>/<c>params</c> modifiers, and default values.
    /// Splits on top-level commas only, so a generic argument list's own commas (e.g.
    /// <c>IRepository&lt;Foo, Bar&gt;</c>) are not mistaken for parameter separators.
    /// </summary>
    private static List<(string Type, string Name)> ParseParams(string paramsText)
    {
        var result = new List<(string, string)>();

        foreach (var raw in SplitTopLevel(paramsText, ','))
        {
            var p = raw.Trim();
            if (p.Length == 0) continue;

            p = Regex.Replace(p, @"^(\[[^\]]*\]\s*)+", "");
            p = Regex.Replace(p, @"^(this|ref|out|in|params)\s+", "");

            var eq = p.IndexOf('=');
            if (eq >= 0) p = p[..eq];
            p = p.Trim();

            var m = Regex.Match(p, @"^(?<type>[\w\.]+(?:<[^<>]*>)?(?:\[\])?)\s+(?<name>\w+)$");
            if (m.Success)
            {
                result.Add((m.Groups["type"].Value, m.Groups["name"].Value));
            }
        }

        return result;
    }

    private static List<string> SplitTopLevel(string s, char sep)
    {
        var parts = new List<string>();
        var depth = 0;
        var start = 0;

        for (var i = 0; i < s.Length; i++)
        {
            var c = s[i];
            if (c is '<' or '(' or '[') depth++;
            else if (c is '>' or ')' or ']') depth--;
            else if (c == sep && depth == 0)
            {
                parts.Add(s[start..i]);
                start = i + 1;
            }
        }

        parts.Add(s[start..]);
        return parts;
    }

    /// <summary>
    /// Finds the index one-past the closing brace matching the <c>{</c> at
    /// <paramref name="openBraceIndex"/>, via depth-balanced scanning. Returns -1 if unbalanced
    /// (should not happen in compiling source).
    /// </summary>
    private static int FindMatchingBraceEnd(string text, int openBraceIndex)
    {
        var depth = 0;
        for (var i = openBraceIndex; i < text.Length; i++)
        {
            if (text[i] == '{') depth++;
            else if (text[i] == '}')
            {
                depth--;
                if (depth == 0) return i + 1;
            }
        }
        return -1;
    }

    private readonly record struct UowLambda(string Kind, int Start, int End);

    /// <summary>
    /// Finds every <c>.ExecuteReadAsync(</c> / <c>.ExecuteAsync(</c> invocation's <c>async sp =&gt;
    /// { ... }</c> lambda body span in <paramref name="clean"/> (already comment/string-stripped
    /// text), using brace balancing from the first <c>{</c> following the call to its matching
    /// <c>}</c>. Every call site in this codebase uses this brace-delimited shape (verified 2026-09-18
    /// across all Services/**/*.cs files — no expression-bodied ExecuteAsync/ExecuteReadAsync lambda
    /// exists); a call not in this shape is skipped rather than mis-parsed, since (a)/(b)'s
    /// whole-file-minus-lambda-regions scan still catches a dependency dereferenced at the call site
    /// itself.
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

            var end = FindMatchingBraceEnd(clean, braceStart);
            if (end < 0) continue;

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
    /// numbers stay accurate for diagnostics. Dereference checks are then anchored on
    /// <c>identifier.</c>, not a bare identifier occurrence, per the same requirement.
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
