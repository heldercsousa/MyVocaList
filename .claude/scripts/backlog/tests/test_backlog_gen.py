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

    def test_query_ignores_a_readme_with_no_frontmatter_fence(self):
        write(os.path.join(self.mgmt, "BusinessFeatures", "broken", "README.md"),
              "no frontmatter at all\n")
        lines = backlog_gen.query_lines(self.root, [PENDING])
        self.assertEqual(len(lines), 1)

    def test_query_skips_a_readme_whose_frontmatter_is_malformed(self):
        # Opens a fence, then fails to parse -- a hard error for regen, but
        # query must degrade to a warning so session start never blocks.
        write(os.path.join(self.mgmt, "BusinessFeatures", "broken", "README.md"),
              "---\nid: X\ntags:\n  - a\n---\n")
        lines = backlog_gen.query_lines(self.root, [PENDING])
        self.assertEqual(len(lines), 1)


if __name__ == "__main__":
    unittest.main()
