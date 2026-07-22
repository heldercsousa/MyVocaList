# .claude/scripts/backlog/model.py
"""Pure item model, validation and ordering for the BACKLOG generator.

No I/O. Every public function is a total function of its arguments, which is
what lets `regen` be idempotent (REQ-SEV-13).
"""
import re

PENDING = "\U0001F4A1 Pending"
SPEC = "\U0001F4CB Spec"
PLAN = "\U0001F5FA️ Plan"
READY = "\U0001F7E2 Ready"
IN_PROGRESS = "\U0001F7E1 In Progress"
DEFERRED = "\U0001F535 Deferred"
BLOCKED = "\U0001F534 Blocked"
DONE = "✅ Done"
FIXED = "✅ Fixed"

STATUSES = (PENDING, SPEC, PLAN, READY, IN_PROGRESS, DEFERRED, BLOCKED, DONE, FIXED)
TERMINAL = (DONE, FIXED)
SEVERITIES = ("Critical", "Major", "Minor")
SECTIONS = ("BusinessFeatures", "DevCycleCraft")
KINDS = ("feature", "bug", "change", "milestone", "group")

REQUIRED = ("id", "title", "status", "target", "goal")

MAX_SENTENCES = 3
MAX_WORDS = 55
DEFAULT_ORDER = 500

_TARGET_DAY = re.compile(r"^\d{4}-\d{2}-\d{2}$")
_TARGET_MONTH = re.compile(r"^\d{4}-\d{2}$")
_CLOSED = re.compile(r"^\d{4}-\d{2}$")

# REQ-SEV-09 banned content: the prose rule in BACKLOG.md's header, mechanized.
_BANNED = (
    # Require at least one digit so ordinary a-f words ("defaced", "facade")
    # are not misread as commit hashes mid-migration.
    (re.compile(r"\b(?=[0-9a-f]{7,40}\b)[a-f]*[0-9][0-9a-f]*\b"), "commit hash"),
    (re.compile(r"\b(PASS|FAIL|CONDITIONAL PASS)\b"), "review verdict"),
    (re.compile(r"\bAC-\d+"), "AC number"),
    (re.compile(r"\b\d+\s*/\s*\d+\b"), "test count"),
    (re.compile(r"\b\d+(\.\d+)?k\s*tokens\b", re.I), "token measurement"),
    (re.compile(r"\S+\.(cs|xaml|py|md)\b"), "file path beyond the pointer"),
)


class Item(object):
    """One backlog row: a feature folder or a bugs/changes item folder."""

    def __init__(self, keys, rel_path):
        self.keys = dict(keys or {})
        self.rel_path = _norm(rel_path)
        self.id = self.keys.get("id", "")
        self.title = self.keys.get("title", "")
        self.status = self.keys.get("status", "")
        self.severity = self.keys.get("severity")
        self.target = self.keys.get("target", "")
        self.section = self.keys.get("section")
        self.parent = self.keys.get("parent")
        self.goal = self.keys.get("goal", "")
        self.gate = self.keys.get("gate")
        self.closed = self.keys.get("closed")
        self.kind = self.keys.get("kind", "feature")
        self.order = self.keys.get("order")
        self.pointer = self.keys.get("pointer") or self.rel_path
        self.depth = _depth(self.rel_path)

    @classmethod
    def from_frontmatter(cls, keys, rel_path):
        return cls(keys, rel_path)

    @property
    def is_terminal(self):
        return self.status in TERMINAL

    @property
    def is_separator(self):
        """Milestone/group rows are layout artifacts, not tracked work."""
        return self.kind in ("milestone", "group")

    def status_label(self):
        """What goes in the Status cell. Separators carry free text there."""
        return self.title if self.kind == "milestone" else self.status

    def __repr__(self):
        return "<Item {0} {1}>".format(self.id, self.rel_path)


def _norm(path):
    return (path or "").replace("\\", "/")


def _path_parent(rel_path):
    """The folder that owns this item: strip the item folder and its bucket.

    'BusinessFeatures/artists-songs/bugs/2026-07-21-BUG-050-x/' ->
    'BusinessFeatures/artists-songs'
    """
    parts = [p for p in _norm(rel_path).split("/") if p]
    if len(parts) >= 2 and parts[-2] in ("bugs", "changes"):
        return "/".join(parts[:-2])
    return "/".join(parts[:-1])


def _depth(rel_path):
    """Render depth = number of bugs/ or changes/ segments above this folder."""
    parts = [p for p in _norm(rel_path).split("/") if p]
    return sum(1 for p in parts if p in ("bugs", "changes"))


def validate(items):
    """Return a list of human-readable errors, each prefixed with the item path."""
    errors = []
    items = list(items or [])
    seen = {}
    ids = set(i.id for i in items if i.id)
    by_path = dict((i.rel_path.rstrip("/"), i) for i in items)

    for it in items:
        def err(msg):
            errors.append("{0}: {1}".format(it.rel_path, msg))

        # Separator rows (milestone/group) carry no goal, status or pointer --
        # they are layout, not work. Everything else must be complete.
        required = ("id", "title", "target") if it.is_separator else REQUIRED
        for key in required:
            if not it.keys.get(key):
                err("missing required key '{0}'".format(key))

        # Section resolution applies to EVERY row: a row that resolves to no
        # section is rendered into neither table and would vanish silently.
        if it.section is not None and it.section not in SECTIONS:
            err("invalid section '{0}'".format(it.section))
        if not it.section and not it.parent:
            err("row resolves to no section -- set 'section' or 'parent'")

        if it.is_separator:
            continue

        if it.status and it.status not in STATUSES:
            err("invalid status '{0}'".format(it.status))
        if it.severity is not None and it.severity not in SEVERITIES:
            err("invalid severity '{0}'".format(it.severity))
        if it.severity == "Minor":
            err("severity 'Minor' must not have a folder (REQ-SEV-03) -- "
                "record it in the parent task-log instead")
        if it.kind not in KINDS:
            err("invalid kind '{0}'".format(it.kind))

        if it.target and not (
            _TARGET_DAY.match(it.target) or _TARGET_MONTH.match(it.target) or it.target == "—"
        ):
            err("invalid target '{0}' (expected YYYY-MM-DD, YYYY-MM or an em dash)".format(it.target))

        if it.is_terminal and not it.closed:
            err("terminal status requires a 'closed: YYYY-MM' month (REQ-SEV-19)")
        if it.closed and not _CLOSED.match(it.closed):
            err("invalid closed '{0}' (expected YYYY-MM)".format(it.closed))
        if it.closed and not it.is_terminal:
            err("'closed' set on a non-terminal item")

        if it.parent and it.parent not in ids:
            err("parent '{0}' names no existing item".format(it.parent))
        elif it.parent:
            # Design section 2: the declared parent must agree with the folder's
            # path parent -- this is what catches a folder filed under the wrong
            # feature (REQ-SEV-21).
            declared = by_path.get(_path_parent(it.rel_path))
            if declared is not None and declared.id != it.parent:
                err("parent '{0}' disagrees with the folder's path parent '{1}'".format(
                    it.parent, declared.id))

        if it.id:
            if it.id in seen:
                err("duplicate id '{0}' (also at {1})".format(it.id, seen[it.id]))
            else:
                seen[it.id] = it.rel_path

        for violation in notes_violations(it.goal, it.gate):
            err(violation)

    return errors


def notes_violations(goal, gate):
    """REQ-SEV-09: <=3 sentences, <=55 words, no banned content."""
    text = " ".join(p for p in (goal or "", gate or "") if p).strip()
    if not text:
        return []
    problems = []
    sentences = [s for s in re.split(r"[.!?]+\s|[.!?]+$", text) if s.strip()]
    if len(sentences) > MAX_SENTENCES:
        problems.append(
            "Notes exceed {0} sentences ({1})".format(MAX_SENTENCES, len(sentences))
        )
    words = [w for w in text.split() if w]
    if len(words) > MAX_WORDS:
        problems.append("Notes exceed {0} words ({1})".format(MAX_WORDS, len(words)))
    for pattern, label in _BANNED:
        if pattern.search(text):
            problems.append("Notes contain banned content ({0})".format(label))
    return problems


def target_sort(target):
    """Normalize a target for sorting: month -> first of month, em dash -> last."""
    if not target or target == "—" or target == "-":
        return "9999-99-99"
    if _TARGET_MONTH.match(target):
        return target + "-01"
    return target


def order_items(items):
    """Order rows: section, then parent chain, then explicit order, target, path."""
    items = list(items or [])
    by_id = dict((i.id, i) for i in items if i.id)

    def own_key(it):
        try:
            explicit = int(it.order)
        except (TypeError, ValueError):
            explicit = DEFAULT_ORDER
        return (explicit, target_sort(it.target), it.rel_path)

    def chain(it):
        """Full sort key: every ancestor's own key, then this item's."""
        keys = []
        node = it
        guard = 0
        while node is not None and guard < 50:
            keys.append(own_key(node))
            node = by_id.get(node.parent) if node.parent else None
            guard += 1
        keys.reverse()
        return keys

    def section_index(it):
        node = it
        guard = 0
        while node is not None and guard < 50:
            if node.section in SECTIONS:
                return SECTIONS.index(node.section)
            node = by_id.get(node.parent) if node.parent else None
            guard += 1
        return len(SECTIONS)

    return sorted(items, key=lambda it: (section_index(it), chain(it)))
