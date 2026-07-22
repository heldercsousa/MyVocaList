"""I/O shell for the BACKLOG generator (design section 3).

Verbs: regen [--check] | query | register | renumber | status.
All logic lives in the pure modules; this file only walks, reads and writes.
"""
import argparse
import os
import re
import sys
import unicodedata

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import render  # noqa: E402
from frontmatter import FrontmatterError, parse  # noqa: E402
from model import Item, TERMINAL, order_items, validate  # noqa: E402

MANAGEMENT = os.path.join("Docs", "Management")
ARCHIVE_DIR = "backlog-archive"

BUG_ID = re.compile(r"\bBUG-(\d{1,4})\b")


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


def slugify(title):
    """Lowercase ASCII slug for a folder name."""
    text = unicodedata.normalize("NFKD", title or "")
    text = "".join(ch for ch in text if not unicodedata.combining(ch))
    text = text.lower()
    text = re.sub(r"[^a-z0-9]+", "-", text)
    return text.strip("-")


def next_bug_id(root):
    """max(BUG-NNN) + 1 over live folders AND every archive file (REQ-SEV-11a).

    Archives are scanned too so a retired id is never reused -- that fact used
    to live in BACKLOG.md, which agents no longer read.
    """
    highest = 0
    items, _errors = walk(root)
    for it in items:
        match = BUG_ID.search(it.id or "")
        if match:
            highest = max(highest, int(match.group(1)))
    archive_dir = os.path.join(root, MANAGEMENT, ARCHIVE_DIR)
    if os.path.isdir(archive_dir):
        for name in os.listdir(archive_dir):
            try:
                text = _read(os.path.join(archive_dir, name))
            except OSError:
                continue
            for match in BUG_ID.finditer(text):
                highest = max(highest, int(match.group(1)))
    return "BUG-{0:03d}".format(highest + 1)


def _folder_for(root, items, parent_id, kind, today, item_id, title):
    """Absolute path of the new item folder."""
    by_id = dict((i.id, i) for i in items if i.id)
    parent = by_id.get(parent_id)
    if parent is None:
        return None
    bucket = "bugs" if kind == "bug" else "changes"
    name_parts = [today]
    if item_id and kind == "bug":
        name_parts.append(item_id)
    name_parts.append(slugify(title))
    return os.path.join(root, MANAGEMENT, parent.rel_path.rstrip("/"),
                        bucket, "-".join(name_parts))


def _readme_text(keys, body):
    """Serialize frontmatter: known keys in canonical order, then any others.

    The trailing pass matters -- re-serializing on `status` must never silently
    drop a key an author added or a key a later spec version introduces.
    """
    ordered = ("id", "title", "status", "severity", "target", "section",
               "parent", "goal", "gate", "pointer", "closed", "order", "kind")
    lines = ["---"]
    for key in ordered:
        if keys.get(key):
            lines.append("{0}: {1}".format(key, _quote(keys[key])))
    for key in sorted(k for k in keys if k not in ordered):
        if keys.get(key):
            lines.append("{0}: {1}".format(key, _quote(keys[key])))
    lines.append("---")
    lines.append("")
    lines.append(body)
    return "\n".join(lines) + "\n"


def _quote(value):
    text = str(value)
    return '"{0}"'.format(text) if (":" in text or text.strip() != text) else text


def sln_add_entry(sln_text, rel_path):
    """Append a SolutionItems line for a Docs/ file, if not already present."""
    win_path = rel_path.replace("/", "\\")
    if win_path in sln_text:
        return sln_text
    line = "\t\t{0} = {0}\n".format(win_path)
    marker = "\tEndProjectSection\n"
    index = sln_text.find(marker)
    if index == -1:
        return sln_text
    return sln_text[:index] + line + sln_text[index:]


def cmd_register(root, section, parent, kind, severity, title, goal, gate,
                 today=None, item_id=None):
    """Create an item folder + README + .sln entry atomically, then regenerate."""
    import datetime
    today = today or datetime.date.today().isoformat()

    if kind == "bug" and severity == "Minor":
        sys.stderr.write("Minor bugs do not get a folder (REQ-SEV-03) -- "
                         "record it in the parent task-log.\n")
        return 2

    items, parse_errors = walk(root)
    if parse_errors:
        for err in parse_errors:
            sys.stderr.write("error: {0}\n".format(err))
        return 2

    if kind == "bug":
        derived = next_bug_id(root)
        if item_id and item_id != derived:
            sys.stderr.write("expected id {0} but the tree says {1}\n".format(item_id, derived))
            return 2
        item_id = derived
    elif not item_id:
        item_id = slugify(title)

    folder = _folder_for(root, items, parent, kind, today, item_id, title)
    if folder is None:
        sys.stderr.write("parent '{0}' names no existing item\n".format(parent))
        return 2
    if os.path.exists(folder):
        sys.stderr.write("folder already exists: {0}\n".format(folder))
        return 2

    keys = {"id": item_id, "title": title, "status": "\U0001F4A1 Pending",
            "target": today, "goal": goal, "kind": kind}
    if severity:
        keys["severity"] = severity
    if gate:
        keys["gate"] = gate
    if parent:
        keys["parent"] = parent
    if section:
        keys["section"] = section

    readme_path = os.path.join(folder, "README.md")
    rel_readme = os.path.relpath(readme_path, root).replace("\\", "/")
    body = "# {0}\n\n{1}\n".format(title, goal)

    # Stage everything, then write -- REQ-SEV-21a atomicity.
    sln_path = os.path.join(root, "MyVocaList.sln")
    staged = {readme_path: _readme_text(keys, body)}
    if os.path.exists(sln_path):
        staged[sln_path] = sln_add_entry(_read(sln_path), rel_readme)

    for path, text in staged.items():
        _write(path, text)
    return cmd_regen(root)


def cmd_status(root, item_id, status, closed):
    """Set an item's status (and closed month), then regenerate."""
    from model import STATUSES, TERMINAL as _TERMINAL
    if status not in STATUSES:
        sys.stderr.write("invalid status '{0}'\n".format(status))
        return 2
    if status in _TERMINAL and not closed:
        sys.stderr.write("a terminal status requires --closed YYYY-MM\n")
        return 2

    items, _errors = walk(root)
    match = [i for i in items if i.id == item_id]
    if not match:
        sys.stderr.write("no item with id '{0}'\n".format(item_id))
        return 2

    item = match[0]
    path = os.path.join(root, MANAGEMENT, item.rel_path.rstrip("/"), "README.md")
    keys, body = parse(_read(path))
    keys["status"] = status
    if closed:
        keys["closed"] = closed
    _write(path, _readme_text(keys, body.strip()))
    return cmd_regen(root)


def cmd_renumber(root, old_id):
    """Reassign a colliding BUG id: rename the folder and rewrite frontmatter."""
    items, _errors = walk(root)
    match = [i for i in items if i.id == old_id]
    if not match:
        sys.stderr.write("no item with id '{0}'\n".format(old_id))
        return 2
    item = match[0]
    new_id = next_bug_id(root)
    old_dir = os.path.join(root, MANAGEMENT, item.rel_path.rstrip("/"))
    new_dir = os.path.join(os.path.dirname(old_dir),
                           os.path.basename(old_dir).replace(old_id, new_id))
    os.rename(old_dir, new_dir)
    path = os.path.join(new_dir, "README.md")
    keys, body = parse(_read(path))
    keys["id"] = new_id
    keys["title"] = (keys.get("title") or "").replace(old_id, new_id)
    _write(path, _readme_text(keys, body.strip()))
    sys.stderr.write("renumbered {0} -> {1}\n".format(old_id, new_id))
    return cmd_regen(root)


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

    register = sub.add_parser("register", help="create a new item folder")
    register.add_argument("--section")
    register.add_argument("--parent", required=True)
    register.add_argument("--kind", choices=("bug", "change"), required=True)
    register.add_argument("--severity", choices=("Critical", "Major", "Minor"))
    register.add_argument("--title", required=True)
    register.add_argument("--goal", required=True)
    register.add_argument("--gate")
    register.add_argument("--id", dest="item_id", help="assert the expected id")
    # REQ-SEV-11a / design section 3: --renumber belongs to `register`, and when
    # present it is the ONLY argument needed, so the otherwise-required flags
    # must not be enforced. Hence the separate subparser rather than a flag on
    # an argument group that already has required=True members.
    renumber = sub.add_parser("renumber", help="reassign a colliding BUG id")
    renumber.add_argument("item_id")

    status = sub.add_parser("status", help="set an item's status")
    status.add_argument("item_id")
    status.add_argument("new_status")
    status.add_argument("--closed", help="YYYY-MM, required for a terminal status")
    return parser


def main(argv=None):
    args = build_parser().parse_args(argv)
    if args.verb == "regen":
        return cmd_regen(args.root, check=args.check)
    if args.verb == "query":
        return cmd_query(args.root, args.status.split(","))
    if args.verb == "register":
        return cmd_register(args.root, args.section, args.parent, args.kind,
                            args.severity, args.title, args.goal, args.gate,
                            item_id=args.item_id)
    if args.verb == "renumber":
        return cmd_renumber(args.root, args.item_id)
    if args.verb == "status":
        return cmd_status(args.root, args.item_id, args.new_status, args.closed)
    build_parser().print_help()
    return 0


if __name__ == "__main__":
    sys.exit(main())
