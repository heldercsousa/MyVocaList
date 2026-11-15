"""Task-log rotation - moves only entries that are ready, like the backlog archive.

Usage:
  python .claude/scripts/tasklog_rotate.py <path/to/task-log.md> [--dry-run]

An entry is one `## ` section. The header (everything before the first `## `) always stays.

An entry is READY to rotate when:
  - the item is closed (README.md beside the log has a Done or Cancelled `status:`), or
  - its heading carries a closed marker (Done, Merged, Closed, Cancelled) AND nothing below holds it.

An entry STAYS (item not closed) when any of these is true:
  - heading has no closed marker (To Review, In Progress, Blocked, no status at all);
  - heading says something is still open ("Done; device check open");
  - heading is a Checkpoint or a Device check section;
  - the body has a `### Checkpoint` block or an unticked `- [ ]` box.

Ready entries are appended, text unchanged, to `task-log-archive-YYYY-MM.md` in the same folder
(month = first date in the heading, else the current month). A new archive file is registered in
MyVocaList.sln next to the task log's own entry.
"""
import datetime
import io
import os
import re
import sys

try:  # headings may hold characters the Windows console cannot print
    sys.stdout.reconfigure(errors="replace")
except Exception:
    pass

CLOSED = re.compile(r"\b(done|merged|closed|cancelled|canceled)\b", re.I)
HOLD_HEADING = re.compile(r"\b(open|pending|to review|in progress|blocked|checkpoint|device check)\b", re.I)
HOLD_BODY = re.compile(r"^(### Checkpoint\b|\s*- \[ \])", re.M)
DATE = re.compile(r"(\d{4})-(\d{2})-\d{2}")
ITEM_CLOSED = re.compile(r"^status:.*(✅|done|cancelled|canceled|❌)", re.I | re.M)


def _read(path):
    return io.open(path, encoding="utf-8", newline="").read()


def _write(path, text):
    io.open(path, "w", encoding="utf-8", newline="").write(text)


def _nl(text):
    return "\r\n" if "\r\n" in text else "\n"


def item_closed(folder):
    readme = os.path.join(folder, "README.md")
    if not os.path.isfile(readme):
        return False
    text = _read(readme)
    if not text.startswith("---"):
        return False
    end = text.find("\n---", 3)
    return bool(ITEM_CLOSED.search(text[: end if end > 0 else len(text)]))


def split(text):
    """Returns (header_lines, [entry_lines, ...]) - an entry starts at a `## ` line."""
    lines = text.split(_nl(text))
    starts = [i for i, l in enumerate(lines) if l.startswith("## ")]
    if not starts:
        return lines, []
    entries = [lines[a:b] for a, b in zip(starts, starts[1:] + [len(lines)])]
    return lines[: starts[0]], entries


def is_ready(entry, closed_item):
    if closed_item:
        return True
    heading = entry[0]
    if not CLOSED.search(heading) or HOLD_HEADING.search(heading):
        return False
    return not HOLD_BODY.search("\n".join(entry[1:]))


def plan(text, closed_item):
    """Returns (header, keep_entries, ready_entries)."""
    header, entries = split(text)
    keep = [e for e in entries if not is_ready(e, closed_item)]
    ready = [e for e in entries if is_ready(e, closed_item)]
    return header, keep, ready


def ready_count(text, folder):
    return len(plan(text, item_closed(folder))[2])


def _month(entry, today):
    m = DATE.search(entry[0])
    return f"{m.group(1)}-{m.group(2)}" if m else today.strftime("%Y-%m")


def _register_in_sln(log_path, archive_path):
    sln = "MyVocaList.sln"
    if not os.path.isfile(sln):
        return "MyVocaList.sln not found - register the archive file by hand."
    to_win = lambda p: os.path.relpath(p).replace("/", "\\")
    log_rel, arc_rel = to_win(log_path), to_win(archive_path)
    text = _read(sln)
    nl = _nl(text)
    lines = text.split(nl)
    if any(l.strip() == f"{arc_rel} = {arc_rel}" for l in lines):
        return None
    for i, l in enumerate(lines):
        if l.strip() == f"{log_rel} = {log_rel}":
            lines.insert(i + 1, l.replace(log_rel, arc_rel))
            _write(sln, nl.join(lines))
            return None
    return f"{log_rel} has no MyVocaList.sln entry - register {arc_rel} by hand."


def rotate(log_path, dry_run=False, today=None):
    today = today or datetime.date.today()
    folder = os.path.dirname(log_path) or "."
    text = _read(log_path)
    nl = _nl(text)
    header, keep, ready = plan(text, item_closed(folder))
    if not ready:
        print(f"tasklog_rotate: nothing ready to rotate in {log_path} ({len(keep)} open entries stay).")
        return 0
    by_month = {}
    for e in ready:
        by_month.setdefault(_month(e, today), []).append(e)
    for e in ready:
        print(f"  rotate -> {_month(e, today)}: {e[0]}")
    for e in keep:
        print(f"  keep: {e[0]}")
    if dry_run:
        print(f"tasklog_rotate: dry run - {len(ready)} entries ready, {len(keep)} stay.")
        return 0
    title = header[0].lstrip("# ").strip() if header and header[0].startswith("#") else "task log"
    for month, entries in sorted(by_month.items()):
        archive = os.path.join(folder, f"task-log-archive-{month}.md")
        body = nl.join(nl.join(e).rstrip() + nl for e in entries)
        if os.path.isfile(archive):
            _write(archive, _read(archive).rstrip() + nl + nl + body)
        else:
            head = (f"# Task log archive {month} - {title}{nl}{nl}"
                    f"Closed entries rotated out of `task-log.md`. Text is unchanged.{nl}{nl}")
            _write(archive, head + body)
            warn = _register_in_sln(log_path, archive)
            if warn:
                print("  WARNING: " + warn)
    pointer = "Closed entries are in the `task-log-archive-YYYY-MM.md` files beside this log."
    head_lines = [l for l in header]
    if pointer not in head_lines:
        while head_lines and not head_lines[-1].strip():
            head_lines.pop()
        head_lines += ["", pointer, ""]
    kept = [l for e in keep for l in e]
    _write(log_path, nl.join(head_lines + kept).rstrip() + nl)
    left = len(head_lines) + len(kept)
    print(f"tasklog_rotate: moved {len(ready)} entries; {log_path} now has about {left} lines.")
    return 0


if __name__ == "__main__":
    args = [a for a in sys.argv[1:] if not a.startswith("--")]
    if len(args) != 1 or not os.path.isfile(args[0]):
        print(__doc__)
        sys.exit(2)
    sys.exit(rotate(args[0], dry_run="--dry-run" in sys.argv))
