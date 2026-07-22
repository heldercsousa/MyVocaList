import io
import os
import shutil
import sys
import tempfile
import unittest
from contextlib import contextmanager

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import backlog_gen  # noqa: E402

# UTF-8 BOM -- Visual Studio writes MyVocaList.sln with one.
BOM = b"\xef\xbb\xbf"

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


@contextmanager
def captured_stderr():
    """Capture sys.stderr writes for warning assertions."""
    original, buffer = sys.stderr, io.StringIO()
    sys.stderr = buffer
    try:
        yield buffer
    finally:
        sys.stderr = original


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

    def test_regen_never_writes_a_bom_into_the_generated_backlog(self):
        # Byte-level: the existing text assertions decode with plain utf-8 and
        # would silently accept a BOM the generator emitted.
        backlog_gen.cmd_regen(self.root)
        with open(os.path.join(self.mgmt, "BACKLOG.md"), "rb") as fh:
            self.assertFalse(fh.read().startswith(BOM))

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

    def test_regen_includes_a_readme_written_with_a_utf8_bom(self):
        # A BOM is not whitespace, so lstrip() leaves it in front of '---' and
        # the file used to take the silent-skip branch: a valid item vanished
        # from the backlog with exit code 0.
        path = os.path.join(self.mgmt, "BusinessFeatures", "bom", "README.md")
        os.makedirs(os.path.dirname(path))
        with open(path, "w", encoding="utf-8-sig", newline="\n") as fh:
            fh.write(readme(id="F-BOM", title="Bom Feature"))
        self.assertEqual(backlog_gen.cmd_regen(self.root), 0)
        self.assertIn("Bom Feature", self._backlog())

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

    def test_register_rejects_unknown_parent_before_staging(self):
        # Note: this is a PRE-FLIGHT rejection (the parent lookup happens before
        # anything is staged), so it does not exercise atomicity -- see
        # test_register_writes_nothing_when_the_new_row_fails_validation.
        before = sorted(os.listdir(os.path.join(self.mgmt, "BusinessFeatures", "feat")))
        backlog_gen.cmd_register(
            self.root, section=None, parent="ghost", kind="bug", severity="Major",
            title="Orphan", goal="x.", gate=None, today="2026-07-22")
        self.assertEqual(before, sorted(os.listdir(
            os.path.join(self.mgmt, "BusinessFeatures", "feat"))))

    def test_register_writes_nothing_when_the_new_row_fails_validation(self):
        # The goal trips the REQ-SEV-09 banned-content rule, which is only
        # detectable once the row exists -- a post-write-class failure. The
        # folder must not survive, and later regens must stay clean.
        with captured_stderr():
            rc = backlog_gen.cmd_register(
                self.root, section=None, parent="F-1", kind="bug", severity="Major",
                title="Bad notes", goal="See notes.cs for the cause.", gate=None,
                today="2026-07-22")
        self.assertNotEqual(rc, 0)
        folder = os.path.join(self.mgmt, "BusinessFeatures", "feat", "bugs",
                              "2026-07-22-BUG-001-bad-notes")
        self.assertFalse(os.path.exists(folder))
        self.assertEqual(backlog_gen.cmd_regen(self.root), 0)

    def test_register_adds_the_sln_entry_to_a_crlf_solution_file(self):
        # A VS-written .sln uses CRLF, so the EndProjectSection marker must be
        # matched newline-agnostically or the HARD GATE is silently unmet.
        with open(os.path.join(self.root, "MyVocaList.sln"), "w",
                  encoding="utf-8", newline="") as fh:
            fh.write(SLN_FIXTURE.replace("\n", "\r\n"))
        backlog_gen.cmd_register(
            self.root, section=None, parent="F-1", kind="bug", severity="Major",
            title="Crlf bug", goal="Fix it.", gate=None, today="2026-07-22")
        with open(os.path.join(self.root, "MyVocaList.sln"), encoding="utf-8") as fh:
            self.assertIn("2026-07-22-BUG-001-crlf-bug", fh.read())

    def _write_sln_bytes(self, data):
        with open(os.path.join(self.root, "MyVocaList.sln"), "wb") as fh:
            fh.write(data)

    def _sln_bytes(self):
        with open(os.path.join(self.root, "MyVocaList.sln"), "rb") as fh:
            return fh.read()

    def _register(self, title):
        backlog_gen.cmd_register(
            self.root, section=None, parent="F-1", kind="bug", severity="Major",
            title=title, goal="Fix it.", gate=None, today="2026-07-22")

    def test_register_preserves_the_bom_and_crlf_of_a_vs_written_sln(self):
        # Visual Studio writes .sln as UTF-8 with BOM and CRLF. Registration
        # must round-trip both byte-for-byte or every register churns the one
        # file the .sln HARD GATE governs. Asserted on RAW BYTES: a decoded-text
        # assertion cannot see a BOM, which is how this regression slipped in.
        self._write_sln_bytes(BOM + SLN_FIXTURE.replace("\n", "\r\n").encode("utf-8"))
        self._register("Bom bug")
        data = self._sln_bytes()
        self.assertTrue(data.startswith(BOM))
        self.assertNotIn(b"\n", data.replace(b"\r\n", b""))

    def test_register_inserts_the_entry_line_with_crlf_in_a_crlf_sln(self):
        self._write_sln_bytes(BOM + SLN_FIXTURE.replace("\n", "\r\n").encode("utf-8"))
        self._register("Eol bug")
        data = self._sln_bytes()
        start = data.index(b"2026-07-22-BUG-001-eol-bug")
        # The inserted line must terminate the same way as its neighbours.
        self.assertTrue(data[start:data.index(b"\n", start) + 1].endswith(b"\r\n"))

    def test_register_introduces_no_bom_in_a_plain_lf_sln(self):
        self._write_sln_bytes(SLN_FIXTURE.encode("utf-8"))
        self._register("Lf bug")
        data = self._sln_bytes()
        self.assertFalse(data.startswith(BOM))
        self.assertNotIn(b"\r\n", data)
        self.assertIn(b"2026-07-22-BUG-001-lf-bug", data)

    def test_register_warns_loudly_when_the_sln_is_absent(self):
        os.remove(os.path.join(self.root, "MyVocaList.sln"))
        with captured_stderr() as err:
            rc = backlog_gen.cmd_register(
                self.root, section=None, parent="F-1", kind="bug", severity="Major",
                title="No sln", goal="Fix it.", gate=None, today="2026-07-22")
        self.assertEqual(rc, 0)
        self.assertIn("warning", err.getvalue().lower())
        self.assertIn(".sln", err.getvalue())

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

    def test_renumber_refuses_to_run_while_a_readme_is_unparseable(self):
        # An unparseable README can hold a live BUG id that the scan cannot see,
        # so renumbering against a partial scan may reuse a retired id.
        write(os.path.join(self.mgmt, "BusinessFeatures", "feat", "bugs",
                           "2026-07-21-BUG-001-dup", "README.md"),
              readme(id="BUG-001", title="BUG-001: dup (Major)", severity="Major",
                     parent="F-1", section=None, kind="bug"))
        write(os.path.join(self.mgmt, "BusinessFeatures", "broken", "README.md"),
              "---\nid: BUG-050\ntags:\n  - a\n---\n")
        with captured_stderr():
            rc = backlog_gen.cmd_renumber(self.root, "BUG-001")
        self.assertEqual(rc, 2)
        self.assertTrue(os.path.isdir(os.path.join(
            self.mgmt, "BusinessFeatures", "feat", "bugs", "2026-07-21-BUG-001-dup")))

    def test_renumber_on_unknown_id_changes_nothing(self):
        self.assertEqual(backlog_gen.cmd_renumber(self.root, "BUG-999"), 2)


if __name__ == "__main__":
    unittest.main()
