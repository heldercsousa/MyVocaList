#!/usr/bin/env python
"""Pre-commit process checks for rules that nothing enforced mechanically.

Three deterministic checks over the STAGED state (the git index), run from
`.claude/githooks/pre-commit` as `python .claude/scripts/process_checks.py --staged`:

1. sln      -- every git-tracked `Docs/` file is registered in MyVocaList.sln and every
               registered `Docs/` entry still exists (catches moves and deletes; the
               PostToolUse hook only auto-registers new files written via `Write`).
               Rule: constraints-registry.md § Visual Studio Solution (HARD GATE).
2. safearea -- a staged .xaml whose root element is `ContentPage` must carry
               `SafeAreaEdges="Container"`, or `"All"` (Container + SoftInput) on pages with
               text input (CLAUDE.md § Constitutional Constraints; Microsoft Learn "Safe area
               layout", .NET MAUI 10). `Container` on a page with a direct text-input element fails.
3. tasklog  -- a task-log entry that this commit adds or edits and whose status is
               `To Review` must contain a `### Changed files` section
               (workflow.md Rule 5). Untouched legacy entries are never judged.

A fourth check runs from the commit-msg hook (`--commit-msg <file>`):
4. item     -- feat:/refactor:/perf:/amend: commits need an `Item: <backlog-id>` trailer that
               resolves to a registered item README id. docs:, chore:, merge:, fix: are exempt.

Exit 1 with a readable message per problem; exit 0 otherwise. Bypass: `--no-verify`.
"""
import re
import subprocess
import sys

# Elements that open the soft keyboard; a page containing one must use SafeAreaEdges="All"
# (Container + SoftInput). Direct children only - inputs hidden inside custom components
# are not detected, so "All" is also accepted on pages where none is found.
_TEXT_INPUTS = {
    "Entry", "Editor", "SearchBar", "TextEdit", "MultilineEdit", "PasswordEdit",
    "NumericEdit", "AutoCompleteEdit", "AutocompleteDXField", "AutocompleteField",
}

# Commit subjects (type before ':' or '(') that must carry an `Item: <backlog-id>` trailer.
ITEM_REQUIRED_TYPES = {"feat", "refactor", "perf", "amend"}

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
    name, attrs = _root_tag(xaml)
    if name.split(":")[-1] != "ContentPage":
        return None
    m = re.search(r'SafeAreaEdges\s*=\s*"(\w+)"', attrs)
    value = m.group(1) if m else None
    if value not in ("Container", "All"):
        return (f"safearea: '{path}' root ContentPage must declare SafeAreaEdges=\"Container\" "
                "(or \"All\" on pages with text input); MAUI 10 defaults to None.")
    body = re.sub(r"<!--.*?-->", "", xaml, flags=re.S)
    tags = {t.split(":")[-1] for t in re.findall(r"<\s*([\w:.]+)", body)}
    if value == "Container" and tags & _TEXT_INPUTS:
        return (f"safearea: '{path}' has text input ({', '.join(sorted(tags & _TEXT_INPUTS))}) but "
                "SafeAreaEdges=\"Container\" - use \"All\" (Container + SoftInput) so the keyboard "
                "does not cover the fields.")
    return None


_ITEM_ID = re.compile(r"^id:\s*(\S+)\s*$", re.M)


def backlog_ids(root="Docs/Management"):
    import os
    ids = set()
    for d, _, files in os.walk(root):
        if "README.md" in files:
            try:
                with open(os.path.join(d, "README.md"), encoding="utf-8-sig") as f:
                    head = f.read(1500)
            except OSError:
                continue
            if head.startswith("---"):
                m = _ITEM_ID.search(head.split("---")[1] if head.count("---") >= 2 else head)
                if m:
                    ids.add(m.group(1))
    return ids


def check_item_trailer(message, known_ids):
    subject = next((l for l in message.splitlines() if l.strip() and not l.startswith("#")), "")
    m = re.match(r"([a-z]+)(\([^)]*\))?!?:", subject)
    if not m or m.group(1) not in ITEM_REQUIRED_TYPES:
        return []
    refs = []
    for l in message.splitlines():
        t = re.match(r"Items?:\s*(.+)$", l.strip())
        if t:
            refs += [x for x in re.split(r"[,\s]+", t.group(1)) if x]
    if not refs:
        return [f"item: '{m.group(1)}:' commits need an 'Item: <backlog-id>' trailer "
                "(register the work first: backlog_gen.py register). Exempt: docs, chore, merge, fix."]
    unknown = [r for r in refs if r not in known_ids]
    return [f"item: '{r}' is not a registered backlog item id (see item README frontmatter)." for r in unknown]


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
    if "--commit-msg" in sys.argv:
        path = sys.argv[sys.argv.index("--commit-msg") + 1]
        with open(path, encoding="utf-8", errors="replace") as f:
            problems = check_item_trailer(f.read(), backlog_ids())
        if problems:
            print("commit-msg: FAILED")
            for p in problems:
                print("  " + p)
            return 1
        return 0
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
