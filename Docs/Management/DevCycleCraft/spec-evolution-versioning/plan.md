# Spec Evolution — Nested folders + generated BACKLOG — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the `Docs/Management/` folder tree the source of truth for backlog items, with `BACKLOG.md` and the monthly archives generated from per-item YAML frontmatter, and replace the session-start 136-line BACKLOG read with a ~12-line query.

**Architecture:** A new pure/shell-split Python package inside `.claude/scripts/backlog/`. Pure modules (`frontmatter.py`, `model.py`, `render.py`) do parsing, validation, ordering and rendering with no I/O; the shell (`backlog_gen.py`) walks the tree, writes files, and exposes five verbs (`register`, `status`, `regen`, `query`, plus `--renumber`). Generation is a total function of frontmatter + fenced preserved regions, which is what makes it idempotent and safe to run at every workflow milestone.

**Tech Stack:** Python 3 (stdlib only — no PyYAML), `unittest` in `.claude/scripts/backlog/tests/`, git, `.claude/githooks/pre-commit`.

**Spec:** `requirements.md` (REQ-SEV-00 … 31, NFR-1 … 5) · `design.md` (§1–§8, decisions R-1/R-2/R-3 approved 2026-07-22).

## Global Constraints

- **Stdlib only.** No PyYAML, no third-party imports, no network. (NFR-1)
- **Frontmatter is a restricted subset:** flat `key: value` only. Nested structures, `-` lists, anchors → validation error naming the key. (NFR-1)
- **Python 2/3-safe style consistent with the existing package:** `.format()` over f-strings, `io`-free plain `open(..., encoding="utf-8")` — match `backlog_lib.py` / `orphan_check.py`.
- **Paths normalized to forward slashes** before comparison or output; files written with **LF** endings. Windows dev + Linux CI. (NFR-3)
- **Row Notes bound:** ≤ 3 sentences AND ≤ 55 whitespace-split words, pointer excluded. (REQ-SEV-09)
- **Date prefix is always `YYYY-MM-DD`**; unknown day → `-01`. (REQ-SEV-00)
- **The eight statuses** are exactly: `💡 Pending`, `📋 Spec`, `🗺️ Plan`, `🟢 Ready`, `🟡 In Progress`, `🔵 Deferred`, `🔴 Blocked`, `✅ Done`. `✅ Fixed` is an accepted terminal synonym of `✅ Done`. Terminal = `✅ Done` / `✅ Fixed`.
- **`.sln` HARD GATE:** every file created/moved/deleted under `Docs/` or `.claude/` gets a `MyVocaList.sln` `SolutionItems` entry in the same commit. `constraints-registry.md` names only `.claude/library/*` and `.claude/rules/*` as exempt. **`.claude/scripts/*` is unresolved — check `MyVocaList.sln` for existing `.claude\scripts\backlog\*` entries at the start of T1 and follow whatever the file already does.** If there are none, treat scripts as exempt and note it in the task-log for Helder to confirm; do not infer silently.
- **Worktree mandatory** for T1–T7 (code). Docs land on `develop` (T8–T13 are docs/migration — see each task).
- **English only** in all code, comments, and docs.

---

## File Structure

| File | Responsibility | Task |
|------|----------------|------|
| `.claude/scripts/backlog/frontmatter.py` | parse a `README.md` → `(dict, body_str)`; raise `FrontmatterError` | T1 |
| `.claude/scripts/backlog/model.py` | `Item` record, `build_tree`, `validate`, sort key | T2 |
| `.claude/scripts/backlog/render.py` | row/table/file rendering, fenced-region splice | T3, T4 |
| `.claude/scripts/backlog/backlog_gen.py` | I/O shell + CLI verbs | T5, T6 |
| `.claude/scripts/backlog/tests/test_frontmatter.py` | T1 tests | T1 |
| `.claude/scripts/backlog/tests/test_model.py` | T2 tests | T2 |
| `.claude/scripts/backlog/tests/test_render.py` | T3/T4 tests | T3, T4 |
| `.claude/scripts/backlog/tests/test_backlog_gen.py` | CLI + idempotency tests | T5, T6 |
| `.claude/scripts/backlog/orphan_check.py` | widen the watched-path set (1 function) | T7 |
| `.claude/githooks/pre-commit` | add blocking `regen --check` | T7 |
| `Docs/Management/**/README.md` | ~50 migrated item/feature folders | T8–T11 |
| `Docs/Management/DevCycleCraft/spec-evolution-versioning/migration/BACKLOG-pre-migration.md` | frozen equivalence fixture | T8 |
| `CLAUDE.md`, `.claude/rules/*`, `.claude/library/*` | the `amend:` bundle | T13 |

**Dependency order (DRY Onion analogue):** T0 → T1 → … → T7 → T7b (merge), then T8 → T9 → T10 → **[handoff seam]** → T11 → T12 → T12b, then T13. All sequential; **no `[P]` parallel tasks** — the file-overlap check shows every task after T2 touches `model.py`'s contract or the same generated files, and every migration task writes `MyVocaList.sln`.

> **Sizing:** the migration tasks below (T9–T13) are written here as coherent *procedures*. They exceed the Rule 2 sizing bound (≤ 5 files / ≤ 2h) as single dispatches, so `tasks.md` **splits them by row group** — T9a/T9b/T9c, T10a/T10b, T11a/T11b/T11c, T12a/T12, T13a/T13b/T13c. Dispatch from `tasks.md`, not from the task headings here; the steps below apply to each split unchanged.

---

### Task 1: Frontmatter parser

**Files:**
- Create: `.claude/scripts/backlog/frontmatter.py`
- Test: `.claude/scripts/backlog/tests/test_frontmatter.py`

**Interfaces:**
- Consumes: nothing.
- Produces: `parse(text) -> (dict, str)` returning `(keys, body_after_frontmatter)`; `class FrontmatterError(Exception)` with attributes `.path` (set by the caller, default `None`) and `.reason` (str). Keys and values are `str`; a value wrapped in matching single or double quotes is unwrapped. Used by T2 and T5.

- [ ] **Step 1: Write the failing tests**

```python
# .claude/scripts/backlog/tests/test_frontmatter.py
import os
import sys
import unittest

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from frontmatter import parse, FrontmatterError  # noqa: E402


class ParseTests(unittest.TestCase):
    def test_parses_flat_keys_and_returns_body(self):
        text = "---\nid: BUG-050\nstatus: \"\U0001F4A1 Pending\"\n---\n\nBody line.\n"
        keys, body = parse(text)
        self.assertEqual(keys["id"], "BUG-050")
        self.assertEqual(keys["status"], "\U0001F4A1 Pending")
        self.assertEqual(body.strip(), "Body line.")

    def test_value_containing_colon_is_kept_whole(self):
        keys, _ = parse("---\ngoal: \"Fix: the thing\"\n---\n")
        self.assertEqual(keys["goal"], "Fix: the thing")

    def test_missing_frontmatter_raises(self):
        with self.assertRaises(FrontmatterError):
            parse("# Just a heading\n")

    def test_unterminated_frontmatter_raises(self):
        with self.assertRaises(FrontmatterError):
            parse("---\nid: X\n")

    def test_nested_structure_raises_naming_the_key(self):
        with self.assertRaises(FrontmatterError) as ctx:
            parse("---\nid: X\ntags:\n  - a\n---\n")
        self.assertIn("tags", ctx.exception.reason)

    def test_list_value_raises(self):
        with self.assertRaises(FrontmatterError):
            parse("---\nid: [a, b]\n---\n")

    def test_duplicate_key_raises(self):
        with self.assertRaises(FrontmatterError) as ctx:
            parse("---\nid: A\nid: B\n---\n")
        self.assertIn("id", ctx.exception.reason)

    def test_blank_lines_and_comments_ignored(self):
        keys, _ = parse("---\n\n# a comment\nid: X\n---\n")
        self.assertEqual(keys, {"id": "X"})


if __name__ == "__main__":
    unittest.main()
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `python -m unittest discover -s .claude/scripts/backlog/tests -p "test_frontmatter*.py" -v`
Expected: FAIL — `ModuleNotFoundError: No module named 'frontmatter'`.

- [ ] **Step 3: Write the implementation**

```python
# .claude/scripts/backlog/frontmatter.py
"""Restricted YAML-frontmatter parser (stdlib only, NFR-1).

Supports exactly one shape: a `---` fenced block of flat `key: value` lines at
the very top of the file. Anything richer (nested block, `-` list, inline list,
anchor) is a FrontmatterError naming the offending key -- the generator must
never silently accept a structure it cannot round-trip.
"""

_FENCE = "---"


class FrontmatterError(Exception):
    """Raised for any unparseable or unsupported frontmatter."""

    def __init__(self, reason, path=None):
        super(FrontmatterError, self).__init__(reason)
        self.reason = reason
        self.path = path


def parse(text):
    """Return (keys_dict, body_str). Raise FrontmatterError on any problem."""
    if text is None:
        raise FrontmatterError("empty file -- no frontmatter block")
    lines = str(text).replace("\r\n", "\n").split("\n")

    idx = 0
    while idx < len(lines) and not lines[idx].strip():
        idx += 1
    if idx >= len(lines) or lines[idx].strip() != _FENCE:
        raise FrontmatterError("file does not start with a '---' frontmatter fence")

    idx += 1
    keys = {}
    while idx < len(lines):
        raw = lines[idx]
        stripped = raw.strip()
        if stripped == _FENCE:
            return (keys, "\n".join(lines[idx + 1:]))
        if not stripped or stripped.startswith("#"):
            idx += 1
            continue
        if raw[:1] in (" ", "\t"):
            raise FrontmatterError(
                "indented/nested value is not supported near: {0}".format(stripped)
            )
        if stripped.startswith("- "):
            raise FrontmatterError("list item is not supported near: {0}".format(stripped))
        if ":" not in stripped:
            raise FrontmatterError("line is not 'key: value': {0}".format(stripped))

        key, _, value = stripped.partition(":")
        key = key.strip()
        value = value.strip()
        if not key:
            raise FrontmatterError("empty key near: {0}".format(stripped))
        if key in keys:
            raise FrontmatterError("duplicate key: {0}".format(key))
        if value.startswith("[") or value.startswith("{"):
            raise FrontmatterError("inline collection is not supported for key: {0}".format(key))
        if not value:
            # A bare `key:` introduces a nested block in YAML -- reject it.
            raise FrontmatterError("key has no inline value (nested block?): {0}".format(key))
        keys[key] = _unquote(value)
        idx += 1

    raise FrontmatterError("unterminated frontmatter block (missing closing '---')")


def _unquote(value):
    """Strip one matching pair of surrounding quotes, if present."""
    if len(value) >= 2 and value[0] == value[-1] and value[0] in ("'", '"'):
        return value[1:-1]
    return value
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `python -m unittest discover -s .claude/scripts/backlog/tests -p "test_frontmatter*.py" -v`
Expected: PASS — 8 tests OK.

- [ ] **Step 5: Commit**

```bash
git add .claude/scripts/backlog/frontmatter.py .claude/scripts/backlog/tests/test_frontmatter.py
git commit -m "feat(backlog-gen): restricted stdlib frontmatter parser (T1)"
```

---

### Task 2: Item model, validation, ordering

**Files:**
- Create: `.claude/scripts/backlog/model.py`
- Test: `.claude/scripts/backlog/tests/test_model.py`

**Interfaces:**
- Consumes: `frontmatter.parse` (T1) — indirectly; T2 takes already-parsed dicts.
- Produces:
  - `STATUSES` (tuple of the 8), `TERMINAL` (`("✅ Done", "✅ Fixed")`), `ACTIVE` (the other 6 + `✅`-less).
  - `Item` — constructed via `Item.from_frontmatter(keys, rel_path)`; attributes `id, title, status, severity, target, section, parent, goal, gate, pointer, closed, order, kind, rel_path, depth`.
  - `validate(items) -> list[str]` — one human-readable error per problem, each starting with the item's `rel_path`.
  - `sort_key(item, index_by_id) -> tuple` and `order_items(items) -> list[Item]`.
  - `notes_violations(goal, gate) -> list[str]` — the REQ-SEV-09 mechanical check, exported for reuse in T3.
  - Used by T3, T5, T6.

- [ ] **Step 1: Write the failing tests**

```python
# .claude/scripts/backlog/tests/test_model.py
import os
import sys
import unittest

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from model import Item, validate, order_items, notes_violations  # noqa: E402

PENDING = "\U0001F4A1 Pending"
DONE = "✅ Done"


def item(**over):
    keys = {
        "id": "X-1",
        "title": "A title",
        "status": PENDING,
        "target": "2026-07-21",
        "section": "DevCycleCraft",
        "goal": "Do the thing.",
    }
    keys.update(over)
    path = keys.pop("_path", "DevCycleCraft/feat/")
    return Item.from_frontmatter(keys, path)


class ValidateTests(unittest.TestCase):
    def test_valid_item_has_no_errors(self):
        self.assertEqual(validate([item()]), [])

    def test_unknown_status_is_an_error(self):
        errors = validate([item(status="banana")])
        self.assertEqual(len(errors), 1)
        self.assertIn("status", errors[0])

    def test_terminal_without_closed_is_an_error(self):
        errors = validate([item(status=DONE)])
        self.assertTrue(any("closed" in e for e in errors))

    def test_terminal_with_closed_is_valid(self):
        self.assertEqual(validate([item(status=DONE, closed="2026-07")]), [])

    def test_minor_severity_folder_is_an_error(self):
        errors = validate([item(severity="Minor")])
        self.assertTrue(any("Minor" in e for e in errors))

    def test_duplicate_id_is_an_error(self):
        errors = validate([item(id="BUG-050"), item(id="BUG-050", _path="DevCycleCraft/other/")])
        self.assertTrue(any("duplicate" in e.lower() for e in errors))

    def test_parent_naming_no_item_is_an_error(self):
        errors = validate([item(parent="ghost")])
        self.assertTrue(any("parent" in e for e in errors))

    def test_missing_required_key_is_an_error(self):
        keys = {"id": "X", "status": PENDING, "target": "2026-07-21", "section": "DevCycleCraft"}
        errors = validate([Item.from_frontmatter(keys, "DevCycleCraft/f/")])
        self.assertTrue(any("title" in e for e in errors))

    def test_bad_target_is_an_error(self):
        self.assertTrue(any("target" in e for e in validate([item(target="July")])))

    def test_error_message_names_the_path(self):
        errors = validate([item(status="banana", _path="DevCycleCraft/thing/")])
        self.assertIn("DevCycleCraft/thing/", errors[0])


class NotesBoundTests(unittest.TestCase):
    def test_within_bound_is_clean(self):
        self.assertEqual(notes_violations("Short goal.", "Short gate."), [])

    def test_four_sentences_violates(self):
        self.assertTrue(notes_violations("One. Two. Three.", "Four."))

    def test_over_55_words_violates(self):
        self.assertTrue(notes_violations(" ".join(["word"] * 56) + ".", ""))

    def test_banned_content_violates(self):
        for banned in ("deadbee1", "code review PASS", "AC-3", "501/501 green", "4.5k tokens"):
            self.assertTrue(notes_violations("Goal " + banned + ".", ""), banned)


class OrderTests(unittest.TestCase):
    def test_explicit_order_beats_target(self):
        a = item(id="a", target="2026-07-01", order="10")
        b = item(id="b", target="2026-01-01")
        self.assertEqual([i.id for i in order_items([b, a])], ["a", "b"])

    def test_month_target_sorts_as_first_of_month(self):
        a = item(id="a", target="2026-07")
        b = item(id="b", target="2026-07-15")
        self.assertEqual([i.id for i in order_items([b, a])], ["a", "b"])

    def test_dash_target_sorts_last(self):
        a = item(id="a", target="—")
        b = item(id="b", target="2026-07-15")
        self.assertEqual([i.id for i in order_items([a, b])], ["b", "a"])

    def test_children_follow_their_parent(self):
        p = item(id="p", target="2026-07-01")
        c = item(id="c", target="2026-01-01", parent="p", _path="DevCycleCraft/feat/bugs/x/")
        other = item(id="z", target="2026-08-01")
        self.assertEqual([i.id for i in order_items([other, c, p])], ["p", "c", "z"])

    def test_depth_comes_from_path(self):
        c = item(id="c", parent="p", _path="DevCycleCraft/feat/bugs/2026-07-21-BUG-1-x/")
        self.assertEqual(c.depth, 1)


if __name__ == "__main__":
    unittest.main()
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `python -m unittest discover -s .claude/scripts/backlog/tests -p "test_model*.py" -v`
Expected: FAIL — `ModuleNotFoundError: No module named 'model'`.

- [ ] **Step 3: Write the implementation**

```python
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

        if it.is_separator:
            continue

        if it.status and it.status not in STATUSES:
            err("invalid status '{0}'".format(it.status))
        if it.severity is not None and it.severity not in SEVERITIES:
            err("invalid severity '{0}'".format(it.severity))
        if it.severity == "Minor":
            err("severity 'Minor' must not have a folder (REQ-SEV-03) -- "
                "record it in the parent task-log instead")
        if it.section is not None and it.section not in SECTIONS:
            err("invalid section '{0}'".format(it.section))
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
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `python -m unittest discover -s .claude/scripts/backlog/tests -p "test_model*.py" -v`
Expected: PASS — 19 tests OK.

- [ ] **Step 5: Commit**

```bash
git add .claude/scripts/backlog/model.py .claude/scripts/backlog/tests/test_model.py
git commit -m "feat(backlog-gen): item model, validation and ordering (T2)"
```

---

### Task 3: Render the live BACKLOG tables into fenced regions

**Files:**
- Create: `.claude/scripts/backlog/render.py`
- Test: `.claude/scripts/backlog/tests/test_render.py`

**Interfaces:**
- Consumes: `model.Item`, `model.order_items`, `model.TERMINAL` (T2).
- Produces:
  - `FENCE_BEGIN = "<!-- BACKLOG:GENERATED:BEGIN {0} -->"`, `FENCE_END = "<!-- BACKLOG:GENERATED:END {0} -->"`.
  - `render_row(item, archived=False) -> str`
  - `render_table(items) -> str`
  - `splice(existing_text, region_name, new_body) -> str` — replaces the fenced region, preserving everything outside byte-for-byte; raises `RenderError` if the fence is absent.
  - `render_backlog(existing_text, items) -> str` — splices the `business-features` and `dev-cycle-craft` regions.
  - Used by T4, T5.

- [ ] **Step 1: Write the failing tests**

```python
# .claude/scripts/backlog/tests/test_render.py
import os
import sys
import unittest

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from model import Item  # noqa: E402
from render import RenderError, render_backlog, render_row, render_table, splice  # noqa: E402

PENDING = "\U0001F4A1 Pending"


def item(**over):
    keys = {
        "id": "X-1", "title": "A title", "status": PENDING,
        "target": "2026-07-21", "section": "BusinessFeatures", "goal": "Do the thing.",
    }
    keys.update(over)
    path = keys.pop("_path", "BusinessFeatures/feat/")
    return Item.from_frontmatter(keys, path)


class RowTests(unittest.TestCase):
    def test_row_has_five_pipes_and_the_pointer(self):
        row = render_row(item())
        self.assertTrue(row.startswith("| 2026-07-21 |"))
        self.assertIn("Goal: Do the thing.", row)
        self.assertIn("`BusinessFeatures/feat/`", row)

    def test_gate_is_included_when_present(self):
        self.assertIn("Gate: Waiting.", render_row(item(gate="Waiting.")))

    def test_depth_renders_arrows(self):
        row = render_row(item(_path="BusinessFeatures/feat/bugs/2026-07-21-BUG-1-x/"))
        self.assertIn("| ↳", row)

    def test_double_depth_renders_two_arrows(self):
        row = render_row(item(_path="BusinessFeatures/f/changes/c/bugs/b/"))
        self.assertIn("↳↳", row)

    def test_archived_row_drops_arrows_and_adds_under_suffix(self):
        child = item(parent="p", _path="BusinessFeatures/feat/bugs/2026-07-21-BUG-1-x/")
        row = render_row(child, archived=True, parent_title="Parent Feature")
        self.assertNotIn("↳", row)
        self.assertIn("(under: Parent Feature)", row)

    def test_explicit_pointer_is_used_verbatim(self):
        self.assertIn("`custom/path/`", render_row(item(pointer="custom/path/")))


class SpliceTests(unittest.TestCase):
    def setUp(self):
        self.text = (
            "# Header\n\nkeep me\n\n"
            "<!-- BACKLOG:GENERATED:BEGIN business-features -->\nOLD\n"
            "<!-- BACKLOG:GENERATED:END business-features -->\n\nfooter\n"
        )

    def test_replaces_only_the_region(self):
        out = splice(self.text, "business-features", "NEW")
        self.assertIn("NEW", out)
        self.assertNotIn("OLD", out)
        self.assertIn("keep me", out)
        self.assertIn("footer", out)

    def test_is_idempotent(self):
        once = splice(self.text, "business-features", "NEW")
        self.assertEqual(once, splice(once, "business-features", "NEW"))

    def test_missing_fence_raises(self):
        with self.assertRaises(RenderError):
            splice("# no fences\n", "business-features", "NEW")

    def test_preserves_content_outside_byte_for_byte(self):
        out = splice(self.text, "business-features", "NEW")
        self.assertTrue(out.startswith("# Header\n\nkeep me\n\n"))
        self.assertTrue(out.endswith("\n\nfooter\n"))


class BacklogTests(unittest.TestCase):
    def test_terminal_items_are_excluded_from_the_live_file(self):
        text = (
            "<!-- BACKLOG:GENERATED:BEGIN business-features -->\n"
            "<!-- BACKLOG:GENERATED:END business-features -->\n"
            "<!-- BACKLOG:GENERATED:BEGIN dev-cycle-craft -->\n"
            "<!-- BACKLOG:GENERATED:END dev-cycle-craft -->\n"
        )
        done = item(id="d", title="Shipped", status="✅ Done", closed="2026-07")
        out = render_backlog(text, [item(id="a", title="Active"), done])
        self.assertIn("Active", out)
        self.assertNotIn("Shipped", out)


if __name__ == "__main__":
    unittest.main()
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `python -m unittest discover -s .claude/scripts/backlog/tests -p "test_render*.py" -v`
Expected: FAIL — `ModuleNotFoundError: No module named 'render'`.

- [ ] **Step 3: Write the implementation**

```python
# .claude/scripts/backlog/render.py
"""Pure rendering of BACKLOG rows, tables and fenced regions.

Everything outside a BACKLOG:GENERATED fence is preserved byte-for-byte
(REQ-SEV-14/20); everything inside is a total function of the items, which is
what makes regeneration idempotent (REQ-SEV-13).
"""
from model import TERMINAL, order_items

FENCE_BEGIN = "<!-- BACKLOG:GENERATED:BEGIN {0} -->"
FENCE_END = "<!-- BACKLOG:GENERATED:END {0} -->"

ARROW = "↳"

TABLE_HEAD_BUSINESS = "| Target | Feature | Status | Notes |\n|--------|---------|--------|-------|"
TABLE_HEAD_CRAFT = "| Target | Activity | Status | Notes |\n|--------|----------|--------|-------|"
TABLE_HEAD_ARCHIVE = "| Target | Feature/Item | Status | Notes |\n|--------|--------------|--------|-------|"


class RenderError(Exception):
    """Raised when a required generated fence is missing."""


def render_row(item, archived=False, parent_title=None):
    """One markdown table row. Archived rows drop depth arrows (design section 3)."""
    # Separator artifacts (REQ-SEV-17). Column order is Target | Label | Status
    # | Notes -- the milestone's marker belongs in the Status cell, matching
    # the frozen fixture's `| 2026-06 | | 🏁 **MVP release** | |`.
    if item.kind == "milestone":
        return "| {0} | | {1} | |".format(item.target, item.status_label())
    if item.kind == "group":
        return "| {0} | **{1}** | — | {2} |".format(item.target, item.title, item.goal or "")

    if archived:
        # Archived rows drop depth arrows: the parent row is not in the file,
        # so the arrows are meaningless and would break the byte-identical
        # round-trip (REQ-SEV-13).
        label = item.title
        if parent_title:
            label = "{0} (under: {1})".format(label, parent_title)
    elif item.depth:
        label = "{0} {1}".format(ARROW * item.depth, item.title)
    else:
        label = item.title

    notes = "Goal: {0}".format(item.goal)
    if item.gate:
        notes = "{0} Gate: {1}".format(notes, item.gate)
    notes = "{0} Pointer: `{1}`.".format(notes, item.pointer)

    return "| {0} | {1} | {2} | {3} |".format(item.target, label, item.status, notes)


def render_table(items, head=TABLE_HEAD_BUSINESS, archived=False, titles_by_id=None):
    """Ordered rows under a table header."""
    titles_by_id = titles_by_id or {}
    rows = [head]
    for it in order_items(items):
        parent_title = titles_by_id.get(it.parent) if archived else None
        rows.append(render_row(it, archived=archived, parent_title=parent_title))
    return "\n".join(rows)


def splice(text, region_name, new_body):
    """Replace a fenced region's body, preserving everything outside it."""
    begin = FENCE_BEGIN.format(region_name)
    end = FENCE_END.format(region_name)
    start = text.find(begin)
    stop = text.find(end)
    if start == -1 or stop == -1 or stop < start:
        raise RenderError("missing generated fence for region '{0}'".format(region_name))
    head = text[:start + len(begin)]
    tail = text[stop:]
    return "{0}\n{1}\n{2}".format(head, new_body, tail)


def render_backlog(existing_text, items):
    """Splice both live tables. Terminal items are excluded (REQ-SEV-16)."""
    active = [i for i in items if i.status not in TERMINAL]
    business = [i for i in active if _section_of(i, items) == "BusinessFeatures"]
    craft = [i for i in active if _section_of(i, items) == "DevCycleCraft"]

    out = splice(existing_text, "business-features",
                 render_table(business, TABLE_HEAD_BUSINESS))
    out = splice(out, "dev-cycle-craft", render_table(craft, TABLE_HEAD_CRAFT))
    return out


def _section_of(item, all_items):
    """Resolve an item's section by walking up its parent chain."""
    by_id = dict((i.id, i) for i in all_items if i.id)
    node = item
    guard = 0
    while node is not None and guard < 50:
        if node.section:
            return node.section
        node = by_id.get(node.parent) if node.parent else None
        guard += 1
    return None
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `python -m unittest discover -s .claude/scripts/backlog/tests -p "test_render*.py" -v`
Expected: PASS — 11 tests OK.

> **Arrow spacing is settled here, not later:** the label is always `"{arrows} {title}"` — arrows concatenated with no internal spaces, one space before the title, at every depth. This matches the frozen fixture's `| ↳ BUG-027: …` and `| ↳↳ Build new MD3-compliant…` rows. Do **not** adjust these tests to match an implementation; if they fail, the implementation is wrong (`testing.md § Builder Must Not Modify Tests`).

- [ ] **Step 5: Commit**

```bash
git add .claude/scripts/backlog/render.py .claude/scripts/backlog/tests/test_render.py
git commit -m "feat(backlog-gen): row/table rendering + fenced-region splice (T3)"
```

---

### Task 4: Monthly archive rendering

**Files:**
- Modify: `.claude/scripts/backlog/render.py` (add `render_archive`, `bucket_by_month`)
- Modify: `.claude/scripts/backlog/tests/test_render.py` (append `ArchiveTests`)

**Interfaces:**
- Consumes: T3's `render_table`, `splice`; `model.TERMINAL`.
- Produces:
  - `bucket_by_month(items) -> dict[str, list[Item]]` — terminal items keyed by `closed`.
  - `render_archive(existing_text, items, month, titles_by_id) -> str`
  - `ARCHIVE_TEMPLATE` — the header used when a month's file does not yet exist.
  - Used by T5.

- [ ] **Step 1: Write the failing tests (append to `test_render.py`)**

```python
class ArchiveTests(unittest.TestCase):
    def setUp(self):
        from render import ARCHIVE_TEMPLATE
        self.template = ARCHIVE_TEMPLATE.format(month="2026-07")

    def test_buckets_terminal_items_by_closed_month(self):
        from render import bucket_by_month
        a = item(id="a", status="✅ Done", closed="2026-07")
        b = item(id="b", status="✅ Fixed", closed="2026-06")
        c = item(id="c")  # active -> never bucketed
        buckets = bucket_by_month([a, b, c])
        self.assertEqual(sorted(buckets.keys()), ["2026-06", "2026-07"])
        self.assertEqual([i.id for i in buckets["2026-07"]], ["a"])

    def test_done_child_archives_while_active_parent_stays(self):
        from render import bucket_by_month
        parent = item(id="p", title="Parent")
        child = item(id="c", title="Child", status="✅ Done", closed="2026-07",
                     parent="p", _path="BusinessFeatures/feat/bugs/2026-07-21-BUG-1-x/")
        buckets = bucket_by_month([parent, child])
        self.assertEqual([i.id for i in buckets["2026-07"]], ["c"])
        self.assertNotIn("p", [i.id for i in buckets["2026-07"]])

    def test_archived_child_keeps_bug_id_greppable(self):
        from render import render_archive
        child = item(id="BUG-048", title="BUG-048: pagination reloads (Major)",
                     status="✅ Done", closed="2026-07", parent="p",
                     _path="DevCycleCraft/f/bugs/2026-07-21-BUG-048-x/")
        out = render_archive(self.template, [child], "2026-07", {"p": "Parent Feature"})
        self.assertIn("BUG-048", out)
        self.assertIn("(under: Parent Feature)", out)

    def test_archive_render_is_idempotent(self):
        from render import render_archive
        a = item(id="a", status="✅ Done", closed="2026-07")
        once = render_archive(self.template, [a], "2026-07", {})
        self.assertEqual(once, render_archive(once, [a], "2026-07", {}))
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `python -m unittest discover -s .claude/scripts/backlog/tests -p "test_render*.py" -v`
Expected: FAIL — `ImportError: cannot import name 'ARCHIVE_TEMPLATE'`.

- [ ] **Step 3: Add the implementation to `render.py`**

```python
ARCHIVE_TEMPLATE = """# BACKLOG Archive — {month}

> Closed backlog rows completed in {month}, moved out of `Docs/Management/BACKLOG.md`. Rows use the slim PO template: Goal + one-sentence outcome + pointer. **Past BUG-NNN / feature lookups must grep all `backlog-archive/` files.**

## Archived rows

<!-- BACKLOG:GENERATED:BEGIN archive -->
<!-- BACKLOG:GENERATED:END archive -->
"""


def bucket_by_month(items):
    """Group terminal items by their `closed` month (REQ-SEV-18).

    Bucketing is per item, never per subtree -- that is what lets a Done
    sub-row archive while its still-active parent stays in the live file.
    """
    buckets = {}
    for it in items or []:
        if it.status in TERMINAL and it.closed:
            buckets.setdefault(it.closed, []).append(it)
    return buckets


def render_archive(existing_text, items, month=None, titles_by_id=None):
    """Splice one month's archive table into its file.

    `month` is accepted for call-site clarity and future header rendering; the
    table body itself is a pure function of `items`.
    """
    body = render_table(
        items, head=TABLE_HEAD_ARCHIVE, archived=True, titles_by_id=titles_by_id or {}
    )
    return splice(existing_text, "archive", body)
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `python -m unittest discover -s .claude/scripts/backlog/tests -p "test_render*.py" -v`
Expected: PASS — 15 tests OK.

- [ ] **Step 5: Commit**

```bash
git add .claude/scripts/backlog/render.py .claude/scripts/backlog/tests/test_render.py
git commit -m "feat(backlog-gen): monthly archive rendering keyed by closed month (T4)"
```

---

### Task 5: CLI shell — `regen`, `--check`, `query`

**Files:**
- Create: `.claude/scripts/backlog/backlog_gen.py`
- Test: `.claude/scripts/backlog/tests/test_backlog_gen.py`

**Interfaces:**
- Consumes: `frontmatter.parse` (T1), `model.*` (T2), `render.*` (T3, T4).
- Produces:
  - `walk(root) -> (items, parse_errors)` — every `README.md` under `Docs/Management/` that **opens with a `---` fence**. A README with no fence is an ordinary doc and is skipped silently; a README that opens a fence and then fails to parse is a hard error.
  - `cmd_regen(root, check=False) -> int` — 0 = clean, 1 = would change / did change under `--check`, 2 = validation error.
  - `cmd_query(root, statuses) -> int`
  - Used by T6, T7.

- [ ] **Step 1: Write the failing tests**

```python
# .claude/scripts/backlog/tests/test_backlog_gen.py
import os
import shutil
import sys
import tempfile
import unittest

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import backlog_gen  # noqa: E402

PENDING = "\U0001F4A1 Pending"

BACKLOG_SKELETON = (
    "# MyVocaList — Product Backlog\n\nHeader prose that must survive.\n\n"
    "## Business Features\n\n"
    "<!-- BACKLOG:GENERATED:BEGIN business-features -->\n"
    "<!-- BACKLOG:GENERATED:END business-features -->\n\n"
    "## Dev Cycle Craft\n\n"
    "<!-- BACKLOG:GENERATED:BEGIN dev-cycle-craft -->\n"
    "<!-- BACKLOG:GENERATED:END dev-cycle-craft -->\n"
)


def write(path, text):
    directory = os.path.dirname(path)
    if not os.path.isdir(directory):
        os.makedirs(directory)
    with open(path, "w", encoding="utf-8", newline="\n") as fh:
        fh.write(text)


def readme(**over):
    keys = {"id": "F-1", "title": "Feature One", "status": PENDING,
            "target": "2026-07-21", "section": "BusinessFeatures", "goal": "Ship it."}
    keys.update(over)
    # An empty value means "omit this key" -- the parser rejects `key:` with no
    # inline value, so a fixture must never emit one.
    lines = ["---"] + ["{0}: {1}".format(k, v)
                       for k, v in sorted(keys.items()) if v not in (None, "")]
    lines += ["---", "", "Body."]
    return "\n".join(lines) + "\n"


# A minimal but REAL .sln shape: sln_add_entry inserts before EndProjectSection,
# so the fixture must contain one (matching constraints-registry.md).
SLN_FIXTURE = (
    "Microsoft Visual Studio Solution File, Format Version 12.00\n"
    "Project(\"{2150E333-8FDC-42A3-9474-1A3956D46DE8}\") = \"Docs\", \"Docs\", "
    "\"{FA1234BC-0001-4000-8000-000000000001}\"\n"
    "\tProjectSection(SolutionItems) = preProject\n"
    "\tEndProjectSection\n"
    "EndProject\n"
    "Global\nEndGlobal\n"
)


class RegenTests(unittest.TestCase):
    def setUp(self):
        self.root = tempfile.mkdtemp()
        self.mgmt = os.path.join(self.root, "Docs", "Management")
        write(os.path.join(self.mgmt, "BACKLOG.md"), BACKLOG_SKELETON)
        write(os.path.join(self.mgmt, "BusinessFeatures", "feat", "README.md"), readme())

    def tearDown(self):
        shutil.rmtree(self.root, ignore_errors=True)

    def _backlog(self):
        with open(os.path.join(self.mgmt, "BACKLOG.md"), encoding="utf-8") as fh:
            return fh.read()

    def test_regen_writes_the_row(self):
        self.assertEqual(backlog_gen.cmd_regen(self.root), 0)
        self.assertIn("Feature One", self._backlog())

    def test_regen_preserves_header_prose(self):
        backlog_gen.cmd_regen(self.root)
        self.assertIn("Header prose that must survive.", self._backlog())

    def test_regen_is_idempotent_byte_for_byte(self):
        backlog_gen.cmd_regen(self.root)
        first = self._backlog()
        backlog_gen.cmd_regen(self.root)
        self.assertEqual(first, self._backlog())

    def test_check_returns_zero_when_clean(self):
        backlog_gen.cmd_regen(self.root)
        self.assertEqual(backlog_gen.cmd_regen(self.root, check=True), 0)

    def test_check_returns_one_when_stale_and_writes_nothing(self):
        before = self._backlog()
        self.assertEqual(backlog_gen.cmd_regen(self.root, check=True), 1)
        self.assertEqual(before, self._backlog())

    def test_validation_error_aborts_without_writing(self):
        write(os.path.join(self.mgmt, "BusinessFeatures", "bad", "README.md"),
              readme(id="F-2", status="banana"))
        before = self._backlog()
        self.assertEqual(backlog_gen.cmd_regen(self.root), 2)
        self.assertEqual(before, self._backlog())

    def test_terminal_item_is_written_to_its_archive_month(self):
        write(os.path.join(self.mgmt, "BusinessFeatures", "feat", "bugs",
                           "2026-07-21-BUG-1-x", "README.md"),
              readme(id="BUG-1", title="BUG-1: a thing (Major)", status="✅ Done",
                     closed="2026-07", parent="F-1", severity="Major", section=None))
        self.assertEqual(backlog_gen.cmd_regen(self.root), 0)
        archive = os.path.join(self.mgmt, "backlog-archive", "BACKLOG-ARCHIVE-2026-07.md")
        self.assertTrue(os.path.exists(archive))
        with open(archive, encoding="utf-8") as fh:
            self.assertIn("BUG-1", fh.read())
        self.assertNotIn("BUG-1", self._backlog())


class QueryTests(unittest.TestCase):
    def setUp(self):
        self.root = tempfile.mkdtemp()
        self.mgmt = os.path.join(self.root, "Docs", "Management")
        write(os.path.join(self.mgmt, "BACKLOG.md"), BACKLOG_SKELETON)
        write(os.path.join(self.mgmt, "BusinessFeatures", "a", "README.md"),
              readme(id="A", title="Active one", status="\U0001F7E1 In Progress"))
        write(os.path.join(self.mgmt, "BusinessFeatures", "b", "README.md"),
              readme(id="B", title="Pending one", status=PENDING))

    def tearDown(self):
        shutil.rmtree(self.root, ignore_errors=True)

    def test_query_filters_by_status(self):
        lines = backlog_gen.query_lines(self.root, ["\U0001F7E1 In Progress"])
        self.assertEqual(len(lines), 1)
        self.assertIn("Active one", lines[0])

    def test_query_skips_malformed_readme_without_failing(self):
        write(os.path.join(self.mgmt, "BusinessFeatures", "broken", "README.md"),
              "no frontmatter at all\n")
        lines = backlog_gen.query_lines(self.root, [PENDING])
        self.assertEqual(len(lines), 1)


if __name__ == "__main__":
    unittest.main()
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `python -m unittest discover -s .claude/scripts/backlog/tests -p "test_backlog_gen*.py" -v`
Expected: FAIL — `ModuleNotFoundError: No module named 'backlog_gen'`.

- [ ] **Step 3: Write the implementation**

```python
# .claude/scripts/backlog/backlog_gen.py
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
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `python -m unittest discover -s .claude/scripts/backlog/tests -p "test_backlog_gen*.py" -v`
Expected: PASS — 9 tests OK.

- [ ] **Step 5: Run the whole suite (nothing regressed)**

Run: `python -m unittest discover -s .claude/scripts/backlog/tests -v`
Expected: PASS — all tests OK, including the pre-existing `backlog_lib` tests.

- [ ] **Step 6: Commit**

```bash
git add .claude/scripts/backlog/backlog_gen.py .claude/scripts/backlog/tests/test_backlog_gen.py
git commit -m "feat(backlog-gen): regen/--check/query CLI shell (T5)"
```

---

### Task 6: `register`, `status`, `--renumber` (+ atomic `.sln` write)

**Files:**
- Modify: `.claude/scripts/backlog/backlog_gen.py`
- Modify: `.claude/scripts/backlog/tests/test_backlog_gen.py` (append `RegisterTests`)

**Interfaces:**
- Consumes: T5's `walk`, `cmd_regen`, `_write`.
- Produces: `next_bug_id(root)`, `slugify(title)`, `folder_name(date, bug_id, title)`, `cmd_register(...)`, `cmd_status(root, item_id, status, closed)`, `cmd_renumber(root, old_id)`; `sln_add_entry(sln_text, rel_path, section_name)`.

- [ ] **Step 1: Write the failing tests (append to `test_backlog_gen.py`)**

```python
class RegisterTests(unittest.TestCase):
    def setUp(self):
        self.root = tempfile.mkdtemp()
        self.mgmt = os.path.join(self.root, "Docs", "Management")
        write(os.path.join(self.mgmt, "BACKLOG.md"), BACKLOG_SKELETON)
        write(os.path.join(self.mgmt, "BusinessFeatures", "feat", "README.md"), readme())
        write(os.path.join(self.root, "MyVocaList.sln"), SLN_FIXTURE)

    def tearDown(self):
        shutil.rmtree(self.root, ignore_errors=True)

    def test_slugify_lowercases_and_hyphenates(self):
        self.assertEqual(backlog_gen.slugify("Song form — Artist NOT locked!"),
                         "song-form-artist-not-locked")

    def test_next_bug_id_starts_at_one_when_tree_is_empty(self):
        self.assertEqual(backlog_gen.next_bug_id(self.root), "BUG-001")

    def test_next_bug_id_is_max_plus_one(self):
        write(os.path.join(self.mgmt, "BusinessFeatures", "feat", "bugs",
                           "2026-07-21-BUG-050-x", "README.md"),
              readme(id="BUG-050", title="BUG-050: x (Major)", severity="Major",
                     parent="F-1", section=None))
        self.assertEqual(backlog_gen.next_bug_id(self.root), "BUG-051")

    def test_next_bug_id_also_scans_archives_so_ids_are_never_reused(self):
        write(os.path.join(self.mgmt, "backlog-archive", "BACKLOG-ARCHIVE-2026-06.md"),
              "| 2026-06 | BUG-099: retired thing | ✅ Fixed | Goal: x. |\n")
        self.assertEqual(backlog_gen.next_bug_id(self.root), "BUG-100")

    def test_register_creates_folder_readme_and_regenerates(self):
        rc = backlog_gen.cmd_register(
            self.root, section=None, parent="F-1", kind="bug", severity="Major",
            title="Artist field not locked", goal="Lock the field.", gate=None,
            today="2026-07-22")
        self.assertEqual(rc, 0)
        folder = os.path.join(self.mgmt, "BusinessFeatures", "feat", "bugs",
                              "2026-07-22-BUG-001-artist-field-not-locked")
        self.assertTrue(os.path.exists(os.path.join(folder, "README.md")))
        with open(os.path.join(self.mgmt, "BACKLOG.md"), encoding="utf-8") as fh:
            self.assertIn("Artist field not locked", fh.read())

    def test_register_adds_the_sln_entry(self):
        backlog_gen.cmd_register(
            self.root, section=None, parent="F-1", kind="bug", severity="Major",
            title="Some bug", goal="Fix it.", gate=None, today="2026-07-22")
        with open(os.path.join(self.root, "MyVocaList.sln"), encoding="utf-8") as fh:
            self.assertIn("2026-07-22-BUG-001-some-bug", fh.read())

    def test_register_rejects_minor_severity(self):
        rc = backlog_gen.cmd_register(
            self.root, section=None, parent="F-1", kind="bug", severity="Minor",
            title="Cosmetic", goal="Tidy.", gate=None, today="2026-07-22")
        self.assertEqual(rc, 2)

    def test_register_is_atomic_nothing_written_on_failure(self):
        before = sorted(os.listdir(os.path.join(self.mgmt, "BusinessFeatures", "feat")))
        backlog_gen.cmd_register(
            self.root, section=None, parent="ghost", kind="bug", severity="Major",
            title="Orphan", goal="x.", gate=None, today="2026-07-22")
        self.assertEqual(before, sorted(os.listdir(
            os.path.join(self.mgmt, "BusinessFeatures", "feat"))))

    def test_status_updates_frontmatter_and_regenerates(self):
        self.assertEqual(backlog_gen.cmd_status(self.root, "F-1", "\U0001F7E1 In Progress", None), 0)
        with open(os.path.join(self.mgmt, "BusinessFeatures", "feat", "README.md"),
                  encoding="utf-8") as fh:
            self.assertIn("\U0001F7E1 In Progress", fh.read())

    def test_status_terminal_requires_closed(self):
        self.assertEqual(backlog_gen.cmd_status(self.root, "F-1", "✅ Done", None), 2)
        self.assertEqual(backlog_gen.cmd_status(self.root, "F-1", "✅ Done", "2026-07"), 0)

    def test_status_on_unknown_id_changes_nothing(self):
        before = self._readme()
        self.assertEqual(backlog_gen.cmd_status(self.root, "NOPE", "\U0001F7E1 In Progress", None), 2)
        self.assertEqual(before, self._readme())

    def _readme(self):
        with open(os.path.join(self.mgmt, "BusinessFeatures", "feat", "README.md"),
                  encoding="utf-8") as fh:
            return fh.read()

    def test_status_preserves_unknown_frontmatter_keys(self):
        path = os.path.join(self.mgmt, "BusinessFeatures", "feat", "README.md")
        write(path, readme(reviewer="Helder"))
        backlog_gen.cmd_status(self.root, "F-1", "\U0001F7E1 In Progress", None)
        with open(path, encoding="utf-8") as fh:
            self.assertIn("reviewer: Helder", fh.read())

    def test_renumber_renames_folder_and_rewrites_id(self):
        write(os.path.join(self.mgmt, "BusinessFeatures", "feat", "bugs",
                           "2026-07-21-BUG-001-dup", "README.md"),
              readme(id="BUG-001", title="BUG-001: dup (Major)", severity="Major",
                     parent="F-1", section=None, kind="bug"))
        self.assertEqual(backlog_gen.cmd_renumber(self.root, "BUG-001"), 0)
        self.assertTrue(os.path.isdir(os.path.join(
            self.mgmt, "BusinessFeatures", "feat", "bugs", "2026-07-21-BUG-002-dup")))
        with open(os.path.join(self.mgmt, "BusinessFeatures", "feat", "bugs",
                               "2026-07-21-BUG-002-dup", "README.md"),
                  encoding="utf-8") as fh:
            text = fh.read()
        self.assertIn("id: BUG-002", text)
        self.assertIn("BUG-002: dup", text)

    def test_renumber_on_unknown_id_changes_nothing(self):
        self.assertEqual(backlog_gen.cmd_renumber(self.root, "BUG-999"), 2)
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `python -m unittest discover -s .claude/scripts/backlog/tests -p "test_backlog_gen*.py" -v`
Expected: FAIL — `AttributeError: module 'backlog_gen' has no attribute 'slugify'`.

- [ ] **Step 3: Add the implementation to `backlog_gen.py`**

```python
import re
import unicodedata

BUG_ID = re.compile(r"\bBUG-(\d{1,4})\b")


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
```

Then extend `build_parser()` with the two verbs and wire them in `main()`:

```python
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
```

Note the deviation from design §3's literal `register --renumber <id>` spelling: argparse cannot express "these six flags are required *unless* `--renumber` is passed" without a custom action. `renumber <id>` is the same operation with a reachable CLI. Record this in the task-log as a spec-text deviation (behaviour unchanged).

```python
    if args.verb == "register":
        return cmd_register(args.root, args.section, args.parent, args.kind,
                            args.severity, args.title, args.goal, args.gate,
                            item_id=args.item_id)
    if args.verb == "renumber":
        return cmd_renumber(args.root, args.item_id)
    if args.verb == "status":
        return cmd_status(args.root, args.item_id, args.new_status, args.closed)
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `python -m unittest discover -s .claude/scripts/backlog/tests -v`
Expected: PASS — all tests OK (≈ 46 total).

- [ ] **Step 5: Commit**

```bash
git add .claude/scripts/backlog/backlog_gen.py .claude/scripts/backlog/tests/test_backlog_gen.py
git commit -m "feat(backlog-gen): register/status/renumber with atomic .sln write (T6)"
```

---

### Task 7: Widen `orphan_check`'s watch set

> **The blocking pre-commit gate is NOT installed here — it moved to T12b.** Installing it now would block T8–T11's own commits: those tasks deliberately leave BACKLOG.md stale (`regen --check` exits 1 mid-migration, by design), so a blocking gate would make the migration uncommittable. The gate goes in only once the equivalence gate proves regeneration is clean.

**Files:**
- Modify: `.claude/scripts/backlog/orphan_check.py` (`backlog_changed_this_session` + the new watch helpers)
- Create: `.claude/scripts/backlog/tests/test_orphan_check_widening.py`

**Interfaces:**
- Consumes: T5's `cmd_regen(check=True)`.
- Produces: no new API; behavioural change only.

- [ ] **Step 1: Write the failing test**

```python
# .claude/scripts/backlog/tests/test_orphan_check_widening.py
import os
import sys
import unittest

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import orphan_check  # noqa: E402


class WatchedPathTests(unittest.TestCase):
    def test_backlog_is_watched(self):
        self.assertIn("Docs/Management/BACKLOG.md", orphan_check.WATCHED_PATHS)

    def test_management_readmes_are_watched(self):
        self.assertTrue(any("README" in p or "Docs/Management" == p
                            for p in orphan_check.WATCHED_PATHS))

    def test_registering_a_folder_counts_as_backlog_activity(self):
        self.assertTrue(orphan_check.is_watched(
            "Docs/Management/BusinessFeatures/artists-songs/bugs/2026-07-22-BUG-053-x/README.md"))

    def test_unrelated_doc_does_not_count(self):
        self.assertFalse(orphan_check.is_watched("Docs/Changelog/changelog.md"))


if __name__ == "__main__":
    unittest.main()
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `python -m unittest discover -s .claude/scripts/backlog/tests -p "test_orphan_check_widening*.py" -v`
Expected: FAIL — `AttributeError: module 'orphan_check' has no attribute 'WATCHED_PATHS'`.

- [ ] **Step 3: Widen `orphan_check.py`**

Add near the top:

```python
# Registering an item the new way writes a folder README, not BACKLOG.md itself
# (BACKLOG.md is generated from it). Both count as "the agent registered the work".
WATCHED_PATHS = ("Docs/Management/BACKLOG.md", "Docs/Management")


def is_watched(path):
    """True for BACKLOG.md or any Docs/Management/**/README.md."""
    norm = (path or "").replace("\\", "/")
    if norm == "Docs/Management/BACKLOG.md":
        return True
    return norm.startswith("Docs/Management/") and norm.endswith("/README.md")
```

Then rewrite `backlog_changed_this_session`'s three git calls. Replace each `-- backlog` pathspec with the widened pair and filter the output through `is_watched`. The full replacement body:

```python
    backlog = "Docs/Management/BACKLOG.md"
    readmes = "Docs/Management"
    try:
        # 1. Working-tree changes vs HEAD.
        wt = subprocess.run(
            ["git", "diff", "--name-only", "HEAD", "--", backlog, readmes],
            capture_output=True, text=True,
        )
        if wt.returncode == 0 and any(is_watched(p) for p in wt.stdout.split()):
            return True
        # 2. Untracked files (a freshly registered item folder).
        others = subprocess.run(
            ["git", "ls-files", "--others", "--exclude-standard", "--", backlog, readmes],
            capture_output=True, text=True,
        )
        if others.returncode == 0 and any(is_watched(p) for p in others.stdout.split()):
            return True
        # 3. In-session commits since the session-start ref.
        start_ref = _session_start_ref()
        if start_ref:
            committed = subprocess.run(
                ["git", "diff", "--name-only", "{0}..HEAD".format(start_ref),
                 "--", backlog, readmes],
                capture_output=True, text=True,
            )
            if committed.returncode == 0 and any(is_watched(p) for p in committed.stdout.split()):
                return True
        return False
    except Exception:
        return False
```

The fail-open `except Exception: return False` is preserved exactly — `orphan_check.py` must never block a session (INV-1).

- [ ] **Step 4: Run the test to verify it passes**

Run: `python -m unittest discover -s .claude/scripts/backlog/tests -v`
Expected: PASS — all tests OK.

- [ ] **Step 5: Commit**

```bash
git add .claude/scripts/backlog/orphan_check.py \
        .claude/scripts/backlog/tests/test_orphan_check_widening.py
git commit -m "feat(backlog-gen): widen orphan_check watch set to item READMEs (T7)"
```

---

## Migration (T8–T12)

> **These tasks edit `Docs/` only.** Per `workflow.md`, docs land on **`develop`** — run T8–T12 on `develop`, not in the T1–T7 worktree, and merge the worktree first so the generator exists.
>
> **Handoff seam: between T10 and T11.** Everything up to T10 is additive (new folders only; BACKLOG.md and archives untouched), so an interrupted migration is a no-op for every other agent.

### Task 8: Freeze the fixture and add the fences

**Files:**
- Create: `Docs/Management/DevCycleCraft/spec-evolution-versioning/migration/BACKLOG-pre-migration.md`
- Modify: `Docs/Management/BACKLOG.md` (insert fences only)
- Modify: `MyVocaList.sln`

- [ ] **Step 1: Freeze the current file verbatim**

```bash
mkdir -p Docs/Management/DevCycleCraft/spec-evolution-versioning/migration
cp Docs/Management/BACKLOG.md \
   Docs/Management/DevCycleCraft/spec-evolution-versioning/migration/BACKLOG-pre-migration.md
```

- [ ] **Step 2: Insert the two fence pairs into `BACKLOG.md`**

Wrap the existing Business Features table with `<!-- BACKLOG:GENERATED:BEGIN business-features -->` / `…:END business-features -->`, and the Dev Cycle Craft table with the `dev-cycle-craft` pair. **Change nothing else** — the rows stay exactly as they are for now.

- [ ] **Step 3: Verify only fences were added**

Run: `git diff --stat Docs/Management/BACKLOG.md`
Expected: 4 insertions, 0 deletions.

- [ ] **Step 4: Register the fixture in the `.sln`**

Add to the `spec-evolution-versioning` Solution Folder's `ProjectSection(SolutionItems)`:
```
Docs\Management\DevCycleCraft\spec-evolution-versioning\migration\BACKLOG-pre-migration.md = Docs\Management\DevCycleCraft\spec-evolution-versioning\migration\BACKLOG-pre-migration.md
```

- [ ] **Step 5: Commit**

```bash
git add Docs/Management/BACKLOG.md MyVocaList.sln \
        Docs/Management/DevCycleCraft/spec-evolution-versioning/migration/
git commit -m "docs(backlog-gen): freeze pre-migration BACKLOG fixture + add generated fences (T8)"
```

---

### Task 9: Feature READMEs (top-level rows)

**Files:** one `README.md` per top-level feature folder named in a BACKLOG row; `MyVocaList.sln`.

- [ ] **Step 1: List the top-level rows to migrate**

Run: `python .claude/scripts/backlog/backlog_gen.py regen --check; echo "exit=$?"`
Expected: exit 0 with empty tables (no READMEs yet) — confirming the fences work.

- [ ] **Step 2: Write one `README.md` per top-level feature**

For each non-`↳` row in either table, create `<pointer-folder>/README.md`:

```markdown
---
id: artists-songs
title: "Artists & Songs Catalog"
status: "🔴 Blocked"
target: 2026-05
section: BusinessFeatures
goal: "Full artist/song catalog management."
gate: "BUG-027 (Critical) makes song registration impossible — smoke test 16C.1 must re-run green before phases 16C.2–16C.5 resume."
kind: feature
---

# Artists & Songs Catalog

Full artist/song catalog management. Specs: `requirements.md`, `design.md`, `tasks.md`.
```

Copy `target`, `status`, `goal` and `gate` **verbatim** from the frozen fixture — this task transcribes, it does not rewrite. Where the existing Notes exceed the REQ-SEV-09 bound, move the overflow sentence into the README body and record the row in the task-log as an allowed diff class (d).

**Assign `order:` while transcribing (REQ-SEV-17 / M5).** The frozen fixture is *not* in pure target order — Helder has curated positions. For each row, compare its fixture position with where the natural sort `(target, path)` would place it; wherever they differ, set `order:` to the row's **1-based position within its section × 10** (10, 20, 30 …). The ×10 spacing leaves room to insert later without renumbering. This is what makes T12's equivalence gate pass — skipping it guarantees row-order hunks that fit none of REQ-SEV-25's four permitted diff classes.

Rows whose pointer is `Docs/Management/cross-cutting-log.md` get a folder under `Docs/Management/cross-cutting/<slug>/` whose body links back to the log (REQ-SEV-28) — the log is retained, never deleted.

- [ ] **Step 3: Validate continuously**

Run after every ~5 files: `python .claude/scripts/backlog/backlog_gen.py regen --check`
Expected: exit 0 or 1 (1 = "would change", which is correct mid-migration). **Exit 2 means a validation error — fix it before writing more files.**

- [ ] **Step 4: Register every new file in the `.sln`**

One `SolutionItems` line per `README.md`, in the matching Solution Folder.

- [ ] **Step 5: Commit**

```bash
git add Docs/Management MyVocaList.sln
git commit -m "docs(backlog-gen): frontmatter READMEs for all top-level feature rows (T9)"
```

---

### Task 10: Sub-rows that already have a folder

**Files:** `README.md` in each existing `bugs/` / `changes/` folder; `MyVocaList.sln`.

- [ ] **Step 1: Enumerate existing item folders**

Run: `git ls-files "Docs/Management/**/changes/**" "Docs/Management/**/bugs/**" | sed 's|/[^/]*$||' | sort -u`

- [ ] **Step 2: Add a `README.md` to each**

Same shape as T9, plus `parent:` (the parent's `id`), `severity:` for bugs, and `kind: bug|change`. Example:

```markdown
---
id: BUG-050
title: "BUG-050: Song form — selecting an artist suggestion does not lock the field (Critical)"
status: "💡 Pending"
severity: Critical
target: 2026-07-21
parent: artists-songs
kind: bug
goal: "Picking a suggestion must lock the Artist field."
gate: "Folded into the inline-artist-create change (T1)."
---

# BUG-050 — artist suggestion does not lock the field

Root cause: `SelectArtist` never sets `IsArtistLocked = true`.
```

- [ ] **Step 3: Add the two separator rows**

Create `Docs/Management/cross-cutting/README.md` with `kind: group`, and the milestone at `Docs/Management/milestones/2026-06-mvp-release/README.md` with:

```markdown
---
id: mvp-release
title: "🏁 **MVP release**"
target: 2026-06
section: BusinessFeatures
kind: milestone
order: 500
---

# MVP release

Layout separator — marks where the MVP line falls in the Business Features table.
```

Separators carry no `status`, `goal` or `pointer`; `validate()` exempts them (`is_separator`). Set `order:` from their fixture position exactly as in T9.

- [ ] **Step 4: Validate**

Run: `python .claude/scripts/backlog/backlog_gen.py regen --check`
Expected: exit 0 or 1, never 2.

- [ ] **Step 5: Commit**

```bash
git add Docs/Management MyVocaList.sln
git commit -m "docs(backlog-gen): frontmatter for existing bugs/ and changes/ folders (T10)"
```

> **⏸ HANDOFF SEAM.** Everything above is additive. If the session ends here, `develop` is fully consistent: BACKLOG.md still reads exactly as before, and nothing else has been rewritten. A resuming session reads `task-log.md`'s Checkpoint block and starts at T11.

---

### Task 11: Counter-examples — bugs with no folder of their own

**Files:** new folders for BUG-027/029/030/031/032, BUG-050/051/052; `git mv` for BUG-012; `MyVocaList.sln`.

- [ ] **Step 1: BUG-050/051/052 (currently pointing at the DX-AC change task-log)**

Create `Docs/Management/BusinessFeatures/artists-songs/bugs/2026-07-21-BUG-05N-<slug>/README.md` for each. Frontmatter `parent: artists-songs`, and the body **must** open with:

```markdown
> History before this folder existed: `DevCycleCraft/autocomplete-component/changes/2026-07-19-dx-autocompleteedit-replacement/task-log.md`
```

Do **not** delete or move anything from that task-log (REQ-SEV-27).

- [ ] **Step 2: BUG-027/029/030/031/032 (currently pointing at the parent task-log)**

Same pattern, dated `2026-07-03`, under `BusinessFeatures/artists-songs/bugs/`, each back-linking `BusinessFeatures/artists-songs/task-log.md`. Preserve each row's `🔵 Deferred` status and its deferral reason as the `gate:`.

- [ ] **Step 3: BUG-012 — flat file becomes a folder, history preserved**

```bash
mkdir -p Docs/Management/BusinessFeatures/venues/bugs/2026-03-01-BUG-012-venuesviewmodel-fetch-slow
git mv Docs/Management/BusinessFeatures/venues/bugs/BUG-012-venuesviewmodel-fetch-slow.md \
       Docs/Management/BusinessFeatures/venues/bugs/2026-03-01-BUG-012-venuesviewmodel-fetch-slow/README.md
```
Then prepend frontmatter to the moved file, keeping its existing content as the body. The `-01` day is REQ-SEV-00's fixed rule for a `2026-03` month target — not a judgement call.

- [ ] **Step 4: Verify the rename kept history**

Run: `git log --follow --oneline -- "Docs/Management/BusinessFeatures/venues/bugs/2026-03-01-BUG-012-venuesviewmodel-fetch-slow/README.md" | head -5`
Expected: the file's pre-move commits are listed.

- [ ] **Step 5: Update the `.sln` (path changed for BUG-012, new entries for the rest) and commit**

```bash
git add Docs/Management MyVocaList.sln
git commit -m "docs(backlog-gen): folders for the 9 counter-example bugs, back-linking prior task-logs (T11)"
```

---

### Task 12: Archives + the equivalence gate

**Files:** `Docs/Management/backlog-archive/BACKLOG-ARCHIVE-2026-0{3,4,5,6,7}.md`; `task-log.md`.

- [ ] **Step 1: Create item folders for every archived row**

For each row in the 5 archive files, create its item folder with `status: ✅ Fixed` (or `✅ Done`) and `closed:` set from the **file name's month** — that is the archive key by definition.

- [ ] **Step 2: Add the fence pair to each archive file**

Wrap each file's existing table(s) with the `archive` fence pair, preserving each file's hand-written header prose above it (REQ-SEV-20).

- [ ] **Step 3: Regenerate everything**

Run: `python .claude/scripts/backlog/backlog_gen.py regen; echo "exit=$?"`
Expected: exit 0.

- [ ] **Step 4: Run the equivalence gate**

Run:
```bash
diff -u Docs/Management/DevCycleCraft/spec-evolution-versioning/migration/BACKLOG-pre-migration.md \
        Docs/Management/BACKLOG.md > /tmp/backlog-equivalence.diff; echo "lines=$(wc -l < /tmp/backlog-equivalence.diff)"
```
Expected: every remaining diff line falls into REQ-SEV-25's four permitted classes (a) trailing whitespace, (b) intra-cell spacing, (c) a counter-example pointer change, (d) Notes shortened with the overflow relocated. **Any other diff is a defect — fix the frontmatter, not the fixture.**

- [ ] **Step 5: Record the enumerated diff in the task-log**

Paste the full diff into `task-log.md` with one classification letter per hunk (REQ-SEV-29). A hunk you cannot classify means the migration is not done.

- [ ] **Step 6: Prove idempotency on real data**

Run: `python .claude/scripts/backlog/backlog_gen.py regen --check; echo "exit=$?"`
Expected: exit 0.

- [ ] **Step 7: Confirm the archives still grep**

Run: `grep -rl "BUG-048" Docs/Management/backlog-archive/`
Expected: `BACKLOG-ARCHIVE-2026-07.md` is listed.

- [ ] **Step 8: Measure the query (REQ-SEV-23 / NFR-2)**

Run:
```bash
python .claude/scripts/backlog/backlog_gen.py query --status "🟡 In Progress,🟢 Ready" | wc -l
```
Expected: ≤ 20 lines. Record the number and the `regen --check` wall-clock in the task-log.

- [ ] **Step 9: Commit**

```bash
git add Docs/Management MyVocaList.sln
git commit -m "docs(backlog-gen): migrate archives + pass the equivalence gate (T12)"
```

---

### Task 12b: Install the blocking pre-commit gate

> Deliberately last-but-one: the gate can only be switched on once `regen --check` exits 0 on real data (proved by T12 step 6). Installing it earlier makes the migration itself uncommittable.

**Files:**
- Modify: `.claude/githooks/pre-commit`

- [ ] **Step 1: Read the current hook before editing it**

Run: `cat .claude/githooks/pre-commit`
Note the existing `.cs`/`.xaml` build+test gate and its early-exit path — the new check is **added alongside**, never replacing it.

- [ ] **Step 2: Confirm the precondition holds**

Run: `python .claude/scripts/backlog/backlog_gen.py regen --check; echo "exit=$?"`
Expected: **exit 0.** If it is not 0, stop — T12 is not actually complete and the gate must not be installed.

- [ ] **Step 3: Append the gate**

```sh
# BACKLOG generation gate (R-2, approved blocking 2026-07-22).
# Fires only when frontmatter or a generated view is staged. This is an exact
# byte comparison with no false positives, which is why it may block -- unlike
# the advisory orphan_check.py, which classifies prose and can be wrong.
if git diff --cached --name-only | grep -qE '^Docs/Management/.*(README\.md|BACKLOG.*\.md)$'; then
    if ! python .claude/scripts/backlog/backlog_gen.py regen --check; then
        echo "pre-commit: BACKLOG is stale or invalid."
        echo "  fix: python .claude/scripts/backlog/backlog_gen.py regen"
        exit 1
    fi
    echo "pre-commit: BACKLOG generation gate OK."
fi
```

- [ ] **Step 4: Prove the gate both blocks and passes**

```bash
# Should PASS (tree is clean):
git commit --allow-empty -m "test: gate passes" && git reset --hard HEAD~1
# Should BLOCK: dirty a row, then try to commit it.
python - <<'PY'
import re, pathlib
p = pathlib.Path("Docs/Management/BACKLOG.md")
p.write_text(p.read_text(encoding="utf-8") + "\n| x | y | z | w |\n", encoding="utf-8")
PY
git add Docs/Management/BACKLOG.md && git commit -m "test: gate blocks"; echo "exit=$?"
git checkout -- Docs/Management/BACKLOG.md
```
Expected: the second commit is rejected with the "BACKLOG is stale or invalid" message and a non-zero exit. Record both outcomes in the task-log.

- [ ] **Step 5: Commit**

```bash
git add .claude/githooks/pre-commit
git commit -m "feat(backlog-gen): install the blocking pre-commit regen gate (T12b)"
```

---

### Task 13: The `amend:` bundle

**Files:** every file in REQ-SEV-30's table + `Docs/Changelog/changelog.md`.

- [ ] **Step 1: Find every stale reference before editing**

Run: `grep -rn "BACKLOG.md" .claude/ CLAUDE.md`
Any hit not in REQ-SEV-30's table must be added to the table before proceeding (REQ-SEV-30's closing sentence).

- [ ] **Step 2: Apply the amendments**

Work through REQ-SEV-30's table row by row. The two load-bearing edits:

`.claude/library/workflow-rules-6-7-8.md` — Rule 7 step 1 becomes:
> 1. **Active handoff file** `…/[feature]/handoff.md` if present — else run `python .claude/scripts/backlog/backlog_gen.py query --status "🟡 In Progress,🟢 Ready"`. **Do not read `Docs/Management/BACKLOG.md`** — it is generated, and the query returns the same active set in ~15 lines.

`.claude/rules/bug-tracking.md` — the ID rule becomes:
> **ID:** allocated by `backlog_gen.py register --kind bug`, which derives `max(BUG-NNN)+1` across live folders **and** all `backlog-archive/` months. Never hand-pick an ID; never read BACKLOG.md for the highest.

- [ ] **Step 3: Add the generated banner to `BACKLOG.md`**

Replace the "agents: do NOT re-fatten this file" rule with:
> **⚠ GENERATED FILE — do not hand-edit.** Rows are generated from per-item YAML frontmatter in `Docs/Management/**/README.md`. To change a row, edit that item's `README.md` (or use `backlog_gen.py register` / `status`) and run `python .claude/scripts/backlog/backlog_gen.py regen`. The row template is now mechanically enforced; a violating row fails the pre-commit gate.

- [ ] **Step 4: Changelog entry**

Add an entry to `Docs/Changelog/changelog.md` with the old rule, the new rule, and the effective date (2026-07-22) for each amended file.

- [ ] **Step 5: Verify nothing contradicts**

Run: `grep -rn "read.*BACKLOG.md\|BACKLOG.md.*read" .claude/ CLAUDE.md`
Expected: no instruction anywhere still tells an agent to read the file.

- [ ] **Step 6: Full verification**

Run:
```bash
python -m unittest discover -s .claude/scripts/backlog/tests -v
python .claude/scripts/backlog/backlog_gen.py regen --check; echo "exit=$?"
```
Expected: all tests OK; exit 0.

- [ ] **Step 7: Commit**

```bash
git add CLAUDE.md .claude/ Docs/Management/BACKLOG.md Docs/Changelog/changelog.md
git commit -m "amend: BACKLOG is generated — nested bugs/changes folders replace hand-written rows

Rationale: BACKLOG.md was read in full (136 lines / ~4.5k tokens) at every
session start regardless of task, and its row template was prose-enforced.
Rows are now generated from per-item frontmatter; Rule 7 step 1 is a query.

Backward compatibility: all ~50 rows migrated with an equivalence gate; the
5 archive months round-trip; prior task-log narratives are back-linked, not
moved. Spec: Docs/Management/DevCycleCraft/spec-evolution-versioning/."
```

---

## Verification Checklist (end of T13)

- [ ] `python -m unittest discover -s .claude/scripts/backlog/tests` — all green
- [ ] `backlog_gen.py regen --check` — exit 0 (REQ-SEV-13)
- [ ] `query --status "🟡 In Progress,🟢 Ready"` ≤ 20 lines (REQ-SEV-23)
- [ ] Equivalence diff enumerated in `task-log.md`, every hunk classified (REQ-SEV-29)
- [ ] `grep -rl "BUG-048" Docs/Management/backlog-archive/` hits (REQ-SEV-18)
- [ ] No `.sln` entry missing for any created/moved file (HARD GATE)
- [ ] No file in `.claude/` still instructs reading BACKLOG.md (REQ-SEV-30)
- [ ] BACKLOG.md carries the generated banner (REQ-SEV-31)
- [ ] `superpowers:verification-before-completion` run before the completion claim

## AC Traceability

| REQ | Task |
|-----|------|
| SEV-00 | T6 (`_folder_for` builds the `YYYY-MM-DD` prefix; `register` passes today), T11 step 3 (the `-01` padding for month-only migrated targets). *Not* `target_sort` — that normalizes sort keys, a different concern. |
| SEV-01/02/05 | T9, T10, T11 |
| SEV-03 | T2 (Minor validation), T6 (`register` rejects Minor) |
| SEV-04/28 | T9 step 2 (cross-cutting folders) |
| SEV-06 | T6 (`sln_add_entry`), T8–T12 `.sln` steps |
| SEV-07/08/10 | T1, T2 |
| SEV-09 | T2 (`notes_violations`) |
| SEV-11/11a | T6 (`next_bug_id` scanning live + archives, `cmd_register`, `cmd_renumber` via the `renumber` subcommand — all three tested), T12b (the blocking gate that catches a duplicate at merge) |
| SEV-12 | T6 (`cmd_status`) |
| SEV-13/15 | T5 (`cmd_regen`, `--check`), T12 step 6 |
| SEV-14/20 | T3 (`splice`), T4 (`ARCHIVE_TEMPLATE`) |
| SEV-16 | T3 (`render_backlog` filters terminal) |
| SEV-17 | T2 (`order_items`, `target_sort`) · T3 (separator rendering) · **T9 step 2 + T10 step 3 (`order:` transcription — this is what makes the equivalence gate pass)** |
| SEV-18/19 | T4 (`bucket_by_month`), T2 (closed validation) |
| SEV-21 | T2 (`validate`, incl. the path-parent check and the Minor-severity rule), T5 (abort-without-writing) |
| SEV-21a | T6 (atomic `register`, unknown-id `status`/`renumber`), T5 (`query_lines` skips a bad file) |
| SEV-22/23/24 | T5 (`query`), T13 step 2 |
| SEV-25/26/27/29 | T11, T12 |
| SEV-30/31 | T13 |
| NFR-1..5 | T1 (stdlib), T12 step 8 (timing, line count), T5 (LF/normalized paths), all tests |
