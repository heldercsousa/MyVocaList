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


if __name__ == "__main__":
    unittest.main()
