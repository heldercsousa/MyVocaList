"""
Tree-wide READ-SCOPE walk (task 7.2, Wave 7).

Produces the tree-wide evidence for REQ-UOW-36/37 that Waves 4-5's per-file
walks did not: (a) coverage of every method named in requirements.md § Scope,
and (b) purity -- zero ExecuteReadAsync/ExecuteAsync lambda bodies anywhere
in Services/*.cs dereference a `_`-prefixed repository/data-service field.

Per the briefing: Python file walk only. No grep/rg (lossy wrapper, not
admissible evidence per requirements.md § Scope).
"""

import os
import re
import sys

SERVICES_DIR = os.path.join(os.path.dirname(__file__), "..", "..", "Services")
SERVICES_DIR = os.path.normpath(SERVICES_DIR)

# ---------------------------------------------------------------------------
# Comment / string stripping (requirements.md REQ-UOW-50 (iii))
# ---------------------------------------------------------------------------

def strip_comments_and_strings(src: str) -> str:
    """Replace comment and string-literal content with spaces (preserving
    line structure / offsets) so identifier matching cannot false-positive
    on prose or literals. Handles //, /* */, and "...", @"...", $"...",
    verbatim/interpolated combos, and char literals well enough for this
    codebase's usage."""
    out = []
    i = 0
    n = len(src)
    while i < n:
        c = src[i]
        # line comment
        if c == "/" and i + 1 < n and src[i + 1] == "/":
            start = i
            while i < n and src[i] != "\n":
                i += 1
            out.append(" " * (i - start))
            continue
        # block comment
        if c == "/" and i + 1 < n and src[i + 1] == "*":
            start = i
            i += 2
            while i + 1 < n and not (src[i] == "*" and src[i + 1] == "/"):
                i += 1
            i = min(i + 2, n)
            seg = src[start:i]
            out.append("".join(ch if ch == "\n" else " " for ch in seg))
            continue
        # verbatim / interpolated-verbatim string: @"...", $@"...", @$"..."
        if c == "@" and i + 1 < n and src[i + 1] == '"':
            start = i
            i += 2
            while i < n:
                if src[i] == '"':
                    if i + 1 < n and src[i + 1] == '"':
                        i += 2
                        continue
                    i += 1
                    break
                i += 1
            seg = src[start:i]
            out.append("".join(ch if ch == "\n" else " " for ch in seg))
            continue
        if (c == "$" and i + 1 < n and src[i + 1] == "@" and i + 2 < n and src[i + 2] == '"'):
            start = i
            i += 3
            while i < n:
                if src[i] == '"':
                    if i + 1 < n and src[i + 1] == '"':
                        i += 2
                        continue
                    i += 1
                    break
                i += 1
            seg = src[start:i]
            out.append("".join(ch if ch == "\n" else " " for ch in seg))
            continue
        # regular / interpolated string "..."
        if c == '"' or (c == "$" and i + 1 < n and src[i + 1] == '"'):
            start = i
            i += 1 if c == '"' else 2
            while i < n and src[i] != '"':
                if src[i] == "\\" and i + 1 < n:
                    i += 2
                    continue
                i += 1
            i = min(i + 1, n)
            seg = src[start:i]
            out.append("".join(ch if ch == "\n" else " " for ch in seg))
            continue
        # char literal 'x'
        if c == "'":
            start = i
            i += 1
            while i < n and src[i] != "'":
                if src[i] == "\\" and i + 1 < n:
                    i += 2
                    continue
                i += 1
            i = min(i + 1, n)
            seg = src[start:i]
            out.append("".join(ch if ch == "\n" else " " for ch in seg))
            continue
        out.append(c)
        i += 1
    return "".join(out)


MARKER_CALL_RE_CACHE = {}


def _marker_call_regex(marker: str):
    """`marker` optionally followed by a generic argument list `<...>` (which
    may itself nest, e.g. `<IEnumerable<Person>>`), then `(`. Real calls in
    this codebase look like `ExecuteReadAsync<Person?>(` or
    `ExecuteReadAsync<(IEnumerable<PersonListItemDto> items, int total)>(`."""
    if marker not in MARKER_CALL_RE_CACHE:
        MARKER_CALL_RE_CACHE[marker] = re.compile(
            r"\b" + re.escape(marker) + r"\b\s*(<.*?>)?\s*\("
        )
    return MARKER_CALL_RE_CACHE[marker]


def _find_marker_call(stripped_src: str, marker: str, start: int):
    """Find the next `marker` call, balancing angle brackets for the optional
    generic argument list so nested `<...>` (e.g. tuple/generic return types)
    do not truncate the match early. Returns (marker_pos, paren_open_pos) or
    (None, None)."""
    pat = re.compile(r"\b" + re.escape(marker) + r"\b")
    m = pat.search(stripped_src, start)
    if not m:
        return None, None
    i = m.end()
    n = len(stripped_src)
    while i < n and stripped_src[i] in " \t\r\n":
        i += 1
    if i < n and stripped_src[i] == "<":
        depth = 1
        i += 1
        while i < n and depth > 0:
            if stripped_src[i] == "<":
                depth += 1
            elif stripped_src[i] == ">":
                depth -= 1
            i += 1
        while i < n and stripped_src[i] in " \t\r\n":
            i += 1
    if i < n and stripped_src[i] == "(":
        return m.start(), i
    # Not actually a call (e.g. a bare mention) -- keep scanning from after this token.
    return _find_marker_call(stripped_src, marker, m.end())


def extract_lambda_bodies(stripped_src: str, marker: str):
    """Find every occurrence of a `<marker>(...)` call (tolerating an
    intervening generic argument list) and brace-balance-extract the body of
    the lambda passed to it. Returns list of (start_idx, end_idx, body_text)."""
    results = []
    idx = 0
    while True:
        pos, paren_pos = _find_marker_call(stripped_src, marker, idx)
        if pos is None:
            break
        # find first '{' after the marker call (the lambda body opening brace)
        j = paren_pos + 1
        depth_paren = 1  # we are inside the marker's own '('
        brace_start = None
        while j < len(stripped_src):
            ch = stripped_src[j]
            if ch == "(":
                depth_paren += 1
            elif ch == ")":
                depth_paren -= 1
                if depth_paren == 0:
                    break
            elif ch == "{" and brace_start is None:
                brace_start = j
            j += 1
        end_of_call = j  # index of matching ')'
        if brace_start is not None and brace_start < end_of_call:
            # brace-balance from brace_start
            depth = 0
            k = brace_start
            while k < len(stripped_src):
                if stripped_src[k] == "{":
                    depth += 1
                elif stripped_src[k] == "}":
                    depth -= 1
                    if depth == 0:
                        k += 1
                        break
                k += 1
            body = stripped_src[brace_start:k]
            results.append((pos, k, body))
            idx = k
        else:
            # no brace body found (e.g. expression-bodied lambda without braces)
            # capture up to end_of_call as the body
            body = stripped_src[brace_start if brace_start is not None else pos:end_of_call]
            results.append((pos, end_of_call, body))
            idx = end_of_call
    return results


# ---------------------------------------------------------------------------
# Part (a): coverage census from requirements.md § Scope
# ---------------------------------------------------------------------------

SCOPE_CENSUS = [
    ("PersonService", "GetPersonByIdAsync"),
    ("PersonService", "GetPersonByNameAsync"),
    ("PersonService", "SearchPersonsAsync"),
    ("PersonService", "SearchPersonsStartsWithAsync"),
    ("SongService", "GetSongByIdAsync"),
    ("BackupService", "GetHistoryAsync"),
    ("VenueService", "GetPagedVenuesForListAsync"),
    ("PersonService", "GetPagedPersonsForListAsync"),
    ("SongService", "GetPagedSongsForListAsync"),
    ("SongService", "ExistsByTitleForArtistAsync"),
    ("CatalogService", "GetPagedCatalogForArtistAsync"),
    ("ArtistService", "GetPagedArtistsForListAsync"),
    ("ArtistService", "SearchArtistsByNameAsync"),
    ("SongKaraokeUrlService", "GetUrlsForSongAsync"),
    ("SongKaraokeUrlService", "GetSuggestedUrlAsync"),
    ("BackupService", "ExportBundleAsync"),  # only GetLatestSnapshotAsync call wrapped
    ("BackupService", "HasRecentBackupAsync"),
    ("ArtistService", "GetDeleteConfirmationAsync"),  # BUG-078 site
    ("SongSuggestionService", "GetLocalAsync"),
    ("SongSuggestionService", "DedupAsync"),
    ("SongSuggestionService", "ResolveLocalArtistIdsAsync"),
    ("SongSuggestionService", "FetchFromProvidersAsync"),  # HTTP -- must stay OUTSIDE
    ("ArtistSuggestionService", "GetLocalAsync"),
    ("ArtistSuggestionService", "GetRemoteAsync"),  # only the repo call inside
    ("CrudListViewModelBase", "DbLoadGate"),  # gate removal, not a Services/*.cs method -- reported separately
]

HTTP_STAYS_OUTSIDE = {
    ("SongSuggestionService", "FetchFromProvidersAsync"),
}


def find_method_span(stripped_src: str, method_name: str):
    """Find a method/property declaration named method_name and return
    (decl_start, body_start, body_end) via brace balancing. Returns None
    if not found. Matches `method_name(` preceded by whitespace/modifiers,
    or `method_name` as a field/const name (best-effort for DbLoadGate)."""
    # Method form: identifier followed by ( ... ) { ... } possibly with
    # async/generic decorations before it. We just look for the exact token
    # followed by '(' at a word boundary, then find the next '{' that starts
    # its body (skipping the parameter list and any 'where' generic clause).
    # A method name can appear multiple times in a file: at its own declaration
    # AND at call sites (including calls that textually precede the declaration,
    # e.g. SongSuggestionService.GetRemoteAsync calls FetchFromProvidersAsync
    # before FetchFromProvidersAsync's own declaration later in the file). We
    # must find the DECLARATION, not merely the first textual occurrence, so we
    # try every occurrence in order and accept the first one that resolves to a
    # method body (`{ ... }`), rejecting occurrences that are plain calls
    # (which lead straight to `;` or `,` with no body of their own).
    pattern = re.compile(r"\b" + re.escape(method_name) + r"\b\s*(<[^>]*>)?\s*\(")
    search_from = 0
    while True:
        m = pattern.search(stripped_src, search_from)
        if not m:
            return None
        # walk from end of the opened paren, balance parens for the param list
        i = m.end() - 1  # points at '('
        depth = 1
        i += 1
        while i < len(stripped_src) and depth > 0:
            if stripped_src[i] == "(":
                depth += 1
            elif stripped_src[i] == ")":
                depth -= 1
            i += 1
        # now skip whitespace / 'where' clauses up to first '{' or ';' (expr-bodied '=>' too)
        j = i
        while j < len(stripped_src) and stripped_src[j] not in "{;":
            j += 1
        if j >= len(stripped_src) or stripped_src[j] == ";":
            # This occurrence is a call site (or interface decl), not a
            # declaration with a body -- keep scanning past it.
            search_from = m.end()
            continue
        brace_start = j
        break
    depth = 0
    k = brace_start
    while k < len(stripped_src):
        if stripped_src[k] == "{":
            depth += 1
        elif stripped_src[k] == "}":
            depth -= 1
            if depth == 0:
                k += 1
                break
        k += 1
    return (m.start(), brace_start, k)


def is_inside_any_lambda(pos: int, lambda_spans):
    for (s, e, _body) in lambda_spans:
        if s <= pos < e:
            return True
    return False


def main():
    if not os.path.isdir(SERVICES_DIR):
        print(f"FATAL: Services dir not found at {SERVICES_DIR}")
        sys.exit(1)

    files = sorted(f for f in os.listdir(SERVICES_DIR) if f.endswith(".cs"))
    print(f"Services/*.cs file count: {len(files)}")
    print(f"Scanning: {SERVICES_DIR}")
    print()

    file_src = {}
    file_stripped = {}
    for fname in files:
        path = os.path.join(SERVICES_DIR, fname)
        with open(path, "r", encoding="utf-8-sig") as fh:
            src = fh.read()
        file_src[fname] = src
        file_stripped[fname] = strip_comments_and_strings(src)

    def find_file_for_class(class_name):
        candidates = [f for f in files if f == class_name + ".cs"]
        if candidates:
            return candidates[0]
        # fallback: substring match
        candidates = [f for f in files if class_name in f]
        return candidates[0] if candidates else None

    # ---- Part (a): coverage ----
    print("=" * 100)
    print("PART (a) -- COVERAGE: every method named in requirements.md § Scope")
    print("=" * 100)
    print(f"{'Service':<26}{'Method':<32}{'Found':<8}{'Repo call site':<16}{'Verdict'}")
    print("-" * 100)

    coverage_rows = []
    for class_name, method_name in SCOPE_CENSUS:
        if class_name == "CrudListViewModelBase":
            coverage_rows.append((class_name, method_name, "N/A", "N/A",
                                   "OUT OF SCOPE for this walk -- ViewModels/, not Services/*.cs; "
                                   "DbLoadGate removal (REQ-UOW-47/48) is a separate Wave, not this AC's target"))
            continue

        fname = find_file_for_class(class_name)
        if fname is None:
            coverage_rows.append((class_name, method_name, "NOT FOUND", "-",
                                   f"FILE NOT FOUND -- Services/{class_name}.cs does not exist in this tree"))
            continue

        stripped = file_stripped[fname]
        span = find_method_span(stripped, method_name)
        if span is None:
            coverage_rows.append((class_name, method_name, "NOT FOUND", "-",
                                   f"METHOD NOT FOUND in {fname} -- may have been removed/renamed"))
            continue

        decl_start, body_start, body_end = span
        method_body = stripped[body_start:body_end]

        # find ExecuteReadAsync / ExecuteAsync lambda spans anywhere in file
        read_lambdas = extract_lambda_bodies(stripped, "ExecuteReadAsync")
        write_lambdas = extract_lambda_bodies(stripped, "ExecuteAsync")
        # ExecuteAsync also matches ExecuteReadAsync's substring accidentally?
        # No -- "ExecuteReadAsync(" != "ExecuteAsync(" as marker strings, find() is exact.

        # Repository call pattern: `_xxxRepository.` or `sp.GetRequiredService<IXRepository>` inside method body
        repo_call_pattern = re.compile(r"\b(_[A-Za-z0-9]*[Rr]epository)\s*\.")
        sp_pattern = re.compile(r"\bsp\.GetRequiredService<I[A-Za-z0-9]*Repository>")

        # locate repo call positions within [body_start, body_end) in the whole-file stripped text
        repo_hits = [mm.start() for mm in repo_call_pattern.finditer(stripped, body_start, body_end)]
        sp_hits = [mm.start() for mm in sp_pattern.finditer(stripped, body_start, body_end)]

        if class_name == "SongSuggestionService" and method_name == "FetchFromProvidersAsync":
            # HTTP method -- must have NO repo calls and must NOT be inside a read lambda
            has_repo = bool(repo_hits) or bool(sp_hits)
            inside_read = any(is_inside_any_lambda(p, read_lambdas) for p in (repo_hits + sp_hits))
            verdict = "OK -- HTTP method, no repo call, correctly outside any read lambda" if not has_repo else \
                      ("VIOLATION -- repo call found inside HTTP method" if not inside_read else
                       "VIOLATION -- repo call found INSIDE a read lambda in HTTP method")
            coverage_rows.append((class_name, method_name, "yes", "-", verdict))
            continue

        if class_name == "ArtistSuggestionService" and method_name == "GetRemoteAsync":
            # only the repo call at GetByNamesCollatedAsync should be inside a lambda; rest is HTTP
            if not repo_hits and not sp_hits:
                coverage_rows.append((class_name, method_name, "yes", "0",
                                       "VIOLATION -- no repo call found (expected GetByNamesCollatedAsync)"))
                continue
            all_hits = repo_hits + sp_hits
            inside = [p for p in all_hits if is_inside_any_lambda(p, read_lambdas)]
            outside = [p for p in all_hits if not is_inside_any_lambda(p, read_lambdas)]
            if outside:
                coverage_rows.append((class_name, method_name, "yes", f"{len(inside)} in / {len(outside)} out",
                                       "VIOLATION -- repo call outside ExecuteReadAsync lambda"))
            else:
                coverage_rows.append((class_name, method_name, "yes", f"{len(inside)} in / 0 out",
                                       "OK -- sole repo call is inside ExecuteReadAsync lambda"))
            continue

        if class_name == "BackupService" and method_name == "ExportBundleAsync":
            # only GetLatestSnapshotAsync call must be wrapped; File.Exists/ZipFile stay outside (not checked here, out of AC)
            if not repo_hits and not sp_hits:
                coverage_rows.append((class_name, method_name, "yes", "0",
                                       "VIOLATION -- no repo call found (expected GetLatestSnapshotAsync)"))
                continue
            all_hits = repo_hits + sp_hits
            inside = [p for p in all_hits if is_inside_any_lambda(p, read_lambdas)]
            outside = [p for p in all_hits if not is_inside_any_lambda(p, read_lambdas)]
            if outside:
                coverage_rows.append((class_name, method_name, "yes", f"{len(inside)} in / {len(outside)} out",
                                       "VIOLATION -- repo call outside ExecuteReadAsync lambda"))
            else:
                coverage_rows.append((class_name, method_name, "yes", f"{len(inside)} in / 0 out",
                                       "OK -- repo call is inside ExecuteReadAsync lambda"))
            continue

        # General case: every repo call found in the method body must lie inside a read lambda
        all_hits = repo_hits + sp_hits
        if not all_hits:
            coverage_rows.append((class_name, method_name, "yes", "0",
                                   "NO REPO CALL FOUND -- cannot verify wrap (check method manually)"))
            continue
        inside = [p for p in all_hits if is_inside_any_lambda(p, read_lambdas)]
        outside = [p for p in all_hits if not is_inside_any_lambda(p, read_lambdas)]
        if outside:
            coverage_rows.append((class_name, method_name, "yes", f"{len(inside)} in / {len(outside)} out",
                                   "VIOLATION -- repo call NOT inside ExecuteReadAsync lambda"))
        else:
            coverage_rows.append((class_name, method_name, "yes", f"{len(inside)} in / 0 out",
                                   "OK -- all repo calls inside ExecuteReadAsync lambda"))

    violations_a = 0
    for row in coverage_rows:
        class_name, method_name, found, repo, verdict = row
        print(f"{class_name:<26}{method_name:<32}{found:<8}{repo:<16}{verdict}")
        if "VIOLATION" in verdict or "NOT FOUND" in verdict or "NO REPO CALL FOUND" in verdict:
            if "OUT OF SCOPE" not in verdict:
                violations_a += 1

    print()
    print(f"Part (a) rows: {len(coverage_rows)}  |  flagged: {violations_a}")
    print()

    # ---- Part (b): purity ----
    print("=" * 100)
    print("PART (b) -- PURITY: zero _-prefixed repo/data-service field derefs inside ANY")
    print("            ExecuteReadAsync / ExecuteAsync lambda body, tree-wide over Services/*.cs")
    print("=" * 100)

    # governed field pattern: I*Repository-typed fields (by naming convention _xxxRepository)
    # plus enumerated data-service fields per REQ-UOW-50(i)
    DATA_SERVICE_FIELD_NAMES = {
        "_artistService", "_songService", "_artistResolution", "_songResolution",
        "_songKaraokeUrlService",
        # also allow common alternate spellings actually used in this codebase
        "_artistResolutionService", "_songResolutionService",
    }

    field_deref_pattern = re.compile(
        r"\b(_[A-Za-z0-9]*[Rr]epository|" +
        r"|".join(re.escape(n) for n in DATA_SERVICE_FIELD_NAMES) +
        r")\s*\."
    )

    purity_violations = []
    total_read_lambdas = 0
    total_write_lambdas = 0

    for fname in files:
        stripped = file_stripped[fname]
        read_lambdas = extract_lambda_bodies(stripped, "ExecuteReadAsync")
        write_lambdas = extract_lambda_bodies(stripped, "ExecuteAsync")
        # avoid double counting: ExecuteAsync marker also matches text of "ExecuteReadAsync"? No,
        # find() looks for exact substring "ExecuteAsync(" so "ExecuteReadAsync(" does not match it
        # (different substring), so these are disjoint call sites.
        total_read_lambdas += len(read_lambdas)
        total_write_lambdas += len(write_lambdas)

        for (s, e, body) in read_lambdas + write_lambdas:
            for m in field_deref_pattern.finditer(body):
                # compute line number in original file
                abs_pos = s + m.start()
                line_no = stripped.count("\n", 0, abs_pos) + 1
                line_text = file_src[fname].splitlines()[line_no - 1].strip()
                purity_violations.append((fname, line_no, m.group(1), line_text))

    print(f"Total ExecuteReadAsync lambda call sites found: {total_read_lambdas}")
    print(f"Total ExecuteAsync (write) lambda call sites found: {total_write_lambdas}")
    print()

    if purity_violations:
        print(f"VIOLATIONS FOUND: {len(purity_violations)}")
        for (fname, line_no, field, line_text) in purity_violations:
            print(f"  {fname}:{line_no}  field={field}  |  {line_text}")
    else:
        print("No _-prefixed repository/data-service field dereferences found inside any "
              "ExecuteReadAsync/ExecuteAsync lambda body, tree-wide over Services/*.cs.")

    print()
    print("=" * 100)
    print("SUMMARY")
    print("=" * 100)
    print(f"Part (a) flagged rows: {violations_a} / {len(coverage_rows)}")
    print(f"Part (b) violations:   {len(purity_violations)}")


if __name__ == "__main__":
    main()
