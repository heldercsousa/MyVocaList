"""Ledger rotation: move closed rows out of `LEDGER.md § Completed`, verbatim.

`LEDGER.md` is read when work is resumed, so its Completed table must stay short. A Completed
row moves to `LEDGER-archive.md` when (requirements.md AC-04.1b):
  1. its merge date parses and is 7 or more days old, or
  2. its branch is confirmed gone: the Branch cell names exactly one branch and either says
     `(deleted ...)` or the branch exists neither locally nor on a remote.
A Branch cell that is empty, a dash, `develop`, or names several branches never confirms "gone".
Rows are copied byte-for-byte. The Active table, header and footer are never touched. Before
writing, the row count is checked (rows before = rows kept + rows moved); a mismatch aborts.

  python .claude/scripts/ledger_rotate.py            rotate
  python .claude/scripts/ledger_rotate.py --dry-run  list what would move, write nothing
"""
import datetime
import os
import re
import subprocess
import sys

LEDGER = os.path.join("Docs", "Management", "LEDGER.md")
ARCHIVE = os.path.join("Docs", "Management", "LEDGER-archive.md")
MAX_AGE_DAYS = 7
COMPLETED_HEADING = "## Completed"
ARCHIVE_TABLE_HEAD = (
    "| ID | Feature / Bug | Branch | Merged into develop | Commit |",
    "|----|---------------|--------|---------------------|--------|",
)
_DATE = re.compile(r"\b(\d{4})-(\d{2})-(\d{2})\b")
_NOT_A_BRANCH = {"", "-", "—", "–", "develop", "main"}


def _cells(row: str):
    return [c.strip() for c in row.strip().strip("|").split("|")]


def is_data_row(line: str) -> bool:
    s = line.strip()
    return s.startswith("|") and not s.startswith("| ID") and not s.startswith("|--") and not s.startswith("|-")


def merge_date(cell: str):
    m = _DATE.search(cell)
    if not m:
        return None
    try:
        return datetime.date(int(m.group(1)), int(m.group(2)), int(m.group(3)))
    except ValueError:
        return None


def single_branch(cell: str):
    """The one branch a Branch cell names, or None when it names none or several."""
    head = cell.split("(")[0]
    if "," in head or "…" in head or " and " in head:
        return None
    name = head.replace("`", "").strip()
    if name in _NOT_A_BRANCH or " " in name:
        return None
    return name


def branch_exists(name: str, cwd: str) -> bool:
    """True when the branch exists locally or on a remote. On any git error: True (stay)."""
    try:
        local = subprocess.run(["git", "branch", "--list", name], cwd=cwd,
                               capture_output=True, text=True, timeout=15)
        remote = subprocess.run(["git", "branch", "-r", "--list", "*/" + name], cwd=cwd,
                                capture_output=True, text=True, timeout=15)
        if local.returncode != 0 or remote.returncode != 0:
            return True
        return bool(local.stdout.strip() or remote.stdout.strip())
    except Exception:
        return True


def should_move(row: str, today, exists=None, cwd="."):
    """Reason string when the row moves, else None."""
    cells = _cells(row)
    if len(cells) < 4:
        return None
    branch_cell, merged_cell = cells[2], cells[3]
    d = merge_date(merged_cell)
    if d is not None and (today - d).days >= MAX_AGE_DAYS:
        return "merged {0}, {1} days ago".format(d.isoformat(), (today - d).days)
    name = single_branch(branch_cell)
    if name:
        if "(deleted" in branch_cell:
            return "branch marked deleted"
        check = exists if exists is not None else (lambda n: branch_exists(n, cwd))
        if not check(name):
            return "branch `{0}` no longer exists".format(name)
    return None


def split_completed(lines):
    """(start, end) line indexes of the Completed section's data rows region, or None."""
    start = next((i for i, l in enumerate(lines) if l.startswith(COMPLETED_HEADING)), None)
    if start is None:
        return None
    end = next((i for i in range(start + 1, len(lines))
                if lines[i].startswith("## ") or lines[i].strip() == "---"), len(lines))
    return start, end


def plan(text: str, today, exists=None, cwd="."):
    """(kept_text, moved_rows_with_reason). Pure: no file is written."""
    nl = "\r\n" if "\r\n" in text else "\n"
    lines = text.split(nl)
    span = split_completed(lines)
    if span is None:
        return text, []
    start, end = span
    kept, moved = [], []
    for i in range(start, end):
        line = lines[i]
        reason = should_move(line, today, exists, cwd) if is_data_row(line) else None
        if reason:
            moved.append((line, reason))
        else:
            kept.append(line)
    before = sum(1 for i in range(start, end) if is_data_row(lines[i]))
    after = sum(1 for l in kept if is_data_row(l))
    if before != after + len(moved):
        raise RuntimeError("row count mismatch: {0} != {1} + {2}".format(before, after, len(moved)))
    return nl.join(lines[:start] + kept + lines[end:]), moved


def append_to_archive(archive_text: str, rows, today) -> str:
    nl = "\r\n" if "\r\n" in archive_text else "\n"
    heading = "## Rotated Completed rows — {0:04d}-{1:02d}".format(today.year, today.month)
    body = archive_text.rstrip("\r\n")
    if heading not in archive_text:
        body += nl + nl + heading + nl + nl + nl.join(ARCHIVE_TABLE_HEAD)
    return body + nl + nl.join(rows) + nl


def movable_count(root=".") -> int:
    """Rows a rotation would move now. Used by ledger-row-guard.py. 0 on any error."""
    try:
        with open(os.path.join(root, LEDGER), encoding="utf-8", newline="") as f:
            text = f.read()
        return len(plan(text, datetime.date.today(), cwd=root)[1])
    except Exception:
        return 0


def completed_count(text: str) -> int:
    nl = "\r\n" if "\r\n" in text else "\n"
    lines = text.split(nl)
    span = split_completed(lines)
    if span is None:
        return 0
    return sum(1 for i in range(span[0], span[1]) if is_data_row(lines[i]))


def main(argv) -> int:
    root = os.environ.get("CLAUDE_PROJECT_DIR", ".")
    ledger, archive = os.path.join(root, LEDGER), os.path.join(root, ARCHIVE)
    with open(ledger, encoding="utf-8", newline="") as f:
        text = f.read()
    today = datetime.date.today()
    new_text, moved = plan(text, today, cwd=root)
    if not moved:
        print("ledger_rotate: nothing to move ({0} Completed rows).".format(completed_count(text)))
        return 0
    for row, reason in moved:
        print("  move: {0} ({1})".format(_cells(row)[0], reason))
    if "--dry-run" in argv:
        print("ledger_rotate: dry run, {0} rows would move; nothing written.".format(len(moved)))
        return 0
    with open(archive, encoding="utf-8", newline="") as f:
        archive_text = f.read()
    new_archive = append_to_archive(archive_text, [r for r, _ in moved], today)
    for row, _ in moved:
        if new_archive.count(row) < 1:
            raise RuntimeError("a moved row is missing from the archive text")
    with open(archive, "w", encoding="utf-8", newline="") as f:
        f.write(new_archive)
    with open(ledger, "w", encoding="utf-8", newline="") as f:
        f.write(new_text)
    print("ledger_rotate: moved {0} rows to {1}; {2} Completed rows left.".format(
        len(moved), ARCHIVE.replace(os.sep, "/"), completed_count(new_text)))
    return 0


if __name__ == "__main__":
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    sys.exit(main(sys.argv[1:]))
