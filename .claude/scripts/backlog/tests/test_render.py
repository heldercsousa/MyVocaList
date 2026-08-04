# .claude/scripts/backlog/tests/test_render.py
import os
import sys
import unittest

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from model import Item  # noqa: E402
from render import RenderError, render_backlog, render_row, render_table, splice  # noqa: E402

PENDING = "\U0001F4A1 Pending"
DEFERRED = "\U0001F535 Deferred"
SUPERSEDED = "\U0001F535 Superseded"
DUPLICATE = "\U0001F535 Duplicate"


def item(**over):
    keys = {
        "id": "X-1", "title": "A title", "status": PENDING,
        "target": "2026-07-21", "section": "BusinessFeatures", "goal": "Do the thing.",
    }
    keys.update(over)
    path = keys.pop("_path", "BusinessFeatures/feat/")
    return Item.from_frontmatter(keys, path)


def _region_body(text, region):
    """The text strictly between a region's BEGIN and END fences."""
    from render import FENCE_BEGIN, FENCE_END
    begin = FENCE_BEGIN.format(region)
    end = FENCE_END.format(region)
    start = text.index(begin) + len(begin)
    stop = text.index(end)
    return text[start:stop].strip("\n")


def _inside_any_region(text, needle):
    """True if `needle` occurs between any BEGIN/END fence pair."""
    import re
    from render import FENCE_BEGIN, FENCE_END
    for region in re.findall(r"BACKLOG:GENERATED:BEGIN (\S+)", text):
        begin = FENCE_BEGIN.format(region)
        end = FENCE_END.format(region)
        if begin not in text or end not in text:
            continue
        start = text.index(begin) + len(begin)
        stop = text.index(end)
        if needle in text[start:stop]:
            return True
    return False


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

    def test_milestone_renders_exactly_like_the_frozen_fixture(self):
        keys = {"id": "mvp", "title": "\U0001F3C1 **MVP release**", "target": "2026-06",
                "section": "BusinessFeatures", "kind": "milestone"}
        row = render_row(Item.from_frontmatter(keys, "milestones/mvp/"))
        self.assertEqual(row, "| 2026-06 | | \U0001F3C1 **MVP release** | |")

    def test_group_renders_exactly_like_the_frozen_fixture(self):
        keys = {"id": "cross-cutting", "title": "Cross-cutting", "target": "2026-07-03",
                "section": "BusinessFeatures", "kind": "group",
                "goal": "Bugs with no single parent business feature"}
        row = render_row(Item.from_frontmatter(keys, "cross-cutting/"))
        self.assertEqual(
            row,
            "| 2026-07-03 | **Cross-cutting** | — | Bugs with no single parent business feature |")


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


class BacklogDeferredTests(unittest.TestCase):
    def test_deferred_item_stays_in_the_live_file(self):
        """🔵 Deferred is active (REQ-SEV-16) -- it must NOT be archived away."""
        text = (
            "<!-- BACKLOG:GENERATED:BEGIN business-features -->\n"
            "<!-- BACKLOG:GENERATED:END business-features -->\n"
            "<!-- BACKLOG:GENERATED:BEGIN dev-cycle-craft -->\n"
            "<!-- BACKLOG:GENERATED:END dev-cycle-craft -->\n"
        )
        deferred = item(id="d", title="Parked work", status=DEFERRED)
        out = render_backlog(text, [deferred])
        self.assertIn("Parked work", out)

    def test_superseded_item_is_excluded_from_the_live_file(self):
        text = (
            "<!-- BACKLOG:GENERATED:BEGIN business-features -->\n"
            "<!-- BACKLOG:GENERATED:END business-features -->\n"
            "<!-- BACKLOG:GENERATED:BEGIN dev-cycle-craft -->\n"
            "<!-- BACKLOG:GENERATED:END dev-cycle-craft -->\n"
        )
        gone = item(id="s", title="Old approach", status=SUPERSEDED, closed="2026-07")
        out = render_backlog(text, [item(id="a", title="Active"), gone])
        self.assertIn("Active", out)
        self.assertNotIn("Old approach", out)


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

    # --- T12-pre: Superseded / Duplicate archive routing + closed suffix -----

    def test_superseded_item_buckets_into_the_archive(self):
        from render import bucket_by_month
        s = item(id="s", status=SUPERSEDED, closed="2026-07")
        buckets = bucket_by_month([s])
        self.assertEqual([i.id for i in buckets["2026-07"]], ["s"])

    def test_duplicate_item_buckets_into_the_archive(self):
        from render import bucket_by_month
        d = item(id="d", status=DUPLICATE, closed="2026-06")
        buckets = bucket_by_month([d])
        self.assertEqual([i.id for i in buckets["2026-06"]], ["d"])

    def test_deferred_item_never_buckets_into_the_archive(self):
        from render import bucket_by_month
        d = item(id="d", status=DEFERRED)
        self.assertEqual(bucket_by_month([d]), {})

    def test_superseded_status_cell_reconstructs_closed_suffix(self):
        from render import render_archive
        s = item(id="s", title="Old venues plan", status=SUPERSEDED,
                 closed="2026-07", section="BusinessFeatures")
        out = render_archive(self.template, [s], "2026-07", {})
        self.assertIn(
            "\U0001F535 Superseded (closed 2026-07)",
            _region_body(out, "archive-business"),
        )

    def test_duplicate_status_cell_reconstructs_closed_suffix(self):
        from render import render_archive
        d = item(id="d", title="Dup bug", status=DUPLICATE, closed="2026-07",
                 section="DevCycleCraft", _path="DevCycleCraft/f/")
        out = render_archive(self.template, [d], "2026-07", {})
        self.assertIn(
            "\U0001F535 Duplicate (closed 2026-07)",
            _region_body(out, "archive-craft"),
        )

    def test_stored_status_stays_bare_enum_live_render_has_no_suffix(self):
        """The suffix belongs to the archive path only -- render_row for a live
        (non-archived) row must never append `(closed ...)` (REQ-SEV-08)."""
        s = item(id="s", status=SUPERSEDED, closed="2026-07")
        self.assertEqual(s.status, "\U0001F535 Superseded")
        self.assertNotIn("(closed", render_row(s, archived=False))

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

    # --- T9e: two archive regions (archive-business / archive-craft) ---------

    def test_template_declares_both_archive_regions(self):
        from render import FENCE_BEGIN, FENCE_END
        for region in ("archive-business", "archive-craft"):
            self.assertIn(FENCE_BEGIN.format(region), self.template)
            self.assertIn(FENCE_END.format(region), self.template)

    def test_template_carries_both_section_headings_outside_the_fences(self):
        """A brand-new month must come out with both hand-written headings, and
        neither may sit inside a generated region (T12 would consume it)."""
        self.assertIn("## Business Features", self.template)
        self.assertIn("## Dev Cycle Craft", self.template)
        for heading in ("## Business Features", "## Dev Cycle Craft"):
            self.assertFalse(
                _inside_any_region(self.template, heading),
                "{0} must be outside both generated regions".format(heading),
            )

    def test_business_item_renders_into_the_business_region(self):
        from render import render_archive
        a = item(id="a", title="Venues CRUD", status="✅ Done", closed="2026-07",
                 section="BusinessFeatures")
        out = render_archive(self.template, [a], "2026-07", {})
        self.assertIn("Venues CRUD", _region_body(out, "archive-business"))
        self.assertNotIn("Venues CRUD", _region_body(out, "archive-craft"))

    def test_craft_item_renders_into_the_craft_region(self):
        from render import render_archive
        a = item(id="a", title="M3 Lists", status="✅ Done", closed="2026-07",
                 section="DevCycleCraft", _path="DevCycleCraft/m3-lists/")
        out = render_archive(self.template, [a], "2026-07", {})
        self.assertIn("M3 Lists", _region_body(out, "archive-craft"))
        self.assertNotIn("M3 Lists", _region_body(out, "archive-business"))

    def test_both_regions_are_spliced_in_one_call(self):
        from render import render_archive
        b = item(id="b", title="Venues CRUD", status="✅ Done", closed="2026-07",
                 section="BusinessFeatures")
        c = item(id="c", title="M3 Lists", status="✅ Done", closed="2026-07",
                 section="DevCycleCraft", _path="DevCycleCraft/m3-lists/")
        out = render_archive(self.template, [b, c], "2026-07", {})
        self.assertIn("Venues CRUD", _region_body(out, "archive-business"))
        self.assertIn("M3 Lists", _region_body(out, "archive-craft"))

    def test_dev_cycle_craft_heading_survives_rendering_byte_for_byte(self):
        from render import render_archive
        b = item(id="b", status="✅ Done", closed="2026-07", section="BusinessFeatures")
        out = render_archive(self.template, [b], "2026-07", {})
        self.assertIn("\n## Dev Cycle Craft\n", out)
        self.assertFalse(_inside_any_region(out, "## Dev Cycle Craft"))
        self.assertFalse(_inside_any_region(out, "## Business Features"))

    def test_section_is_resolved_through_the_parent_chain(self):
        """A bug folder carries no `section:`; it inherits its parent feature's."""
        from render import render_archive
        parent = item(id="p", title="Parent Feature", section="DevCycleCraft",
                      _path="DevCycleCraft/f/")
        child = Item.from_frontmatter(
            {"id": "BUG-1", "title": "BUG-1: a bug", "status": "✅ Fixed",
             "target": "2026-07-21", "goal": "Fix it.", "parent": "p", "closed": "2026-07"},
            "DevCycleCraft/f/bugs/2026-07-21-BUG-1-x/")
        out = render_archive(self.template, [child], "2026-07", {"p": "Parent Feature"},
                             all_items=[parent, child])
        self.assertIn("BUG-1", _region_body(out, "archive-craft"))
        self.assertNotIn("BUG-1", _region_body(out, "archive-business"))

    def test_section_falls_back_to_the_path_prefix(self):
        """No `section:` and no resolvable parent in the bucket -- the folder the
        item lives in still says which section it belongs to."""
        from render import render_archive
        child = Item.from_frontmatter(
            {"id": "BUG-2", "title": "BUG-2: a bug", "status": "✅ Fixed",
             "target": "2026-07-21", "goal": "Fix it.", "parent": "p", "closed": "2026-07"},
            "DevCycleCraft/f/bugs/2026-07-21-BUG-2-x/")
        out = render_archive(self.template, [child], "2026-07", {})
        self.assertIn("BUG-2", _region_body(out, "archive-craft"))

    def test_unresolvable_section_raises_instead_of_dropping_the_row(self):
        """Fail loud: a row that cannot be placed must never be silently lost."""
        from render import render_archive
        orphan = Item.from_frontmatter(
            {"id": "BUG-3", "title": "BUG-3: a bug", "status": "✅ Fixed",
             "target": "2026-07-21", "goal": "Fix it.", "closed": "2026-07"},
            "cross-cutting/f/bugs/2026-07-21-BUG-3-x/")
        with self.assertRaises(RenderError):
            render_archive(self.template, [orphan], "2026-07", {})

    def test_under_suffix_is_preserved_by_the_split(self):
        from render import render_archive
        child = item(id="BUG-048", title="BUG-048: pagination reloads (Major)",
                     status="✅ Done", closed="2026-07", parent="p",
                     section="DevCycleCraft",
                     _path="DevCycleCraft/f/bugs/2026-07-21-BUG-048-x/")
        out = render_archive(self.template, [child], "2026-07", {"p": "Parent Feature"})
        body = _region_body(out, "archive-craft")
        self.assertIn("BUG-048", body)
        self.assertIn("(under: Parent Feature)", body)
        self.assertNotIn("↳", body)

    def test_two_region_render_is_idempotent(self):
        from render import render_archive
        b = item(id="b", status="✅ Done", closed="2026-07", section="BusinessFeatures")
        c = item(id="c", title="Craft", status="✅ Done", closed="2026-07",
                 section="DevCycleCraft", _path="DevCycleCraft/c/")
        once = render_archive(self.template, [b, c], "2026-07", {})
        self.assertEqual(once, render_archive(once, [b, c], "2026-07", {}))

    def test_an_empty_region_still_renders_its_table_head(self):
        from render import render_archive, TABLE_HEAD_ARCHIVE
        b = item(id="b", status="✅ Done", closed="2026-07", section="BusinessFeatures")
        out = render_archive(self.template, [b], "2026-07", {})
        self.assertEqual(_region_body(out, "archive-craft"), TABLE_HEAD_ARCHIVE)


if __name__ == "__main__":
    unittest.main()
