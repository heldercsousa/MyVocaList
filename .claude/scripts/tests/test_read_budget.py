"""read_budget tests — the decision behind read-slice-guard.py (enforcement logic)."""
import os
import sys
import tempfile
import unittest

sys.path.insert(0, os.path.normpath(os.path.join(os.path.dirname(__file__), "..")))
import read_budget as rb  # noqa: E402


class DecideTests(unittest.TestCase):
    def setUp(self):
        self._tmp = tempfile.TemporaryDirectory()
        self.root = self._tmp.name

    def tearDown(self):
        self._tmp.cleanup()

    def _file(self, rel, lines, width=20, binary=False):
        path = os.path.join(self.root, *rel.split("/"))
        os.makedirs(os.path.dirname(path), exist_ok=True)
        with open(path, "wb") as f:
            if binary:
                f.write(b"\x89PNG\0\0" * 5000)
            else:
                f.write(("x" * (width - 1) + "\n").encode() * lines)
        return path

    def test_small_file_passes_whatever_the_limit(self):
        p = self._file("src/Small.cs", 150)
        self.assertIsNone(rb.decide(p))
        self.assertIsNone(rb.decide(p, limit=5000))

    def test_long_file_full_read_is_denied(self):
        p = self._file("src/Long.cs", 500)
        self.assertIn("500 lines", rb.decide(p))

    def test_offset_without_limit_is_denied(self):
        p = self._file("src/Long.cs", 500)
        self.assertIsNotNone(rb.decide(p, offset=100))

    def test_limit_over_slice_cap_is_denied(self):
        p = self._file("src/Long.cs", 500)
        self.assertIn("slice cap", rb.decide(p, limit=5000))
        self.assertIsNotNone(rb.decide(p, limit=rb.MAX_SLICE_LINES + 1))

    def test_limit_within_slice_cap_passes(self):
        p = self._file("src/Long.cs", 500)
        self.assertIsNone(rb.decide(p, offset=200, limit=rb.MAX_SLICE_LINES))

    def test_few_lines_but_heavy_file_full_read_is_denied(self):
        p = self._file("Docs/Heavy.md", 140, width=250)
        self.assertIn("KB", rb.decide(p))

    def test_heavy_slice_is_denied_and_light_slice_passes(self):
        p = self._file("Docs/Heavy.md", 140, width=250)
        self.assertIn("slice budget", rb.decide(p, limit=140))
        self.assertIsNone(rb.decide(p, offset=10, limit=40))

    def test_backlog_denial_points_to_query(self):
        p = self._file("Docs/Management/BACKLOG.md", 140, width=250)
        self.assertIn("backlog_gen.py query", rb.decide(p))

    def test_exempt_paths_pass(self):
        for rel in (".claude/library/big.md", "CLAUDE.md", "App/App.csproj", "App/GlobalUsings.cs"):
            self.assertIsNone(rb.decide(self._file(rel, 500)), rel)

    def test_binary_file_passes(self):
        self.assertIsNone(rb.decide(self._file("img/shot.png", 0, binary=True)))

    def test_missing_file_passes(self):
        self.assertIsNone(rb.decide(os.path.join(self.root, "nope.cs")))

    def test_non_numeric_limit_counts_as_no_limit(self):
        p = self._file("src/Long.cs", 500)
        self.assertIsNotNone(rb.decide(p, limit="abc"))


if __name__ == "__main__":
    unittest.main()
