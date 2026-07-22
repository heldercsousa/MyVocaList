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

# One generated region per hand-written table in an archive file. Order matters:
# it is the order the regions are spliced and the order they appear in the file.
ARCHIVE_REGIONS = (
    ("BusinessFeatures", "archive-business"),
    ("DevCycleCraft", "archive-craft"),
)
ARCHIVE_SECTIONS = tuple(section for section, _region in ARCHIVE_REGIONS)

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


ARCHIVE_TEMPLATE = """# BACKLOG Archive — {month}

> Closed backlog rows completed in {month}, moved out of `Docs/Management/BACKLOG.md`. Rows use the slim PO template: Goal + one-sentence outcome + pointer. **Past BUG-NNN / feature lookups must grep all `backlog-archive/` files.**

## Business Features

<!-- BACKLOG:GENERATED:BEGIN archive-business -->
<!-- BACKLOG:GENERATED:END archive-business -->

## Dev Cycle Craft

<!-- BACKLOG:GENERATED:BEGIN archive-craft -->
<!-- BACKLOG:GENERATED:END archive-craft -->
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


def render_archive(existing_text, items, month=None, titles_by_id=None, all_items=None):
    """Splice one month's archive tables into its file -- one per section.

    Each archive file is hand-written with TWO tables (`## Business Features`
    and `## Dev Cycle Craft`), so it carries TWO generated regions. Splitting
    them is what keeps the `## Dev Cycle Craft` heading -- hand-written prose --
    OUTSIDE every fence, where regeneration preserves it byte-for-byte
    (REQ-SEV-14/20). A single flat region would have swallowed it.

    `month` is accepted for call-site clarity and future header rendering; each
    table body is a pure function of the items routed to it.

    `all_items` is the pool used to resolve an item's section through its parent
    chain; it defaults to `items`, and the item's own folder prefix is the
    fallback, so the common case needs no extra argument from the caller.
    """
    items = list(items or [])
    pool = list(all_items) if all_items else items
    titles_by_id = titles_by_id or {}

    by_region = dict((region, []) for _section, region in ARCHIVE_REGIONS)
    for it in items:
        by_region[_archive_region_of(it, pool)].append(it)

    out = existing_text
    for _section, region in ARCHIVE_REGIONS:
        body = render_table(
            by_region[region], head=TABLE_HEAD_ARCHIVE, archived=True,
            titles_by_id=titles_by_id,
        )
        out = splice(out, region, body)
    return out


def _archive_region_of(item, all_items):
    """Which archive region an item belongs in. Never guesses -- raises instead.

    A row that cannot be placed must fail loudly: silently dropping it would
    lose an archived BUG-NNN from the only file `grep` can still find it in
    (REQ-SEV-18).
    """
    section = _section_of(item, all_items) or _section_from_path(item.rel_path)
    for candidate, region in ARCHIVE_REGIONS:
        if section == candidate:
            return region
    raise RenderError(
        "cannot resolve archive section for '{0}' ({1}) -- give it a `section:` "
        "or a parent that has one".format(item.id or "<no id>", item.rel_path)
    )


def _section_from_path(rel_path):
    """Fallback section: the folder an item lives in already names its section."""
    parts = [p for p in (rel_path or "").split("/") if p]
    if parts and parts[0] in ARCHIVE_SECTIONS:
        return parts[0]
    return None


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
