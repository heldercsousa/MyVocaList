"""I/O shell for the BACKLOG generator (design section 3).

Verbs: regen [--check] | query | register | status  (register/status land in T6).
All logic lives in the pure modules; this file only walks, reads and writes.
"""
import argparse
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import render  # noqa: E402
from frontmatter import FrontmatterError, parse  # noqa: E402
from model import Item, TERMINAL, order_items, validate  # noqa: E402

MANAGEMENT = os.path.join("Docs", "Management")
ARCHIVE_DIR = "backlog-archive"


def _read(path):
    with open(path, encoding="utf-8") as fh:
        return fh.read()


def _write(path, text):
    directory = os.path.dirname(path)
    if directory and not os.path.isdir(directory):
        os.makedirs(directory)
    with open(path, "w", encoding="utf-8", newline="\n") as fh:
        fh.write(text)


def _rel(root, path):
    rel = os.path.relpath(path, os.path.join(root, MANAGEMENT))
    return rel.replace("\\", "/")


def walk(root):
    """Return (items, errors) for every README.md carrying frontmatter."""
    items, errors = [], []
    base = os.path.join(root, MANAGEMENT)
    for dirpath, dirnames, filenames in os.walk(base):
        if ARCHIVE_DIR in dirpath.replace("\\", "/").split("/"):
            continue
        if "README.md" not in filenames:
            continue
        full = os.path.join(dirpath, "README.md")
        rel_dir = _rel(root, dirpath)
        try:
            text = _read(full)
        except OSError as exc:
            errors.append("{0}/README.md: unreadable ({1})".format(rel_dir, exc))
            continue
        # A README with no frontmatter fence at all is an ordinary doc, not a
        # backlog item -- skip it silently. Only a README that *starts* a fence
        # and then fails to parse is an error (M3: pre-existing docs must not
        # break regen).
        if not text.lstrip().startswith("---"):
            continue
        try:
            keys, _body = parse(text)
        except FrontmatterError as exc:
            errors.append("{0}/README.md: {1}".format(rel_dir, exc.reason))
            continue
        items.append(Item.from_frontmatter(keys, rel_dir + "/"))
    return items, errors


def _render_all(root, items):
    """Return {abs_path: new_text} for BACKLOG.md and every archive month."""
    outputs = {}
    backlog_path = os.path.join(root, MANAGEMENT, "BACKLOG.md")
    if not os.path.exists(backlog_path):
        raise IOError("BACKLOG.md not found at {0}".format(backlog_path))
    outputs[backlog_path] = render.render_backlog(_read(backlog_path), items)

    titles = dict((i.id, i.title) for i in items if i.id)
    for month, month_items in render.bucket_by_month(items).items():
        path = os.path.join(root, MANAGEMENT, ARCHIVE_DIR,
                            "BACKLOG-ARCHIVE-{0}.md".format(month))
        existing = _read(path) if os.path.exists(path) else render.ARCHIVE_TEMPLATE.format(month=month)
        outputs[path] = render.render_archive(existing, month_items, month, titles)
    return outputs


def cmd_regen(root, check=False):
    """0 = clean/written, 1 = stale (check mode), 2 = validation error."""
    items, parse_errors = walk(root)
    errors = parse_errors + validate(items)
    if errors:
        sys.stderr.write("BACKLOG validation failed -- nothing written:\n")
        for err in errors:
            sys.stderr.write("  - {0}\n".format(err))
        return 2

    outputs = _render_all(root, items)
    stale = []
    for path, text in sorted(outputs.items()):
        current = _read(path) if os.path.exists(path) else None
        if current != text:
            stale.append(path)

    if check:
        if stale:
            sys.stderr.write("BACKLOG is stale -- run: python .claude/scripts/backlog/backlog_gen.py regen\n")
            for path in stale:
                sys.stderr.write("  - {0}\n".format(path))
            return 1
        return 0

    for path in stale:
        _write(path, outputs[path])
    return 0


def query_lines(root, statuses):
    """Compact active-work lines. Never raises on a bad file (REQ-SEV-21a)."""
    items, errors = walk(root)
    for err in errors:
        sys.stderr.write("warning: skipped {0}\n".format(err))
    wanted = set(s.strip() for s in statuses if s.strip())
    rows = [i for i in items if not wanted or i.status in wanted]
    lines = []
    for it in order_items(rows):
        lines.append("{0} {1}  {2}{3}  → {4}".format(
            it.status, it.target, "↳" * it.depth, it.title, it.pointer))
    return lines


def cmd_query(root, statuses):
    for line in query_lines(root, statuses):
        print(line)
    return 0


def build_parser():
    parser = argparse.ArgumentParser(prog="backlog_gen")
    parser.add_argument("--root", default=".", help="repo root (default: cwd)")
    sub = parser.add_subparsers(dest="verb")

    regen = sub.add_parser("regen", help="regenerate BACKLOG.md + archives")
    regen.add_argument("--check", action="store_true",
                       help="exit 1 if regeneration would change anything; write nothing")

    query = sub.add_parser("query", help="list items by status")
    query.add_argument("--status", default="",
                       help="comma-separated statuses, e.g. \"\U0001F7E1 In Progress,\U0001F7E2 Ready\"")
    return parser


def main(argv=None):
    args = build_parser().parse_args(argv)
    if args.verb == "regen":
        return cmd_regen(args.root, check=args.check)
    if args.verb == "query":
        return cmd_query(args.root, args.status.split(","))
    build_parser().print_help()
    return 0


if __name__ == "__main__":
    sys.exit(main())
