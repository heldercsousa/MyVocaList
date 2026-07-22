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

    def test_separator_needs_no_goal_or_status(self):
        keys = {"id": "mvp", "title": "🏁 **MVP release**", "target": "2026-06",
                "section": "BusinessFeatures", "kind": "milestone"}
        self.assertEqual(validate([Item.from_frontmatter(keys, "milestones/mvp/")]), [])

    def test_row_resolving_to_no_section_is_an_error(self):
        keys = {"id": "mvp", "title": "M", "target": "2026-06", "kind": "milestone"}
        errors = validate([Item.from_frontmatter(keys, "milestones/mvp/")])
        self.assertTrue(any("section" in e for e in errors))

    def test_child_inherits_section_via_parent_so_is_valid(self):
        parent = item(id="p")
        child = item(id="c", parent="p", section=None,
                     _path="BusinessFeatures/feat/bugs/2026-07-21-BUG-1-x/")
        self.assertEqual(validate([parent, child]), [])


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
