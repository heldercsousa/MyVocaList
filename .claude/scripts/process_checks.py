#!/usr/bin/env python
"""Pre-commit process checks for rules that nothing enforced mechanically.

Three deterministic checks over the STAGED state (the git index), run from
`.claude/githooks/pre-commit` as `python .claude/scripts/process_checks.py --staged`:

1. sln      -- every git-tracked `Docs/` file is registered in MyVocaList.sln and every
               registered `Docs/` entry still exists (catches moves and deletes; the
               PostToolUse hook only auto-registers new files written via `Write`).
               Rule: constraints-registry.md § Visual Studio Solution (HARD GATE).
2. safearea -- a staged .xaml whose root element is `ContentPage` must carry
               `SafeAreaEdges="Container"` (CLAUDE.md § Constitutional Constraints).
               Files in SAFEAREA_KNOWN_VIOLATIONS predate this check and are waived
               until fixed; remove an entry when its page is corrected.
3. tasklog  -- a task-log entry that this commit adds or edits and whose status is
               `To Review` must contain a `### Changed files` section
               (workflow.md Rule 5). Untouched legacy entries are never judged.

Exit 1 with a readable message per problem; exit 0 otherwise. Bypass: `--no-verify`.
"""
import re
import subprocess
import sys

# Pages that already use SafeAreaEdges="All" when this check was added (2026-11-10).
# Awaiting Helder's ruling (see AC-FIELD ledger detail); delete entries as they are fixed.
SAFEAREA_KNOWN_VIOLATIONS = {
    "MyVocaList/UI/Pages/Artists/ArtistFormPage.xaml",
    "MyVocaList/UI/Pages/People/PersonFormPage.xaml",
    "MyVocaList/UI/Pages/Venues/VenueFormPage.xaml",
}

_SLN_ENTRY = re.compile(r"(Docs/[^=\r\n]+?) = ")


def check_sln(tracked_docs, sln_text):
    registered = set(_SLN_ENTRY.findall(sln_text.replace("\\", "/")))
    tracked = set(tracked_docs)
    problems = []
    for f in sorted(tracked - registered):
        problems.append(f"sln: '{f}' is tracked but not registered in MyVocaList.sln "
                        "(add it to the matching Solution Folder).")
    for f in sorted(registered - tracked):
        problems.append(f"sln: MyVocaList.sln still lists '{f}' but the file no longer exists "
                        "(moved or deleted - update the entry).")
    return problems


def _root_tag(xaml):
    body = re.sub(r"<\?.*?\?>|<!--.*?-->", "", xaml, flags=re.S)
    m = re.search(r"<\s*([\w:.]+)([^>]*)>", body, flags=re.S)
    return (m.group(1), m.group(2)) if m else ("", "")


def check_safearea(path, xaml):
    if path in SAFEAREA_KNOWN_VIOLATIONS:
        return None
    name, attrs = _root_tag(xaml)
    if name.split(":")[-1] != "ContentPage":
        return None
    if re.search(r'SafeAreaEdges\s*=\s*"Container"', attrs):
        return None
    return (f"safearea: '{path}' root ContentPage lacks SafeAreaEdges=\"Container\" "
            "(MAUI 10 defaults to None - CLAUDE.md Constitutional Constraints).")


def check_tasklog(path, content, added_lines):
    """added_lines: 1-based line numbers added/changed in this commit."""
    lines = content.split("\n")
    starts = [i for i, l in enumerate(lines) if l.startswith("## ")]
    problems = []
    for n, s in enumerate(starts):
        e = starts[n + 1] if n + 1 < len(starts) else len(lines)
        block = lines[s:e]
        if not any((s + 1) <= a <= e for a in added_lines):
            continue
        status = next((l for l in block if re.match(r"\*\*Status:\*\*", l.strip())), "")
        if not re.search(r"to\s+review", status, re.I):
            continue
        if not re.search(r"^#{2,4}\s*Changed files", "\n".join(block), re.I | re.M):
            problems.append(f"tasklog: '{path}' entry '{lines[s].strip()[:70]}' is 'To Review' "
                            "without a '### Changed files' section (invalid - workflow.md Rule 5).")
    return problems


def _git(*args):
    r = subprocess.run(["git", *args], capture_output=True)
    return r.stdout.decode("utf-8", errors="replace")


def _added_lines(path):
    out = _git("diff", "--cached", "-U0", "--", path)
    added = set()
    for m in re.finditer(r"^@@ -\S+ \+(\d+)(?:,(\d+))? @@", out, flags=re.M):
        start, count = int(m.group(1)), int(m.group(2) or 1)
        added.update(range(start, start + count))
    return added


def run_staged():
    staged = [p for p in _git("diff", "--cached", "--name-only", "--diff-filter=ACMRD").splitlines() if p]
    problems = []
    if any(p.startswith("Docs/") or p == "MyVocaList.sln" for p in staged):
        tracked = [p for p in _git("ls-files", "Docs").splitlines() if p]
        sln = _git("show", ":MyVocaList.sln").lstrip("﻿")
        problems += check_sln(tracked, sln)
    live = {p for p in _git("diff", "--cached", "--name-only", "--diff-filter=ACM").splitlines() if p}
    for p in sorted(live):
        if p.endswith(".xaml"):
            msg = check_safearea(p, _git("show", ":" + p))
            if msg:
                problems.append(msg)
        elif p.endswith("task-log.md"):
            problems += check_tasklog(p, _git("show", ":" + p), _added_lines(p))
    return problems


def main():
    if "--staged" not in sys.argv:
        print(__doc__)
        return 0
    problems = run_staged()
    if problems:
        print("process-checks: FAILED")
        for p in problems:
            print("  " + p)
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
