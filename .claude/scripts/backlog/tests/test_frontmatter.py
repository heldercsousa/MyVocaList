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
